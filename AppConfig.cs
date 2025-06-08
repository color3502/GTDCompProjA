using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace GTDCompanion
{
    public static class AppConfig
    {
        private static readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);

        static AppConfig()
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using Stream? stream = asm.GetManifestResourceStream("GTDCompanion.AppConfig.json");
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    var json = reader.ReadToEnd();
                    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (data != null)
                    {
                        foreach (var kv in data)
                            _values[kv.Key] = kv.Value;
                    }
                }
            }
            catch
            {
                // ignore failures and leave dictionary empty
            }
        }

        public static string Get(string key) =>
            _values.TryGetValue(key, out var value) ? value : string.Empty;

        public static void PopulateEnvironment()
        {
            foreach (var kv in _values)
                Environment.SetEnvironmentVariable(kv.Key, kv.Value);
        }
    }
}
