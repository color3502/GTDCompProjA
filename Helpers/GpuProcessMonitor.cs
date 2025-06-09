using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;

namespace GTDCompanion.Helpers
{
    public static class GpuProcessMonitor
    {
        private static readonly Dictionary<int, DateTime> _usageStart = new();
        private static readonly HashSet<string> _knownPaths = new();
        private static Timer? _timer;
        private static readonly string StorePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GTDCompanion", "GpuProcesses.json");

        public static void Start()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;
            Load();
            _timer = new Timer(30_000);
            _timer.Elapsed += async (_, __) => await CheckAsync();
            _timer.AutoReset = true;
            _timer.Start();
        }

        public static void Stop()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }
        }

        private static void Load()
        {
            try
            {
                if (File.Exists(StorePath))
                {
                    var json = File.ReadAllText(StorePath);
                    var list = JsonSerializer.Deserialize<List<string>>(json);
                    if (list != null)
                        foreach (var p in list)
                            _knownPaths.Add(p);
                }
            }
            catch { }
        }

        private static void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(StorePath)!;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                var json = JsonSerializer.Serialize(_knownPaths, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(StorePath, json);
            }
            catch { }
        }

        private static async Task CheckAsync()
        {
            try
            {
                var usage = GetGpuUsageByProcess();
                var now = DateTime.Now;
                foreach (var kv in usage.Where(k => k.Value >= 10))
                {
                    if (!_usageStart.ContainsKey(kv.Key))
                    {
                        _usageStart[kv.Key] = now;
                    }
                    else if (now - _usageStart[kv.Key] >= TimeSpan.FromMinutes(2))
                    {
                        var info = GetProcessInfo(kv.Key);
                        if (info != null && !_knownPaths.Contains(info.Value.path))
                        {
                            _knownPaths.Add(info.Value.path);
                            Save();
                            await SendToApi(info.Value);
                        }
                    }
                }

                // remove entries that are no longer above threshold
                foreach (var pid in _usageStart.Keys.ToList())
                {
                    if (!usage.ContainsKey(pid) || usage[pid] < 10)
                        _usageStart.Remove(pid);
                }
            }
            catch { }
        }

        private static Dictionary<int, double> GetGpuUsageByProcess()
        {
            var result = new Dictionary<int, double>();
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "root\\CIMV2",
                    "SELECT * FROM Win32_PerfFormattedData_GPUPerformanceCounters_GPUEngine");

                foreach (ManagementObject obj in searcher.Get())
                {
                    if (obj["IDProcess"] == null)
                        continue;

                    if (!int.TryParse(obj["IDProcess"].ToString(), out int pid))
                        continue;

                    double pct = 0;

                    if (obj.Properties["PercentGPUTime"] != null &&
                        double.TryParse(obj["PercentGPUTime"].ToString(), out var p1))
                    {
                        pct = p1;
                    }
                    else if (obj.Properties["UtilizationPercentage"] != null &&
                        double.TryParse(obj["UtilizationPercentage"].ToString(), out var p2))
                    {
                        pct = p2;
                    }

                    if (pct > 0)
                    {
                        if (!result.ContainsKey(pid))
                            result[pid] = 0;
                        result[pid] += pct;
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    var logDir = Path.GetDirectoryName(StorePath)!;
                    if (!Directory.Exists(logDir))
                        Directory.CreateDirectory(logDir);
                    File.AppendAllText(Path.Combine(logDir, "GpuMonitor.log"), $"{DateTime.Now:u} {ex}\n");
                }
                catch { }
            }

            return result;
        }

        private static (string exe, string path)? GetProcessInfo(int pid)
        {
            try
            {
                using var proc = Process.GetProcessById(pid);
                var path = proc.MainModule?.FileName;
                if (string.IsNullOrWhiteSpace(path))
                    return null;
                return (Path.GetFileName(path), path);
            }
            catch
            {
                return null;
            }
        }

        private static async Task SendToApi((string exe, string path) info)
        {
            try
            {
                using var http = new HttpClient();
                var payload = JsonSerializer.Serialize(new { exe = info.exe, path = info.path });
                await http.PostAsync(
                    "https://gametrydivision.com/api/gtdcompanion/getexetocheck",
                    new StringContent(payload, Encoding.UTF8, "application/json"));
            }
            catch { }
        }
    }
}
