using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using ClassIsland.Core.Abstractions.Controls;
using FreezeIsland.Models.Rules;

namespace FreezeIsland.Automation.Rules;

/// <summary>
/// “锁定的范围包含”规则的设置控件。
/// </summary>
public sealed class FreezeIslandLockScopeRuleSettingsControl : RuleSettingsControlBase<FreezeIslandLockScopeRuleSettings>
{
    private static readonly FreezeIslandRuleLockScope[] Scopes =
    {
        FreezeIslandRuleLockScope.Keyboard,
        FreezeIslandRuleLockScope.Mouse,
        FreezeIslandRuleLockScope.MouseMovement,
        FreezeIslandRuleLockScope.MouseLeftButton,
        FreezeIslandRuleLockScope.MouseMiddleButton,
        FreezeIslandRuleLockScope.MouseRightButton,
        FreezeIslandRuleLockScope.MouseSideButtons,
        FreezeIslandRuleLockScope.MouseWheel
    };

    private readonly ComboBox _scopeBox = new()
    {
        ItemsSource = new[]
        {
            "键盘",
            "鼠标",
            "鼠标移动",
            "鼠标左键",
            "鼠标中键",
            "鼠标右键",
            "鼠标侧键",
            "鼠标滚轮"
        },
        Width = 180
    };

    private bool _loading;
    private bool _ready;

    public FreezeIslandLockScopeRuleSettingsControl()
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
                    Text = "范围：",
                    VerticalAlignment = VerticalAlignment.Center
                },
                _scopeBox
            }
        };

        _scopeBox.SelectionChanged += (_, _) =>
        {
            var index = _scopeBox.SelectedIndex;
            if (_loading || !_ready || Settings is null || index < 0 || index >= Scopes.Length)
            {
                return;
            }

            Settings.Scope = Scopes[index];
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
                var index = Array.IndexOf(Scopes, settings.Scope);
                _scopeBox.SelectedIndex = index < 0 ? 0 : index;
                _ready = true;
            }
            finally
            {
                _loading = false;
            }
        };
    }
}
