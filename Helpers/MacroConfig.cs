using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using GTDCompanion.Helpers;

namespace GTDCompanion.Helpers
{
    public class MacroConfig
    {
        public static async Task SaveMacroAsync(List<MacroStep> steps, string filePath)
        {
            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(steps, jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }

        public static async Task<List<MacroStep>> LoadMacroAsync(string filePath)
        {
            if (!File.Exists(filePath))
                return new List<MacroStep>();

            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<MacroStep>>(json) ?? new List<MacroStep>();
        }
    }

    public class MacroStep
    {
        public string Tipo { get; set; } = "Clique";
        public int X { get; set; }
        public int Y { get; set; }
        public string Botao { get; set; } = "Left";
        public int Cliques { get; set; } = 1;
        public string AlvoTipo { get; set; } = "Ponto";
        public string Teclas { get; set; } = "";
        public double Delay { get; set; } = 0.2;
        public int Repeticoes { get; set; } = 1;
    }
}
