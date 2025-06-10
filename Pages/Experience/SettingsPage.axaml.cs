using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using GTDCompanion.Helpers;

namespace GTDCompanion.Pages
{
    public partial class SettingsPage : UserControl
    {
        public SettingsPage()
        {
            InitializeComponent();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                StartMinimizedBox.IsChecked = IsStartupEnabled();
            }
            else
            {
                StartMinimizedBox.IsChecked = false;
                StartMinimizedBox.IsEnabled = false;
            }

            StartMinimizedBox.IsCheckedChanged += OnStartMinimizedChanged;

            string lang = GTDConfigHelper.GetString("General", "language", "pt-BR");
            foreach (var item in LangCombo.Items!)
            {
                if (item is ComboBoxItem cbi && (cbi.Tag as string) == lang)
                    LangCombo.SelectedItem = item;
            }
            LangCombo.SelectionChanged += LangCombo_SelectionChanged;
        }

        private void OnStartMinimizedChanged(object? sender, RoutedEventArgs e)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            var isChecked = StartMinimizedBox.IsChecked ?? false;
            using var key = Registry.CurrentUser.OpenSubKey(
                "Software\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true);
            if (key == null)
                return;

            if (isChecked)
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                key.SetValue("GTDCompanion", $"\"{exePath}\" minimized");
            }
            else
            {
                key.DeleteValue("GTDCompanion", false);
            }
        }

        private static bool IsStartupEnabled()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return false;

            using var key = Registry.CurrentUser.OpenSubKey(
                "Software\\Microsoft\\Windows\\CurrentVersion\\Run", writable: false);
            if (key == null)
                return false;
            var value = key.GetValue("GTDCompanion") as string;
            return !string.IsNullOrWhiteSpace(value);
        }

        private void LangCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (LangCombo.SelectedItem is ComboBoxItem cbi && cbi.Tag is string lang)
            {
                GTDConfigHelper.Set("General", "language", lang);
                LocalizationManager.SetCulture(lang);
            }
        }
    }
}
