using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FreezeIsland.Models;

/// <summary>
/// “FreezeIsland 锁定状态”组件的可配置项。
/// </summary>
public partial class FreezeIslandComponentSettings : ObservableObject
{
    public const string LockedStateLabel = "键鼠已锁定";
    public const string CountdownStateLabel = "即将锁定";
    public const string DefaultUnlockedText = "键鼠未锁定";
    public const string DefaultCountdownText = CountdownStateLabel + " {seconds}s";
    public const string DefaultLockedText = LockedStateLabel + " · {hotkey} 解锁";

    [ObservableProperty] private bool _showIcon = true;

    [ObservableProperty] private double _iconSize = 20;

    [ObservableProperty] private Color _lockedColor = Color.Parse("#E53935");

    [ObservableProperty] private Color _countdownColor = Color.Parse("#FB8C00");

    [ObservableProperty] private Color _unlockedColor = Color.Parse("#43A047");

    [ObservableProperty] private bool _showText = true;

    [ObservableProperty] private bool _showHotkey = true;

    [ObservableProperty] private bool _showCountdownSeconds = true;

    [ObservableProperty] private bool _hideWhenUnlocked;

    [ObservableProperty] private bool _useCustomFontSize;

    [ObservableProperty] private double _fontSize = 18;

    [ObservableProperty] private string _unlockedText = DefaultUnlockedText;

    [ObservableProperty] private string _countdownText = DefaultCountdownText;

    [ObservableProperty] private string _lockedText = DefaultLockedText;
}
