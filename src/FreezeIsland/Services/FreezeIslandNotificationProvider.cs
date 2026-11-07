using Avalonia.Threading;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Core.Models.Notification.Templates;
using FreezeIsland.Models;
using Microsoft.Extensions.Logging;

namespace FreezeIsland.Services;

[NotificationProviderInfo(
    ProviderId,
    "FreezeIsland",
    "\uE72E",
    "在倒计时、锁定和解锁时显示全屏提醒。")]
[NotificationChannelInfo(
    ChannelId,
    "键鼠锁定提醒",
    "\uE72E",
    "倒计时、锁定和解锁状态的全屏提醒。")]
public sealed class FreezeIslandNotificationProvider : NotificationProviderBase, IDisposable
{
    public const string ProviderId = "6E2A1B9C-7D4F-4A3B-8C1D-2E5F6A7B8C9D";
    public const string ChannelId = "A1B2C3D4-E5F6-4A7B-8C9D-0E1F2A3B4C5D";

    // Lucide triangle-alert
    private const string WarningIconGlyph = "\ue193";

    private readonly FreezeIslandStateService _state;
    private readonly FreezeIslandSettingsService _settings;
    private readonly ILogger<FreezeIslandNotificationProvider> _logger;
    private NotificationRequest? _countdownRequest;

    public FreezeIslandNotificationProvider(
        FreezeIslandStateService state,
        FreezeIslandSettingsService settings,
        ILogger<FreezeIslandNotificationProvider> logger)
    {
        _state = state;
        _settings = settings;
        _logger = logger;
        _state.StateChanged += OnStateChanged;
        try
        {
            if (AppBase.Current != null)
            {
                AppBase.Current.AppStopping += OnAppStopping;
            }
        }
        catch
        {
            // 应用退出阶段忽略。
        }
    }

    public void Dispose()
    {
        _state.StateChanged -= OnStateChanged;
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

        CancelCountdown();
    }

    private void OnAppStopping(object? sender, EventArgs e)
    {
        CancelCountdown();
    }

    private void OnStateChanged(object? sender, LockStateChangedEventArgs e)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => HandleStateChanged(e));
            return;
        }

        HandleStateChanged(e);
    }

    private void HandleStateChanged(LockStateChangedEventArgs e)
    {
        switch (e.NewState)
        {
            case LockState.Countdown:
                ShowCountdown(e.CountdownSeconds);
                break;
            case LockState.Locked:
                CancelCountdown();
                ShowLocked();
                break;
            case LockState.Unlocked:
                if (e.OldState == LockState.Countdown)
                {
                    CancelCountdown();
                }
                else if (e.OldState == LockState.Locked)
                {
                    ShowUnlocked(e.Reason);
                }

                break;
        }
    }

    private void ShowCountdown(int countdownSeconds)
    {
        CancelCountdown();
        var content = CreateWarningContent(
            $"将在 {countdownSeconds} 秒后锁定键盘和鼠标，按 {_state.HotkeyText} 取消");
        content.Duration = TimeSpan.FromSeconds(Math.Max(1, countdownSeconds));
        content.SpeechContent = "即将锁定键盘和鼠标";
        var request = new NotificationRequest
        {
            MaskContent = content
        };

        _countdownRequest = request;
        Show(request);
    }

    private void ShowLocked()
    {
        var content = CreateWarningContent(
            $"键盘鼠标已锁定，按 {_state.HotkeyText} 解锁");
        content.Duration = TimeSpan.FromSeconds(_settings.GetSnapshot().NotificationDurationSeconds);
        content.SpeechContent = "键盘鼠标已锁定";
        Show(new NotificationRequest
        {
            MaskContent = content
        });
    }

    private void ShowUnlocked(UnlockReason? reason)
    {
        var text = reason == UnlockReason.SecureDesktop
            ? "检测到 Windows 安全桌面，已自动解锁"
            : "键盘鼠标已解锁，可以继续操作";
        var content = CreateWarningContent(text);
        content.Duration = TimeSpan.FromSeconds(_settings.GetSnapshot().NotificationDurationSeconds);
        content.SpeechContent = "键盘鼠标已解锁";
        Show(new NotificationRequest
        {
            MaskContent = content
        });
    }

    private static NotificationContent CreateWarningContent(string text)
    {
        var content = NotificationContent.CreateTwoIconsMask(text, hasRightIcon: false);

        // 宿主模板暂时不会根据 HasRightIcon 隐藏右侧图标，显式替换图标以只保留左侧警示图标。
        if (content.Content is TwoIconsMaskTemplateData data)
        {
            data.LeftIconSource = new LucideIconSource(WarningIconGlyph);
            data.RightIconSource = null;
        }

        return content;
    }

    private void Show(NotificationRequest request)
    {
        try
        {
            Channel(ChannelId).ShowNotification(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "显示 FreezeIsland 提醒失败");
        }
    }

    private void CancelCountdown()
    {
        try
        {
            _countdownRequest?.Cancel();
        }
        catch
        {
            // 提醒可能已经被宿主取消。
        }

        _countdownRequest = null;
    }
}
