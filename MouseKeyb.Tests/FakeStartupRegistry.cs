using System.Collections.Generic;

namespace MouseKeyb.Tests;

/// <summary>
/// Fake implementation of IStartupRegistry for unit testing.
/// </summary>
public class FakeStartupRegistry : IStartupRegistry
{
    /// <summary>
    /// Gets the dictionary simulating the startup registry keys.
    /// </summary>
    public Dictionary<string, string> RegistryValues { get; } = new();

    /// <summary>
    /// Simulates setting a startup path.
    /// </summary>
    public void SetStartup(string appName, string executablePath)
    {
        RegistryValues[appName] = executablePath;
    }

    /// <summary>
    /// Simulates removing a startup key.
    /// </summary>
    public void RemoveStartup(string appName)
    {
        RegistryValues.Remove(appName);
    }
}
