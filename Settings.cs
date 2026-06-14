using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Linq;

namespace MouseKeyb;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum KeyEventType
{
    Press = 0,
    Down = 1,
    Up = 2
}

/// <summary>
/// Model representing a single key capture in a keyboard shortcut.
/// </summary>
public class KeyStroke
{
    public ushort Vk { get; set; }
    public string Name { get; set; } = string.Empty;
    public KeyEventType Type { get; set; } = KeyEventType.Press;
}

/// <summary>
/// Model representing the mapping between a gesture pattern and simulated keys.
/// </summary>
public class GestureMapping : INotifyPropertyChanged
{
    private string _pattern = string.Empty;
    private string _actionName = string.Empty;
    private List<KeyStroke> _keys = new();

    public string Pattern
    {
        get => _pattern;
        set { _pattern = value; OnPropertyChanged(); }
    }

    public string ActionName
    {
        get => _actionName;
        set { _actionName = value; OnPropertyChanged(); }
    }

    public List<KeyStroke> Keys
    {
        get => _keys;
        set
        {
            _keys = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(KeysString));
        }
    }

    /// <summary>
    /// Gets or sets the string representation of the key strokes (e.g. <Ctrl>+C or C).
    /// </summary>
    [JsonIgnore]
    public string KeysString
    {
        get
        {
            if (Keys == null || Keys.Count == 0) return string.Empty;
            return string.Join("+", Keys.Select(k => FormatKeyStroke(k)));
        }
        set
        {
            var newKeys = ParseKeysString(value);
            _keys = newKeys;
            OnPropertyChanged(nameof(Keys));
        }
    }

    private static string FormatKeyStroke(KeyStroke k)
    {
        string suffix = "";
        if (k.Type == KeyEventType.Down) suffix = " Down";
        else if (k.Type == KeyEventType.Up) suffix = " Up";

        bool isMod = IsModifier(k.Name);
        return isMod ? $"<{k.Name}{suffix}>" : $"{k.Name}{suffix}";
    }

    private static bool IsModifier(string name)
    {
        return name == "Ctrl" || name == "Alt" || name == "Shift" || name == "Win";
    }

    public static List<KeyStroke> CollapseEvents(List<KeyStroke> rawEvents)
    {
        var collapsed = new List<KeyStroke>();
        for (int i = 0; i < rawEvents.Count; i++)
        {
            if (i < rawEvents.Count - 1 &&
                rawEvents[i].Vk == rawEvents[i + 1].Vk &&
                rawEvents[i].Type == KeyEventType.Down &&
                rawEvents[i + 1].Type == KeyEventType.Up)
            {
                collapsed.Add(new KeyStroke
                {
                    Vk = rawEvents[i].Vk,
                    Name = rawEvents[i].Name,
                    Type = KeyEventType.Press
                });
                i++;
            }
            else
            {
                collapsed.Add(rawEvents[i]);
            }
        }
        return collapsed;
    }

    public static List<KeyStroke> ParseKeysString(string input)
    {
        var result = new List<KeyStroke>();
        if (string.IsNullOrWhiteSpace(input)) return result;

        var parts = input.Split('+');
        var activeModifiers = new List<KeyStroke>();

        foreach (var part in parts)
        {
            var trimmed = part.Trim().Replace("<", "").Replace(">", "");
            if (string.IsNullOrEmpty(trimmed)) continue;

            KeyEventType type = KeyEventType.Press;
            string keyName = trimmed;
            if (trimmed.EndsWith(" Down", StringComparison.OrdinalIgnoreCase))
            {
                type = KeyEventType.Down;
                keyName = trimmed.Substring(0, trimmed.Length - 5).Trim();
            }
            else if (trimmed.EndsWith(" Up", StringComparison.OrdinalIgnoreCase))
            {
                type = KeyEventType.Up;
                keyName = trimmed.Substring(0, trimmed.Length - 3).Trim();
            }

            ushort vk = GetVkFromName(keyName);
            if (vk == 0) continue;

            var normalizedName = NormalizeKeyName(keyName);
            bool isMod = IsModifier(normalizedName);

            if (isMod && type == KeyEventType.Press)
            {
                var downStroke = new KeyStroke { Vk = vk, Name = normalizedName, Type = KeyEventType.Down };
                result.Add(downStroke);
                activeModifiers.Add(downStroke);
            }
            else
            {
                result.Add(new KeyStroke { Vk = vk, Name = normalizedName, Type = type });

                if (!isMod && type == KeyEventType.Press)
                {
                    for (int i = activeModifiers.Count - 1; i >= 0; i--)
                    {
                        result.Add(new KeyStroke 
                        { 
                            Vk = activeModifiers[i].Vk, 
                            Name = activeModifiers[i].Name, 
                            Type = KeyEventType.Up 
                        });
                    }
                    activeModifiers.Clear();
                }
            }
        }

        for (int i = activeModifiers.Count - 1; i >= 0; i--)
        {
            result.Add(new KeyStroke 
            { 
                Vk = activeModifiers[i].Vk, 
                Name = activeModifiers[i].Name, 
                Type = KeyEventType.Up 
            });
        }

        return result;
    }

    private static ushort GetVkFromName(string name)
    {
        name = name.ToUpperInvariant();
        
        // Modifiers
        if (name == "CTRL" || name == "CONTROL") return 0xA2; // VK_LCONTROL
        if (name == "ALT") return 0xA4; // VK_LMENU
        if (name == "SHIFT") return 0xA0; // VK_LSHIFT
        if (name == "WIN" || name == "WINDOWS" || name == "LWIN" || name == "RWIN") return 0x5B;

        // Special keys
        if (name == "ENTER" || name == "RETURN") return 0x0D;
        if (name == "TAB") return 0x09;
        if (name == "ESC" || name == "ESCAPE") return 0x1B;
        if (name == "SPACE" || name == "ESPAÇO") return 0x20;
        if (name == "BACKSPACE" || name == "BACK") return 0x08;
        if (name == "DEL" || name == "DELETE") return 0x2E;
        if (name == "INS" || name == "INSERT") return 0x2D;
        if (name == "HOME") return 0x24;
        if (name == "END") return 0x23;
        if (name == "PGUP" || name == "PAGEUP") return 0x21;
        if (name == "PGDN" || name == "PAGEDOWN") return 0x22;
        if (name == "LEFT" || name == "ESQUERDA") return 0x25;
        if (name == "UP" || name == "CIMA") return 0x26;
        if (name == "RIGHT" || name == "DIREITA") return 0x27;
        if (name == "DOWN" || name == "BAIXO") return 0x28;

        // F-keys
        if (name.StartsWith("F") && name.Length > 1 && ushort.TryParse(name.Substring(1), out ushort fNum) && fNum >= 1 && fNum <= 12)
        {
            return (ushort)(0x6F + fNum);
        }

        // Single letter/number
        if (name.Length == 1)
        {
            char c = name[0];
            if (c >= 'A' && c <= 'Z') return (ushort)c;
            if (c >= '0' && c <= '9') return (ushort)c;
        }

        return 0;
    }

    private static string NormalizeKeyName(string name)
    {
        var upper = name.ToUpperInvariant();
        if (upper == "CTRL" || upper == "CONTROL") return "Ctrl";
        if (upper == "ALT") return "Alt";
        if (upper == "SHIFT") return "Shift";
        if (upper == "WIN" || upper == "WINDOWS") return "Win";
        
        if (name.Length > 1)
        {
            return char.ToUpper(name[0]) + name.Substring(1).ToLower();
        }
        return name.ToUpperInvariant();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Model representing all application configuration settings.
/// </summary>
public class AppSettings
{
    public List<GestureMapping> Mappings { get; set; } = new();
    public double SegmentThreshold { get; set; } = 40.0;

    private static readonly (string Pattern, string ActionName, string Keys)[] DefaultDefinitions = new[]
    {
        ("D", "Browser - Fechar Aba", "<Ctrl>+W"),
        ("L", "Browser - Voltar", "<Alt>+Left"),
        ("R", "Browser - Avançar", "<Alt>+Right"),
        ("U", "Browser - Nova Aba", "<Ctrl>+T"),
        ("UD", "Browser - Reabrir Aba Fechada", "<Ctrl>+<Shift>+T"),
        ("DR", "Browser - Próxima Aba", "<Ctrl>+Tab"),
        ("DL", "Browser - Aba Anterior", "<Ctrl>+<Shift>+Tab"),
        ("RU", "Browser - Recarregar Página", "F5"),
        ("UL", "Windows - Fechar Janela", "<Alt>+F4"),
        ("LU", "Windows - Mostrar Área de Trabalho", "<Win>+D"),
        ("LD", "Windows - Alternar Janelas", "<Win>+Tab")
    };

    /// <summary>
    /// Creates a default settings instance.
    /// Usage example: var defaultSettings = AppSettings.CreateDefault();
    /// </summary>
    public static AppSettings CreateDefault()
    {
        var settings = new AppSettings();
        foreach (var def in DefaultDefinitions)
        {
            settings.Mappings.Add(new GestureMapping
            {
                Pattern = def.Pattern,
                ActionName = def.ActionName,
                Keys = GestureMapping.ParseKeysString(def.Keys)
            });
        }
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
