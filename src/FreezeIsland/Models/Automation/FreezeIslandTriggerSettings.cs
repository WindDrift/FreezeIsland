namespace FreezeIsland.Models.Automation;

public enum FreezeIslandTriggerState
{
    Locked,
    Unlocked
}

public sealed class FreezeIslandTriggerSettings
{
    public FreezeIslandTriggerState State { get; set; } = FreezeIslandTriggerState.Locked;
}
