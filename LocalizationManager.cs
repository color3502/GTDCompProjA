using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace GTDCompanion
{
    public static class LocalizationManager
    {
        private static readonly Dictionary<string, string> _strings = new();
        private const string DefaultCulture = "pt-BR";

        static LocalizationManager()
        {
            LoadCulture(null);
        }

        public static void LoadCulture(string? cultureName)
        {
            if (string.IsNullOrWhiteSpace(cultureName))
                cultureName = CultureInfo.InstalledUICulture.Name;

            var asm = Assembly.GetExecutingAssembly();
            var candidates = new List<string>
            {
                $"GTDCompanion.Locales.{cultureName}.json",
                $"GTDCompanion.Locales.{cultureName.Split('-')[0]}.json",
                $"GTDCompanion.Locales.{DefaultCulture}.json"
            };

            foreach (var resource in candidates)
            {
                using Stream? s = asm.GetManifestResourceStream(resource);
                if (s != null)
                {
                    using var reader = new StreamReader(s);
                    var json = reader.ReadToEnd();
                    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (data != null)
                    {
                        _strings.Clear();
                        foreach (var kv in data)
                            _strings[kv.Key] = kv.Value;
                        return;
                    }
                }
            }
        }

        public static string Get(string key)
        {
            return _strings.TryGetValue(key, out var value) ? value : key;
        }
    }
}
