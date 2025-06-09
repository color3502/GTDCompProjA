using Avalonia.Controls;
using Avalonia.Interactivity;

namespace GTDCompanion.Pages
{
    public partial class ExperiencePage : UserControl
    {
        public ExperiencePage()
        {
            InitializeComponent();
            StartMinimizedBox.IsChecked = GTDConfigHelper.GetBool("Experience", "StartMinimized", true);
            StartMinimizedBox.Checked += OnStartMinimizedChanged;
            StartMinimizedBox.Unchecked += OnStartMinimizedChanged;
        }

        private void OnStartMinimizedChanged(object? sender, RoutedEventArgs e)
        {
            var isChecked = StartMinimizedBox.IsChecked ?? false;
            GTDConfigHelper.Set("Experience", "StartMinimized", isChecked ? "true" : "false");
        }
    }
}
