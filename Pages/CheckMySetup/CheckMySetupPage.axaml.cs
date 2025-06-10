using GTDCompanion; // para enxergar MiraConfig
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.Runtime.InteropServices;
using LibreHardwareMonitor.Hardware;

namespace GTDCompanion.Pages
{
    public partial class CheckMySetupPage : UserControl
    {
        private Dictionary<string, string>? _lastUserSpecs;  // Labels para usuário
        private Dictionary<string, string>? _lastApiSpecs;   // Chaves para API

        public CheckMySetupPage()
        {
            InitializeComponent();
            PinBox.Text = GTDConfigHelper.LoadGtdId();
            AppConfig.PopulateEnvironment();
            LoadSpecs();
        }

        // Atualiza exibição dos dados do setup
        private async void LoadSpecs()
        {
            await Task.Run(() => CollectSpecs(out _lastUserSpecs, out _lastApiSpecs));
            if (_lastUserSpecs != null)
                ShowSpecs(_lastUserSpecs);
        }

        // Mostra os dados no StackPanel
        private void ShowSpecs(Dictionary<string, string> specs)
        {
            SpecsStack.Children.Clear();
            foreach (var kv in specs)
            {
                SpecsStack.Children.Add(new TextBlock
                {
                    Text = $"{kv.Key}: {kv.Value}",
                    Foreground = Brushes.White,
                    FontSize = 12,
                });
            }
        }

        /// <summary>
        /// Coleta as informações e preenche dois dicionários:
        /// userSpecs: labels amigáveis
        /// apiSpecs: chaves para API
        /// </summary>
        private void CollectSpecs(out Dictionary<string, string> userSpecs, out Dictionary<string, string> apiSpecs)
        {
            userSpecs = new();
            apiSpecs = new();
            try
            {
                // SO
                var so = RuntimeInformation.OSDescription;
                userSpecs["Sistema Operacional"] = so;
                apiSpecs["so"] = so;

                // Placa-mãe
                var mbManuf = GetWmi("Win32_BaseBoard", "Manufacturer");
                var mbProd = GetWmi("Win32_BaseBoard", "Product");
                userSpecs["Placa-mãe"] = $"{mbManuf} - {mbProd}";
                apiSpecs["motherboard-manuf"] = mbManuf;
                apiSpecs["motherboard"] = mbProd;

                // CPU
                var cpu = GetWmi("Win32_Processor", "Name");
                var cpuClock = GetWmi("Win32_Processor", "MaxClockSpeed") + " MHz";
                userSpecs["Processador"] = cpu;
                userSpecs["Clock CPU"] = cpuClock;
                apiSpecs["cpu"] = cpu;
                apiSpecs["cpu-clock"] = cpuClock;

                // Núcleos e threads
                int cores = GetWmiAll("Win32_Processor", "NumberOfCores").Sum(x => int.TryParse(x, out var n) ? n : 0);
                int threads = GetWmiAll("Win32_Processor", "NumberOfLogicalProcessors").Sum(x => int.TryParse(x, out var n) ? n : 0);
                userSpecs["Núcleos/Threads"] = $"{cores} / {threads}";
                apiSpecs["cores"] = $"{cores}/{threads}";

                // RAM
                var memStr = GetWmi("Win32_ComputerSystem", "TotalPhysicalMemory");
                if (double.TryParse(memStr, out double memBytes))
                {
                    userSpecs["Memória RAM"] = $"{(int)Math.Round(memBytes / 1024 / 1024 / 1024)} GB";
                    apiSpecs["mram"] = $"{(int)Math.Round(memBytes / 1024 / 1024 / 1024)} GB";
                }
                else
                {
                    userSpecs["Memória RAM"] = "Desconhecida";
                    apiSpecs["mram"] = "";
                }

                // Disco
                var totalDisk = GetTotalDisk();
                userSpecs["HD Total"] = $"{totalDisk} GB";
                apiSpecs["disk"] = $"{totalDisk} GB";

                // GPU(s) e VRAM
                GetGpuInfo(out var gpuStr, out var vramStr);
                userSpecs["GPU(s)"] = gpuStr;
                apiSpecs["gpu"] = gpuStr;
                userSpecs["VRAM"] = vramStr;
                apiSpecs["vram"] = vramStr;

                // Garantia de campos obrigatórios na API
                foreach (var key in new[] { "so", "cpu", "cores", "mram", "disk", "gpu" })
                {
                    if (!apiSpecs.ContainsKey(key) || string.IsNullOrWhiteSpace(apiSpecs[key]))
                        apiSpecs[key] = "";
                }
            }
            catch (Exception ex)
            {
                userSpecs.Clear();
                userSpecs["Erro"] = $"Falha ao coletar specs: {ex.Message}";
                apiSpecs.Clear();
            }
        }

