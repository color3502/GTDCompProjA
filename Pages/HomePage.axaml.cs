using Avalonia.Controls;
using System.Diagnostics;
using System.Reflection;

namespace GTDCompanion.Pages
{
    public partial class HomePage : UserControl
    {
        public HomePage()
        {
            InitializeComponent();

            // Pegando a versão FileVersion
            var version = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()!.Location).FileVersion;
            VersionText.Text = $"Versão {version}";
        }
    }
}
