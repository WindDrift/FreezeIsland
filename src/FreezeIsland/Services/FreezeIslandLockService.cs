using Avalonia.Threading;
using ClassIsland.Core;
using FreezeIsland.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FreezeIsland.Services;

/// <summary>
/// 键鼠锁定状态机与钩子/倒计时/安全桌面编排。
/// </summary>
public sealed class FreezeIslandLockService : IHostedService, IDisposable
{
    private readonly FreezeIslandSettingsService _settingsService;
    private readonly FreezeIslandStateService _stateService;
    private readonly ILogger<FreezeIslandLockService> _logger;
    private readonly object _sync = new();
    private readonly object _stopSync = new();

    private InputHookThread? _hookThread;
    private Timer? _countdownTimer;
    private Timer? _secureDesktopTimer;
    private int _countdownRemaining;
    private LockState _state = LockState.Unlocked;
    private volatile bool _isLocked;
    private volatile bool _lockKeyboard = true;
    private volatile bool _lockMouse = true;
    private volatile bool _allowMouseMovement = true;
    private volatile HotkeyGesture? _hotkey;
    private UnlockReason _pendingLockReason = UnlockReason.Hotkey;
    private bool _started;
    private bool _stopped;

    public FreezeIslandLockService(
        FreezeIslandSettingsService settingsService,
        FreezeIslandStateService stateService,
        ILogger<FreezeIslandLockService> logger)
    {
        _settingsService = settingsService;
        _stateService = stateService;
        _logger = logger;
    }

    public HotkeyGesture? CurrentHotkey => _hotkey;

    public bool IsLocked => _isLocked;

    public bool LockMouse => _lockMouse;

    public bool AllowMouseMovement => _allowMouseMovement;