        private void GetGpuInfo(out string gpuStr, out string vramStr)
        {
            var names = new List<string>();
            double maxVram = 0;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    var comp = new Computer { IsGpuEnabled = true };
                    comp.Open();
                    foreach (var hw in comp.Hardware)
                    {
                        if (hw.HardwareType == HardwareType.GpuNvidia || hw.HardwareType == HardwareType.GpuAmd || hw.HardwareType == HardwareType.GpuIntel)
                        {
                            names.Add(hw.Name);
                            hw.Update();
                            foreach (var s in hw.Sensors)
                            {
                                if ((s.SensorType == SensorType.SmallData || s.SensorType == SensorType.Data) &&
                                    s.Name.ToLower().Contains("memory") && s.Name.ToLower().Contains("total") && s.Value.HasValue)
                                {
                                    maxVram = Math.Max(maxVram, s.Value.Value);
                                }
                            }
                        }
                    }
                    comp.Close();
                }
                catch
                {
                    // Ignored
                }
            }

            if (names.Count == 0)
                names.AddRange(GetWmiAll("Win32_VideoController", "Name"));

            if (maxVram <= 0)
            {
                var vrams = GetWmiAll("Win32_VideoController", "AdapterRAM")
                    .Select(x => double.TryParse(x, out var v) ? v / 1024 / 1024 / 1024 : 0)
                    .ToList();
                if (vrams.Count > 0)
                    maxVram = vrams.Max();
            }

            gpuStr = names.Count > 0 ? string.Join(" | ", names.Distinct()) : "Desconhecida";
            vramStr = maxVram > 0 ? $"{Math.Round(maxVram, 2)} GB" : string.Empty;
        }

        private string GetWmi(string className, string prop)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "Não suportado";
            using var searcher = new ManagementObjectSearcher($"SELECT {prop} FROM {className}");
            foreach (ManagementObject obj in searcher.Get())
                return obj[prop]?.ToString() ?? "Desconhecido";
            return "Desconhecido";
        }
        private List<string> GetWmiAll(string className, string prop)
        {
            var list = new List<string>();
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return list;
            using var searcher = new ManagementObjectSearcher($"SELECT {prop} FROM {className}");
            foreach (ManagementObject obj in searcher.Get())
                list.Add(obj[prop]?.ToString() ?? "");
            return list;
        }
        private string GetTotalDisk()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "Não suportado";
            double total = 0;
            using var searcher = new ManagementObjectSearcher("SELECT Size FROM Win32_DiskDrive");
            foreach (ManagementObject obj in searcher.Get())
                if (double.TryParse(obj["Size"]?.ToString(), out double sz))
                    total += sz;
            return Math.Round(total / 1024 / 1024 / 1024, 2).ToString();
        }

        // Envia os dados para a API
        private async void SendBtn_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            StatusText.Text = "Enviando...";
            StatusText.Foreground = Brushes.Yellow;
            SendBtn.IsEnabled = false;

            var gtdId = PinBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(gtdId))
            {
                StatusText.Text = "Informe o PIN (GTID)!";
                StatusText.Foreground = Brushes.OrangeRed;
                SendBtn.IsEnabled = true;
                return;
            }

            // Salva o GTID/PIN para lembrar
            GTDConfigHelper.SaveGtdId(gtdId);

            // Garante specs atualizadas
            var internalCheck = Environment.GetEnvironmentVariable("INTERNAL_CHECK") ?? "";
            CollectSpecs(out var userSpecs, out var apiSpecs);
            apiSpecs["gtd_id"] = gtdId;
            apiSpecs["internal_check"] = internalCheck;

            var urlApiGtd = Environment.GetEnvironmentVariable("URL_GTD_API") ?? "";

            try
            {
                using var http = new HttpClient();
                var payload = JsonSerializer.Serialize(
                    apiSpecs.Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                        .ToDictionary(kv => kv.Key, kv => kv.Value));
                var resp = await http.PostAsync(
                    urlApiGtd + "/gtd/players/setup-report",
                    new StringContent(payload, Encoding.UTF8, "application/json")
                );
                var respContent = await resp.Content.ReadAsStringAsync();

                if (resp.IsSuccessStatusCode)
                {
                    StatusText.Text = "Perfil atualizado com sucesso!";
                    StatusText.Foreground = Brushes.LightGreen;
                }
                else if ((int)resp.StatusCode == 403)
                {
                    StatusText.Text = "PIN da Comunidade inválido!";
                    StatusText.Foreground = Brushes.OrangeRed;
                }
                else if (!string.IsNullOrWhiteSpace(respContent))
                {
                    StatusText.Text = $"Erro ({(int)resp.StatusCode}): {respContent}";
                    StatusText.Foreground = Brushes.OrangeRed;
                }
                else
                {
                    StatusText.Text = $"Erro: {resp.StatusCode}";
                    StatusText.Foreground = Brushes.OrangeRed;
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Falha: {ex.Message}";
                StatusText.Foreground = Brushes.OrangeRed;
            }
            finally
            {
                SendBtn.IsEnabled = true;
            }
        }
    }
}
