using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using ClassIsland.Core.Abstractions.Controls;
using FreezeIsland.Models.Automation;

namespace FreezeIsland.Automation;

public sealed class FreezeIslandTriggerSettingsControl : TriggerSettingsControlBase<FreezeIslandTriggerSettings>
{
    private readonly ComboBox _stateBox = new()
    {
        ItemsSource = new[] { "已锁定", "已解锁" },
        Width = 180
    };
    private bool _loading;
    private bool _ready;

    public FreezeIslandTriggerSettingsControl()
    {
        Content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 4),
            Children =
            {
                new TextBlock
                {
                    Text = "状态：",
                    VerticalAlignment = VerticalAlignment.Center
                },
                _stateBox
            }
        };

        _stateBox.SelectionChanged += (_, _) =>
        {
            if (_loading || !_ready)
            {
                return;
            }

            Settings.State = _stateBox.SelectedIndex == 1
                ? FreezeIslandTriggerState.Unlocked
                : FreezeIslandTriggerState.Locked;
        };

        Loaded += (_, _) =>
        {
            _loading = true;
            try
            {
                _stateBox.SelectedIndex = Settings.State == FreezeIslandTriggerState.Unlocked ? 1 : 0;
                _ready = true;
            }
            finally
            {
                _loading = false;
            }
        };
    }
}
