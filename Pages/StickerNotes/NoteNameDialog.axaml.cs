using Avalonia.Controls;
using Avalonia.Interactivity;

namespace GTDCompanion.Pages
{
    public partial class NoteNameDialog : Window
    {
        public string? ResultName { get; private set; }
        public bool IsCleared { get; private set; }

        public NoteNameDialog(string currentName)
        {
            InitializeComponent();
            NameTextBox.Text = currentName;
            OkButton.Click += OkButton_Click;
            CancelButton.Click += (_, __) => Close();
            ClearButton.Click += ClearButton_Click;
        }

        private void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            ResultName = NameTextBox.Text;
            Close();
        }

        private void ClearButton_Click(object? sender, RoutedEventArgs e)
        {
            IsCleared = true;
            Close();
        }
    }
}
