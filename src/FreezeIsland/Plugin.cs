using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Extensions.Registry;
using FreezeIsland.Automation;
using FreezeIsland.Automation.Actions;
using FreezeIsland.Automation.Triggers;
using FreezeIsland.Controls;
using FreezeIsland.Services;
using FreezeIsland.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FreezeIsland;

/// <summary>
/// FreezeIsland 插件入口。
/// </summary>
[PluginEntrance]
public class Plugin : PluginBase
{
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        services.AddSingleton(sp => new FreezeIslandSettingsService(
            PluginConfigFolder,
            sp.GetRequiredService<ILogger<FreezeIslandSettingsService>>()));
        services.AddSingleton<FreezeIslandStateService>();
        services.AddSingleton<FreezeIslandLockService>();
        services.AddHostedService(sp => sp.GetRequiredService<FreezeIslandLockService>());

        services.AddNotificationProvider<FreezeIslandNotificationProvider>();
        services.AddComponent<FreezeIslandComponent, FreezeIslandComponentSettingsControl>();
        services.AddSettingsPage<FreezeIslandSettingsPage>();

        services.AddAction<LockAction>();
        services.AddAction<UnlockAction>();
        services.AddAction<ToggleAction>();
        services.AddTrigger<FreezeIslandLockStateTrigger, FreezeIslandTriggerSettingsControl>();
    }
}
