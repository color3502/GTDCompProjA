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
        private static KeyboardMouseStats LoadStats()
        {
            try
            {
                if (File.Exists(StatsPath))
                {
                    var json = File.ReadAllText(StatsPath);
                    var loaded = JsonSerializer.Deserialize<KeyboardMouseStats>(json);
                    if (loaded != null)
                        return loaded;
                }
            }
            catch { }
            return new KeyboardMouseStats();
        }

        private static void SaveStats(KeyboardMouseStats stats)
        {
            try
            {
                var dir = Path.GetDirectoryName(StatsPath)!;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                var json = JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(StatsPath, json);
            }
            catch { }
        }

        public static KeyboardMouseStats Stats => LoadStats();

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

        private static TimeSpan _sessionActive = TimeSpan.Zero;
        private static TimeSpan _sessionIdle = TimeSpan.Zero;

        public static event Action? StatsUpdated;

        public static void Load()
        {
            // No caching - stats are read directly from the JSON file when needed
        }

        private static void IncrementDailyClicks(KeyboardMouseStats stats)
        {
            var key = DateTime.Now.ToString("yyyy-MM-dd");
            if (!stats.DailyClicks.ContainsKey(key))
                stats.DailyClicks[key] = 0;
            stats.DailyClicks[key]++;
        }

        private static void IncrementDailyKeyPresses(KeyboardMouseStats stats)
        {
            var key = DateTime.Now.ToString("yyyy-MM-dd");
            if (!stats.DailyKeyPresses.ContainsKey(key))
                stats.DailyKeyPresses[key] = 0;
            stats.DailyKeyPresses[key]++;
        }

        public static void Start()
        {
            if (!OperatingSystem.IsWindows())
                return;
            if (_keyboardHook != IntPtr.Zero)
                return;
            StartTime = DateTime.Now;
            _sessionActive = TimeSpan.Zero;
            _sessionIdle = TimeSpan.Zero;
            _lastMouseMove = DateTime.Now;
            _keyboardProc = KeyboardHookCallback;
            _mouseProc = MouseHookCallback;
            _keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, GetModuleHandle(null), 0);
            _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, GetModuleHandle(null), 0);
            _timer = new System.Timers.Timer(60000);
            _timer.Elapsed += (_, __) => {
                var s = LoadStats();
                if (DateTime.Now - _lastMouseMove >= TimeSpan.FromMinutes(5))
                {
                    s.IdleTime += TimeSpan.FromMinutes(1);
                    _sessionIdle += TimeSpan.FromMinutes(1);
                }
                else
                {
                    s.ActiveTime += TimeSpan.FromMinutes(1);
                    _sessionActive += TimeSpan.FromMinutes(1);
                }
                SaveStats(s);
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
            var s = LoadStats();
            s.LastMaintenance = DateTime.Now;
            SaveStats(s);
            StatsUpdated?.Invoke();
        }

        public static TimeSpan GetTotalActiveTime()
        {
            var s = LoadStats();
            return s.ActiveTime + GetPartialActiveTime();
        }

        private static TimeSpan GetPartialActiveTime()
        {
            TimeSpan sessionDuration = DateTime.Now - StartTime;
            TimeSpan recorded = _sessionActive + _sessionIdle;
            TimeSpan partial = sessionDuration - recorded;
            if (partial < TimeSpan.Zero)
                partial = TimeSpan.Zero;
            if (DateTime.Now - _lastMouseMove >= TimeSpan.FromMinutes(5))
                return TimeSpan.Zero;
            return partial;
        }

        private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            const int WM_KEYUP = 0x0101;
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYUP)
            {
                var s = LoadStats();
                var info = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                s.KeyPresses++;
                if (!s.KeyCounts.ContainsKey(info.vkCode))
                    s.KeyCounts[info.vkCode] = 0;
                s.KeyCounts[info.vkCode]++;
                IncrementDailyKeyPresses(s);
                SaveStats(s);
                StatsUpdated?.Invoke();
            }
            return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
        }

        private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var s = LoadStats();
                switch ((int)wParam)
                {
                    case WM_LBUTTONDOWN:
                        s.LeftClicks++;
                        IncrementDailyClicks(s);
                        _lastMouseMove = DateTime.Now;
                        break;
                    case WM_RBUTTONDOWN:
                        s.RightClicks++;
                        IncrementDailyClicks(s);
                        _lastMouseMove = DateTime.Now;
                        break;
                    case WM_MOUSEWHEEL:
                        var m = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                        int delta = (short)((m.mouseData >> 16) & 0xffff);
                        s.ScrollTicks += Math.Abs(delta) / 120;
                        _lastMouseMove = DateTime.Now;
                        break;
                    case WM_MOUSEMOVE:
                        var mm = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                        if (_hasLastPoint)
                        {
                            int dx = mm.pt.x - _lastPoint.x;
                            int dy = mm.pt.y - _lastPoint.y;
                            double dist = Math.Sqrt(dx * dx + dy * dy);
                            s.MousePixelsMoved += dist;
                        }
                        _lastPoint = mm.pt;
                        _hasLastPoint = true;
                        _lastMouseMove = DateTime.Now;
                        break;
                }
                SaveStats(s);
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
