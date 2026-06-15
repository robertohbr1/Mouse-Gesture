using System;
using System.Collections.Generic;
using System.Text;

namespace MouseKeyb;

/// <summary>
/// Recognizes strokes and patterns from a list of 2D screen coordinates.
/// </summary>
public class GestureRecognizer
{
    /// <summary>
    /// Gets or sets the minimum movement distance in pixels required to register a gesture segment.
    /// </summary>
    public double SegmentThreshold { get; set; } = 40.0;

    /// <summary>
    /// Translates a sequence of coordinates into a gesture pattern string (e.g. "D" or "DR").
    /// Usage example: var pattern = recognizer.Recognize(pointsList);
    /// </summary>
    public string Recognize(List<POINT> points)
    {
        if (points.Count < 2)
        {
            return string.Empty;
        }

        return ProcessPoints(points);
    }

    private string ProcessPoints(List<POINT> points)
    {
        var pattern = new StringBuilder();
        var anchor = points[0];
        char lastDir = '\0';

        foreach (var pt in points)
        {
            char dir = CheckSegment(pt, ref anchor, lastDir);
            if (dir != '\0')
            {
                pattern.Append(dir);
                lastDir = dir;
            }
        }

        return pattern.ToString();
    }

    private char CheckSegment(POINT current, ref POINT anchor, char lastDir)
    {
        double dx = current.x - anchor.x;
        double dy = current.y - anchor.y;
        double dist = Math.Sqrt(dx * dx + dy * dy);
        if (dist < SegmentThreshold)
        {
            return '\0';
        }

        char dir = ClassifyDirection(dx, dy);
        anchor = current;

        if (dir == lastDir)
        {
            return '\0';
        }

        return dir;
    }

    private char ClassifyDirection(double dx, double dy)
    {
        double angle = Math.Atan2(dy, dx) * 180 / Math.PI;
        if (angle >= -45 && angle < 45)
        {
            return 'R';
        }

        if (angle >= 45 && angle < 135)
        {
            return 'D';
        }

        if (angle >= -135 && angle < -45)
        {
            return 'U';
        }

        return 'L';
    }
}
