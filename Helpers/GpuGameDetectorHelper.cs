using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GTDCompanion.Helpers
{
    public class GpuGameDetectorHelper
    {
        // Configurações ajustáveis
        private const float GPU_USAGE_THRESHOLD = 10f;     // mínimo % GPU para considerar (ajuste)
        private const int MINUTES_THRESHOLD = 2;           // minutos seguidos acima do threshold
        private const int MONITOR_INTERVAL_SECONDS = 1;   // frequência de checagem

        private static readonly string ApiUrl = "https://gametrydivision.com/api/gtdcompanion/getexetocheck";

        // Estrutura para guardar histórico
        private class GpuUsageEntry
        {
            public string FileName { get; set; } = string.Empty;
            public string FullPath { get; set; } = string.Empty;
            public int Pid { get; set; }
            public DateTime? HighUsageStart { get; set; }
            public bool Sent { get; set; }
        }

        private static readonly Dictionary<int, GpuUsageEntry> usageHistory = new();
        private static Timer? timer;

        // Inicializador público: chama uma vez só!
        public static void Start()
        {
            timer = new Timer(async _ => await MonitorAndSend(), null, 0, MONITOR_INTERVAL_SECONDS * 1000);
        }

        private static async Task MonitorAndSend()
        {
            try
            {
                var category = new PerformanceCounterCategory("GPU Engine");
                var instances = category.GetInstanceNames();

                var now = DateTime.UtcNow;

                // Pega só os que são "engtype_3D"
                foreach (var instance in instances)
                {
                    if (!instance.Contains("engtype_3D")) continue;

                    int pid;
                    try
                    {
                        var pidStr = instance.Split('_').FirstOrDefault(s => s.StartsWith("pid"))?.Replace("pid_", "");
                        if (!int.TryParse(pidStr, out pid)) continue;
                        var counter = new PerformanceCounter("GPU Engine", "Utilization Percentage", instance);
                        float value = counter.NextValue();

                        // Ignora valores negativos/bizarros
                        if (value < 0) continue;

                        Process proc = null;
                        try { proc = Process.GetProcessById(pid); } catch { continue; }
                        var exePath = "";
                        var exeName = "";

                        try
                        {
                            exePath = proc.MainModule?.FileName ?? "";
                            exeName = Path.GetFileName(exePath);
                        }
                        catch { continue; }

                        if (string.IsNullOrWhiteSpace(exeName) || string.IsNullOrWhiteSpace(exePath)) continue;
                        if (!usageHistory.ContainsKey(pid))
                        {
                            usageHistory[pid] = new GpuUsageEntry
                            {
                                FileName = exeName,
                                FullPath = exePath,
                                Pid = pid
                            };
                        }

                        if (value >= GPU_USAGE_THRESHOLD)
                        {
                            if (usageHistory[pid].HighUsageStart == null)
                                usageHistory[pid].HighUsageStart = now;

                            if (!usageHistory[pid].Sent &&
                                (now - usageHistory[pid].HighUsageStart.Value).TotalMinutes >= MINUTES_THRESHOLD)
                            {
                                usageHistory[pid].Sent = true;
                                await SendUnknownGame(usageHistory[pid]);
                            }
                        }
                        else
                        {
                            usageHistory[pid].HighUsageStart = null;
                        }
                    }
                    catch { continue; }
                }

                // Limpeza de processos encerrados
                var toRemove = usageHistory
                    .Where(kv => !ProcessExists(kv.Key))
                    .Select(kv => kv.Key)
                    .ToList();
                foreach (var pid in toRemove)
                    usageHistory.Remove(pid);

            }
            catch (Exception ex)
            {
                // Log opcional
            }
        }

        private static bool ProcessExists(int pid)
        {
            try { return Process.GetProcessById(pid) != null; } catch { return false; }
        }

        private static async Task SendUnknownGame(GpuUsageEntry entry)
        {
            try
            {
                var obj = new[]
                {
                    new {
                        nome_arquivo = entry.FileName,
                        caminho_arquivo = entry.FullPath
                    }
                };
                var json = JsonSerializer.Serialize(obj);
                using var http = new HttpClient();
                var resp = await http.PostAsync(ApiUrl, new StringContent(json, Encoding.UTF8, "application/json"));
                // Log opcional: resp.StatusCode
            }
            catch (Exception ex)
            {
                // Log opcional
            }
        }
    }
}
