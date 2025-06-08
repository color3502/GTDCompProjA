using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
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
                using var client = new HttpClient();
                var tempFile = Path.Combine(Path.GetTempPath(), Path.GetFileName(DownloadUrl));
                using var resp = await client.GetAsync(DownloadUrl);
                resp.EnsureSuccessStatusCode();
                await using (var fs = File.Create(tempFile))
                {
                    await resp.Content.CopyToAsync(fs);
                }
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
            }
        }
    }
}
