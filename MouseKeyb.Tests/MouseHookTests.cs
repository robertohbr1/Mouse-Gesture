using System.Collections.Generic;
using Xunit;
using MouseKeyb;

namespace MouseKeyb.Tests;

/// <summary>
/// Unit tests for MouseHook click interception logic, particularly Ctrl+Right Click.
/// </summary>
public class MouseHookTests
{
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_RBUTTONUP = 0x0205;
    private const int WM_MOUSEMOVE = 0x0200;

    /// <summary>
    /// Verifies that clicking right mouse button with Ctrl pressed triggers volume control opening and intercepts the event.
    /// </summary>
    [Fact]
    public void HandleMouseEvent_WhenCtrlIsPressed_ShouldOpenVolumeControlAndSuppressEvent()
    {
        var hook = new MouseHook();
        bool volumeControlOpened = false;

        hook.IsCtrlKeyPressed = () => true;
        hook.OpenVolumeControlAction = () => { volumeControlOpened = true; };

        var point = new POINT { x = 10, y = 20 };
        bool result = hook.HandleMouseEvent(WM_RBUTTONDOWN, point);

        Assert.True(result);
        Assert.True(volumeControlOpened);
    }

    /// <summary>
    /// Verifies that when right mouse button is released after a Ctrl+Right Click, it intercepts the UP event.
    /// </summary>
    [Fact]
    public void HandleMouseEvent_WhenCtrlIsPressed_ShouldSuppressMouseUp()
    {
        var hook = new MouseHook();
        hook.IsCtrlKeyPressed = () => true;
        hook.OpenVolumeControlAction = () => { };

        var point = new POINT { x = 10, y = 20 };
        
        // Down event
        bool downResult = hook.HandleMouseEvent(WM_RBUTTONDOWN, point);
        Assert.True(downResult);

        // Up event
        bool upResult = hook.HandleMouseEvent(WM_RBUTTONUP, point);
        Assert.True(upResult);
    }

    /// <summary>
    /// Verifies that when Ctrl is not pressed, right click down starts normal tracking and does not trigger volume control.
    /// </summary>
    [Fact]
    public void HandleMouseEvent_WhenCtrlIsNotPressed_ShouldFallbackToNormalRightClick()
    {
        var hook = new MouseHook();
        bool volumeControlOpened = false;
        bool rightButtonDownTriggered = false;

        hook.IsCtrlKeyPressed = () => false;
        hook.OpenVolumeControlAction = () => { volumeControlOpened = true; };
        hook.RightButtonDown += (sender, pt) => { rightButtonDownTriggered = true; };

        var point = new POINT { x = 10, y = 20 };
        bool result = hook.HandleMouseEvent(WM_RBUTTONDOWN, point);

        Assert.True(result); // Hook intercepts right down to track gesture
        Assert.False(volumeControlOpened);
        Assert.True(rightButtonDownTriggered);
    }
}
