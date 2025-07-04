using Avalonia.Controls;
using GTDCompanion.Helpers;
using System.Linq;
using System;
using WindowsInput.Native;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace GTDCompanion.Pages
{
    public partial class KeyboardMouseStatsPage : UserControl
    {
        private readonly DispatcherTimer _updateTimer;

        public KeyboardMouseStatsPage()
        {
            InitializeComponent();
            StatsTracker.StatsUpdated += () => UpdateStats();
            UpdateStats();
            _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _updateTimer.Tick += (_, __) => UpdateStats();
            _updateTimer.Start();
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

            double meters = s.ScrollTicks * 0.025;
            ScrollText.Text = $"Scroll: {meters:F2} m";
            double dist = s.MousePixelsMoved * 0.00020;
            DistanceText.Text = $"Distância: {dist:F3} m";
            var top3 = s.KeyCounts.OrderByDescending(kv => kv.Value).Take(3)
                .Select(kv =>
                {
                    var name = System.Enum.GetName(typeof(WindowsInput.Native.VirtualKeyCode), kv.Key);
                    if (name != null && name.StartsWith("VK_"))
                        name = name.Substring(3);
                    return $"{name} ({kv.Value})";
                });
            TopKeysText.Text = "Top 3 teclas: " + string.Join(", ", top3);

            TimeSpan up = DateTime.Now - StatsTracker.StartTime;
            UptimeText.Text = $"Tempo de atividade: {FormatTimeSpan(up)}";
            IdleText.Text = $"Tempo ocioso: {FormatTimeSpan(s.IdleTime)}";
            var totalActive = StatsTracker.GetTotalActiveTime();
            ActiveTimeText.Text = $"Ativo geral: {FormatTimeSpan(totalActive, false)}";
            TimeSpan sinceMaint = DateTime.Now - s.LastMaintenance;
            MaintenanceText.Text = $"Desde manutenção: {FormatTimeSpan(sinceMaint)}";
        }

        private static string FormatTimeSpan(TimeSpan ts, bool showSeconds = true)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (ts.Days > 0)
                parts.Add($"{ts.Days}d");
            if (ts.Hours > 0)
                parts.Add($"{ts.Hours}h");
            if (ts.Minutes > 0)
                parts.Add($"{ts.Minutes}m");
            if (showSeconds && (parts.Count == 0 || ts.Seconds > 0))
                parts.Add($"{ts.Seconds}s");
            return string.Join(" ", parts);
        }

        private void ResetMaintenance_Click(object? sender, RoutedEventArgs e)
        {
            StatsTracker.ResetMaintenance();
        }
    }
}
