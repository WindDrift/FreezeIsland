using System.ComponentModel;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Reactive;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Assists;
using ClassIsland.Core.Attributes;
using FreezeIsland.Models;
using FreezeIsland.Services;

namespace FreezeIsland.Controls;

[ComponentInfo(
    "7D3B1C2A-5E4F-4A6B-9C8D-0F1E2A3B4C5D",
    "FreezeIsland 锁定状态",
    "\uE72E",
    "显示键盘鼠标的锁定状态与解锁热键。")]
public sealed class FreezeIslandComponent : ComponentBase<FreezeIslandComponentSettings>
{
    private readonly FreezeIslandStateService _state;
    private readonly FreezeIslandComponentSettings _fallbackSettings = new();
    private readonly StackPanel _panel;
    private readonly Avalonia.Controls.Shapes.Path _icon;
    private readonly TextBlock _text;
    private FreezeIslandComponentSettings? _settings;
    private IDisposable? _editModeSubscription;
    private bool _subscribed;

    public FreezeIslandComponent(FreezeIslandStateService state)
    {
        _state = state;

        _icon = new Avalonia.Controls.Shapes.Path
        {
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center
        };

        _text = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center
        };

        _panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { _icon, _text }
        };

        Content = _panel;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        UpdateVisual();
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        if (_subscribed)
        {
            return;
        }

        _subscribed = true;
        _state.StateChanged += OnStateChanged;
        _state.PropertyChanged += OnStatePropertyChanged;

        _settings = Settings;
        if (_settings != null)
        {
            _settings.PropertyChanged += OnSettingsPropertyChanged;
        }

        _editModeSubscription = this
            .GetObservable(MainWindowStylesAssist.MainWindowInEditModeProperty)
            .Subscribe(new AnonymousObserver<bool>(_ => UpdateVisual()));

        UpdateVisual();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        if (!_subscribed)
        {
            return;
        }

        _subscribed = false;
        _state.StateChanged -= OnStateChanged;
        _state.PropertyChanged -= OnStatePropertyChanged;
        if (_settings != null)
        {
            _settings.PropertyChanged -= OnSettingsPropertyChanged;
            _settings = null;
        }

        _editModeSubscription?.Dispose();
        _editModeSubscription = null;
    }

    private void OnStateChanged(object? sender, LockStateChangedEventArgs e)
    {
        UpdateVisual();
    }

    private void OnStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(FreezeIslandStateService.CountdownRemaining)
            or nameof(FreezeIslandStateService.HotkeyText))
        {
            UpdateVisual();
        }
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        var settings = Settings ?? _fallbackSettings;
        var state = _state.State;

        _icon.IsVisible = settings.ShowIcon;
        var iconSize = Math.Clamp(settings.IconSize, 12, 48);
        _icon.Width = iconSize;
        _icon.Height = iconSize;

        switch (state)
        {
            case LockState.Locked:
                _icon.Data = LockVisuals.LockedGeometry;
                _icon.Fill = new SolidColorBrush(settings.LockedColor);
                break;
            case LockState.Countdown:
                _icon.Data = LockVisuals.LockedGeometry;
                _icon.Fill = new SolidColorBrush(settings.CountdownColor);
                break;
            default:
                _icon.Data = LockVisuals.UnlockedGeometry;
                _icon.Fill = new SolidColorBrush(settings.UnlockedColor);
                break;
        }

        if (settings.UseCustomFontSize)
        {
            _text.FontSize = Math.Clamp(settings.FontSize, 12, 40);
        }
        else
        {
            _text.ClearValue(TextBlock.FontSizeProperty);
        }

        var text = RenderText(settings, state);
        _text.Text = text;
        _text.IsVisible = settings.ShowText && !string.IsNullOrWhiteSpace(text);
        _panel.Spacing = _icon.IsVisible && _text.IsVisible ? 8 : 0;

        var editMode = MainWindowStylesAssist.GetMainWindowInEditMode(this);
        IsVisible = !(settings.HideWhenUnlocked && state == LockState.Unlocked && !editMode);
    }

    private string RenderText(FreezeIslandComponentSettings settings, LockState state)
    {
        var template = state switch
        {
            LockState.Locked => settings.LockedText,
            LockState.Countdown => settings.CountdownText,
            _ => settings.UnlockedText
        };

        if (string.IsNullOrWhiteSpace(template))
        {
            return string.Empty;
        }

        if (state == LockState.Locked
            && !settings.ShowHotkey
            && template == FreezeIslandComponentSettings.DefaultLockedText)
        {
            return "键鼠已锁定";
        }

        if (state == LockState.Countdown
            && !settings.ShowCountdownSeconds
            && template == FreezeIslandComponentSettings.DefaultCountdownText)
        {
            return "即将锁定";
        }

        var text = template;
        if (text.Contains("{hotkey}", StringComparison.Ordinal))
        {
            text = text.Replace(
                "{hotkey}",
                settings.ShowHotkey ? _state.HotkeyText : string.Empty,
                StringComparison.Ordinal);
        }

        if (text.Contains("{seconds}", StringComparison.Ordinal))
        {
            text = text.Replace(
                "{seconds}",
                settings.ShowCountdownSeconds ? _state.CountdownRemaining.ToString() : string.Empty,
                StringComparison.Ordinal);
        }

        text = Regex.Replace(text, @"\s+", " ").Trim();
        return text.Trim('·', '，', ',', '、', '|', '-', ' ');
    }
}
