using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using LibreHardwareMonitor.Hardware;
using RTSSSharedMemoryNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using GTDCompanion; // Para GTDConfigHelper

namespace GTDCompanion.Pages
{
    public partial class BenchmarkOverlayWindow : Window
    {
        private OverlaySettings currentSettings;
        private DispatcherTimer timer;
        private Computer computer;
        private List<ISensor> cpuSensors;
        private List<ISensor> gpuSensors;
        private List<ISensor> memSensors;
        private Point? dragOffset;
        private bool isLocked = false;

        public BenchmarkOverlayWindow(OverlaySettings settings)
        {
            InitializeComponent();
            currentSettings = settings;

            computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true
            };
            computer.Open();

            cpuSensors = new List<ISensor>();
            gpuSensors = new List<ISensor>();
            memSensors = new List<ISensor>();

            UpdateSensorsList();

            // RESTAURA POSIÇÃO
            var pos = LoadOverlayPosition();
            if (pos != null)
                this.Position = pos.Value;

            ApplyVisualOpacity();
            LoadOverlayImages();
            UpdateOverlay();

            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000) };
            timer.Tick += (_, _) => { UpdateSensorsList(); UpdateOverlay(); };
            timer.Start();

            // Arrastar a janela ao clicar em qualquer lugar (só se não estiver bloqueada)
            this.PointerPressed += Overlay_PointerPressed;
            this.PointerMoved += Overlay_PointerMoved;

            // Sempre aplicar estado de bloqueio ao criar
            ApplyLockState();
        }

        public void ApplySettings(OverlaySettings settings)
        {
            currentSettings = settings;
            ApplyVisualOpacity();
            LoadOverlayImages();
            UpdateOverlay();
            ApplyLockState();
        }

        private void ApplyVisualOpacity()
        {
            var alpha = (byte)(currentSettings.OverlayOpacity * 255);
            RootBorder.Background = new SolidColorBrush(Color.FromArgb(alpha, 24, 26, 32));
        }

        private void LoadOverlayImages()
        {
            if (currentSettings.ShowImageTop && !string.IsNullOrEmpty(currentSettings.ImageTopPath) && File.Exists(currentSettings.ImageTopPath))
            {
                TopImage.Source = new Avalonia.Media.Imaging.Bitmap(currentSettings.ImageTopPath);
                TopImage.IsVisible = true;
            }
            else
            {
                TopImage.Source = null;
                TopImage.IsVisible = false;
            }
            if (currentSettings.ShowImageBottom && !string.IsNullOrEmpty(currentSettings.ImageBottomPath) && File.Exists(currentSettings.ImageBottomPath))
            {
                BottomImage.Source = new Avalonia.Media.Imaging.Bitmap(currentSettings.ImageBottomPath);
                BottomImage.IsVisible = true;
            }
            else
            {
                BottomImage.Source = null;
                BottomImage.IsVisible = false;
            }
        }

        private void UpdateSensorsList()
        {
            cpuSensors.Clear();
            gpuSensors.Clear();
            memSensors.Clear();

            foreach (var hardware in computer.Hardware)
            {
                hardware.Update();
                if (hardware.HardwareType == HardwareType.Cpu)
                    cpuSensors.AddRange(hardware.Sensors);
                if (hardware.HardwareType == HardwareType.GpuAmd || hardware.HardwareType == HardwareType.GpuNvidia)
                    gpuSensors.AddRange(hardware.Sensors);
                if (hardware.HardwareType == HardwareType.Memory)
                    memSensors.AddRange(hardware.Sensors);
            }
        }

        private string GetCpuTemperatureValue()
        {
            var tempSensors = cpuSensors.Where(s => s.SensorType == SensorType.Temperature && s.Value.HasValue).ToList();
            if (tempSensors.Count == 0)
                return "--";
            var maxTemp = tempSensors.Max(s => s.Value.GetValueOrDefault());
            return $"{maxTemp:0.0}";
        }

        private string GetCpuWattsValue()
        {
            var powerSensors = cpuSensors.Where(s => s.SensorType == SensorType.Power && s.Value.HasValue).ToList();
            if (powerSensors.Count == 0)
                return "--";
            var maxWatts = powerSensors.Max(s => s.Value.GetValueOrDefault());
            return $"{maxWatts:0.0}";
        }

        private string GetCpuSensorValue(SensorType type, string? nameContains = null)
        {
            var sensor = cpuSensors.FirstOrDefault(s => s.SensorType == type && (nameContains == null || s.Name.Contains(nameContains)));
            return sensor != null && sensor.Value.HasValue ? $"{sensor.Value.Value:0.0}" : "--";
        }

        private string GetGpuSensorValue(SensorType type, string? nameContains = null)
        {
            var sensor = gpuSensors.FirstOrDefault(s => s.SensorType == type && (nameContains == null || s.Name.Contains(nameContains)));
            return sensor != null && sensor.Value.HasValue ? $"{sensor.Value.Value:0.0}" : "--";
        }

        private string GetMemSensorValue(SensorType type)
        {
            var sensor = memSensors.FirstOrDefault(s => s.SensorType == type);
            return sensor != null && sensor.Value.HasValue ? $"{sensor.Value.Value:0.0}" : "--";
        }

        private string GetFps()
        {
            try
            {
                var appEntries = OSD.GetAppEntries();
                var mainGame = appEntries?.FirstOrDefault(a => a.InstantaneousFrames > 0);
                if (mainGame != null)
                    return $"{mainGame.InstantaneousFrames:0}";
                return "--";
            }
            catch
            {
                return "--";
            }
        }

        private void UpdateOverlay()
        {
            var items = new List<(string, string)>();

            void Add(string label, bool enabled, Func<string> getter)
            {
                if (enabled) items.Add((label, getter()));
            }

            Add("FPS", currentSettings.ShowFPS, GetFps);
            Add("FPS 1%", currentSettings.ShowFPS1Percent, () => "--");
            Add("Média FPS 1 Min", currentSettings.ShowFPSAvg1Min, () => "--");
            Add("Processadores", currentSettings.ShowCpuCount, () => Environment.ProcessorCount.ToString());
            Add("Uso CPU (%)", currentSettings.ShowCpuUsage, () => GetCpuSensorValue(SensorType.Load, "Total") + " %");
            Add("Temp. CPU", currentSettings.ShowCpuTemp, () => GetCpuTemperatureValue() + " °C");
            Add("Watts CPU", currentSettings.ShowCpuWatts, () => GetCpuWattsValue() + " W");
            Add("Memória Total", currentSettings.ShowMemTotal, () => GetMemSensorValue(SensorType.Data) + " GB");
            Add("Memória em Uso (%)", currentSettings.ShowMemUsage, () => GetMemSensorValue(SensorType.Load) + " %");
            Add("GPU", currentSettings.ShowGpuName, () =>
            {
                var hw = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.GpuNvidia || h.HardwareType == HardwareType.GpuAmd);
                return hw != null ? hw.Name : "--";
            });
            Add("Uso GPU (%)", currentSettings.ShowGpuUsage, () => GetGpuSensorValue(SensorType.Load) + " %");
            Add("Clock VRAM", currentSettings.ShowGpuVramClock, () => GetGpuSensorValue(SensorType.Clock, "Memory") + " MHz");
            Add("Consumo VRAM", currentSettings.ShowGpuVramUsage, () => GetGpuSensorValue(SensorType.SmallData, "Memory Used") + " GB");
            Add("Watts GPU", currentSettings.ShowGpuWatts, () => GetGpuSensorValue(SensorType.Power) + " W");

            ItemsPanel.Children.Clear();
            foreach (var (label, value) in items)
            {
                ItemsPanel.Children.Add(new TextBlock
                {
                    Text = $"{label}: {value}",
                    FontSize = 12,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(currentSettings.FontColor),
                    Margin = new Thickness(0, 0, 0, 2)
                });
            }

            this.Height = items.Count * 22 + 24 + (TopImage.IsVisible ? 85 : 0) + (BottomImage.IsVisible ? 85 : 0);
            this.Width = 240;
        }

        public void BringToFront()
        {
            this.Topmost = false;
            this.Topmost = true;
        }

        // ARRASTE (apenas se não estiver bloqueada)
        private void Overlay_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (isLocked) return;
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                dragOffset = e.GetPosition(this);
        }
        private void Overlay_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (isLocked) return;
            if (dragOffset.HasValue && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                var screenPos = this.Position;
                var mouse = e.GetPosition(this);
                var offsetX = mouse.X - dragOffset.Value.X;
                var offsetY = mouse.Y - dragOffset.Value.Y;
                this.Position = new PixelPoint(screenPos.X + (int)offsetX, screenPos.Y + (int)offsetY);
                SaveOverlayPosition();
            }
        }

        // CLICK-THROUGH (usando Win32, só funciona no Windows)
        private void ApplyLockState()
        {
            isLocked = currentSettings.LockOverlay;
            if (OperatingSystem.IsWindows())
            {
                var hwnd = GetWindowHandle();
                if (hwnd != IntPtr.Zero)
                {
                    int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                    if (isLocked)
                    {
                        // Torna click-through
                        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED);
                    }
                    else
                    {
                        // Remove o click-through
                        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle & ~WS_EX_TRANSPARENT);
                    }
                }
            }
            this.IsHitTestVisible = !isLocked;
            this.Topmost = true;
        }

        // Salvar/restaurar posição
        private PixelPoint? LoadOverlayPosition()
        {
            int x = GTDConfigHelper.GetInt("BenchmarkOverlay", "PosX", -1);
            int y = GTDConfigHelper.GetInt("BenchmarkOverlay", "PosY", -1);
            if (x >= 0 && y >= 0)
                return new PixelPoint(x, y);
            return null;
        }
        private void SaveOverlayPosition()
        {
            GTDConfigHelper.Set("BenchmarkOverlay", "PosX", this.Position.X.ToString());
            GTDConfigHelper.Set("BenchmarkOverlay", "PosY", this.Position.Y.ToString());
        }

        // Helpers Win32
        private IntPtr GetWindowHandle()
        {
            var handle = (this.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero);
            return handle;
        }
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    }
}
