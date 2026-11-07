using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using FreezeIsland.Models;
using FreezeIsland.Services;

namespace FreezeIsland.Automation.Actions;

[ActionInfo("winddrift.freezeisland.action.toggle", "切换键鼠锁定", "\uE8AB")]
public sealed class ToggleAction : ActionBase
{
    private readonly FreezeIslandLockService _lockService;

    public ToggleAction(FreezeIslandLockService lockService)
    {
        _lockService = lockService;
    }

    protected override async Task OnInvoke()
    {
        await base.OnInvoke();
        _lockService.Toggle(UnlockReason.Automation);
    }
}
