using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Globalization;

namespace GTDCompanion.Pages
{
    public partial class StickerNoteWindow : Window
    {
        private bool dragging;
        private PixelPoint dragOffset;
        private readonly int index;

        public StickerNoteWindow(int index)
        {
            this.index = index;
            InitializeComponent();

            var cfg = GTDConfigHelper.LoadStickerNoteConfig(index);
            NoteTextBox.Text = cfg.Text;
            Opacity = cfg.Opacity;
            TransparencySlider.Value = cfg.Opacity;
            if (cfg.PosX >= 0 && cfg.PosY >= 0)
                Position = new PixelPoint(cfg.PosX, cfg.PosY);

            TransparencySlider.PropertyChanged += (_, e) =>
            {
                if (e.Property.Name == "Value")
                {
                    var v = System.Math.Round(TransparencySlider.Value, 2);
                    Opacity = v;
                    SaveConfig();
                }
            };

            NoteTextBox.GetObservable(TextBox.TextProperty).Subscribe(_ => SaveConfig());

            PointerPressed += OnPointerPressed;
            PointerReleased += OnPointerReleased;
            PointerMoved += OnPointerMoved;

            if (this.FindControl<Button>("CloseButton") is Button closeBtn)
                closeBtn.Click += (_, __) => this.Close();
        }

        private void SaveConfig()
        {
            var cfg = new StickerNoteConfig
            {
                Text = NoteTextBox.Text ?? string.Empty,
                Opacity = Opacity,
                PosX = this.Position.X,
                PosY = this.Position.Y
            };
            GTDConfigHelper.SaveStickerNoteConfig(index, cfg);
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
    }

    public class StickerNoteConfig
    {
        public string Text { get; set; } = string.Empty;
        public double Opacity { get; set; } = 0.9;
        public int PosX { get; set; } = -1;
        public int PosY { get; set; } = -1;
    }
}
