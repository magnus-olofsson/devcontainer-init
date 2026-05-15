using System.Text.Json;
using DevcontainerInit.Models;

namespace DevcontainerInit.Services;

public class ConfigService
{
    private static readonly string GlobalConfigDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dci");
    private static readonly string GlobalConfigPath =
        Path.Combine(GlobalConfigDir, "config.json");
    private const string LocalConfigFileName = ".dci.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public AppConfig Load()
    {
        var config = LoadGlobalConfig();
        var localConfig = LoadLocalConfig();

        if (localConfig is not null)
        {
            if (!string.IsNullOrWhiteSpace(localConfig.GitHubOwner))
                config.GitHubOwner = localConfig.GitHubOwner;
            if (!string.IsNullOrWhiteSpace(localConfig.GitHubRepo))
                config.GitHubRepo = localConfig.GitHubRepo;
            if (!string.IsNullOrWhiteSpace(localConfig.TemplatesPath))
                config.TemplatesPath = localConfig.TemplatesPath;
        }

        return config;
    }

    public bool GlobalConfigExists() => File.Exists(GlobalConfigPath);

    public void SaveGlobalConfig(AppConfig config)
    {
        Directory.CreateDirectory(GlobalConfigDir);
        File.WriteAllText(GlobalConfigPath, JsonSerializer.Serialize(config, JsonOptions));
    }

    public string GetGlobalConfigPath() => GlobalConfigPath;
    public string GetLocalConfigPath() => Path.Combine(Directory.GetCurrentDirectory(), LocalConfigFileName);

    private AppConfig LoadGlobalConfig()
    {
        if (!File.Exists(GlobalConfigPath))
            return new AppConfig();

        try
        {
            var json = File.ReadAllText(GlobalConfigPath);
            return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }

    private AppConfig? LoadLocalConfig()
    {
        var localPath = Path.Combine(Directory.GetCurrentDirectory(), LocalConfigFileName);
        if (!File.Exists(localPath))
            return null;

        try
        {
            var json = File.ReadAllText(localPath);
            return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
