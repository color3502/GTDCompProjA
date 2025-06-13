using System;
using System.IO;
using System.Text.Json;

namespace GTDCompanion.Helpers
{
    public static class StickerNoteStorage
    {
        private const int NoteCount = 10;
        private static readonly string DataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GTDCompanion");
        private static readonly string NotesPath = Path.Combine(DataDir, "notes.json");

        private static StickerNoteData[] LoadAll()
        {
            if (!File.Exists(NotesPath))
                return CreateDefaultArray();

            try
            {
                var json = File.ReadAllText(NotesPath);
                var arr = JsonSerializer.Deserialize<StickerNoteData[]>(json);
                if (arr != null && arr.Length == NoteCount)
                    return arr;
            }
            catch { }

            return CreateDefaultArray();
        }

        private static void SaveAll(StickerNoteData[] data)
        {
            if (!Directory.Exists(DataDir))
                Directory.CreateDirectory(DataDir);

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(NotesPath, json);
        }

        private static StickerNoteData[] CreateDefaultArray()
        {
            var arr = new StickerNoteData[NoteCount];
            for (int i = 0; i < NoteCount; i++)
                arr[i] = new StickerNoteData();
            return arr;
        }

        public static StickerNoteData Load(int index)
        {
            var all = LoadAll();
            if (index < 1 || index > NoteCount) return new StickerNoteData();
            return all[index - 1];
        }

        public static void Save(int index, StickerNoteData data)
        {
            if (index < 1 || index > NoteCount) return;
            var all = LoadAll();
            all[index - 1] = data;
            SaveAll(all);
        }
    }

    public class StickerNoteData
    {
        public string Text { get; set; } = string.Empty;
        public double Opacity { get; set; } = 0.9;
        public int PosX { get; set; } = -1;
        public int PosY { get; set; } = -1;
        public string Title { get; set; } = string.Empty;
    }
}
