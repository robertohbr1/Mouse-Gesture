using Microsoft.Win32;

namespace MouseKeyb;

/// <summary>
/// Implementation of IStartupRegistry using Windows Registry.
/// </summary>
public class WindowsStartupRegistry : IStartupRegistry
{
    private const string RunRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>
    /// Adds or updates the registry key to execute the application on Windows startup.
    /// </summary>
    public void SetStartup(string appName, string executablePath)
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
        key?.SetValue(appName, $"\"{executablePath}\"");
    }

    /// <summary>
    /// Removes the registry key so the application does not execute on Windows startup.
    /// </summary>
    public void RemoveStartup(string appName)
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
        key?.DeleteValue(appName, false);
    }
}
