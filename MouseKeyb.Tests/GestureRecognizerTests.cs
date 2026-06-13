using System.Collections.Generic;
using Xunit;
using MouseKeyb;

namespace MouseKeyb.Tests;

/// <summary>
/// Unit tests for the GestureRecognizer class to verify correct translation of coordinates.
/// </summary>
public class GestureRecognizerTests
{
    /// <summary>
    /// Verifies that a straight movement down is recognized as 'D'.
    /// Usage example: runs automatically via dotnet test.
    /// </summary>
    [Fact]
    public void Recognize_ShouldReturnDown_WhenMovingDown()
    {
        var recognizer = new GestureRecognizer { SegmentThreshold = 20.0 };
        var points = new List<POINT>
        {
            new() { x = 0, y = 0 },
            new() { x = 0, y = 10 },
            new() { x = 0, y = 30 }
        };
        var result = recognizer.Recognize(points);
        Assert.Equal("D", result);
    }

    /// <summary>
    /// Verifies that a straight movement right is recognized as 'R'.
    /// Usage example: runs automatically via dotnet test.
    /// </summary>
    [Fact]
    public void Recognize_ShouldReturnRight_WhenMovingRight()
    {
        var recognizer = new GestureRecognizer { SegmentThreshold = 20.0 };
        var points = new List<POINT>
        {
            new() { x = 0, y = 0 },
            new() { x = 10, y = 0 },
            new() { x = 30, y = 0 }
        };
        var result = recognizer.Recognize(points);
        Assert.Equal("R", result);
    }

    /// <summary>
    /// Verifies that a L-shape gesture (Down then Right) is recognized as 'DR'.
    /// Usage example: runs automatically via dotnet test.
    /// </summary>
    [Fact]
    public void Recognize_ShouldReturnDownRight_WhenMovingDownThenRight()
    {
        var recognizer = new GestureRecognizer { SegmentThreshold = 20.0 };
        var points = new List<POINT>
        {
            new() { x = 0, y = 0 },
            new() { x = 0, y = 25 },
            new() { x = 10, y = 25 },
            new() { x = 30, y = 25 }
        };
        var result = recognizer.Recognize(points);
        Assert.Equal("DR", result);
    }

    /// <summary>
    /// Verifies that mouse movement below the threshold returns an empty string.
    /// Usage example: runs automatically via dotnet test.
    /// </summary>
    [Fact]
    public void Recognize_ShouldReturnEmpty_WhenMovementIsBelowThreshold()
    {
        var recognizer = new GestureRecognizer { SegmentThreshold = 20.0 };
        var points = new List<POINT>
        {
            new() { x = 0, y = 0 },
            new() { x = 0, y = 10 }
        };
        var result = recognizer.Recognize(points);
        Assert.Equal(string.Empty, result);
    }
}
