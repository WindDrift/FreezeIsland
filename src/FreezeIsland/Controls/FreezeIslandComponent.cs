using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using FreezeIsland.Models;
using FreezeIsland.Services;
using Path = Avalonia.Controls.Shapes.Path;

namespace FreezeIsland.Controls;

[ComponentInfo(
    "7D3B1C2A-5E4F-4A6B-9C8D-0F1E2A3B4C5D",
    "FreezeIsland 锁定状态",
    "\uE72E",
    "显示键盘鼠标的锁定状态与解锁热键。")]
public sealed class FreezeIslandComponent : ComponentBase
{
    private readonly FreezeIslandStateService _state;
    private readonly Path _icon;
    private readonly TextBlock _text;
    private bool _subscribed;

    public FreezeIslandComponent(FreezeIslandStateService state)
    {
        _state = state;

        _icon = new Path
        {
            Width = 20,
            Height = 20,
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center
        };

        _text = new TextBlock
        {
            FontSize = 18,
            VerticalAlignment = VerticalAlignment.Center
        };

        Content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { _icon, _text }
        };

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
        _state.PropertyChanged += OnPropertyChanged;
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
        _state.PropertyChanged -= OnPropertyChanged;
    }

    private void OnStateChanged(object? sender, LockStateChangedEventArgs e)
    {
        UpdateVisual();
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(FreezeIslandStateService.CountdownRemaining)
            or nameof(FreezeIslandStateService.HotkeyText))
        {
            UpdateVisual();
        }
    }

    private void UpdateVisual()
    {
        switch (_state.State)
        {
            case LockState.Locked:
                _icon.Data = Geometry.Parse(LockVisuals.LockedPath);
                _icon.Fill = LockVisuals.LockedBrush;
                _text.Text = $"键鼠已锁定 · {_state.HotkeyText} 解锁";
                break;
            case LockState.Countdown:
                _icon.Data = Geometry.Parse(LockVisuals.LockedPath);
                _icon.Fill = LockVisuals.CountdownBrush;
                _text.Text = $"即将锁定 {_state.CountdownRemaining}s";
                break;
            default:
                _icon.Data = Geometry.Parse(LockVisuals.UnlockedPath);
                _icon.Fill = LockVisuals.UnlockedBrush;
                _text.Text = "键鼠未锁定";
                break;
        }
    }
}
