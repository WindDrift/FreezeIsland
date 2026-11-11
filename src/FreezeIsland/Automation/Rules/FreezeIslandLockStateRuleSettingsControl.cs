using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using ClassIsland.Core.Abstractions.Controls;
using FreezeIsland.Models.Rules;

namespace FreezeIsland.Automation.Rules;

/// <summary>
/// “键鼠锁定状态是”规则的设置控件。
/// </summary>
public sealed class FreezeIslandLockStateRuleSettingsControl : RuleSettingsControlBase<FreezeIslandLockStateRuleSettings>
{
    private readonly ComboBox _stateBox = new()
    {
        ItemsSource = new[] { "已锁定", "已解锁" },
        Width = 180
    };

    private bool _loading;
    private bool _ready;

    public FreezeIslandLockStateRuleSettingsControl()
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
            if (_loading || !_ready || Settings is null)
            {
                return;
            }

            Settings.State = _stateBox.SelectedIndex == 1
                ? FreezeIslandRuleLockState.Unlocked
                : FreezeIslandRuleLockState.Locked;
        };

        Loaded += (_, _) =>
        {
            var settings = Settings;
            if (settings is null)
            {
                return;
            }

            _loading = true;
            try
            {
                _stateBox.SelectedIndex = settings.State == FreezeIslandRuleLockState.Unlocked ? 1 : 0;
                _ready = true;
            }
            finally
            {
                _loading = false;
            }
        };
    }
}
