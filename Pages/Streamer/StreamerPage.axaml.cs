using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace GTDCompanion.Pages
{
    public partial class StreamerPage : UserControl
    {
        private ChatOverlay? _overlay;

        public StreamerPage()
        {
            InitializeComponent();
            var cfg = GTDConfigHelper.LoadStreamerConfig();
            UseYoutubeCheck.IsChecked = cfg.UseYouTube;
            UseTwitchCheck.IsChecked = cfg.UseTwitch;
            YoutubeLinkBox.Text = cfg.YoutubeLink;
            TwitchSlugBox.Text = cfg.TwitchSlug;
            OpacitySlider.Value = cfg.OverlayOpacity;
            FontSizeSlider.Value = cfg.FontSize;

            UseYoutubeCheck.Checked += (_, __) => Save();
            UseYoutubeCheck.Unchecked += (_, __) => Save();
            UseTwitchCheck.Checked += (_, __) => Save();
            UseTwitchCheck.Unchecked += (_, __) => Save();
            YoutubeLinkBox.PropertyChanged += (_, e) => { if (e.Property.Name == "Text") Save(); };
            TwitchSlugBox.PropertyChanged += (_, e) => { if (e.Property.Name == "Text") Save(); };
            OpacitySlider.PropertyChanged += (_, e) => { if (e.Property.Name == "Value") Save(); };
            FontSizeSlider.PropertyChanged += (_, e) => { if (e.Property.Name == "Value") Save(); };

            OpenOverlayButton.Click += OpenOverlayButton_Click;
        }

        private void Save()
        {
            var cfg = new StreamerConfig
            {
                UseYouTube = UseYoutubeCheck.IsChecked ?? false,
                UseTwitch = UseTwitchCheck.IsChecked ?? false,
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
