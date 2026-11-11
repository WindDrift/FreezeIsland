using System.ComponentModel;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Services;
using FreezeIsland.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FreezeIsland.Services;

/// <summary>
/// 在锁定状态、输入钩子可用性或锁定范围设置变化时通知宿主重新判定规则集，
/// 使“键鼠锁定状态是”“锁定的范围包含”等规则及时生效，
/// 并驱动依赖规则集的“规则集不再满足时恢复行动”。
/// </summary>
public sealed class FreezeIslandRulesetNotifier : IHostedService, IDisposable
{
    /// <summary>
    /// 单次通知最多连续判定的轮数。规则集判定可能反过来改变锁定状态（例如工作流里的锁定行动），
    /// 因此允许有限次重判，同时避免规则配置成环时卡住 UI 线程。
    /// </summary>
    private const int MaxDrainRounds = 8;

    private readonly FreezeIslandStateService _stateService;
    private readonly FreezeIslandSettingsService _settingsService;
    private readonly IRulesetService _rulesetService;
    private readonly ILogger<FreezeIslandRulesetNotifier> _logger;
    private readonly object _sync = new();

    private bool _dirty;
    private bool _dispatching;
    private bool _subscribed;
    private volatile bool _disposed;

    public FreezeIslandRulesetNotifier(
        FreezeIslandStateService stateService,
        FreezeIslandSettingsService settingsService,
        IRulesetService rulesetService,
        ILogger<FreezeIslandRulesetNotifier> logger)
    {
        _stateService = stateService;
        _settingsService = settingsService;
        _rulesetService = rulesetService;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_subscribed)
        {
            return Task.CompletedTask;
        }

        _subscribed = true;
        _stateService.StateChanged += OnStateChanged;
        _stateService.PropertyChanged += OnStatePropertyChanged;
        _settingsService.Changed += OnSettingsChanged;

        // 启动时补一次判定，让插件重启后已有的规则立即得到正确结果。
        Notify();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Unsubscribe();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _disposed = true;
        Unsubscribe();
    }

    private void Unsubscribe()
    {
        if (!_subscribed)
        {
            return;
        }

        _subscribed = false;
        _stateService.StateChanged -= OnStateChanged;
        _stateService.PropertyChanged -= OnStatePropertyChanged;
        _settingsService.Changed -= OnSettingsChanged;
    }

    private void OnStateChanged(object? sender, LockStateChangedEventArgs e) => Notify();

    private void OnStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // 钩子可用性参与“已锁定”规则的判定。
        if (e.PropertyName is nameof(FreezeIslandStateService.HookAvailable))
        {
            Notify();
        }
    }

    private void OnSettingsChanged(object? sender, EventArgs e) => Notify();

    /// <summary>
    /// 标记需要重新判定。同一时刻只允许一条通知链，链上收到的变化会合并到下一轮。
    /// 通知一律排队到 UI 线程，避免在状态机持锁或设置保存的过程中重入自动化行动。
    /// </summary>
    private void Notify()
    {
        if (_disposed)
        {
            return;
        }

        lock (_sync)
        {
            _dirty = true;
            if (_dispatching)
            {
                return;
            }

            _dispatching = true;
        }

        try
        {
            Dispatcher.UIThread.Post(Drain);
        }
        catch (Exception ex)
        {
            lock (_sync)
            {
                _dispatching = false;
                _dirty = false;
            }

            _logger.LogDebug(ex, "排队 FreezeIsland 规则集通知失败");
        }
    }

    private void Drain()
    {
        try
        {
            for (var round = 0; round < MaxDrainRounds; round++)
            {
                lock (_sync)
                {
                    if (_disposed || !_dirty)
                    {
                        _dispatching = false;
                        return;
                    }

                    _dirty = false;
                }

                try
                {
                    _rulesetService.NotifyStatusChanged();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "通知 ClassIsland 重新判定规则集失败");
                }
            }

            lock (_sync)
            {
                _dispatching = false;
                _dirty = false;
            }

            _logger.LogDebug("规则集判定在 {Rounds} 轮内仍未稳定，已停止继续通知", MaxDrainRounds);
        }
        catch (Exception ex)
        {
            lock (_sync)
            {
                _dispatching = false;
                _dirty = false;
            }

            _logger.LogDebug(ex, "重新判定 ClassIsland 规则集时出现异常");
        }
    }
}
