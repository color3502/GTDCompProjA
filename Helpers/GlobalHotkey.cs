using System;
using System.Runtime.InteropServices;

namespace GTDCompanion.Helpers
{
    public class GlobalHotkey : IDisposable
    {
        private IntPtr _hookID = IntPtr.Zero;
        private LowLevelKeyboardProc? _proc;
        private readonly Action _callback;
        private readonly int _vk;

        public const int VK_F7 = 0x76;
        public const int VK_F8 = 0x77;

        public GlobalHotkey(int virtualKeyCode, Action callback)
        {
            _callback = callback;
            _vk = virtualKeyCode;
        }

        public void Register()
        {
            if (!OperatingSystem.IsWindows())
                return;
            _proc = HookCallback;
            _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(null), 0);
        }

        public void Dispose()
        {
            if (!OperatingSystem.IsWindows())
                return;
            if (_hookID != IntPtr.Zero)
                UnhookWindowsHookEx(_hookID);
            _hookID = IntPtr.Zero;
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            const int WM_KEYDOWN = 0x0100;
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                var info = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                // Aciona a ação quando a tecla configurada é pressionada
                if (info.vkCode == _vk)
                {
                    _callback();
                    return (IntPtr)1; // consume
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private const int WH_KEYBOARD_LL = 13;

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);


        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);
    }
}
