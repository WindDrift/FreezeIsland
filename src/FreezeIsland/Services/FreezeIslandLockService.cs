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
    private const int SecureDesktopPollMilliseconds = 500;

    private readonly FreezeIslandSettingsService _settingsService;
    private readonly FreezeIslandStateService _stateService;
    private readonly ILogger<FreezeIslandLockService> _logger;
    private readonly object _sync = new();
    private readonly object _stopSync = new();
    private readonly object _hookSync = new();

    private InputHookThread? _hookThread;
    private Timer? _countdownTimer;
    private Timer? _secureDesktopTimer;
    private int _countdownRemaining;
    private LockState _state = LockState.Unlocked;
    private volatile bool _isLocked;
    private volatile InputLockPolicy _policy = InputLockPolicy.Default;
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

    public InputLockPolicy Policy => _policy;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_started)
        {
            return;
        }

        _started = true;
        ApplySettings();
        _settingsService.Changed += OnSettingsChanged;

        if (!EnsureHookAvailable())
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

    private bool IsHookAvailable => _hookThread is { Available: true };

    private bool EnsureHookAvailable()
    {
        if (IsHookAvailable)
        {
            return true;
        }

        lock (_hookSync)
        {
            if (IsHookAvailable)
            {
                return true;
            }

            _hookThread?.Dispose();
            _hookThread = null;

            var hookThread = new InputHookThread(this, _logger);
            if (!hookThread.Start())
            {
                hookThread.Dispose();
                RunOnUi(() => _stateService.SetHookAvailable(false));
                return false;
            }

            _hookThread = hookThread;
            RunOnUi(() => _stateService.SetHookAvailable(true));
            return true;
        }
    }

    /// <summary>
    /// 捕获下一次真实键盘按键，用于设置页添加排除键。
    /// </summary>
    public Task<uint?> CaptureNextKeyAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        var hookThread = _hookThread;
        if (hookThread == null || !hookThread.Available)
        {
            return Task.FromResult<uint?>(null);
        }

        return hookThread.CaptureNextKeyAsync(timeout, cancellationToken);
    }

    public void CancelKeyCapture()
    {
        _hookThread?.CancelCapture();
    }

    public void RequestLock(UnlockReason reason)
    {
        EnsureHookAvailable();

        lock (_sync)
        {
            if (_state != LockState.Unlocked)
            {
                return;
            }

            if (!IsHookAvailable)
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

        if (!IsHookAvailable)
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

        _secureDesktopTimer = new Timer(
            SecureDesktopTick,
            null,
            SecureDesktopPollMilliseconds,
            SecureDesktopPollMilliseconds);
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

        _policy = InputLockPolicy.Create(settings);
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
                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "在 UI 线程更新 FreezeIsland 状态失败");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "投递 FreezeIsland 状态更新失败");
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
        CancelKeyCapture();
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
            _hookThread?.Dispose();
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
