using System;
using GTDCompanion.Pages;

namespace GTDCompanion.Helpers
{
    public static class GlobalHotkeyService
    {
        private static GlobalHotkey? _f7;
        private static GlobalHotkey? _f8;

        public static void Register()
        {
            _f7 = new GlobalHotkey(GlobalHotkey.VK_F7, () => MiraPage.ToggleOverlayGlobal());
            _f7.Register();
            _f8 = new GlobalHotkey(GlobalHotkey.VK_F8, () => MacroPage.Instance?.ToggleMacroExecution());
            _f8.Register();
        }

        public static void Unregister()
        {
            _f7?.Dispose();
            _f8?.Dispose();
            _f7 = null;
            _f8 = null;
        }
    }
}
