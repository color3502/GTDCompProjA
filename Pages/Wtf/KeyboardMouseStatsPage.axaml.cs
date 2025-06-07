using Avalonia.Controls;
using GTDCompanion.Helpers;
using System.Linq;
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
