using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MouseKeyb;

/// <summary>
/// Model representing a single key capture in a keyboard shortcut.
/// </summary>
public class KeyStroke
{
    public ushort Vk { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Model representing the mapping between a gesture pattern and simulated keys.
/// </summary>
public class GestureMapping
{
    public string Pattern { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public List<KeyStroke> Keys { get; set; } = new();
}

/// <summary>
/// Model representing all application configuration settings.
/// </summary>
public class AppSettings
{
    public List<GestureMapping> Mappings { get; set; } = new();
    public double SegmentThreshold { get; set; } = 40.0;

    /// <summary>
    /// Creates a default settings instance.
    /// Usage example: var defaultSettings = AppSettings.CreateDefault();
    /// </summary>
    public static AppSettings CreateDefault()
    {
        var settings = new AppSettings();
        settings.Mappings.Add(new GestureMapping
        {
            Pattern = "D",
            ActionName = "Fechar Aba (Ctrl+W)",
            Keys = new List<KeyStroke>
            {
                new() { Vk = 0x11, Name = "Ctrl" },
                new() { Vk = 0x57, Name = "W" }
            }
        });
        return settings;
    }
}

/// <summary>
/// Manages saving and loading of the settings JSON file from application data storage.
/// </summary>
public class SettingsStore
{
    private readonly string _filePath;

    /// <summary>
    /// Initializes a new instance of SettingsStore and ensures the directory exists.
    /// Usage example: var store = new SettingsStore();
    /// </summary>
    public SettingsStore()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string dir = Path.Combine(appData, "MouseKeyb");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        _filePath = Path.Combine(dir, "settings.json");
    }

    /// <summary>
    /// Loads the application settings from file or returns default settings if file doesn't exist.
    /// Usage example: var settings = store.Load();
    /// </summary>
    public AppSettings Load()
    {
        if (!File.Exists(_filePath))
        {
            var defaults = AppSettings.CreateDefault();
            Save(defaults);
            return defaults;
        }

        try
        {
            string json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? AppSettings.CreateDefault();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load settings from {_filePath}. Check JSON structure.", ex);
        }
    }

    /// <summary>
    /// Saves the provided settings to the settings JSON file.
    /// Usage example: store.Save(settings);
    /// </summary>
    public void Save(AppSettings settings)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(settings, options);
        File.WriteAllText(_filePath, json);
    }
}
