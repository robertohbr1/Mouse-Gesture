using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MouseKeyb;

/// <summary>
/// Represents a 2D coordinate point using integers.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct POINT
{
    public int x;
    public int y;
}

/// <summary>
/// Installs and manages a low-level global mouse hook in Windows.
/// </summary>
public class MouseHook
{
    private const int WH_MOUSE_LL = 14;
    private const int WM_MOUSEMOVE = 0x0200;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_RBUTTONUP = 0x0205;

    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const double DragThreshold = 15.0;

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    private LowLevelMouseProc? _proc;
    private IntPtr _hookId = IntPtr.Zero;
    private bool _isTracking;
    private bool _isGestureActive;
    private bool _isSimulatingRightClick;
    private bool _isCtrlRightClickActive;
    private POINT _startPoint;
    private readonly List<POINT> _points = new();

    /// <summary>
    /// Function to check if control key is pressed. Overridable for testing.
    /// </summary>
    public Func<bool> IsCtrlKeyPressed { get; set; } = DefaultIsCtrlKeyPressed;

    /// <summary>
    /// Action to open the circular menu. Overridable for testing.
    /// </summary>
    public Action OpenCircularMenuAction { get; set; } = DefaultOpenCircularMenu;

    public event EventHandler<POINT>? RightButtonDown;
    public event EventHandler<POINT>? GestureMove;
    public event EventHandler<List<POINT>>? GestureComplete;
    public event EventHandler<POINT>? RightButtonUp;

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
    private static extern short GetKeyState(int keyCode);

    /// <summary>
    /// Starts the global mouse hook.
    /// Usage example: mouseHook.Start();
    /// </summary>
    public void Start()
    {
        if (_hookId != IntPtr.Zero)
        {
            return;
        }
        _proc = HookCallback;
        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule;
        var hMod = GetModuleHandle(module?.ModuleName);
        _hookId = SetWindowsHookEx(WH_MOUSE_LL, _proc, hMod, 0);
        if (_hookId == IntPtr.Zero)
        {
            int err = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"SetWindowsHookEx failed with error code: {err}.");
        }
    }

    /// <summary>
    /// Stops the global mouse hook.
    /// Usage example: mouseHook.Stop();
    /// </summary>
    public void Stop()
    {
        if (_hookId == IntPtr.Zero)
        {
            return;
        }
        bool result = UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;
        _proc = null;
        if (!result)
        {
            throw new InvalidOperationException("Failed to uninstall the mouse hook.");
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var msg = (int)wParam;
            var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            if (HandleMouseEvent(msg, hookStruct.pt))
            {
                return (IntPtr)1; // Suppress event
            }
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    internal bool HandleMouseEvent(int message, POINT pt)
    {
        if (_isSimulatingRightClick)
        {
            return false;
        }
        if (message == WM_RBUTTONDOWN)
        {
            return ProcessRightButtonDown(pt);
        }
        if (message == WM_RBUTTONUP)
        {
            return ProcessRightButtonUp(pt);
        }
        if (message == WM_MOUSEMOVE && _isTracking)
        {
            return HandleMouseMove(pt);
        }
        return false;
    }

    private bool ProcessRightButtonDown(POINT pt)
    {
        if (IsCtrlKeyPressed())
        {
            _isCtrlRightClickActive = true;
            OpenCircularMenuAction();
            return true;
        }
        return HandleRightButtonDown(pt);
    }

    private bool ProcessRightButtonUp(POINT pt)
    {
        if (_isCtrlRightClickActive)
        {
            _isCtrlRightClickActive = false;
            return true;
        }
        if (_isTracking)
        {
            return HandleRightButtonUp(pt);
        }
        return false;
    }

    private bool HandleRightButtonDown(POINT pt)
    {
        _isTracking = true;
        _isGestureActive = false;
        _startPoint = pt;
        _points.Clear();
        _points.Add(pt);
        RightButtonDown?.Invoke(this, pt);
        return true; // Intercept right click down to track movement
    }

    private bool HandleMouseMove(POINT pt)
    {
        if (_points.Count > 0 && GetDistance(pt, _points[^1]) < 5.0)
        {
            return false;
        }
        _points.Add(pt);
        if (!_isGestureActive && GetDistance(pt, _startPoint) > DragThreshold)
        {
            _isGestureActive = true;
        }
        if (_isGestureActive)
        {
            GestureMove?.Invoke(this, pt);
        }
        return false; // DO NOT intercept mouse movement, let the cursor move freely!
    }

    private bool HandleRightButtonUp(POINT pt)
    {
        _isTracking = false;
        RightButtonUp?.Invoke(this, pt);
        if (_isGestureActive)
        {
            GestureComplete?.Invoke(this, new List<POINT>(_points));
            return true; // Intercept release since gesture completed
        }
        TriggerSyntheticRightClick();
        return true;
    }

    private void TriggerSyntheticRightClick()
    {
        _isSimulatingRightClick = true;
        mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
        mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
        _isSimulatingRightClick = false;
    }

    private double GetDistance(POINT p1, POINT p2)
    {
        return Math.Sqrt(Math.Pow(p1.x - p2.x, 2) + Math.Pow(p1.y - p2.y, 2));
    }

    private static bool DefaultIsCtrlKeyPressed()
    {
        return (GetKeyState(0x11) & 0x8000) != 0;
    }

    private static void DefaultOpenCircularMenu()
    {
        // Default implementation does nothing
        return;
    }
}
