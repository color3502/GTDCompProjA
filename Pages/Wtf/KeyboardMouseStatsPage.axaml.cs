using Avalonia.Controls;
using GTDCompanion.Helpers;
using System.Linq;
using System;
using WindowsInput.Native;

namespace GTDCompanion.Pages
{
    public partial class KeyboardMouseStatsPage : UserControl
    {
        public KeyboardMouseStatsPage()
        {
            InitializeComponent();
            StatsTracker.StatsUpdated += () => UpdateStats();
            UpdateStats();
        }

        private void UpdateStats()
        {
            var s = StatsTracker.Stats;
            KeyPressText.Text = $"Teclas pressionadas: {s.KeyPresses}";
            LeftClickText.Text = $"Cliques esquerdo: {s.LeftClicks}";
            RightClickText.Text = $"Cliques direito: {s.RightClicks}";

            var todayKey = DateTime.Now.ToString("yyyy-MM-dd");
            s.DailyKeyPresses.TryGetValue(todayKey, out int todayKeys);
            TodayKeysText.Text = $"Teclas hoje: {todayKeys}";
            s.DailyClicks.TryGetValue(todayKey, out int todayClicks);
            TodayClicksText.Text = $"Cliques hoje: {todayClicks}";

            DateTime startWeek = DateTime.Now.Date.AddDays(-6);
            int weekKeys = s.DailyKeyPresses
                .Where(kv => DateTime.TryParse(kv.Key, out var d) && d >= startWeek)
                .Sum(kv => kv.Value);
            WeekKeysText.Text = $"Últimos 7 dias: {weekKeys}";
            int weekClicks = s.DailyClicks
                .Where(kv => DateTime.TryParse(kv.Key, out var d) && d >= startWeek)
                .Sum(kv => kv.Value);
            WeekClicksText.Text = $"Últimos 7 dias: {weekClicks}";

            DateTime startYear = DateTime.Now.Date.AddMonths(-12);
            int yearKeys = s.DailyKeyPresses
                .Where(kv => DateTime.TryParse(kv.Key, out var d) && d >= startYear)
                .Sum(kv => kv.Value);
            YearKeysText.Text = $"Últimos 12 meses: {yearKeys}";
            int yearClicks = s.DailyClicks
                .Where(kv => DateTime.TryParse(kv.Key, out var d) && d >= startYear)
                .Sum(kv => kv.Value);
            YearClicksText.Text = $"Últimos 12 meses: {yearClicks}";

            double meters = s.ScrollTicks * 0.01;
            ScrollText.Text = $"Scroll: {meters:F2} m";
            var top3 = s.KeyCounts.OrderByDescending(kv => kv.Value).Take(3)
                .Select(kv =>
                {
                    var name = System.Enum.GetName(typeof(WindowsInput.Native.VirtualKeyCode), kv.Key);
                    if (name != null && name.StartsWith("VK_"))
                        name = name.Substring(3);
                    return $"{name} ({kv.Value})";
                });
            TopKeysText.Text = "Top 3 teclas: " + string.Join(", ", top3);
        }
    }
}
