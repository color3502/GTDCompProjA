using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Globalization;

namespace GTDCompanion.Pages
{
    public partial class SettingsPage : UserControl
    {
        public SettingsPage()
        {
            InitializeComponent();

            LocalizationManager.CultureChanged += ApplyTranslations;
            this.DetachedFromVisualTree += (_, __) => LocalizationManager.CultureChanged -= ApplyTranslations;

            ApplyTranslations();

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

            InitLanguageCombo();
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

        private void InitLanguageCombo()
        {
            LanguageCombo.Items.Clear();
            foreach (var c in LocalizationManager.GetAvailableCultures())
            {
                var ci = CultureInfo.GetCultureInfo(c);
                LanguageCombo.Items.Add(new ComboBoxItem { Content = ci.NativeName, Tag = c });
            }

            string saved = GTDConfigHelper.GetString("General", "Language", string.Empty);
            if (string.IsNullOrWhiteSpace(saved))
                saved = CultureInfo.InstalledUICulture.Name;
            int idx = 0;
            foreach (ComboBoxItem item in LanguageCombo.Items)
            {
                if ((string?)item.Tag == saved)
                {
                    LanguageCombo.SelectedIndex = idx;
                    break;
                }
                idx++;
            }

            LanguageCombo.SelectionChanged += LanguageCombo_SelectionChanged;
        }

        private void LanguageCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (LanguageCombo.SelectedItem is ComboBoxItem item && item.Tag is string culture)
            {
                GTDConfigHelper.Set("General", "Language", culture);
                LocalizationManager.LoadCulture(culture);
            }
        }

        private void ApplyTranslations()
        {
            AppBehaviorLabel.Text = LocalizationManager.Get("settings_app_behavior");
            StartMinimizedBox.Content = LocalizationManager.Get("settings_start_minimized");
            LanguageLabel.Text = LocalizationManager.Get("settings_language");
        }
    }
}
