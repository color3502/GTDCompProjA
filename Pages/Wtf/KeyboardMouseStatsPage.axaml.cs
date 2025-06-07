using Avalonia.Controls;
using GTDCompanion.Helpers;
using System.Linq;

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
            double meters = s.ScrollTicks * 0.01;
            ScrollText.Text = $"Scroll: {meters:F2} m";
            var top3 = s.KeyCounts.OrderByDescending(kv => kv.Value).Take(3)
                .Select(kv => $"{(Avalonia.Input.Key)kv.Key} ({kv.Value})");
            TopKeysText.Text = "Top 3 teclas: " + string.Join(", ", top3);
        }
    }
}
