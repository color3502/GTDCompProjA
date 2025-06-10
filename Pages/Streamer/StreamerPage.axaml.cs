using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Threading;

namespace GTDCompanion.Pages
{
    public partial class StreamerPage : UserControl
    {
        private ChatOverlay? _overlay;

        public StreamerPage()
        {
            InitializeComponent();
            var cfg = GTDConfigHelper.LoadStreamerConfig();
            PlatformCombo.SelectedIndex = cfg.Platform.Equals("Twitch", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            YoutubeLinkBox.Text = cfg.YoutubeLink;
            TwitchSlugBox.Text = cfg.TwitchSlug;
            OpacitySlider.Value = cfg.OverlayOpacity;
            FontSizeSlider.Value = cfg.FontSize;
            UpdateVisibility();

            PlatformCombo.SelectionChanged += (_, __) => { Save(); UpdateVisibility(); };
            YoutubeLinkBox.PropertyChanged += (_, e) => { if (e.Property.Name == "Text") Save(); };
            TwitchSlugBox.PropertyChanged += (_, e) => { if (e.Property.Name == "Text") Save(); };
            OpacitySlider.PropertyChanged += (_, e) => { if (e.Property.Name == "Value") Save(); };
            FontSizeSlider.PropertyChanged += (_, e) => { if (e.Property.Name == "Value") Save(); };

            OpenOverlayButton.Click += OpenOverlayButton_Click;
        }

        private void UpdateVisibility()
        {
            bool twitch = PlatformCombo.SelectedIndex == 1;
            YoutubeLabel.IsVisible = !twitch;
            YoutubeLinkBox.IsVisible = !twitch;
            YoutubeError.IsVisible = false;
            TwitchLabel.IsVisible = twitch;
            TwitchSlugBox.IsVisible = twitch;
            TwitchError.IsVisible = false;
        }

        private void Save()
        {
            var cfg = new StreamerConfig
            {
                Platform = PlatformCombo.SelectedIndex == 1 ? "Twitch" : "YouTube",
                YoutubeLink = YoutubeLinkBox.Text ?? string.Empty,
                TwitchSlug = TwitchSlugBox.Text ?? string.Empty,
                OverlayOpacity = OpacitySlider.Value,
                FontSize = (int)FontSizeSlider.Value
            };
            GTDConfigHelper.SaveStreamerConfig(cfg);
            _overlay?.UpdateAppearance(cfg.OverlayOpacity, cfg.FontSize);
        }

        private void OpenOverlayButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_overlay == null || !_overlay.IsVisible)
            {
                var cfg = GTDConfigHelper.LoadStreamerConfig();
                _overlay = new ChatOverlay(cfg);
                _overlay.ConnectionFailed += OnConnectionFailed;
                _overlay.Closed += (_, __) => { _overlay = null; };
                _overlay.Show();
            }
            else
            {
                _overlay.Activate();
            }
        }

        private void OnConnectionFailed(string platform)
        {
            if (platform == "YouTube")
                YoutubeError.Text = "Não foi possível conectar. Verifique o link.";
            else
                TwitchError.Text = "Não foi possível conectar. Verifique o slug.";
        }
    }
}
