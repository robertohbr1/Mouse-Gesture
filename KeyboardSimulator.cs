using System;
using System.Runtime.InteropServices;

namespace MouseKeyb;

/// <summary>
/// Native structures and methods for Windows input simulation.
/// </summary>
internal static class NativeInput
{
    public const uint INPUT_KEYBOARD = 1;
    public const uint KEYEVENTF_KEYUP = 0x0002;

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion
    {
        [FieldOffset(0)]
        public KEYBDINPUT ki;
        [FieldOffset(0)]
        public MOUSEINPUT mi;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    public static extern uint MapVirtualKey(uint uCode, uint uMapType);
}

/// <summary>
/// Simulates global keyboard inputs in Windows using SendInput API.
/// </summary>
public class KeyboardSimulator
{
    /// <summary>
    /// Simulates a keyboard sequence by pressing all keys in order, then releasing them in reverse order.
    /// Usage example: simulator.SimulateKeys(new ushort[] { 0x11, 0x57 }); // Simulates Ctrl + W
    /// </summary>
    public void SimulateKeys(ushort[] virtualKeys)
    {
        if (virtualKeys.Length == 0)
        {
            return;
        }

        var inputs = CreateInputSequence(virtualKeys);
        SendNativeInputs(inputs);
    }

    private NativeInput.INPUT[] CreateInputSequence(ushort[] virtualKeys)
    {
        int count = virtualKeys.Length;
        var inputs = new NativeInput.INPUT[count * 2];
        PopulateKeyDowns(inputs, virtualKeys);
        PopulateKeyUps(inputs, virtualKeys);
        return inputs;
    }

    private void PopulateKeyDowns(NativeInput.INPUT[] inputs, ushort[] virtualKeys)
    {
        for (int i = 0; i < virtualKeys.Length; i++)
        {
            inputs[i] = CreateInput(virtualKeys[i], 0);
        }
    }

    private void PopulateKeyUps(NativeInput.INPUT[] inputs, ushort[] virtualKeys)
    {
        int count = virtualKeys.Length;
        for (int i = 0; i < count; i++)
        {
            // Release keys in reverse order to prevent system keyboard state issues.
            ushort keyToRelease = virtualKeys[count - 1 - i];
            inputs[count + i] = CreateInput(keyToRelease, NativeInput.KEYEVENTF_KEYUP);
        }
    }

    private NativeInput.INPUT CreateInput(ushort virtualKey, uint flags)
    {
        ushort scanCode = (ushort)NativeInput.MapVirtualKey(virtualKey, 0);
        return new NativeInput.INPUT
        {
            type = NativeInput.INPUT_KEYBOARD,
            U = new NativeInput.InputUnion
            {
                ki = new NativeInput.KEYBDINPUT
                {
                    wVk = virtualKey,
                    wScan = scanCode,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = UIntPtr.Zero
                }
            }
        };
    }

    private void SendNativeInputs(NativeInput.INPUT[] inputs)
    {
        int size = Marshal.SizeOf(typeof(NativeInput.INPUT));
        uint sent = NativeInput.SendInput((uint)inputs.Length, inputs, size);
        if (sent != inputs.Length)
        {
            throw new InvalidOperationException($"Input simulation failed. Expected {inputs.Length} inputs, sent {sent}.");
        }
    }
}
