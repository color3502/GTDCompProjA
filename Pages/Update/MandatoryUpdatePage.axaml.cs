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

        public MandatoryUpdatePage()
        {
            InitializeComponent();
        }

        private async void OnUpdateClick(object? sender, RoutedEventArgs e)
        {
            await DownloadAndRun();
        }

        public async Task DownloadAndRun()
        {
            if (string.IsNullOrWhiteSpace(DownloadUrl))
                return;
            try
            {
                ProgressText.IsVisible = true;
                UpdateButton.IsEnabled = false;
                var progress = new Progress<double>(p => ProgressText.Text = $"{p:0}%");
                var tempFile = await UpdateDownloader.DownloadAsync(DownloadUrl, progress);
                var psi = new ProcessStartInfo
                {
                    FileName = tempFile,
                    UseShellExecute = true
                };
                Process.Start(psi);
                Environment.Exit(0);
            }
            catch
            {
                // ignore
                UpdateButton.IsEnabled = true;
                ProgressText.IsVisible = false;
            }
        }
    }
}
