using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace GTDCompanion.Pages
{
    public partial class ChatOverlay : Window
    {
        private readonly StreamerConfig _config;
        private readonly ObservableCollection<ChatMessage> _messages = new();
        private CancellationTokenSource? _cts;
        
        public void UpdateAppearance(double opacity, int fontSize)
        {
            Dispatcher.UIThread.Post(() =>
            {
                Opacity = opacity;
                FontSize = fontSize;
            });
        }
        private bool dragging;
        private PixelPoint dragOffset;
        private bool _collapsed;
        private double _originalHeight;

        public event Action<string>? ConnectionFailed;

        public ChatOverlay(StreamerConfig cfg)
        {
            _config = cfg;
            InitializeComponent();
            MessagesList.ItemsSource = _messages;
            Opacity = cfg.OverlayOpacity;
            Height = 400;
            FontSize = cfg.FontSize;
            CustomTitleBar.PointerPressed += CustomTitleBar_PointerPressed;
            PointerPressed += OnPointerPressed;
            PointerReleased += OnPointerReleased;
            PointerMoved += OnPointerMoved;
            if (this.FindControl<Button>("CloseButton") is Button closeBtn)
                closeBtn.Click += (_, __) => Close();
            _originalHeight = Height;
            Opened += async (_, __) => await StartAsync();
            Closed += (_, __) => _cts?.Cancel();
        }

        private void CustomTitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                return;
            if (e.ClickCount == 2)
            {
                var pos = e.GetPosition(this);
                if (pos.Y <= 30)
                    ToggleCollapse();
            }
        }

        private void ToggleCollapse()
        {
            if (!_collapsed)
            {
                _originalHeight = Height;
                Height = 30;
                _collapsed = true;
            }
            else
            {
                Height = _originalHeight;
                _collapsed = false;
            }
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                dragging = true;
                var screenPos = this.PointToScreen(e.GetPosition(this));
                dragOffset = new PixelPoint(screenPos.X - Position.X, screenPos.Y - Position.Y);
                e.Pointer.Capture(this);
            }
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            dragging = false;
            e.Pointer.Capture(null);
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (dragging)
            {
                var screenPos = this.PointToScreen(e.GetPosition(this));
                Position = new PixelPoint(screenPos.X - dragOffset.X, screenPos.Y - dragOffset.Y);
            }
        }

        private async Task StartAsync()
        {
            _cts = new CancellationTokenSource();
            bool any = false;
            if (_config.UseTwitch)
            {
                any = true;
                _ = Task.Run(() => RunTwitchAsync(_config.TwitchSlug, _cts.Token));
            }
            if (_config.UseYouTube)
            {
                any = true;
                _ = Task.Run(() => RunYoutubeAsync(_config.YoutubeLink, _cts.Token));
            }
            if (!any)
            {
                ConnectionFailed?.Invoke("None");
                Close();
            }
            await Task.CompletedTask;
        }

        private string NormalizeTwitchSlug(string slug)
        {
            slug = slug.Trim();
            if (slug.Contains("twitch.tv", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var uri = new Uri(slug);
                    slug = uri.AbsolutePath.Trim('/');
                }
                catch { }
            }
            if (slug.StartsWith("@"))
                slug = slug.Substring(1);
            if (slug.StartsWith("#"))
                slug = slug.Substring(1);
            return slug;
        }

        private async Task RunTwitchAsync(string slug, CancellationToken token)
        {
            slug = NormalizeTwitchSlug(slug);
            if (string.IsNullOrWhiteSpace(slug))
            {
                ConnectionFailed?.Invoke("Twitch");
                return;
            }
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync("irc.chat.twitch.tv", 6667, token);
                using var stream = client.GetStream();
                var writer = new StreamWriter(stream) { AutoFlush = true };
                var reader = new StreamReader(stream);
                await writer.WriteAsync("PASS SCHMOOPIIE\r\n");
                await writer.WriteAsync("NICK justinfan12345\r\n");
                await writer.WriteAsync($"JOIN #{slug}\r\n");
                _ = Task.Run(async () =>
                {
                    char[] buffer = new char[1];
                    while (!token.IsCancellationRequested && !reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync() ?? string.Empty;
                        if (line.StartsWith("PING"))
                        {
                            await writer.WriteAsync(line.Replace("PING", "PONG") + "\r\n");
                            continue;
                        }
                        var idx = line.IndexOf("PRIVMSG");
                        if (idx > -1)
                        {
                            var userEnd = line.IndexOf('!', 1);
                            var user = line.Substring(1, userEnd - 1);
                            var msg = line[(line.IndexOf(':', idx) + 1)..];
                            bool donation = msg.Contains("cheer", StringComparison.OrdinalIgnoreCase) ||
                                            msg.Contains("subscribed", StringComparison.OrdinalIgnoreCase);
                            AddMessage(user, msg, donation, "Twitch");
                        }
                    }
                }, token);
            }
            catch
            {
                ConnectionFailed?.Invoke("Twitch");
            }
        }

        private string? ExtractVideoId(string url)
        {
            try
            {
                var uri = new Uri(url);
                var query = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in query)
                {
                    var kv = part.Split('=');
                    if (kv.Length == 2 && kv[0] == "v")
                        return kv[1];
                }
            }
            catch { }
            return null;
        }

        private async Task RunYoutubeAsync(string link, CancellationToken token)
        {
            string apiKey = Environment.GetEnvironmentVariable("YOUTUBE_API_KEY") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(link) || string.IsNullOrWhiteSpace(apiKey))
            {
                ConnectionFailed?.Invoke("YouTube");
                return;
            }
            string? videoId = ExtractVideoId(link);
            if (videoId == null)
            {
                ConnectionFailed?.Invoke("YouTube");
                return;
            }
            using var http = new HttpClient();
            try
            {
                var vidUrl = $"https://www.googleapis.com/youtube/v3/videos?part=liveStreamingDetails&id={videoId}&key={apiKey}";
                var vidJson = await http.GetStringAsync(vidUrl, token);
                using var vd = JsonDocument.Parse(vidJson);
                var liveId = vd.RootElement.GetProperty("items")[0].GetProperty("liveStreamingDetails").GetProperty("activeLiveChatId").GetString();
                if (string.IsNullOrWhiteSpace(liveId))
                {
                    ConnectionFailed?.Invoke("YouTube");
                    return;
                }
                string pageToken = string.Empty;
                while (!token.IsCancellationRequested)
                {
                    string chatUrl = $"https://www.googleapis.com/youtube/v3/liveChat/messages?liveChatId={liveId}&part=snippet,authorDetails&key={apiKey}&pageToken={pageToken}";
                    var chatJson = await http.GetStringAsync(chatUrl, token);
                    using var cd = JsonDocument.Parse(chatJson);
                    if (cd.RootElement.TryGetProperty("nextPageToken", out var np))
                        pageToken = np.GetString() ?? string.Empty;
                    foreach (var item in cd.RootElement.GetProperty("items").EnumerateArray())
                    {
                        var author = item.GetProperty("authorDetails").GetProperty("displayName").GetString() ?? "";
                        var msg = item.GetProperty("snippet").GetProperty("displayMessage").GetString() ?? "";
                        bool donation = item.GetProperty("snippet").TryGetProperty("superChatDetails", out _) ||
                                        item.GetProperty("snippet").TryGetProperty("superStickerDetails", out _);
                        AddMessage(author, msg, donation, "YouTube");
                    }
                    await Task.Delay(2000, token);
                }
            }
            catch
            {
                ConnectionFailed?.Invoke("YouTube");
            }
        }

        private void AddMessage(string user, string text, bool donation, string source)
        {
            var msg = new ChatMessage
            {
                User = user,
                Text = text,
                DonationShadow = donation ? 1.0 : 0.0,
                SourceIcon = source == "Twitch" ? "🟣" : "🔴"
            };
            Dispatcher.UIThread.Post(() =>
            {
                _messages.Add(msg);
                Scroll.ScrollToEnd();
            });
        }
    }

    public class ChatMessage
    {
        public string User { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public double DonationShadow { get; set; } = 0.0;
        public string SourceIcon { get; set; } = string.Empty;
    }
}
