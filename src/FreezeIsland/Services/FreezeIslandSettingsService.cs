using System.Text.Json;
using FreezeIsland.Models;
using Microsoft.Extensions.Logging;

namespace FreezeIsland.Services;

/// <summary>
/// 读写插件设置（PluginConfigFolder/settings.json）。
/// </summary>
public sealed class FreezeIslandSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<FreezeIslandSettingsService> _logger;
    private readonly object _sync = new();
    private FreezeIslandSettings _settings;

    public FreezeIslandSettingsService(string pluginConfigFolder, ILogger<FreezeIslandSettingsService> logger)
    {
        _logger = logger;
        Directory.CreateDirectory(pluginConfigFolder);
        SettingsPath = Path.Combine(pluginConfigFolder, "settings.json");
        _settings = Load();
    }

    public string SettingsPath { get; }

    public FreezeIslandSettings Settings => _settings;

    public event EventHandler? Changed;

    public FreezeIslandSettings GetSnapshot()
    {
        lock (_sync)
        {
            return _settings.Clone();
        }
    }

    public void Save()
    {
        FreezeIslandSettings snapshot;
        lock (_sync)
        {
            _settings.Normalize();
            snapshot = _settings.Clone();
            try
            {
                var json = JsonSerializer.Serialize(snapshot, JsonOptions);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存 FreezeIsland 设置失败：{Path}", SettingsPath);
            }
        }

        try
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FreezeIsland 设置变更事件处理失败");
        }
    }

    private FreezeIslandSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var loaded = JsonSerializer.Deserialize<FreezeIslandSettings>(json, JsonOptions);
                if (loaded != null)
                {
                    loaded.Normalize();
                    return loaded;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读取 FreezeIsland 设置失败，将使用默认值：{Path}", SettingsPath);
        }

        var defaults = new FreezeIslandSettings();
        defaults.Normalize();
        try
        {
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(defaults, JsonOptions));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "写入默认 FreezeIsland 设置失败：{Path}", SettingsPath);
        }

        return defaults;
    }
}
