using System.Text.Json.Serialization;

namespace FreezeIsland.Models.Rules;

/// <summary>
/// “键鼠锁定状态是”规则要判定的状态。
/// </summary>
[JsonConverter(typeof(TolerantEnumJsonConverter<FreezeIslandRuleLockState>))]
public enum FreezeIslandRuleLockState
{
    /// <summary>
    /// 键盘鼠标已被锁定，输入正被拦截。
    /// </summary>
    Locked,

    /// <summary>
    /// 键盘鼠标未被锁定，包括锁定倒计时期间。
    /// </summary>
    Unlocked
}

/// <summary>
/// “键鼠锁定状态是”规则的设置。
/// </summary>
public sealed class FreezeIslandLockStateRuleSettings
{
    public FreezeIslandRuleLockState State { get; set; } = FreezeIslandRuleLockState.Locked;
}
