using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using GTDCompanion; // for GTDConfigHelper

namespace GTDCompanion.Helpers
{
    public static class ProcessMonitor
    {
        private class ProcInfo
        {
            public string ProcessName { get; set; } = string.Empty;
            public string FileName { get; set; } = string.Empty;
            public string FilePath { get; set; } = string.Empty;
            public string? GtdId { get; set; }
        }

        private static readonly string KnownPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GTDCompanion", "KnownProcesses.json");

        private static readonly HashSet<string> Known = new(StringComparer.OrdinalIgnoreCase);

        private static readonly string[] GraphicsLibraries = new[]
        {
            "d3d9.dll",
            "d3d10.dll",
            "d3d11.dll",
            "d3d12.dll",
            "vulkan-1.dll",
            "opengl32.dll"
        };
        private static Timer? _timer;

        static ProcessMonitor()
        {
            LoadKnown();
        }

        private static void LoadKnown()
        {
            try
            {
                if (File.Exists(KnownPath))
                {
                    var json = File.ReadAllText(KnownPath);
                    var arr = JsonSerializer.Deserialize<List<ProcInfo>>(json);
                    if (arr != null)
                    {
                        foreach (var p in arr)
                        {
                            if (!string.IsNullOrWhiteSpace(p.FilePath))
                                Known.Add(p.FilePath);
                        }
                    }
                }
            }
            catch { }
        }

        private static void SaveKnown(List<ProcInfo> list)
        {
            try
            {
                var dir = Path.GetDirectoryName(KnownPath)!;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(KnownPath, json);
            }
            catch { }
        }

        private static string GetDisplayName(Process proc)
        {
            try
            {
                var desc = proc.MainModule?.FileVersionInfo?.FileDescription;
                if (!string.IsNullOrWhiteSpace(desc))
                    return desc;
            }
            catch { }
            return proc.ProcessName;
        }

        private static bool ProcessUsesGraphics(Process proc)
        {
            try
            {
                foreach (ProcessModule module in proc.Modules)
                {
                    var name = module.ModuleName;
                    if (string.IsNullOrEmpty(name))
                        continue;
                    foreach (var lib in GraphicsLibraries)
                    {
                        if (string.Equals(name, lib, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
            }
            catch { }
            return false;
        }

        public static void Start()
        {
            if (!OperatingSystem.IsWindows())
                return;
            if (_timer != null)
                return;
            _timer = new Timer(_ => CheckProcesses(), null, 0, 5000);
        }

        public static void Stop()
        {
            _timer?.Dispose();
            _timer = null;
        }

        private static void CheckProcesses()
        {
            var newList = new List<ProcInfo>();
            var gtdIdValue = GTDConfigHelper.LoadGtdId();
            string? gtdId = string.IsNullOrWhiteSpace(gtdIdValue) ? null : gtdIdValue;
            try
            {
                foreach (var proc in Process.GetProcesses())
                {
                    try
                    {
                        if (proc.WorkingSet64 < 10 * 1024 * 1024)
                            continue;
                        var path = proc.MainModule?.FileName ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(path))
                            continue;
                        if (!ProcessUsesGraphics(proc))
                            continue;
                        if (Known.Contains(path))
                            continue;
                        var info = new ProcInfo
                        {
                            ProcessName = GetDisplayName(proc),
                            FileName = Path.GetFileName(path),
                            FilePath = path,
                            GtdId = gtdId
                        };
                        Known.Add(path);
                        newList.Add(info);
                    }
                    catch
                    {
                        // ignore processes we cannot access
                    }
                    finally
                    {
                        try { proc.Dispose(); } catch { }
                    }
                }

                if (newList.Count > 0)
                {
                    var existing = new List<ProcInfo>();
                    try
                    {
                        if (File.Exists(KnownPath))
                        {
                            var json = File.ReadAllText(KnownPath);
                            var arr = JsonSerializer.Deserialize<List<ProcInfo>>(json);
                            if (arr != null)
                                existing = arr;
                        }
                    }
                    catch { }

                    existing.AddRange(newList);
                    SaveKnown(existing);
                    _ = SendToApiAsync(newList);
                }
            }
            catch { }
        }

        private static async Task SendToApiAsync(List<ProcInfo> list)
        {
            try
            {
                using var http = new HttpClient();
                var json = JsonSerializer.Serialize(list);
                await http.PostAsync(
                    "https://gametrydivision.com/api/gtdcompanion/getexetocheck",
                    new StringContent(json, Encoding.UTF8, "application/json"));
            }
            catch { }
        }
    }
}
