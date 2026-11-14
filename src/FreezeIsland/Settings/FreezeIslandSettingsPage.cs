using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
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
    private static readonly TimeSpan KeyCaptureTimeout = TimeSpan.FromSeconds(15);

    private readonly FreezeIslandSettingsService _settingsService;
    private readonly FreezeIslandStateService _stateService;
    private readonly FreezeIslandLockService _lockService;
    private readonly (ToggleSwitch Toggle, KeyboardLockScope Scope)[] _keyboardToggles;
    private readonly (ToggleSwitch Toggle, MouseLockScope Scope)[] _mouseToggles;

    private bool _suppress;
    private bool _subscribed;
    private bool _pageLoaded;
    private bool _capturingKey;
    private CancellationTokenSource? _captureCts;

    public FreezeIslandSettingsPage(
        FreezeIslandSettingsService settingsService,
        FreezeIslandStateService stateService,
        FreezeIslandLockService lockService)
    {
        _settingsService = settingsService;
        _stateService = stateService;
        _lockService = lockService;

        InitializeComponent();

        _keyboardToggles = new[]
        {
            (KeyboardLettersToggle, KeyboardLockScope.Letters),
            (KeyboardNumberRowToggle, KeyboardLockScope.NumberRow),
            (KeyboardSymbolsToggle, KeyboardLockScope.Symbols),
            (KeyboardFunctionKeysToggle, KeyboardLockScope.FunctionKeys),
            (KeyboardNumpadToggle, KeyboardLockScope.Numpad),
            (KeyboardNavigationToggle, KeyboardLockScope.EditAndNavigation),
            (KeyboardControlKeysToggle, KeyboardLockScope.ControlKeys),
            (KeyboardModifiersToggle, KeyboardLockScope.Modifiers),
            (KeyboardMediaKeysToggle, KeyboardLockScope.MediaAndSystem),
            (KeyboardOtherToggle, KeyboardLockScope.Other)
        };

        _mouseToggles = new[]
        {
            (MouseLeftToggle, MouseLockScope.LeftButton),
            (MouseMiddleToggle, MouseLockScope.MiddleButton),
            (MouseRightToggle, MouseLockScope.RightButton),
            (MouseSideButtonsToggle, MouseLockScope.SideButtons),
            (MouseWheelToggle, MouseLockScope.Wheel)
        };

        LoadFromSettings();

        foreach (var item in _keyboardToggles)
        {
            item.Toggle.PropertyChanged += OnKeyboardScopePropertyChanged;
        }

        foreach (var item in _mouseToggles)
        {
            item.Toggle.PropertyChanged += OnMouseScopePropertyChanged;
        }

        LockKeyboardToggle.PropertyChanged += OnMasterPropertyChanged;
        LockMouseToggle.PropertyChanged += OnMasterPropertyChanged;
        MouseMovementLockToggle.PropertyChanged += OnMouseMovementPropertyChanged;
        ExperimentalFeaturesToggle.PropertyChanged += OnExperimentalFeaturesChanged;

        HotkeyRecorderControl.GestureChanged += (_, _) => ApplySettingsFromControls();
        ResetHotkeyButton.Click += OnResetHotkeyClick;
        AddExcludedKeyButton.Click += OnAddExcludedKeyClick;
        ClearExcludedKeysButton.Click += OnClearExcludedKeysClick;

        CountdownBox.ValueChanged += (_, _) => ApplySettingsFromControls();
        NotificationDurationBox.ValueChanged += (_, _) => ApplySettingsFromControls();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _pageLoaded = true;
        if (_subscribed)
        {
            return;
        }

        _subscribed = true;
        _stateService.StateChanged += OnStateChanged;
        _stateService.PropertyChanged += OnStatePropertyChanged;
        UpdateStatusText();
        UpdateHookStatus();

        if (!_capturingKey)
        {
            AddExcludedKeyButton.Content = "添加按键";
            RebuildExcludedKeys();
        }
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        _pageLoaded = false;
        CancelKeyCapture();

        if (!_subscribed)
        {
            return;
        }

        _subscribed = false;
        _stateService.StateChanged -= OnStateChanged;
        _stateService.PropertyChanged -= OnStatePropertyChanged;
    }

    private void OnMasterPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_suppress || e.Property != ToggleSwitch.IsCheckedProperty)
        {
            return;
        }

        var master = (ToggleSwitch)sender!;
        var keyboardOn = LockKeyboardToggle.IsChecked == true;
        var mouseOn = LockMouseToggle.IsChecked == true;
        if (!keyboardOn && !mouseOn)
        {
            _suppress = true;
            master.IsChecked = true;
            _suppress = false;
            ShowError("至少需要开启键盘锁定或鼠标锁定之一。");
            return;
        }

        _suppress = true;
        if (keyboardOn && ComputeKeyboardScope() == KeyboardLockScope.None)
        {
            SetKeyboardScopes(KeyboardLockScope.All);
        }

        if (mouseOn && ComputeMouseScope() == MouseLockScope.None && MouseMovementLockToggle.IsChecked != true)
        {
            SetMouseScopes(MouseLockScope.All);
        }

        UpdateScopeToggleAvailability();
        _suppress = false;

        ApplySettingsFromControls();
    }

    private void OnKeyboardScopePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_suppress || e.Property != ToggleSwitch.IsCheckedProperty)
        {
            return;
        }

        if (LockKeyboardToggle.IsChecked == true && ComputeKeyboardScope() == KeyboardLockScope.None)
        {
            _suppress = true;
            ((ToggleSwitch)sender!).IsChecked = true;
            _suppress = false;
            ShowError("锁定键盘已开启时，至少保留一个键盘分区。");
            return;
        }

        ApplySettingsFromControls();
    }

    private void OnMouseScopePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_suppress || e.Property != ToggleSwitch.IsCheckedProperty)
        {
            return;
        }

        if (LockMouseToggle.IsChecked == true
            && ComputeMouseScope() == MouseLockScope.None
            && MouseMovementLockToggle.IsChecked != true)
        {
            _suppress = true;
            ((ToggleSwitch)sender!).IsChecked = true;
            _suppress = false;
            ShowError("未选择任何鼠标按键时，请至少保留一个分区或开启“锁定鼠标移动”。");
            return;
        }

        ApplySettingsFromControls();
    }

    private void OnMouseMovementPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_suppress || e.Property != ToggleSwitch.IsCheckedProperty)
        {
            return;
        }

        if (LockMouseToggle.IsChecked == true
            && MouseMovementLockToggle.IsChecked != true
            && ComputeMouseScope() == MouseLockScope.None)
        {
            _suppress = true;
            MouseMovementLockToggle.IsChecked = true;
            _suppress = false;
            ShowError("未选择任何鼠标按键时，必须保留“锁定鼠标移动”。");
            return;
        }

        ApplySettingsFromControls();
    }

    private void OnExperimentalFeaturesChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_suppress || e.Property != ToggleSwitch.IsCheckedProperty)
        {
            return;
        }

        if (ExperimentalFeaturesToggle.IsChecked != true && _capturingKey)
        {
            CancelKeyCapture();
        }

        ApplySettingsFromControls();
        RebuildExcludedKeys();
    }

    private void OnStateChanged(object? sender, LockStateChangedEventArgs e)
    {
        if (e.NewState != LockState.Unlocked && _capturingKey)
        {
            CancelKeyCapture();
        }

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

    private void OnClearExcludedKeysClick(object? sender, RoutedEventArgs e)
    {
        if (_capturingKey)
        {
            return;
        }

        var excludedKeys = _settingsService.Settings.ExcludedKeys;
        if (excludedKeys == null || excludedKeys.Count == 0)
        {
            return;
        }

        excludedKeys.Clear();
        _settingsService.Save();
        RebuildExcludedKeys();
        SetExcludeKeyStatus("已清空排除按键。", isError: false);
    }

    private async void OnAddExcludedKeyClick(object? sender, RoutedEventArgs e)
    {
        if (_capturingKey)
        {
            CancelKeyCapture();
            return;
        }

        if (ExperimentalFeaturesToggle.IsChecked != true)
        {
            SetExcludeKeyStatus("请先在页面底部开启【实验性功能】。", isError: true);
            return;
        }

        if (!_stateService.HookAvailable)
        {
            SetExcludeKeyStatus("输入钩子不可用，无法捕获按键。", isError: true);
            return;
        }

        if (_stateService.State != LockState.Unlocked)
        {
            SetExcludeKeyStatus("请先取消倒计时或解锁，再添加排除按键。", isError: true);
            return;
        }

        _capturingKey = true;
        _captureCts = new CancellationTokenSource();
        AddExcludedKeyButton.Content = "取消捕获";
        ClearExcludedKeysButton.IsEnabled = false;
        SetExcludeKeyStatus("等待按键…（Esc 取消）", isError: false);

        try
        {
            var virtualKey = await _lockService.CaptureNextKeyAsync(KeyCaptureTimeout, _captureCts.Token);
            if (!_pageLoaded)
            {
                return;
            }

            if (virtualKey == null)
            {
                SetExcludeKeyStatus("未捕获到按键（已取消或超时）。", isError: false);
                return;
            }

            AddExcludedKey(virtualKey.Value);
        }
        catch (Exception)
        {
            if (_pageLoaded)
            {
                SetExcludeKeyStatus("捕获按键失败，请重试。", isError: true);
            }
        }
        finally
        {
            _capturingKey = false;
            _captureCts?.Dispose();
            _captureCts = null;

            if (_pageLoaded)
            {
                AddExcludedKeyButton.Content = "添加按键";
                RebuildExcludedKeys();
            }
        }
    }

    private void LoadFromSettings()
    {
        _suppress = true;
        try
        {
            var settings = _settingsService.Settings;
            HotkeyRecorderControl.SetGesture(
                HotkeyParser.TryParse(settings.Hotkey, out var gesture)
                    ? gesture
                    : HotkeyParser.Default);

            LockKeyboardToggle.IsChecked = settings.LockKeyboard;
            LockMouseToggle.IsChecked = settings.LockMouse;

            foreach (var item in _keyboardToggles)
            {
                item.Toggle.IsChecked = (settings.KeyboardScope & item.Scope) != 0;
            }

            foreach (var item in _mouseToggles)
            {
                item.Toggle.IsChecked = (settings.MouseScope & item.Scope) != 0;
            }

            MouseMovementLockToggle.IsChecked = !settings.AllowMouseMovement;
            ExperimentalFeaturesToggle.IsChecked = settings.EnableExperimentalFeatures;
            CountdownBox.Value = settings.CountdownSeconds;
            NotificationDurationBox.Value = settings.NotificationDurationSeconds;

            UpdateScopeToggleAvailability();
            UpdateStatusText();
            UpdateHookStatus();
            RebuildExcludedKeys();
        }
        finally
        {
            _suppress = false;
        }
    }

    private void ApplySettingsFromControls()
    {
        if (_suppress)
        {
            return;
        }

        var settings = _settingsService.Settings;
        settings.Hotkey = HotkeyRecorderControl.Gesture?.ToString() ?? "Ctrl+Alt+F";
        settings.LockKeyboard = LockKeyboardToggle.IsChecked == true;
        settings.LockMouse = LockMouseToggle.IsChecked == true;
        settings.KeyboardScope = ComputeKeyboardScope();
        settings.MouseScope = ComputeMouseScope();
        settings.AllowMouseMovement = MouseMovementLockToggle.IsChecked != true;
        settings.EnableExperimentalFeatures = ExperimentalFeaturesToggle.IsChecked == true;
        settings.CountdownSeconds = (int)(CountdownBox.Value ?? 5);
        settings.NotificationDurationSeconds = (int)(NotificationDurationBox.Value ?? 3);

        UpdateScopeToggleAvailability();
        HideError();
        _settingsService.Save();
    }

    private KeyboardLockScope ComputeKeyboardScope()
    {
        var scope = KeyboardLockScope.None;
        foreach (var item in _keyboardToggles)
        {
            if (item.Toggle.IsChecked == true)
            {
                scope |= item.Scope;
            }
        }

        return scope;
    }

    private MouseLockScope ComputeMouseScope()
    {
        var scope = MouseLockScope.None;
        foreach (var item in _mouseToggles)
        {
            if (item.Toggle.IsChecked == true)
            {
                scope |= item.Scope;
            }
        }

        return scope;
    }

    private void SetKeyboardScopes(KeyboardLockScope scopes)
    {
        foreach (var item in _keyboardToggles)
        {
            item.Toggle.IsChecked = (scopes & item.Scope) != 0;
        }
    }

    private void SetMouseScopes(MouseLockScope scopes)
    {
        foreach (var item in _mouseToggles)
        {
            item.Toggle.IsChecked = (scopes & item.Scope) != 0;
        }
    }

    private void UpdateScopeToggleAvailability()
    {
        var experimentalOn = ExperimentalFeaturesToggle.IsChecked == true;
        var keyboardOn = LockKeyboardToggle.IsChecked == true;
        foreach (var item in _keyboardToggles)
        {
            item.Toggle.IsEnabled = keyboardOn && experimentalOn;
        }

        var mouseOn = LockMouseToggle.IsChecked == true;
        foreach (var item in _mouseToggles)
        {
            item.Toggle.IsEnabled = mouseOn;
        }

        MouseMovementLockToggle.IsEnabled = mouseOn;
        AddExcludedKeyButton.IsEnabled = experimentalOn;
        ClearExcludedKeysButton.IsEnabled = experimentalOn
                                            && (_settingsService.Settings.ExcludedKeys?.Count ?? 0) > 0
                                            && !_capturingKey;
    }

    private void AddExcludedKey(uint virtualKey)
    {
        var displayName = VirtualKeyMap.GetDisplayName(virtualKey);
        var settings = _settingsService.Settings;
        settings.ExcludedKeys ??= new List<int>();

        if (settings.ExcludedKeys.Contains((int)virtualKey))
        {
            SetExcludeKeyStatus($"“{displayName}”已在排除列表中。", isError: false);
            return;
        }

        if (settings.ExcludedKeys.Count >= 64)
        {
            SetExcludeKeyStatus("排除按键最多支持 64 个，请先移除部分按键。", isError: true);
            return;
        }

        settings.ExcludedKeys.Add((int)virtualKey);
        _settingsService.Save();
        RebuildExcludedKeys();
        SetExcludeKeyStatus($"已排除“{displayName}”。", isError: false);
    }

    private void RemoveExcludedKey(uint virtualKey)
    {
        var excludedKeys = _settingsService.Settings.ExcludedKeys;
        if (excludedKeys == null || excludedKeys.RemoveAll(key => key == (int)virtualKey) == 0)
        {
            return;
        }

        _settingsService.Save();
        RebuildExcludedKeys();
        SetExcludeKeyStatus($"已移除“{VirtualKeyMap.GetDisplayName(virtualKey)}”。", isError: false);
    }

    private void RebuildExcludedKeys()
    {
        ExcludedKeysPanel.Children.Clear();

        var excludedKeys = _settingsService.Settings.ExcludedKeys ?? new List<int>();
        foreach (var virtualKey in excludedKeys.OrderBy(key => key))
        {
            ExcludedKeysPanel.Children.Add(CreateExcludedKeyChip((uint)virtualKey));
        }

        NoExcludedKeysText.IsVisible = excludedKeys.Count == 0;
        AddExcludedKeyButton.IsEnabled = ExperimentalFeaturesToggle.IsChecked == true;
        ClearExcludedKeysButton.IsEnabled = ExperimentalFeaturesToggle.IsChecked == true
                                            && excludedKeys.Count > 0
                                            && !_capturingKey;
    }

    private Control CreateExcludedKeyChip(uint virtualKey)
    {
        var label = new TextBlock
        {
            Text = VirtualKeyMap.GetDisplayName(virtualKey),
            VerticalAlignment = VerticalAlignment.Center
        };

        var removeButton = new Button
        {
            Content = "×",
            Padding = new Thickness(6, 0),
            MinWidth = 24,
            MinHeight = 24,
            FontSize = 14,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            IsEnabled = ExperimentalFeaturesToggle.IsChecked == true
        };
        ToolTip.SetTip(removeButton, "移除");
        removeButton.Click += (_, _) => RemoveExcludedKey(virtualKey);

        var content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4
        };
        content.Children.Add(label);
        content.Children.Add(removeButton);

        return new Border
        {
            Child = content,
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 3),
            Margin = new Thickness(0, 0, 6, 6),
            Background = GetBrush("SubtleFillColorSecondaryBrush")
        };
    }

    private void CancelKeyCapture()
    {
        _captureCts?.Cancel();
        _lockService.CancelKeyCapture();
    }

    private void SetExcludeKeyStatus(string message, bool isError)
    {
        ExcludeKeyStatusText.Text = message;
        ExcludeKeyStatusText.IsVisible = true;
        ExcludeKeyStatusText.Foreground = isError
            ? GetBrush("SystemFillColorCriticalBrush")
            : GetBrush("TextFillColorSecondaryBrush");
    }

    private IBrush? GetBrush(string resourceKey)
        => this.TryFindResource(resourceKey, out var resource) && resource is IBrush brush ? brush : null;

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
