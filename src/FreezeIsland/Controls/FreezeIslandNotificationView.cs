using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace FreezeIsland.Controls;

public enum NotificationVisualState
{
    Countdown,
    Locked,
    Unlocked
}

/// <summary>
/// 全屏遮罩中的 FreezeIsland 提示内容。
/// </summary>
public sealed class FreezeIslandNotificationView : UserControl
{
    private readonly NotificationVisualState _state;
    private readonly string _hotkey;
    private readonly string? _reason;
    private readonly TextBlock _subtitle = new();
    private DispatcherTimer? _timer;
    private int _remaining;

    public FreezeIslandNotificationView(
        NotificationVisualState state,
        string hotkey,
        int countdownSeconds,
        string? reason = null)
    {
        _state = state;
        _hotkey = string.IsNullOrWhiteSpace(hotkey) ? "Ctrl+Alt+F" : hotkey;
        _reason = reason;
        _remaining = Math.Max(0, countdownSeconds);

        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        Content = BuildContent();

        if (state == NotificationVisualState.Countdown)
        {
            StartTimer();
        }

        DetachedFromVisualTree += (_, _) => DisposeTimer();
    }

    private Control BuildContent()
    {
        var title = _state switch
        {
            NotificationVisualState.Countdown => "即将锁定键盘和鼠标",
            NotificationVisualState.Locked => "键鼠已锁定",
            _ => "键鼠已解锁"
        };

        var statusText = _state switch
        {
            NotificationVisualState.Countdown => $"{_remaining} 秒后锁定 · 按 {_hotkey} 取消",
            NotificationVisualState.Locked => $"按 {_hotkey} 解锁",
            _ => _reason == "安全桌面"
                ? "检测到 Windows 安全桌面，已自动解锁"
                : "可以继续操作"
        };

        _subtitle.Text = statusText;
        _subtitle.TextAlignment = TextAlignment.Center;
        _subtitle.TextWrapping = TextWrapping.Wrap;
        _subtitle.Margin = new Thickness(0, 8, 0, 0);

        var titleBlock = new TextBlock
        {
            Text = title,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };

        return new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { titleBlock, _subtitle }
        };
    }

    private void StartTimer()
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (_, _) =>
        {
            if (_remaining <= 0)
            {
                DisposeTimer();
                return;
            }

            _remaining--;
            _subtitle.Text = $"{_remaining} 秒后锁定 · 按 {_hotkey} 取消";
            if (_remaining <= 0)
            {
                DisposeTimer();
            }
        };
        _timer.Start();
    }

    public void DisposeTimer()
    {
        if (_timer == null)
        {
            return;
        }

        _timer.Stop();
        _timer = null;
    }
}
