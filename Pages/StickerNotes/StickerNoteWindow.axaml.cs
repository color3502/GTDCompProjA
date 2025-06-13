using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System; // Needed for Action-based Subscribe
using GTDCompanion.Helpers;

namespace GTDCompanion.Pages
{
    public partial class StickerNoteWindow : Window
    {
        private bool dragging;
        private PixelPoint dragOffset;
        private readonly int index;
        private bool _collapsed = false;
        private double _originalHeight = 0;
        private readonly double lineHeight;
        private readonly double windowOffset;

        public StickerNoteWindow(int index)
        {
            this.index = index;
            InitializeComponent();

            if (this.FindControl<Button>("CopyButton") is Button copyBtn)
            {
                copyBtn.Click += CopyButton_Click;
            }

            lineHeight = NoteTextBox.Height / 4;
            windowOffset = Height - NoteTextBox.Height;

            var cfg = StickerNoteStorage.Load(index);
            NoteTextBox.Text = cfg.Text;
            UpdateWindowTitle(cfg.Text);
            Opacity = cfg.Opacity;
            TransparencySlider.Value = cfg.Opacity;
            if (cfg.PosX >= 0 && cfg.PosY >= 0)
                Position = new PixelPoint(cfg.PosX, cfg.PosY);
            AdjustSize(cfg.Text);

            TransparencySlider.PropertyChanged += (_, e) =>
            {
                if (e.Property.Name == "Value")
                {
                    var v = System.Math.Round(TransparencySlider.Value, 2);
                    Opacity = v;
                    SaveConfig();
                }
            };

            NoteTextBox.GetObservable(TextBox.TextProperty).Subscribe(text =>
            {
                SaveConfig();
                UpdateWindowTitle(text ?? string.Empty);
                AdjustSize(text ?? string.Empty);
            });

            PointerPressed += OnPointerPressed;
            PointerReleased += OnPointerReleased;
            PointerMoved += OnPointerMoved;

            // Handle double click on the title bar to collapse/expand the overlay
            // Using the window's PointerPressed ensures the event fires even if
            // the DockPanel has no background set.
            this.PointerPressed += CustomTitleBar_PointerPressed;

            _originalHeight = Height;
        }

        private void SaveConfig()
        {
            var data = new StickerNoteData
            {
                Text = NoteTextBox.Text ?? string.Empty,
                Opacity = Opacity,
                PosX = Position.X,
                PosY = Position.Y
            };
            StickerNoteStorage.Save(index, data);
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
            SaveConfig();
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (dragging)
            {
                var screenPos = this.PointToScreen(e.GetPosition(this));
                Position = new PixelPoint(screenPos.X - dragOffset.X, screenPos.Y - dragOffset.Y);
            }
        }

        private async void CopyButton_Click(object? sender, RoutedEventArgs e)
        {
            if (this.Clipboard != null)
                await this.Clipboard.SetTextAsync(NoteTextBox.Text ?? string.Empty);
        }

        private void UpdateWindowTitle(string text)
        {
            var sanitized = text.Replace("\n", " ").Replace("\r", " ");
            if (sanitized.Length > 20)
                sanitized = sanitized.Substring(0, 20);
            Title = sanitized;
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
                _originalHeight = this.Height;
                this.Height = 45;
                _collapsed = true;
            }
            else
            {
                this.Height = _originalHeight;
                _collapsed = false;
            }
        }

        private void AdjustSize(string text)
        {
            var lines = text.Split('\n').Length;
            if (lines < 4) lines = 4;
            if (lines > 20) lines = 20;
            var newHeight = lineHeight * lines;
            if (Math.Abs(NoteTextBox.Height - newHeight) > 0.1)
            {
                NoteTextBox.Height = newHeight;
                Height = windowOffset + newHeight;
            }
        }
    }

    public class StickerNoteConfig
    {
        public string Text { get; set; } = string.Empty;
        public double Opacity { get; set; } = 0.9;
        public int PosX { get; set; } = -1;
        public int PosY { get; set; } = -1;
    }
}
