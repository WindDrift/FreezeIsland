using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using FreezeIsland.Models;
using FreezeIsland.Services;

namespace FreezeIsland.Automation.Actions;

[ActionInfo("winddrift.freezeisland.action.unlock", "解锁键鼠", "\uE785")]
public sealed class UnlockAction : ActionBase
{
    private readonly FreezeIslandLockService _lockService;

    public UnlockAction(FreezeIslandLockService lockService)
    {
        _lockService = lockService;
    }

    protected override async Task OnInvoke()
    {
        await base.OnInvoke();
        _lockService.Unlock(UnlockReason.Automation);
    }
}
