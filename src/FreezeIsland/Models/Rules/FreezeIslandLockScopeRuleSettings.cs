using System.Text.Json.Serialization;

namespace FreezeIsland.Models.Rules;

/// <summary>
/// “锁定的范围包含”规则可选的范围。判定的是当前设置，而不是某一时刻是否正在拦截输入。
/// </summary>
[JsonConverter(typeof(TolerantEnumJsonConverter<FreezeIslandRuleLockScope>))]
public enum FreezeIslandRuleLockScope
{
    /// <summary>
    /// “锁定键盘”主开关已开启。
    /// </summary>
    Keyboard,

    /// <summary>
    /// “锁定鼠标”主开关已开启。
    /// </summary>
    Mouse,

    /// <summary>
    /// 鼠标指针被冻结，即开启了“锁定鼠标移动”且“锁定鼠标”主开关开启。
    /// </summary>
    MouseMovement,

    /// <summary>
    /// 鼠标左键在锁定范围内。
    /// </summary>
    MouseLeftButton,

    /// <summary>
    /// 鼠标中键在锁定范围内。
    /// </summary>
    MouseMiddleButton,

    /// <summary>
    /// 鼠标右键在锁定范围内。
    /// </summary>
    MouseRightButton,

    /// <summary>
    /// 鼠标侧键在锁定范围内。
    /// </summary>
    MouseSideButtons,

    /// <summary>
    /// 鼠标滚轮在锁定范围内。
    /// </summary>
    MouseWheel
}

/// <summary>
/// “锁定的范围包含”规则的设置。
/// </summary>
public sealed class FreezeIslandLockScopeRuleSettings
{
    public FreezeIslandRuleLockScope Scope { get; set; } = FreezeIslandRuleLockScope.Keyboard;
}
