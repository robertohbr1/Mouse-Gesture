using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace MouseKeyb;

/// <summary>
/// A temporary low-level global keyboard hook to consume key presses during recording.
/// </summary>
public class KeyboardRecordHook
{
    private const int WH_KEYBOARD_LL = 13;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    private LowLevelKeyboardProc? _proc;
    private IntPtr _hookId = IntPtr.Zero;

    /// <summary>
    /// Event fired when a key event occurs. Parameters: msg, vkCode.
    /// </summary>
    public Action<int, int>? KeyCallback { get; set; }

    /// <summary>
    /// Starts the global keyboard hook.
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
        _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, hMod, 0);
    }

    /// <summary>
    /// Stops the global keyboard hook.
    /// </summary>
    public void Stop()
    {
        if (_hookId == IntPtr.Zero)
        {
            return;
        }
        UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;
        _proc = null;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = (int)wParam;
            int vkCode = Marshal.ReadInt32(lParam);
            KeyCallback?.Invoke(msg, vkCode);
            return (IntPtr)1; // Consume key, prevent further processing by Windows
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }
}