    public bool LockKeyboard => _lockKeyboard;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_started)
        {
            return;
        }

        _started = true;
        ApplySettings();
        _settingsService.Changed += OnSettingsChanged;

        _hookThread = new InputHookThread(this, _logger);
        var available = _hookThread.Start();
        RunOnUi(() => _stateService.SetHookAvailable(available));
        if (!available)
        {
            _logger.LogError("FreezeIsland 输入钩子不可用，锁定功能将被拒绝，直到钩子安装成功");
        }

        try
        {
            if (AppBase.Current != null)
            {
                AppBase.Current.AppStopping += OnAppStopping;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "订阅 AppStopping 失败");
        }

        UpdateState(LockState.Unlocked, 0, null);
        _logger.LogInformation("FreezeIsland 已启动，热键 {Hotkey}", _hotkey);

        if (Environment.GetEnvironmentVariable("FREEZEISLAND_SMOKE_TEST") == "1")
        {
            StartSmokeTest();
        }

        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        StopCore();
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        StopCore();
    }

    public void OnHotkeyPressed()
    {
        Toggle(UnlockReason.Hotkey);
    }

    public void RequestLock(UnlockReason reason)
    {
        lock (_sync)
        {
            if (_state != LockState.Unlocked)
            {
                return;
            }

            if (!_stateService.HookAvailable)
            {
                _logger.LogWarning("输入钩子不可用，已拒绝锁定请求");
                return;
            }

            var countdown = _settingsService.GetSnapshot().CountdownSeconds;
            _pendingLockReason = reason;
            if (countdown > 0)
            {
                _state = LockState.Countdown;
                _countdownRemaining = countdown;
                _countdownTimer?.Dispose();
                _countdownTimer = new Timer(CountdownTick, null, 1000, 1000);
                StartSecureDesktopWatcher();
                _logger.LogInformation("FreezeIsland 进入倒计时：{Seconds} 秒", countdown);
                UpdateState(LockState.Countdown, countdown, reason);
            }
            else
            {
                LockNowLocked(reason);
            }
        }
    }

    public void CancelCountdown(UnlockReason reason)
    {
        lock (_sync)
        {
            if (_state != LockState.Countdown)
            {
                return;
            }

            StopCountdownTimer();
            StopSecureDesktopWatcher();
            _countdownRemaining = 0;
            _state = LockState.Unlocked;
            _logger.LogInformation("FreezeIsland 已取消锁定倒计时");
            UpdateState(LockState.Unlocked, 0, reason);
        }
    }

    public void Unlock(UnlockReason reason)
    {
        lock (_sync)
        {
            if (_state == LockState.Unlocked)
            {
                return;
            }

            StopCountdownTimer();
            StopSecureDesktopWatcher();
            _countdownRemaining = 0;
            _isLocked = false;
            _state = LockState.Unlocked;
            _logger.LogInformation("FreezeIsland 已解锁：{Reason}", reason);
            UpdateState(LockState.Unlocked, 0, reason);
        }
    }

    public void Toggle(UnlockReason reason)
    {
        lock (_sync)
        {
            switch (_state)
            {
                case LockState.Locked:
                    Unlock(reason);
                    break;
                case LockState.Countdown:
                    CancelCountdown(reason);
                    break;
                default:
                    RequestLock(reason);
                    break;
            }
        }
    }

    private void LockNowLocked(UnlockReason reason)
    {
        if (_state == LockState.Locked)
        {
            return;
        }

        if (!_stateService.HookAvailable)
        {
            _logger.LogWarning("输入钩子不可用，已拒绝锁定请求");
            return;
        }

        StopCountdownTimer();
        _countdownRemaining = 0;
        _isLocked = true;
        _state = LockState.Locked;
        StartSecureDesktopWatcher();
        _logger.LogInformation("FreezeIsland 已锁定：{Reason}", reason);
        UpdateState(LockState.Locked, 0, reason);
    }

    private void CountdownTick(object? state)
    {
        lock (_sync)
        {
            if (_state != LockState.Countdown)
            {
                return;
            }

            _countdownRemaining--;
            if (_countdownRemaining <= 0)
            {
                LockNowLocked(_pendingLockReason);
                return;
            }

            var remaining = _countdownRemaining;
            RunOnUi(() => _stateService.SetCountdown(remaining));
        }
    }

    private void StartSecureDesktopWatcher()
    {
        if (_secureDesktopTimer != null)
        {
            return;
        }

        _secureDesktopTimer = new Timer(SecureDesktopTick, null, 250, 250);
    }

    private void StopSecureDesktopWatcher()
    {
        _secureDesktopTimer?.Dispose();
        _secureDesktopTimer = null;
    }

    private void SecureDesktopTick(object? state)
    {
        LockState current;
        lock (_sync)
        {
            current = _state;
        }

        if (current is not (LockState.Locked or LockState.Countdown))
        {
            return;
        }

        if (!SecureDesktopWatcher.IsSecureDesktopActive())
        {
            return;
        }

        _logger.LogInformation("检测到 Winlogon 安全桌面，自动解除锁定");
        if (current == LockState.Locked)
        {
            Unlock(UnlockReason.SecureDesktop);
        }
        else
        {
            CancelCountdown(UnlockReason.SecureDesktop);
        }
    }

    private void StopCountdownTimer()
    {
        _countdownTimer?.Dispose();
        _countdownTimer = null;
    }

    private void ApplySettings()
    {
        var settings = _settingsService.GetSnapshot();
        if (HotkeyParser.TryParse(settings.Hotkey, out var gesture) && HotkeyParser.IsValid(gesture, out _))
        {
            _hotkey = gesture;
        }
        else
        {
            _hotkey = HotkeyParser.Default;
        }

        _lockKeyboard = settings.LockKeyboard;
        _lockMouse = settings.LockMouse;
        _allowMouseMovement = settings.AllowMouseMovement;
        var text = _hotkey?.ToString() ?? "Ctrl+Alt+F";
        RunOnUi(() => _stateService.SetHotkey(text));
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        ApplySettings();
        _logger.LogInformation("FreezeIsland 设置已更新");
    }

    private void OnAppStopping(object? sender, EventArgs e)
    {
        Unlock(UnlockReason.Shutdown);
    }

    private void StartSmokeTest()
    {
        _logger.LogWarning("FREEZEISLAND_SMOKE_TEST=1：3 秒后按当前倒计时锁定，再等待 3 秒自动解锁");
        _ = Task.Run(async () =>
        {
            await Task.Delay(3000);
            RequestLock(UnlockReason.SmokeTest);
            await Task.Delay(TimeSpan.FromSeconds(_settingsService.GetSnapshot().CountdownSeconds + 3));
            Unlock(UnlockReason.SmokeTest);
            _logger.LogInformation("FreezeIsland 冒烟测试完成");
        });
    }

    private void UpdateState(LockState state, int countdownSeconds, UnlockReason? reason)
    {
        RunOnUi(() => _stateService.SetState(state, countdownSeconds, reason));
    }

    private void RunOnUi(Action action)
    {
        try
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                action();
            }
            else
            {
                Dispatcher.UIThread.Post(action);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "切换 FreezeIsland 状态到 UI 线程失败");
        }
    }

    private void StopCore()
    {
        lock (_stopSync)
        {
            if (_stopped)
            {
                return;
            }

            _stopped = true;
        }

        _settingsService.Changed -= OnSettingsChanged;
        try
        {
            if (AppBase.Current != null)
            {
                AppBase.Current.AppStopping -= OnAppStopping;
            }
        }
        catch
        {
            // 应用退出阶段忽略。
        }

        lock (_sync)
        {
            StopCountdownTimer();
            StopSecureDesktopWatcher();
            _countdownRemaining = 0;
            _isLocked = false;
            _state = LockState.Unlocked;
        }

        try
        {
            _hookThread?.Stop();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "停止 FreezeIsland 输入钩子失败");
        }

        _hookThread = null;
        RunOnUi(() => _stateService.SetHookAvailable(false));
        _logger.LogInformation("FreezeIsland 已停止");
    }
}
