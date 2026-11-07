using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using FreezeIsland.Controls;
using FreezeIsland.Models;
using FreezeIsland.Services;

namespace FreezeIsland.Settings;

[SettingsPageInfo(
    "winddrift.freezeisland.settings",
    "FreezeIsland",
    "\uE72E",
    "\uE72E",
    SettingsPageCategory.External)]
public sealed class FreezeIslandSettingsPage : SettingsPageBase
{
    private readonly FreezeIslandSettingsService _settingsService;
    private readonly FreezeIslandStateService _stateService;
    private readonly HotkeyRecorder _hotkeyRecorder = new();
    private readonly CheckBox _lockKeyboard = new() { Content = "锁定键盘" };
    private readonly CheckBox _lockMouse = new() { Content = "锁定鼠标按键/滚轮" };
    private readonly CheckBox _allowMouseMovement = new() { Content = "允许鼠标指针移动" };
    private readonly NumericUpDown _countdown = new()
    {
        Minimum = 0,
        Maximum = 10,
        Increment = 1,
        FormatString = "0",
        Width = 120
    };
    private readonly NumericUpDown _notificationDuration = new()
    {
        Minimum = 1,
        Maximum = 10,
        Increment = 1,
        FormatString = "0",
        Width = 120
    };
    private readonly TextBlock _error = new()
    {
        Foreground = Brushes.IndianRed,
        IsVisible = false
    };
    private readonly TextBlock _hookStatus = new()
    {
        Opacity = 0.8
    };
    private bool _loading;

    public FreezeIslandSettingsPage(
        FreezeIslandSettingsService settingsService,
        FreezeIslandStateService stateService)
    {
        _settingsService = settingsService;
        _stateService = stateService;

        Content = BuildContent();
        LoadFromSettings();

        _hotkeyRecorder.GestureChanged += (_, _) => ApplySettingsFromControls();
        _lockKeyboard.Click += (_, _) => ApplySettingsFromControls();
        _lockMouse.Click += (_, _) => ApplySettingsFromControls();
        _allowMouseMovement.Click += (_, _) => ApplySettingsFromControls();
        _countdown.ValueChanged += (_, _) => ApplySettingsFromControls();
        _notificationDuration.ValueChanged += (_, _) => ApplySettingsFromControls();
    }

    private Control BuildContent()
    {
        var panel = new StackPanel
        {
            Spacing = 12,
            Margin = new Thickness(24)
        };

        panel.Children.Add(new TextBlock
        {
            Text = "FreezeIsland",
            FontSize = 24,
            FontWeight = FontWeight.Bold
        });

        panel.Children.Add(new TextBlock
        {
            Text = "使用全局热键锁定/解锁键盘与鼠标。默认热键为 Ctrl+Alt+F；按热键后先进入倒计时，再次按下可取消。",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.8
        });

        panel.Children.Add(Section("热键"));
        var resetButton = new Button
        {
            Content = "恢复默认",
            Margin = new Thickness(8, 0, 0, 0)
        };
        resetButton.Click += (_, _) =>
        {
            _hotkeyRecorder.SetGesture(HotkeyParser.Default);
            ApplySettingsFromControls();
        };
        panel.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children = { _hotkeyRecorder, resetButton }
        });

        panel.Children.Add(Section("锁定范围"));
        panel.Children.Add(_lockKeyboard);
        panel.Children.Add(_lockMouse);
        panel.Children.Add(_allowMouseMovement);

        panel.Children.Add(Section("倒计时"));
        panel.Children.Add(Row("锁定前倒计时（0 表示立即锁定）", _countdown, "秒"));

        panel.Children.Add(Section("提醒"));
        panel.Children.Add(Row("锁定/解锁全屏提醒时长", _notificationDuration, "秒"));

        panel.Children.Add(Section("状态"));
        panel.Children.Add(_hookStatus);
        panel.Children.Add(_error);

        return new ScrollViewer
        {
            Content = panel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }

    private static TextBlock Section(string text) => new()
    {
        Text = text,
        FontSize = 18,
        FontWeight = FontWeight.SemiBold,
        Margin = new Thickness(0, 12, 0, 0)
    };

    private static Control Row(string label, Control control, string suffix)
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                new TextBlock
                {
                    Text = label,
                    VerticalAlignment = VerticalAlignment.Center
                },
                control,
                new TextBlock
                {
                    Text = suffix,
                    VerticalAlignment = VerticalAlignment.Center,
                    Opacity = 0.7
                }
            }
        };
    }

    private void LoadFromSettings()
    {
        _loading = true;
        try
        {
            var settings = _settingsService.Settings;
            if (HotkeyParser.TryParse(settings.Hotkey, out var gesture))
            {
                _hotkeyRecorder.SetGesture(gesture);
            }
            else
            {
                _hotkeyRecorder.SetGesture(HotkeyParser.Default);
            }

            _lockKeyboard.IsChecked = settings.LockKeyboard;
            _lockMouse.IsChecked = settings.LockMouse;
            _allowMouseMovement.IsChecked = settings.AllowMouseMovement;
            _allowMouseMovement.IsEnabled = settings.LockMouse;
            _countdown.Value = settings.CountdownSeconds;
            _notificationDuration.Value = settings.NotificationDurationSeconds;
            _hookStatus.Text = _stateService.HookAvailable
                ? "输入钩子：已就绪"
                : "输入钩子：不可用（当前无法锁定输入）";
        }
        finally
        {
            _loading = false;
        }
    }

    private void ApplySettingsFromControls()
    {
        if (_loading)
        {
            return;
        }

        var invalid = false;
        if (_lockKeyboard.IsChecked != true && _lockMouse.IsChecked != true)
        {
            ShowError("至少需要选择键盘或鼠标之一。");
            _lockKeyboard.IsChecked = true;
            invalid = true;
        }

        var settings = _settingsService.Settings;
        settings.Hotkey = _hotkeyRecorder.Gesture?.ToString() ?? "Ctrl+Alt+F";
        settings.LockKeyboard = _lockKeyboard.IsChecked == true;
        settings.LockMouse = _lockMouse.IsChecked == true;
        settings.AllowMouseMovement = _allowMouseMovement.IsChecked == true;
        settings.CountdownSeconds = (int)(_countdown.Value ?? 5);
        settings.NotificationDurationSeconds = (int)(_notificationDuration.Value ?? 3);
        _allowMouseMovement.IsEnabled = settings.LockMouse;

        if (!invalid)
        {
            HideError();
        }

        _settingsService.Save();
    }

    private void ShowError(string message)
    {
        _error.Text = message;
        _error.IsVisible = true;
    }

    private void HideError()
    {
        _error.IsVisible = false;
    }
}
