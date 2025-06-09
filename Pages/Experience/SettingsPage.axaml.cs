using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;

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

            StartMinimizedBox.Checked += OnStartMinimizedChanged;
            StartMinimizedBox.Unchecked += OnStartMinimizedChanged;
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
            using var key = Registry.CurrentUser.OpenSubKey(
                "Software\\Microsoft\\Windows\\CurrentVersion\\Run", writable: false);
            if (key == null)
                return false;
            var value = key.GetValue("GTDCompanion") as string;
            return !string.IsNullOrWhiteSpace(value);
        }
    }
}
