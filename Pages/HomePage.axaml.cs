using Avalonia.Controls;
using System.Diagnostics;

namespace GTDCompanion.Pages
{
    public partial class HomePage : UserControl
    {
        public HomePage()
        {
            InitializeComponent();

            // Pegando a versão FileVersion de forma segura
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrWhiteSpace(exePath))
            {
                var version = FileVersionInfo.GetVersionInfo(exePath).FileVersion;
                VersionText.Text = $"Versão {version}";
            }
        }
    }
}
