using System;

namespace MouseKeyb;

/// <summary>
/// Application service to apply startup registry settings.
/// </summary>
public static class StartupService
{
    private const string AppName = "MouseKeyb";

    /// <summary>
    /// Applies the startup setting to the Windows Registry.
    /// </summary>
    public static void Apply(IStartupRegistry registry, bool startWithWindows)
    {
        if (startWithWindows)
        {
            string? exePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exePath))
            {
                registry.SetStartup(AppName, exePath);
            }
        }
        else
        {
            registry.RemoveStartup(AppName);
        }
    }
}
