using System.IO;
using System.Text.Json;

namespace HeroDangle;

public sealed class AppConfig
{
    public string CharmId { get; set; } = "nazar";
    public string CustomEmoji { get; set; } = "⭐";
    public double AnchorXNormalized { get; set; } = 0.72;
    public uint HotkeyModifiers { get; set; } = NativeMethods.ModControl | NativeMethods.ModAlt;
    public uint HotkeyVirtualKey { get; set; } = 0x44; // D
    public bool LaunchOnStartup { get; set; }
    public bool Summoned { get; set; } = true;
}

public static class ConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string DirectoryPath { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HeroDangle");

    public static string FilePath => Path.Combine(DirectoryPath, "config.json");

    public static AppConfig Load()
    {
        try
        {
            if (!File.Exists(FilePath))
                return new AppConfig();

            string json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }

    public static void Save(AppConfig config)
    {
        Directory.CreateDirectory(DirectoryPath);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(config, JsonOptions));
    }
}
