using ClassIsland.Shared;
using FreezeIsland.Models;
using FreezeIsland.Models.Rules;
using FreezeIsland.Services;

namespace FreezeIsland.Automation.Rules;

/// <summary>
/// 提供给 ClassIsland 规则集的规则处理程序。
/// 规则在插件初始化阶段只能注册委托，因此服务在判定时才通过 <see cref="IAppHost"/> 解析。
/// </summary>
public static class FreezeIslandRules
{
    public const string LockStateRuleId = "winddrift.freezeisland.rule.lockState";

    public const string LockScopeRuleId = "winddrift.freezeisland.rule.lockScope";

    /// <summary>
    /// 判定“键鼠锁定状态是”。倒计时阶段输入尚未被拦截，按未锁定处理。
    /// </summary>
    public static bool HandleLockState(object? settings)
    {
        var stateService = IAppHost.TryGetService<FreezeIslandStateService>();
        if (stateService == null)
        {
            return false;
        }

        var ruleSettings = settings as FreezeIslandLockStateRuleSettings
                           ?? new FreezeIslandLockStateRuleSettings();

        // 输入钩子不可用时无法真正拦截输入，此时“已锁定”不成立。
        var isLocked = stateService.State == LockState.Locked && stateService.HookAvailable;
        return ruleSettings.State == FreezeIslandRuleLockState.Locked ? isLocked : !isLocked;
    }

    /// <summary>
    /// 判定“锁定的范围包含”。判定的是当前设置，未锁定时同样可以成立。
    /// </summary>
    public static bool HandleLockScope(object? settings)
    {
        var settingsService = IAppHost.TryGetService<FreezeIslandSettingsService>();
        if (settingsService == null)
        {
            return false;
        }

        var ruleSettings = settings as FreezeIslandLockScopeRuleSettings
                           ?? new FreezeIslandLockScopeRuleSettings();
        var current = settingsService.GetSnapshot();

        return ruleSettings.Scope switch
        {
            FreezeIslandRuleLockScope.Keyboard => current.LockKeyboard,
            FreezeIslandRuleLockScope.Mouse => current.LockMouse,
            FreezeIslandRuleLockScope.MouseMovement => current.LockMouse && !current.AllowMouseMovement,
            FreezeIslandRuleLockScope.MouseLeftButton => HasMouseScope(current, MouseLockScope.LeftButton),
            FreezeIslandRuleLockScope.MouseMiddleButton => HasMouseScope(current, MouseLockScope.MiddleButton),
            FreezeIslandRuleLockScope.MouseRightButton => HasMouseScope(current, MouseLockScope.RightButton),
            FreezeIslandRuleLockScope.MouseSideButtons => HasMouseScope(current, MouseLockScope.SideButtons),
            FreezeIslandRuleLockScope.MouseWheel => HasMouseScope(current, MouseLockScope.Wheel),
            _ => false
        };
    }

    private static bool HasMouseScope(FreezeIslandSettings settings, MouseLockScope scope)
        => settings.LockMouse && (settings.MouseScope & scope) != 0;
}
