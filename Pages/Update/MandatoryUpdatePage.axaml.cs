using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using GTDCompanion.Helpers;
using System.Threading.Tasks;

namespace GTDCompanion.Pages
{
    public partial class MandatoryUpdatePage : UserControl
    {
        public string DownloadUrl { get; set; } = string.Empty;
        private string? _downloadedFile;

        public MandatoryUpdatePage()
        {
            InitializeComponent();
        }

        private async void OnUpdateClick(object? sender, RoutedEventArgs e)
        {
            if (_downloadedFile == null)
                await DownloadUpdate();
            else
                InstallUpdate();
        }

        private async Task DownloadUpdate()
        {
            if (string.IsNullOrWhiteSpace(DownloadUrl))
                return;
            try
            {
                ProgressText.IsVisible = true;
                UpdateButton.IsEnabled = false;
                var progress = new Progress<double>(p => ProgressText.Text = $"{p:0}%");
                _downloadedFile = await UpdateDownloader.DownloadAsync(DownloadUrl, progress);
                UpdateButton.Content = "Instalar Atualização";
                UpdateButton.IsEnabled = true;
            }
            catch
            {
                UpdateButton.IsEnabled = true;
                ProgressText.IsVisible = false;
            }
        }

        private void InstallUpdate()
        {
            if (string.IsNullOrWhiteSpace(_downloadedFile))
                return;
            var psi = new ProcessStartInfo
            {
                FileName = _downloadedFile,
                UseShellExecute = true
            };
            Process.Start(psi);
            Environment.Exit(0);
        }
    }
}
