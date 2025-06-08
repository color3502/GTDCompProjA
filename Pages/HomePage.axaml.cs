using Avalonia.Controls;
using System.Diagnostics;
using Avalonia.Interactivity;
using GTDCompanion.Helpers;
using System.Threading.Tasks;

namespace GTDCompanion.Pages
{
    public partial class HomePage : UserControl
    {
        private string? _updateUrl;
        private string? _downloadedFile;

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

        public void ShowOptionalUpdate(string version, string url)
        {
            _updateUrl = url;
            UpdateText.Text = $"Nova versão {version} disponível";
            UpdatePanel.IsVisible = true;
        }

        private async void UpdateButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_downloadedFile == null)
                await DownloadUpdate();
            else
                InstallUpdate();
        }

        private async Task DownloadUpdate()
        {
            if (string.IsNullOrWhiteSpace(_updateUrl))
                return;
            try
            {
                UpdateProgress.IsVisible = true;
                UpdateButton.IsEnabled = false;
                var progress = new Progress<double>(p => UpdateProgress.Text = $"{p:0}%");
                _downloadedFile = await UpdateDownloader.DownloadAsync(_updateUrl, progress);
                UpdateButton.Content = "Instalar Atualização";
                UpdateButton.IsEnabled = true;
            }
            catch
            {
                UpdateButton.IsEnabled = true;
                UpdateProgress.IsVisible = false;
            }
        }

        private void InstallUpdate()
        {
            if (string.IsNullOrWhiteSpace(_downloadedFile))
                return;
            var psi = new ProcessStartInfo { FileName = _downloadedFile, UseShellExecute = true };
            Process.Start(psi);
            Environment.Exit(0);
        }
    }
}
