using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using GTDCompanion.Pages;
using System.Text.Json;
using System.Net.Http;
using System.IO;
using Avalonia.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System;
using GTDCompanion.Helpers;

namespace GTDCompanion
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer? _updateTimer;
        private HomePage? _homePage;

        public MainWindow()
        {
            InitializeComponent();

            // Custom barra de título: eventos de arrastar, minimizar, fechar
            var customTitleBar = this.FindControl<Border>("CustomTitleBar");
            var closeBtn = this.FindControl<Button>("CloseButton");
            var minBtn = this.FindControl<Button>("MinimizeButton");

            AppConfig.PopulateEnvironment();

            
            if (customTitleBar is not null)
            {
                customTitleBar.PointerPressed += CustomTitleBar_PointerPressed;
            }
            if (closeBtn is not null)
            {
                closeBtn.Click += (_, _) => this.Hide();
            }
            if (minBtn is not null)
            {
                minBtn.Click += (_, _) => this.WindowState = WindowState.Minimized;
            }

            // Defer until UI is fully loaded
            this.Opened += async (_, _) => await InitWithLicenseCheck();
            this.Closing += (_, e) => { e.Cancel = true; this.Hide(); };
        }

        // Arrastar janela pela barra custom
        private void CustomTitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                BeginMoveDrag(e);
            }
        }

        private async Task InitWithLicenseCheck()
        {
            MainMenuBox.IsVisible = false;
            var gtdId = GTDConfigHelper.LoadGtdId();

            // Se não houver ID, exibe tela de licença
            if (string.IsNullOrWhiteSpace(gtdId))
            {
                var licensePage = new LicensePage();
                licensePage.LicenseValidated += ShowMainContent;
                MainContent.Content = licensePage;
                return;
            }

            // Se houver ID, tenta validar
            var tempPage = new LicensePage();
            bool valid = await tempPage.VerifyLicence(gtdId);
            if (valid)
            {
                ShowMainContent();
            }
            else
            {
                var licensePage = new LicensePage();
                licensePage.LicenseValidated += ShowMainContent;
                MainContent.Content = licensePage;
            }
        }

        private void ShowMainContent()
        {
            MainMenuBox.IsVisible = true;
            _homePage = new HomePage();
            MainContent.Content = _homePage;
            StartUpdateTimer();
        }

        private void MenuInicio_Click(object? sender, RoutedEventArgs e)
        {
            if (_homePage == null)
                _homePage = new HomePage();
            MainContent.Content = _homePage;
        }

        private void BenchmarkOverlayPage_Click(object? sender, RoutedEventArgs e)
        {
            MainContent.Content = new BenchmarkOverlayPage();
        }

        private void MenuMira_Click(object? sender, RoutedEventArgs e)
        {
            MainContent.Content = new MiraPage();
        }

        private void CheckMySetup_Page(object? sender, RoutedEventArgs e)
        {
            MainContent.Content = new CheckMySetupPage();
        }

        private void TranslatorOverlay_Click(object? sender, RoutedEventArgs e)
        {
            MainContent.Content = new TranslatorPage();
        }

        private void StickerNotesPage_Click(object? sender, RoutedEventArgs e)
        {
            MainContent.Content = new StickerNotesPage();
        }

        private void MacroPage_Click(object? sender, RoutedEventArgs e)
        {
            MainContent.Content = new MacroPage();
        }

        private void KeyboardMouseStatsPage_Click(object? sender, RoutedEventArgs e)
        {
            MainContent.Content = new KeyboardMouseStatsPage();
        }

        private void MenuSobre_Click(object? sender, RoutedEventArgs e)
        {
            var key = Environment.GetEnvironmentVariable("URL_GTD") ?? "";
            // Abre o link do Discord no navegador padrão
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = key,
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        private void MenuSuporte_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // Abre o link do Discord no navegador padrão
            var key = Environment.GetEnvironmentVariable("URL_DISCORD_SUPPORT_CHANNEL") ?? "";
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = key,
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        private void AcessarDiscord_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // Abre o link do Discord no navegador padrão
            var key = Environment.GetEnvironmentVariable("URL_DISCORD_WELCOME_CHANNEL") ?? "";
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = key,
                UseShellExecute = true
            };
            Process.Start(psi);
        }


        private void StartUpdateTimer()
        {
            _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
            _updateTimer.Tick += async (_, __) => await CheckForUpdates();
            _updateTimer.Start();
            _ = CheckForUpdates();
        }

        private class UpdateInfo
        {
            public string? latest { get; set; }
            public string? url { get; set; }
            public string? changelog { get; set; }
            public bool mandatory { get; set; }
        }

        private async Task CheckForUpdates()
        {
            var apiUrl = Environment.GetEnvironmentVariable("URL_GTD_COMPANION_UPDATE") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(apiUrl))
                return;
            try
            {
                using var http = new HttpClient();
                var json = await http.GetStringAsync(apiUrl);
                var info = JsonSerializer.Deserialize<UpdateInfo>(json);
                if (info == null || string.IsNullOrWhiteSpace(info.latest) || string.IsNullOrWhiteSpace(info.url))
                    return;

                var exe = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrWhiteSpace(exe))
                    return;
                var current = new Version(FileVersionInfo.GetVersionInfo(exe).FileVersion);
                if (Version.TryParse(info.latest, out var latest) && latest > current)
                {
                    if (info.mandatory)
                    {
                        var page = new MandatoryUpdatePage();
                        page.DownloadUrl = info.url;
                        page.ChangelogText.Text = info.changelog ?? string.Empty;
                        MainMenuBox.IsVisible = false;
                        MainContent.Content = page;
                    }
                    else
                    {
                        _homePage?.ShowOptionalUpdate(info.latest, info.url);
                    }
                }
            }
            catch { }
        }
    }
}
