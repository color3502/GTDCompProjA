using IniParser;
using IniParser.Model;
using System.IO;
using System;
using Avalonia.Media;
using System.Globalization;
using GTDCompanion.Pages;

namespace GTDCompanion
{
    public static class GTDTranslatorEvents
        {
            // Define o evento
            public static event Action? IdiomaAlterado;

            // Chama este método sempre que o idioma for alterado
            public static void NotificarIdiomaAlterado()
            {
                IdiomaAlterado?.Invoke();
            }
    }
    public static class GTDConfigHelper
        {

        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GTDCompanion", "GTDConfig.ini");

        // --- Tradução: métodos genéricos para salvar/carregar config por seção e chave ---

        public static string Get(string section, string key, string fallback = "")
        {
            if (!File.Exists(ConfigPath)) return fallback;
            var parser = new FileIniDataParser();
            var data = parser.ReadFile(ConfigPath);
            if (data[section] is null || data[section][key] is null) return fallback;
            return data[section][key];
        }

        public static void Set(string section, string key, string value)
        {
            var dir = Path.GetDirectoryName(ConfigPath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var parser = new FileIniDataParser();
            var data = File.Exists(ConfigPath) ? parser.ReadFile(ConfigPath) : new IniData();
            if (data[section] is null) data.Sections.AddSection(section);
            data[section][key] = value ?? "";
            parser.WriteFile(ConfigPath, data);
        }

        public static bool GetBool(string section, string key, bool fallback = false)
        {
            var str = Get(section, key, fallback ? "true" : "false");
            return str.Equals("1") || str.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        public static int GetInt(string section, string key, int fallback = 0)
        {
            var str = Get(section, key, fallback.ToString());
            return int.TryParse(str, out var i) ? i : fallback;
        }

        public static double GetDouble(string section, string key, double fallback = 1.0)
        {
            var str = Get(section, key, fallback.ToString(CultureInfo.InvariantCulture));
            return double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : fallback;
        }

        public static string GetString(string section, string key, string fallback = "")
        {
            var str = Get(section, key, fallback.ToString());
            return str ?? "";
        }

        // ---- (Se quiser suporte para outras configs customizadas, mantenha abaixo os seus) ----

        public static MiraConfig LoadMiraConfig()
        {
            var config = new MiraConfig
            {
                Modelo = 0,
                Cor = Color.Parse("#00FFFF"),
                Tamanho = 60,
                Espessura = 4,
                Alpha = 255
            };

            if (!File.Exists(ConfigPath))
                return config;

            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile(ConfigPath);

            var miraSection = data["Mira"];
            if (miraSection != null)
            {
                int.TryParse(miraSection["Modelo"], out config.Modelo);
                if (!string.IsNullOrWhiteSpace(miraSection["Cor"]))
                    config.Cor = Color.Parse(miraSection["Cor"]);
                int.TryParse(miraSection["Tamanho"], out config.Tamanho);
                int.TryParse(miraSection["Espessura"], out config.Espessura);
                byte.TryParse(miraSection["Alpha"], out config.Alpha);
            }
            return config;
        }

        public static void SaveMiraConfig(MiraConfig config)
        {
            var dir = Path.GetDirectoryName(ConfigPath)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var parser = new FileIniDataParser();
            IniData data = File.Exists(ConfigPath) ? parser.ReadFile(ConfigPath) : new IniData();

            data["Mira"]["Modelo"] = config.Modelo.ToString();
            data["Mira"]["Cor"] = config.Cor.ToString();
            data["Mira"]["Tamanho"] = config.Tamanho.ToString();
            data["Mira"]["Espessura"] = config.Espessura.ToString();
            data["Mira"]["Alpha"] = config.Alpha.ToString();

            parser.WriteFile(ConfigPath, data);
        }

        public static string LoadGtdId()
        {
            var path = ConfigPath;
            if (!File.Exists(path)) return "";
            var parser = new FileIniDataParser();
            var data = parser.ReadFile(path);
            return data["Perfil"]["GtdId"] ?? "";
        }

        public static void SaveGtdId(string gtdId)
        {
            var path = ConfigPath;
            var dir = Path.GetDirectoryName(path)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var parser = new FileIniDataParser();
            var data = File.Exists(path) ? parser.ReadFile(path) : new IniData();
            data["Perfil"]["GtdId"] = gtdId ?? "";
            parser.WriteFile(path, data);
        }

        // ---- Sticker Notes ----
        public static StickerNoteConfig LoadStickerNoteConfig(int index)
        {
            string section = $"StickerNote{index}";
            return new StickerNoteConfig
            {
                Text = GetString(section, "Text", ""),
                Opacity = GetDouble(section, "Opacity", 0.9),
                PosX = GetInt(section, "PosX", -1),
                PosY = GetInt(section, "PosY", -1)
            };
        }

        public static void SaveStickerNoteConfig(int index, StickerNoteConfig config)
        {
            string section = $"StickerNote{index}";
            Set(section, "Text", config.Text);
            Set(section, "Opacity", config.Opacity.ToString(CultureInfo.InvariantCulture));
            Set(section, "PosX", config.PosX.ToString());
            Set(section, "PosY", config.PosY.ToString());
        }
    }
}
