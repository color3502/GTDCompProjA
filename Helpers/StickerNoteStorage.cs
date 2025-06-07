using System;
using System.IO;
using System.Text.Json;

namespace GTDCompanion.Helpers
{
    public static class StickerNoteStorage
    {
        private static readonly string DataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GTDCompanion");

        public static StickerNoteData Load(int index)
        {
            var path = Path.Combine(DataDir, $"note{index}.json");
            if (!File.Exists(path))
                return new StickerNoteData();
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<StickerNoteData>(json) ?? new StickerNoteData();
        }

        public static void Save(int index, StickerNoteData data)
        {
            if (!Directory.Exists(DataDir))
                Directory.CreateDirectory(DataDir);
            var path = Path.Combine(DataDir, $"note{index}.json");
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }

    public class StickerNoteData
    {
        public string Text { get; set; } = string.Empty;
        public double Opacity { get; set; } = 0.9;
        public int PosX { get; set; } = -1;
        public int PosY { get; set; } = -1;
    }
}
