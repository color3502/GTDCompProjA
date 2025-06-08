using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using RTSSSharedMemoryNET;
using GTDCompanion; // ← para usar GTDConfigHelper

namespace GTDCompanion.Pages
{
    public partial class BenchmarkOverlayPage : UserControl
    {
        private static BenchmarkOverlayWindow? overlayWindow;
        private bool rtssAvailable = false;

        private readonly string[] possibleRtssPaths = new[]
        {
            @"C:\Program Files (x86)\RivaTuner Statistics Server\RTSS.exe",
            @"C:\Program Files\RivaTuner Statistics Server\RTSS.exe"
        };

        private readonly string[] rtssProcessNames = new[]
        {
            "RTSS", "EncoderServer", "EncoderServer64", "RTSSHooksLoader", "RTSSHooksLoader64"
        };

        // Helper para evitar duplo-save
        private bool loadingSettings = false;

        private void UpdateToggleButton()
        {
            ShowOverlayBtn.Content = (overlayWindow != null && overlayWindow.IsVisible)
                ? "Ocultar Benchmark" : "Mostrar Benchmark";
        }

        public BenchmarkOverlayPage()
        {
            InitializeComponent();
            InitImageOptions(); // << NOVO

            ShowOverlayBtn.Click += ShowOverlayBtn_Click;
            DownloadRtssBtn.Click += DownloadRtssBtn_Click;

            LockOverlayBox.IsCheckedChanged += SettingChanged;

            foreach (var cb in new[] { FpsBox, Fps1PercentBox, FpsAvg1MinBox, CpuCountBox, CpuUsageBox, CpuTempBox, CpuWattsBox, MemTotalBox, MemUsageBox, GpuNameBox, GpuUsageBox, GpuVramClockBox, GpuVramUsageBox, GpuWattsBox })
            {
                cb.IsCheckedChanged += SettingChanged;
            }
            FontColorCombo.SelectionChanged += SettingChanged;
            OpacitySlider.PropertyChanged += (s, e) =>
            {
                if (e.Property == Slider.ValueProperty) SettingChanged(null, null);
            };

            // Carrega configurações do usuário
            LoadOverlaySettings();

            // Checar RTSS ao abrir página
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                rtssAvailable = await CheckAndStartRTSS();
                SetRtssUi(rtssAvailable);
                UpdateToggleButton();
            });

