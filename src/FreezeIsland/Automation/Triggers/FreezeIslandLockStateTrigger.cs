using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using FreezeIsland.Models;
using FreezeIsland.Models.Automation;
using FreezeIsland.Services;

namespace FreezeIsland.Automation.Triggers;

[TriggerInfo("winddrift.freezeisland.trigger.lockstate", "键鼠锁定状态变化", "\uE72E")]
public sealed class FreezeIslandLockStateTrigger : TriggerBase<FreezeIslandTriggerSettings>
{
    private readonly FreezeIslandStateService _state;
    private bool _loaded;

    public FreezeIslandLockStateTrigger(FreezeIslandStateService state)
    {
        _state = state;
    }

    public override void Loaded()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        _state.StateChanged += OnStateChanged;
    }

    public override void UnLoaded()
    {
        if (!_loaded)
        {
            return;
        }

        _loaded = false;
        _state.StateChanged -= OnStateChanged;
    }

    private void OnStateChanged(object? sender, LockStateChangedEventArgs e)
    {
        if (e.NewState is not (LockState.Locked or LockState.Unlocked))
        {
            return;
        }

        var isLocked = e.NewState == LockState.Locked;
        var wantLocked = Settings.State == FreezeIslandTriggerState.Locked;

        if (isLocked == wantLocked)
        {
            Trigger();
        }
        else
        {
            TriggerRevert();
        }
    }
}
