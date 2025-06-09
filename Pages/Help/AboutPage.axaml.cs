using Avalonia.Controls;
using System.Diagnostics;

namespace GTDCompanion.Pages
{
    public partial class AboutPage : UserControl
    {
        public AboutPage()
        {
            InitializeComponent();

            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrWhiteSpace(exePath))
            {
                var version = FileVersionInfo.GetVersionInfo(exePath).FileVersion;
                VersionText.Text = $"Versão {version}";
            }
        }
    }
}