            UpdateToggleButton();
        }

        private async void ShowOverlayBtn_Click(object? sender, RoutedEventArgs e)
        {
            if (!rtssAvailable)
            {
                ShowAlert("O RTSS não está instalado ou iniciado. Baixe e inicie o RTSS para utilizar o FPS!");
                return;
            }

            if (overlayWindow != null)
            {
                overlayWindow.Close();
                overlayWindow = null;
                await Task.Run(() => KillRtssProcesses());
                rtssAvailable = false;
                SetRtssUi(false);
                UpdateToggleButton();
                return;
            }

            await Task.Run(() => KillRtssProcesses());
            var started = await StartRtss();
            if (!started)
            {
                ShowAlert("Falha ao iniciar o RTSS. Verifique a instalação.");
                SetRtssUi(false);
                return;
            }

            overlayWindow = new BenchmarkOverlayWindow(GetOverlaySettings());
            overlayWindow.Closed += (_, _) => { overlayWindow = null; UpdateToggleButton(); };
            overlayWindow.Show();
            rtssAvailable = true;
            SetRtssUi(true);
            UpdateToggleButton();
        }

        private void DownloadRtssBtn_Click(object? sender, RoutedEventArgs e)
        {
            var url = "https://www.guru3d.com/files-details/rtss-rivatuner-statistics-server-download.html";
            try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
            catch { }
        }

        private void SettingChanged(object? sender, EventArgs? e)
        {
            if (loadingSettings) return; // Não salva ao carregar
            SaveOverlaySettings();
            if (overlayWindow != null)
                overlayWindow.ApplySettings(GetOverlaySettings());
        }

        private OverlaySettings GetOverlaySettings()
        {
            return new OverlaySettings
            {
                ShowFPS = FpsBox.IsChecked ?? false,
                ShowFPS1Percent = Fps1PercentBox.IsChecked ?? false,
                ShowFPSAvg1Min = FpsAvg1MinBox.IsChecked ?? false,
                ShowCpuCount = CpuCountBox.IsChecked ?? false,
                ShowCpuUsage = CpuUsageBox.IsChecked ?? false,
                ShowCpuTemp = CpuTempBox.IsChecked ?? false,
                ShowCpuWatts = CpuWattsBox.IsChecked ?? false,
                ShowMemTotal = MemTotalBox.IsChecked ?? false,
                ShowMemUsage = MemUsageBox.IsChecked ?? false,
                ShowGpuName = GpuNameBox.IsChecked ?? false,
                ShowGpuUsage = GpuUsageBox.IsChecked ?? false,
                ShowGpuVramClock = GpuVramClockBox.IsChecked ?? false,
                ShowGpuVramUsage = GpuVramUsageBox.IsChecked ?? false,
                ShowGpuWatts = GpuWattsBox.IsChecked ?? false,
                ShowImageTop = ShowImageTopBox.IsChecked ?? false,
                ShowImageBottom = ShowImageBottomBox.IsChecked ?? false,
                LockOverlay = LockOverlayBox.IsChecked ?? false,
                ImageTopPath = GTDConfigHelper.GetString("BenchmarkOverlay", "ImageTopPath", ""),
                ImageBottomPath = GTDConfigHelper.GetString("BenchmarkOverlay", "ImageBottomPath", ""),
                FontColor = FontColorCombo.SelectedIndex switch
                {
                    0 => Colors.White,
                    1 => Colors.Lime,
                    2 => Colors.Red,
                    3 => Colors.Yellow,
                    4 => Colors.Blue,
                    5 => Colors.Cyan,
                    6 => Colors.Violet,
                    _ => Colors.White,
                },
                OverlayOpacity = OpacitySlider.Value
            };
        }

        private void LoadOverlaySettings()
        {
            loadingSettings = true;
            var s = new OverlaySettings();

            s.ShowFPS = GTDConfigHelper.GetBool("BenchmarkOverlay", "ShowFPS", true);
            s.ShowFPS1Percent = GTDConfigHelper.GetBool("BenchmarkOverlay", "ShowFPS1Percent", false);
            s.ShowFPSAvg1Min = GTDConfigHelper.GetBool("BenchmarkOverlay", "ShowFPSAvg1Min", false);
            s.ShowCpuCount = GTDConfigHelper.GetBool("BenchmarkOverlay", "ShowCpuCount", true);
            s.ShowCpuUsage = GTDConfigHelper.GetBool("BenchmarkOverlay", "ShowCpuUsage", true);
            s.ShowCpuTemp = GTDConfigHelper.GetBool("BenchmarkOverlay", "ShowCpuTemp", true);
            s.ShowCpuWatts = GTDConfigHelper.GetBool("BenchmarkOverlay", "ShowCpuWatts", false);
            s.ShowMemTotal = GTDConfigHelper.GetBool("BenchmarkOverlay", "ShowMemTotal", true);
            s.ShowMemUsage = GTDConfigHelper.GetBool("BenchmarkOverlay", "ShowMemUsage", true);
            s.ShowGpuName = GTDConfigHelper.GetBool("BenchmarkOverlay", "ShowGpuName", true);
            s.ShowGpuUsage = GTDConfigHelper.GetBool("BenchmarkOverlay", "ShowGpuUsage", true);
            s.ShowGpuVramClock = GTDConfigHelper.GetBool("BenchmarkOverlay", "ShowGpuVramClock", false);
            s.ShowGpuVramUsage = GTDConfigHelper.GetBool("BenchmarkOverlay", "ShowGpuVramUsage", true);
            s.ShowGpuWatts = GTDConfigHelper.GetBool("BenchmarkOverlay", "ShowGpuWatts", false);
            s.LockOverlay = GTDConfigHelper.GetBool("BenchmarkOverlay", "LockOverlay", false);
    

            var colorName = GTDConfigHelper.GetString("BenchmarkOverlay", "FontColor", "White");
            FontColorCombo.SelectedIndex = colorName switch
            {
                "White" => 0,
                "Lime" => 1,
                "Red" => 2,
                "Yellow" => 3,
                "Blue" => 4,
                "Cyan" => 5,
                "Violet" => 6,
                _ => 0,
            };
            OpacitySlider.Value = GTDConfigHelper.GetDouble("BenchmarkOverlay", "OverlayOpacity", 0.85);

            FpsBox.IsChecked = s.ShowFPS;
            Fps1PercentBox.IsChecked = s.ShowFPS1Percent;
            FpsAvg1MinBox.IsChecked = s.ShowFPSAvg1Min;
            CpuCountBox.IsChecked = s.ShowCpuCount;
            CpuUsageBox.IsChecked = s.ShowCpuUsage;
            CpuTempBox.IsChecked = s.ShowCpuTemp;
            CpuWattsBox.IsChecked = s.ShowCpuWatts;
            MemTotalBox.IsChecked = s.ShowMemTotal;
            MemUsageBox.IsChecked = s.ShowMemUsage;
            GpuNameBox.IsChecked = s.ShowGpuName;
            GpuUsageBox.IsChecked = s.ShowGpuUsage;
            GpuVramClockBox.IsChecked = s.ShowGpuVramClock;
            GpuVramUsageBox.IsChecked = s.ShowGpuVramUsage;
            GpuWattsBox.IsChecked = s.ShowGpuWatts;            

            s.ShowImageTop = GTDConfigHelper.GetBool("BenchmarkOverlay", "ShowImageTop", false);
            s.ShowImageBottom = GTDConfigHelper.GetBool("BenchmarkOverlay", "ShowImageBottom", false);
            s.ImageTopPath = GTDConfigHelper.GetString("BenchmarkOverlay", "ImageTopPath", "");
            s.ImageBottomPath = GTDConfigHelper.GetString("BenchmarkOverlay", "ImageBottomPath", "");

            ShowImageTopBox.IsChecked = s.ShowImageTop;
            ShowImageBottomBox.IsChecked = s.ShowImageBottom;
            UploadImageTopBtn.IsVisible = s.ShowImageTop;
            UploadImageBottomBtn.IsVisible = s.ShowImageBottom;
            ImageTopLabel.IsVisible = s.ShowImageTop;
            ImageBottomLabel.IsVisible = s.ShowImageBottom;
            ImageTopLabel.Text = Path.GetFileName(s.ImageTopPath);
            ImageBottomLabel.Text = Path.GetFileName(s.ImageBottomPath);
            LockOverlayBox.IsChecked = s.LockOverlay;

            loadingSettings = false;
        }

        private void SaveOverlaySettings()
        {
            var s = GetOverlaySettings();

            GTDConfigHelper.Set("BenchmarkOverlay", "ShowFPS", s.ShowFPS ? "true" : "false");
            GTDConfigHelper.Set("BenchmarkOverlay", "ShowFPS1Percent", s.ShowFPS1Percent ? "true" : "false");
            GTDConfigHelper.Set("BenchmarkOverlay", "ShowFPSAvg1Min", s.ShowFPSAvg1Min ? "true" : "false");
            GTDConfigHelper.Set("BenchmarkOverlay", "ShowCpuCount", s.ShowCpuCount ? "true" : "false");
            GTDConfigHelper.Set("BenchmarkOverlay", "ShowCpuUsage", s.ShowCpuUsage ? "true" : "false");
            GTDConfigHelper.Set("BenchmarkOverlay", "ShowCpuTemp", s.ShowCpuTemp ? "true" : "false");
            GTDConfigHelper.Set("BenchmarkOverlay", "ShowCpuWatts", s.ShowCpuWatts ? "true" : "false");
            GTDConfigHelper.Set("BenchmarkOverlay", "ShowMemTotal", s.ShowMemTotal ? "true" : "false");
            GTDConfigHelper.Set("BenchmarkOverlay", "ShowMemUsage", s.ShowMemUsage ? "true" : "false");
            GTDConfigHelper.Set("BenchmarkOverlay", "ShowGpuName", s.ShowGpuName ? "true" : "false");
            GTDConfigHelper.Set("BenchmarkOverlay", "ShowGpuUsage", s.ShowGpuUsage ? "true" : "false");
            GTDConfigHelper.Set("BenchmarkOverlay", "ShowGpuVramClock", s.ShowGpuVramClock ? "true" : "false");
            GTDConfigHelper.Set("BenchmarkOverlay", "ShowGpuVramUsage", s.ShowGpuVramUsage ? "true" : "false");
            GTDConfigHelper.Set("BenchmarkOverlay", "ShowGpuWatts", s.ShowGpuWatts ? "true" : "false");
            GTDConfigHelper.Set("BenchmarkOverlay", "ShowImageTop", s.ShowImageTop ? "true" : "false");
            GTDConfigHelper.Set("BenchmarkOverlay", "ShowImageBottom", s.ShowImageBottom ? "true" : "false");
            GTDConfigHelper.Set("BenchmarkOverlay", "LockOverlay", s.LockOverlay ? "true" : "false");

            string color = FontColorCombo.SelectedIndex switch
            {
                0 => "White",
                1 => "Lime",
                2 => "Red",
                3 => "Yellow",
                4 => "Blue",
                5 => "Cyan",
                6 => "Violet",
                _ => "White",
            };
            GTDConfigHelper.Set("BenchmarkOverlay", "FontColor", color);
            GTDConfigHelper.Set("BenchmarkOverlay", "OverlayOpacity", s.OverlayOpacity.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        private void SetRtssUi(bool available)
        {
            RtssWarning.Text = available
                ? ""
                : "⚠️ O RTSS (RivaTuner Statistics Server) não está instalado ou não está em execução.\nO overlay de FPS só funcionará se o RTSS estiver aberto.";
            RtssWarning.IsVisible = !available;
            DownloadRtssBtn.IsVisible = !available;
            ShowOverlayBtn.IsEnabled = available;
        }

        private void ShowAlert(string msg)
        {
            var dlg = new Window
            {
                Width = 400,
                Height = 120,
                Content = new StackPanel
                {
                    Spacing = 12,
                    Margin = new Thickness(16),
                    Children =
                    {
                        new TextBlock { Text = msg, Foreground = Brushes.Red, FontWeight = FontWeight.Bold },
                        new Button { Content = "OK", Width = 70, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right }
                    }
                }
            };
            ((Button)((StackPanel)dlg.Content).Children[1]).Click += (s, e) => dlg.Close();
            var owner = this.VisualRoot as Window;
            if (owner != null)
                dlg.ShowDialog(owner);
            else
                dlg.Show();
        }

        private void KillRtssProcesses()
        {
            foreach (var procName in rtssProcessNames)
            {
                try
                {
                    var procs = Process.GetProcessesByName(procName);
                    foreach (var p in procs)
                    {
                        try { if (!p.HasExited) p.Kill(true); }
                        catch { }
                    }
                }
                catch { }
            }
            System.Threading.Thread.Sleep(900);
        }

        private async Task<bool> StartRtss()
        {
            var exePath = possibleRtssPaths.FirstOrDefault(File.Exists);
            if (exePath == null) return false;

            try
            {
                var pi = Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true
                });
                await Task.Delay(1800);
                var proc = Process.GetProcessesByName("RTSS").FirstOrDefault();
                return proc != null && !proc.HasExited;
            }
            catch { return false; }
        }

        private async Task<bool> CheckAndStartRTSS()
        {
            var proc = Process.GetProcessesByName("RTSS").FirstOrDefault();
            if (proc != null && !proc.HasExited)
                return true;

            var exePath = possibleRtssPaths.FirstOrDefault(File.Exists);
            if (exePath == null)
                return false;

            try
            {
                var pi = Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true
                });
                await Task.Delay(1500);
                proc = Process.GetProcessesByName("RTSS").FirstOrDefault();
                return proc != null && !proc.HasExited;
            }
            catch { return false; }
        }


        private void InitImageOptions()
        {
            ShowImageTopBox.IsCheckedChanged += (s, e) =>
            {
                UploadImageTopBtn.IsVisible = ShowImageTopBox.IsChecked ?? false;
                ImageTopLabel.IsVisible = ShowImageTopBox.IsChecked ?? false;
                SettingChanged(null, null);
            };
            ShowImageBottomBox.IsCheckedChanged += (s, e) =>
            {
                UploadImageBottomBtn.IsVisible = ShowImageBottomBox.IsChecked ?? false;
                ImageBottomLabel.IsVisible = ShowImageBottomBox.IsChecked ?? false;
                SettingChanged(null, null);
            };

            UploadImageTopBtn.Click += async (_, __) =>
            {
                var path = await SelectImageFile();
                if (!string.IsNullOrEmpty(path))
                {
                    ImageTopLabel.Text = Path.GetFileName(path);
                    GTDConfigHelper.Set("BenchmarkOverlay", "ImageTopPath", path);
                    SettingChanged(null, null);
                }
            };
            UploadImageBottomBtn.Click += async (_, __) =>
            {
                var path = await SelectImageFile();
                if (!string.IsNullOrEmpty(path))
                {
                    ImageBottomLabel.Text = Path.GetFileName(path);
                    GTDConfigHelper.Set("BenchmarkOverlay", "ImageBottomPath", path);
                    SettingChanged(null, null);
                }
            };
        }



        private async Task<string?> SelectImageFile()
        {
            var win = this.VisualRoot as Window;
            if (win == null || win.StorageProvider == null)
                return null;

            var files = await win.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Selecionar Imagem (PNG ou JPG)",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("Imagens")
                    {
                        Patterns = new[] { "*.png", "*.jpg", "*.jpeg" }
                    }
                }
            });

            var file = files?.FirstOrDefault();
            return file?.Path.LocalPath;
        }
    }

    public class OverlaySettings
    {
        public bool ShowFPS { get; set; }
        public bool ShowFPS1Percent { get; set; }
        public bool ShowFPSAvg1Min { get; set; }
        public bool ShowImageTop { get; set; }
        public bool ShowImageBottom { get; set; }
        public string? ImageTopPath { get; set; }
        public string? ImageBottomPath { get; set; }
        public bool ShowCpuCount { get; set; }
        public bool ShowCpuUsage { get; set; }
        public bool ShowCpuTemp { get; set; }
        public bool ShowCpuWatts { get; set; }
        public bool ShowMemTotal { get; set; }
        public bool ShowMemUsage { get; set; }
        public bool ShowGpuName { get; set; }
        public bool ShowGpuUsage { get; set; }
        public bool ShowGpuVramClock { get; set; }
        public bool ShowGpuVramUsage { get; set; }
        public bool ShowGpuWatts { get; set; }
        public Color FontColor { get; set; }
        public double OverlayOpacity { get; set; }
        public bool LockOverlay { get; set; }
        
    }
}
