using Avalonia.Controls;
using Avalonia.Interactivity;

namespace GTDCompanion.Pages
{
    public partial class SettingsPage : UserControl
    {
        public SettingsPage()
        {
            InitializeComponent();
            StartMinimizedBox.IsChecked = GTDConfigHelper.GetBool("Behavior", "StartMinimized", true);
            StartMinimizedBox.Checked += OnStartMinimizedChanged;
            StartMinimizedBox.Unchecked += OnStartMinimizedChanged;
        }

        private void OnStartMinimizedChanged(object? sender, RoutedEventArgs e)
        {
            var isChecked = StartMinimizedBox.IsChecked ?? false;
            GTDConfigHelper.Set("Behavior", "StartMinimized", isChecked ? "true" : "false");
        }
    }
}
