namespace MouseKeyb;

/// <summary>
/// Interface for managing Windows startup registration.
/// </summary>
public interface IStartupRegistry
{
    /// <summary>
    /// Configures the application to start with Windows.
    /// </summary>
    void SetStartup(string appName, string executablePath);

    /// <summary>
    /// Removes the application from starting with Windows.
    /// </summary>
    void RemoveStartup(string appName);
}
