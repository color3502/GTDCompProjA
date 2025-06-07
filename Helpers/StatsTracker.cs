using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace GTDCompanion.Helpers
{
    public class KeyboardMouseStats
    {
        public int KeyPresses { get; set; }
        public int LeftClicks { get; set; }
        public int RightClicks { get; set; }
        public int ScrollTicks { get; set; }
        public Dictionary<int, int> KeyCounts { get; } = new();
    }

    public static class StatsTracker
    {
        public static KeyboardMouseStats Stats { get; } = new();

        private static IntPtr _keyboardHook;
        private static IntPtr _mouseHook;
        private static LowLevelProc? _keyboardProc;
        private static LowLevelProc? _mouseProc;

        public static event Action? StatsUpdated;

        public static void Load()
        {
            Stats.KeyPresses = GTDConfigHelper.GetInt("Stats", "KeyPresses", 0);
            Stats.LeftClicks = GTDConfigHelper.GetInt("Stats", "LeftClicks", 0);
            Stats.RightClicks = GTDConfigHelper.GetInt("Stats", "RightClicks", 0);
            Stats.ScrollTicks = Math.Abs(GTDConfigHelper.GetInt("Stats", "ScrollTicks", 0));
            var json = GTDConfigHelper.Get("Stats", "KeyCounts", "{}");
            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<int, int>>(json);
                if (dict != null)
                {
                    foreach (var kv in dict)
                        Stats.KeyCounts[kv.Key] = kv.Value;
                }
            }
            catch { }
        }

        private static void Save()
        {
            GTDConfigHelper.Set("Stats", "KeyPresses", Stats.KeyPresses.ToString());
            GTDConfigHelper.Set("Stats", "LeftClicks", Stats.LeftClicks.ToString());
            GTDConfigHelper.Set("Stats", "RightClicks", Stats.RightClicks.ToString());
            GTDConfigHelper.Set("Stats", "ScrollTicks", Stats.ScrollTicks.ToString());
            var json = JsonSerializer.Serialize(Stats.KeyCounts);
            GTDConfigHelper.Set("Stats", "KeyCounts", json);
        }

        public static void Start()
        {
            if (!OperatingSystem.IsWindows())
                return;
            if (_keyboardHook != IntPtr.Zero)
                return;
            _keyboardProc = KeyboardHookCallback;
            _mouseProc = MouseHookCallback;
            _keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, GetModuleHandle(null), 0);
            _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, GetModuleHandle(null), 0);
        }

        public static void Stop()
        {
            if (!OperatingSystem.IsWindows())
                return;
            if (_keyboardHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_keyboardHook);
                _keyboardHook = IntPtr.Zero;
            }
            if (_mouseHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_mouseHook);
                _mouseHook = IntPtr.Zero;
            }
        }

        private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            const int WM_KEYDOWN = 0x0100;
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                var info = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                Stats.KeyPresses++;
                if (!Stats.KeyCounts.ContainsKey(info.vkCode))
                    Stats.KeyCounts[info.vkCode] = 0;
                Stats.KeyCounts[info.vkCode]++;
                Save();
                StatsUpdated?.Invoke();
            }
            return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
        }

        private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                switch ((int)wParam)
                {
                    case WM_LBUTTONDOWN:
                        Stats.LeftClicks++;
                        break;
                    case WM_RBUTTONDOWN:
                        Stats.RightClicks++;
                        break;
                    case WM_MOUSEWHEEL:
                        var m = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                        int delta = (short)((m.mouseData >> 16) & 0xffff);
                        Stats.ScrollTicks += Math.Abs(delta) / 120;
                        break;
                }
                Save();
                StatsUpdated?.Invoke();
            }
            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;

        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_MOUSEWHEEL = 0x020A;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x; public int y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public int mouseData;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint threadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);
    }
}
