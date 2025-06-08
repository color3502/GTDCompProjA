using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using GTDCompanion.Pages;
using GTDCompanion.Helpers;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Linq;
using Avalonia.Platform;
using dotenv.net;




namespace GTDCompanion
{
    public partial class App : Application
    {
        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;
                desktop.MainWindow = new MainWindow();
                desktop.MainWindow.Closed += (_, __) =>
                {
                    foreach (var w in desktop.Windows.ToArray())
                    {
                        if (w != desktop.MainWindow)
                            w.Close();
                    }
                };

                StatsTracker.Load();
                StatsTracker.Start();

                DotEnv.Load();



                var tray = new TrayIcon
                {
                    Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://GTDCompanion/Assets/icon.ico"))),
                    ToolTipText = "GTD Companion"
                };
                var menu = new NativeMenu();
                var discordItem = new NativeMenuItem("Acesse o Discord");
                var urlDiscord = Environment.GetEnvironmentVariable("URL_DISCORD")  ?? "";
                discordItem.Click += (_, __) =>
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = urlDiscord,
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                };

                var exitItem = new NativeMenuItem("Encerrar");
                exitItem.Click += (_, __) =>
                {
                    StatsTracker.Stop();
                    desktop.Shutdown();
                };
                menu.Items.Add(discordItem);
                menu.Items.Add(exitItem);
                tray.Menu = menu;
                tray.Clicked += (_, __) =>
                {
                    desktop.MainWindow?.Show();
                    if (desktop.MainWindow != null)
                        desktop.MainWindow.WindowState = WindowState.Normal;
                };
                tray.IsVisible = true;

                desktop.MainWindow.Show();
            }

            base.OnFrameworkInitializationCompleted();
        }      
    }
}