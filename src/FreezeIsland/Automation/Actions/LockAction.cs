using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using FreezeIsland.Models;
using FreezeIsland.Services;

namespace FreezeIsland.Automation.Actions;

[ActionInfo("winddrift.freezeisland.action.lock", "锁定键鼠", "\uE72E")]
public sealed class LockAction : ActionBase
{
    private readonly FreezeIslandLockService _lockService;

    public LockAction(FreezeIslandLockService lockService)
    {
        _lockService = lockService;
    }

    protected override async Task OnInvoke()
    {
        await base.OnInvoke();
        _lockService.RequestLock(UnlockReason.Automation);
    }

    protected override async Task OnRevert()
    {
        await base.OnRevert();
        _lockService.Unlock(UnlockReason.AutomationRevert);
    }
}
