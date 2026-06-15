using System.Collections.Generic;
using Xunit;
using MouseKeyb;

namespace MouseKeyb.Tests;

/// <summary>
/// Unit tests for the key sequence parsing and formatting logic in GestureMapping.
/// </summary>
public class KeysParsingTests
{
    /// <summary>
    /// Verifies that a control combination string like '<Ctrl>+C' is parsed correctly.
    /// </summary>
    [Fact]
    public void ParseKeysString_ShouldParseControlCombination()
    {
        string input = "<Ctrl>+C";
        List<KeyStroke> result = GestureMapping.ParseKeysString(input);

        Assert.Equal(3, result.Count);
        
        Assert.Equal((ushort)0xA2, result[0].Vk);
        Assert.Equal("Ctrl", result[0].Name);
        Assert.Equal(KeyEventType.Down, result[0].Type);

        Assert.Equal((ushort)0x43, result[1].Vk);
        Assert.Equal("C", result[1].Name);
        Assert.Equal(KeyEventType.Press, result[1].Type);

        Assert.Equal((ushort)0xA2, result[2].Vk);
        Assert.Equal("Ctrl", result[2].Name);
        Assert.Equal(KeyEventType.Up, result[2].Type);
    }

    /// <summary>
    /// Verifies that a simple key string like 'C' is parsed correctly.
    /// </summary>
    [Fact]
    public void ParseKeysString_ShouldParseSimpleKey()
    {
        string input = "C";
        List<KeyStroke> result = GestureMapping.ParseKeysString(input);

        Assert.Single(result);
        Assert.Equal((ushort)0x43, result[0].Vk);
        Assert.Equal("C", result[0].Name);
    }

    /// <summary>
    /// Verifies that a multiple modifier key string like '<Ctrl>+<Alt>+T' is parsed correctly.
    /// </summary>
    [Fact]
    public void ParseKeysString_ShouldParseMultipleModifiers()
    {
        string input = "<Ctrl>+<Alt>+T";
        List<KeyStroke> result = GestureMapping.ParseKeysString(input);

        Assert.Equal(5, result.Count);

        Assert.Equal((ushort)0xA2, result[0].Vk);
        Assert.Equal("Ctrl", result[0].Name);
        Assert.Equal(KeyEventType.Down, result[0].Type);

        Assert.Equal((ushort)0xA4, result[1].Vk);
        Assert.Equal("Alt", result[1].Name);
        Assert.Equal(KeyEventType.Down, result[1].Type);

        Assert.Equal((ushort)0x54, result[2].Vk);
        Assert.Equal("T", result[2].Name);
        Assert.Equal(KeyEventType.Press, result[2].Type);

        Assert.Equal((ushort)0xA4, result[3].Vk);
        Assert.Equal("Alt", result[3].Name);
        Assert.Equal(KeyEventType.Up, result[3].Type);

        Assert.Equal((ushort)0xA2, result[4].Vk);
        Assert.Equal("Ctrl", result[4].Name);
        Assert.Equal(KeyEventType.Up, result[4].Type);
    }

    /// <summary>
    /// Verifies formatting of the key sequence list back to string.
    /// </summary>
    [Fact]
    public void KeysString_Getter_ShouldFormatCorrectly()
    {
        var mapping = new GestureMapping
        {
            Keys = new List<KeyStroke>
            {
                new() { Vk = 0x11, Name = "Ctrl" },
                new() { Vk = 0x43, Name = "C" }
            }
        };

        Assert.Equal("<Ctrl>+C", mapping.KeysString);

        mapping.Keys = new List<KeyStroke>
        {
            new() { Vk = 0x4B, Name = "K" }
        };

        Assert.Equal("K", mapping.KeysString);
    }

    /// <summary>
    /// Verifies that explicit Down and Up suffixes like '<Shift Down>+End+<Shift Up>' are parsed correctly.
    /// </summary>
    [Fact]
    public void ParseKeysString_ShouldParseExplicitUpDownSequence()
    {
        string input = "<Shift Down>+End+<Shift Up>";
        List<KeyStroke> result = GestureMapping.ParseKeysString(input);

        Assert.Equal(3, result.Count);

        Assert.Equal((ushort)0xA0, result[0].Vk);
        Assert.Equal("Shift", result[0].Name);
        Assert.Equal(KeyEventType.Down, result[0].Type);

        Assert.Equal((ushort)0x23, result[1].Vk); // End is 0x23 (VK_END)
        Assert.Equal("End", result[1].Name);
        Assert.Equal(KeyEventType.Press, result[1].Type);

        Assert.Equal((ushort)0xA0, result[2].Vk);
        Assert.Equal("Shift", result[2].Name);
        Assert.Equal(KeyEventType.Up, result[2].Type);
    }

    /// <summary>
    /// Verifies that CollapseEvents reduces a Down followed by an Up event of the same key into a Press event,
    /// while keeping unaligned modifier Hold events unchanged.
    /// </summary>
    [Fact]
    public void CollapseEvents_ShouldCollapseModifierHoldWithKeyPress()
    {
        var rawEvents = new List<KeyStroke>
        {
            new() { Vk = 0xA0, Name = "Shift", Type = KeyEventType.Down },
            new() { Vk = 0x23, Name = "End", Type = KeyEventType.Down },
            new() { Vk = 0x23, Name = "End", Type = KeyEventType.Up },
            new() { Vk = 0xA0, Name = "Shift", Type = KeyEventType.Up }
        };

        List<KeyStroke> collapsed = GestureMapping.CollapseEvents(rawEvents);

        Assert.Equal(3, collapsed.Count);

        Assert.Equal("Shift", collapsed[0].Name);
        Assert.Equal(KeyEventType.Down, collapsed[0].Type);

        Assert.Equal("End", collapsed[1].Name);
        Assert.Equal(KeyEventType.Press, collapsed[1].Type);

        Assert.Equal("Shift", collapsed[2].Name);
        Assert.Equal(KeyEventType.Up, collapsed[2].Type);
    }
}
