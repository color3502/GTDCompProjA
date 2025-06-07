using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GTDCompanion.Pages;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Linq;

namespace GTDCompanion
{
    public partial class App : Application
    {
        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnMainWindowClose;
                desktop.MainWindow = new MainWindow();
                desktop.MainWindow.Closed += (_, __) =>
                {
                    foreach (var w in desktop.Windows.ToArray())
                    {
                        if (w != desktop.MainWindow)
                            w.Close();
                    }
                };
                desktop.MainWindow.Show();
            }

            base.OnFrameworkInitializationCompleted();
        }

        private async Task<bool> CheckLicenseAsync(string gtdId)
        {
            try
            {
                using var http = new HttpClient();
                var payload = JsonSerializer.Serialize(new { app_licence = gtdId });
                var resp = await http.PostAsync(
                    "https://gametrydivision.com/api/gtd/gtdcompanion/check",
                    new StringContent(payload, Encoding.UTF8, "application/json")
                );
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}