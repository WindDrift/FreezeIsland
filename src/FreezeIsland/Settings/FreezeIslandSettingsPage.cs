using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Shared;
using FreezeIsland.Models;
using FreezeIsland.Services;

namespace FreezeIsland.Settings;

[SettingsPageInfo(
    "winddrift.freezeisland.settings",
    "FreezeIsland",
    "\uE72E",
    "\uE72E",
    SettingsPageCategory.External)]
public sealed partial class FreezeIslandSettingsPage : SettingsPageBase
{
    private readonly FreezeIslandSettingsService _settingsService;
    private readonly FreezeIslandStateService _stateService;
    private bool _loading;
    private bool _subscribed;

    // 供 Avalonia XAML 运行时加载器/设计器使用；ClassIsland 实际通过 DI 构造函数创建。
    public FreezeIslandSettingsPage()
        : this(
            IAppHost.GetService<FreezeIslandSettingsService>(),
            IAppHost.GetService<FreezeIslandStateService>())
    {
    }

    public FreezeIslandSettingsPage(
        FreezeIslandSettingsService settingsService,
        FreezeIslandStateService stateService)
    {
        _settingsService = settingsService;
        _stateService = stateService;

        InitializeComponent();
        LoadFromSettings();

        HotkeyRecorderControl.GestureChanged += (_, _) => ApplySettingsFromControls();
        ResetHotkeyButton.Click += OnResetHotkeyClick;

        LockKeyboardToggle.PropertyChanged += OnTogglePropertyChanged;
        LockMouseToggle.PropertyChanged += OnTogglePropertyChanged;
        AllowMouseMovementToggle.PropertyChanged += OnTogglePropertyChanged;

        CountdownBox.ValueChanged += (_, _) => ApplySettingsFromControls();
        NotificationDurationBox.ValueChanged += (_, _) => ApplySettingsFromControls();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_subscribed)
        {
            return;
        }

        _subscribed = true;
        _stateService.StateChanged += OnStateChanged;
        _stateService.PropertyChanged += OnStatePropertyChanged;
        UpdateStatusText();
        UpdateHookStatus();
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        if (!_subscribed)
        {
            return;
        }

        _subscribed = false;
        _stateService.StateChanged -= OnStateChanged;
        _stateService.PropertyChanged -= OnStatePropertyChanged;
    }

    private void OnTogglePropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ToggleSwitch.IsCheckedProperty)
        {
            ApplySettingsFromControls();
        }
    }

    private void OnStateChanged(object? sender, LockStateChangedEventArgs e)
    {
        UpdateStatusText();
    }

    private void OnStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(FreezeIslandStateService.CountdownRemaining)
            or nameof(FreezeIslandStateService.HotkeyText)
            or nameof(FreezeIslandStateService.HookAvailable))
        {
            UpdateStatusText();
            UpdateHookStatus();
        }
    }

    private void OnResetHotkeyClick(object? sender, RoutedEventArgs e)
    {
        HotkeyRecorderControl.SetGesture(HotkeyParser.Default);
        ApplySettingsFromControls();
    }

    private void LoadFromSettings()
    {
        _loading = true;
        try
        {
            var settings = _settingsService.Settings;
            HotkeyRecorderControl.SetGesture(
                HotkeyParser.TryParse(settings.Hotkey, out var gesture)
                    ? gesture
                    : HotkeyParser.Default);

            LockKeyboardToggle.IsChecked = settings.LockKeyboard;
            LockMouseToggle.IsChecked = settings.LockMouse;
            AllowMouseMovementToggle.IsChecked = settings.AllowMouseMovement;
            AllowMouseMovementToggle.IsEnabled = settings.LockMouse;
            CountdownBox.Value = settings.CountdownSeconds;
            NotificationDurationBox.Value = settings.NotificationDurationSeconds;

            UpdateStatusText();
            UpdateHookStatus();
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
        if (LockKeyboardToggle.IsChecked != true && LockMouseToggle.IsChecked != true)
        {
            ShowError("至少需要选择键盘或鼠标之一。");
            LockKeyboardToggle.IsChecked = true;
            invalid = true;
        }

        var settings = _settingsService.Settings;
        settings.Hotkey = HotkeyRecorderControl.Gesture?.ToString() ?? "Ctrl+Alt+F";
        settings.LockKeyboard = LockKeyboardToggle.IsChecked == true;
        settings.LockMouse = LockMouseToggle.IsChecked == true;
        settings.AllowMouseMovement = AllowMouseMovementToggle.IsChecked == true;
        settings.CountdownSeconds = (int)(CountdownBox.Value ?? 5);
        settings.NotificationDurationSeconds = (int)(NotificationDurationBox.Value ?? 3);
        AllowMouseMovementToggle.IsEnabled = settings.LockMouse;

        if (!invalid)
        {
            HideError();
        }

        _settingsService.Save();
    }

    private void UpdateStatusText()
    {
        StatusText.Text = _stateService.State switch
        {
            LockState.Locked => $"键鼠已锁定 · {_stateService.HotkeyText} 解锁",
            LockState.Countdown => $"即将锁定 · {_stateService.CountdownRemaining}s",
            _ => "键鼠未锁定"
        };
    }

    private void UpdateHookStatus()
    {
        HookStatusText.Text = _stateService.HookAvailable
            ? "输入钩子：已就绪，可以锁定输入。"
            : "输入钩子：不可用，当前无法锁定输入。";
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }

    private void HideError()
    {
        ErrorText.IsVisible = false;
    }
}
