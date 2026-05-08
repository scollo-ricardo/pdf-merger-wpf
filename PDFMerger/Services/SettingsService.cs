using System.IO;
using System.Text.Json;
using PDFMerger.Models;

namespace PDFMerger.Services;

public static class SettingsService
{
    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PDFMerger");

    private static readonly string SettingsPath =
        Path.Combine(SettingsDir, "settings.json");

    public static AppSettings Current { get; private set; } = new AppSettings();

    public static void Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded != null)
                    Current = loaded;
            }
        }
        catch
        {
            Current = new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
            Current = settings;
        }
        catch
        {
            // Silently fail on save errors
        }
    }
}
