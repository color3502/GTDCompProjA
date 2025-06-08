using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.IO;
using System.Linq;

namespace GTDCompanion.Helpers
{
    public class KeyboardMouseStats
    {
        public int KeyPresses { get; set; }
        public int LeftClicks { get; set; }
        public int RightClicks { get; set; }
        public int ScrollTicks { get; set; }
        public double MousePixelsMoved { get; set; }
        public TimeSpan IdleTime { get; set; }
        public TimeSpan ActiveTime { get; set; }
        public DateTime LastMaintenance { get; set; } = DateTime.Now;
        public Dictionary<int, int> KeyCounts { get; set; } = new();
        public Dictionary<string, int> DailyClicks { get; set; } = new();
        public Dictionary<string, int> DailyKeyPresses { get; set; } = new();
    }

    public static class StatsTracker
    {
        public static KeyboardMouseStats Stats { get; } = new();

        private static readonly string StatsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GTDCompanion", "KeyboardMouseStats.json");

        private static IntPtr _keyboardHook;
        private static IntPtr _mouseHook;
        private static LowLevelProc? _keyboardProc;
        private static LowLevelProc? _mouseProc;
        private static System.Timers.Timer? _timer;
        private static POINT _lastPoint;
        private static bool _hasLastPoint;
        private static DateTime _lastMouseMove = DateTime.Now;
        public static DateTime StartTime { get; private set; } = DateTime.Now;

        public static event Action? StatsUpdated;

        public static void Load()
        {
            try
            {
                if (File.Exists(StatsPath))
                {
                    var json = File.ReadAllText(StatsPath);
                    var loaded = JsonSerializer.Deserialize<KeyboardMouseStats>(json);
                    if (loaded != null)
                    {
                        Stats.KeyPresses = loaded.KeyPresses;
                        Stats.LeftClicks = loaded.LeftClicks;
                        Stats.RightClicks = loaded.RightClicks;
                        Stats.ScrollTicks = loaded.ScrollTicks;
                        Stats.MousePixelsMoved = loaded.MousePixelsMoved;
                        Stats.IdleTime = loaded.IdleTime;
                        Stats.ActiveTime = loaded.ActiveTime;
                        if (loaded.LastMaintenance != DateTime.MinValue)
                            Stats.LastMaintenance = loaded.LastMaintenance;
                        Stats.KeyCounts.Clear();
                        foreach (var kv in loaded.KeyCounts)
                            Stats.KeyCounts[kv.Key] = kv.Value;
                        Stats.DailyClicks.Clear();
                        foreach (var kv in loaded.DailyClicks)
                            Stats.DailyClicks[kv.Key] = kv.Value;
                        Stats.DailyKeyPresses.Clear();
                        if (loaded.DailyKeyPresses != null)
                        {
                            foreach (var kv in loaded.DailyKeyPresses)
                                Stats.DailyKeyPresses[kv.Key] = kv.Value;
                        }
                        return;
                    }
                }
            }
            catch { }

            // Fallback to old config if json not found
            Stats.KeyPresses = GTDConfigHelper.GetInt("Stats", "KeyPresses", 0);
            Stats.LeftClicks = GTDConfigHelper.GetInt("Stats", "LeftClicks", 0);
            Stats.RightClicks = GTDConfigHelper.GetInt("Stats", "RightClicks", 0);
            Stats.ScrollTicks = Math.Abs(GTDConfigHelper.GetInt("Stats", "ScrollTicks", 0));
            var jsonOld = GTDConfigHelper.Get("Stats", "KeyCounts", "{}");
            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<int, int>>(jsonOld);
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
            try
            {
                var dir = Path.GetDirectoryName(StatsPath)!;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                var json = JsonSerializer.Serialize(Stats, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(StatsPath, json);
            }
            catch { }
        }

        private static void IncrementDailyClicks()
        {
            var key = DateTime.Now.ToString("yyyy-MM-dd");
            if (!Stats.DailyClicks.ContainsKey(key))
                Stats.DailyClicks[key] = 0;
            Stats.DailyClicks[key]++;
        }

        private static void IncrementDailyKeyPresses()
        {
            var key = DateTime.Now.ToString("yyyy-MM-dd");
            if (!Stats.DailyKeyPresses.ContainsKey(key))
                Stats.DailyKeyPresses[key] = 0;
            Stats.DailyKeyPresses[key]++;
        }

        public static void Start()
        {
            if (!OperatingSystem.IsWindows())
                return;
            if (_keyboardHook != IntPtr.Zero)
                return;
            StartTime = DateTime.Now;
            _lastMouseMove = DateTime.Now;
            _keyboardProc = KeyboardHookCallback;
            _mouseProc = MouseHookCallback;
            _keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, GetModuleHandle(null), 0);
            _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, GetModuleHandle(null), 0);
            _timer = new System.Timers.Timer(60000);
            _timer.Elapsed += (_, __) => {
                if (DateTime.Now - _lastMouseMove >= TimeSpan.FromMinutes(5))
                    Stats.IdleTime += TimeSpan.FromMinutes(1);
                else
                    Stats.ActiveTime += TimeSpan.FromMinutes(1);
                Save();
                StatsUpdated?.Invoke();
            };
            _timer.AutoReset = true;
            _timer.Start();
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
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }
        }

        public static void ResetMaintenance()
        {
            Stats.LastMaintenance = DateTime.Now;
            Save();
            StatsUpdated?.Invoke();
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
                IncrementDailyKeyPresses();
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
                        IncrementDailyClicks();
                        _lastMouseMove = DateTime.Now;
                        break;
                    case WM_RBUTTONDOWN:
                        Stats.RightClicks++;
                        IncrementDailyClicks();
                        _lastMouseMove = DateTime.Now;
                        break;
                    case WM_MOUSEWHEEL:
                        var m = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                        int delta = (short)((m.mouseData >> 16) & 0xffff);
                        Stats.ScrollTicks += Math.Abs(delta) / 120;
                        _lastMouseMove = DateTime.Now;
                        break;
                    case WM_MOUSEMOVE:
                        var mm = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                        if (_hasLastPoint)
                        {
                            int dx = mm.pt.x - _lastPoint.x;
                            int dy = mm.pt.y - _lastPoint.y;
                            double dist = Math.Sqrt(dx * dx + dy * dy);
                            Stats.MousePixelsMoved += dist;
                        }
                        _lastPoint = mm.pt;
                        _hasLastPoint = true;
                        _lastMouseMove = DateTime.Now;
                        break;
                }
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
        private const int WM_MOUSEMOVE = 0x0200;

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
