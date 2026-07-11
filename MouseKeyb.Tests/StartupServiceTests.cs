using Xunit;

namespace MouseKeyb.Tests;

/// <summary>
/// Unit tests for the StartupService applying registry configurations.
/// </summary>
public class StartupServiceTests
{
    /// <summary>
    /// Verifies that calling Apply with true creates the registry startup key.
    /// </summary>
    [Fact]
    public void Apply_WithTrue_ShouldSetStartupPath()
    {
        var registry = new FakeStartupRegistry();

        StartupService.Apply(registry, true);

        Assert.True(registry.RegistryValues.ContainsKey("MouseKeyb"));
        Assert.False(string.IsNullOrEmpty(registry.RegistryValues["MouseKeyb"]));
    }

    /// <summary>
    /// Verifies that calling Apply with false removes the registry startup key.
    /// </summary>
    [Fact]
    public void Apply_WithFalse_ShouldRemoveStartup()
    {
        var registry = new FakeStartupRegistry();
        registry.SetStartup("MouseKeyb", "fakePath.exe");

        StartupService.Apply(registry, false);

        Assert.False(registry.RegistryValues.ContainsKey("MouseKeyb"));
    }
}
