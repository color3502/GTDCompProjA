using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System;
using System.Runtime.InteropServices;

namespace GTDCompanion.Pages
{
    public partial class MacroOverlay : Window
    {
        public int StepIndex { get; private set; }
        private bool dragging = false;
        private PixelPoint dragStartWindow;
        private PixelPoint dragStartMouseScreen;

        public event Action<int, int, int>? PositionUpdated;

        public MacroOverlay(int stepIndex, int? x = null, int? y = null)
        {
            InitializeComponent();

            StepIndex = stepIndex;
            NumeroPasso.Text = (stepIndex + 1).ToString();

            PointerPressed += OnPointerPressed;
            PointerReleased += OnPointerReleased;
            PointerMoved += OnPointerMoved;

            EnableTransparency();

            // Se vier x/y absolutos, posicione o centro do overlay em x/y
            if (x.HasValue && y.HasValue)
            {
                int px = x.Value - (int)(Width / 2);
                int py = y.Value - (int)(Height / 2);
                Position = new PixelPoint(px, py);
            }
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            dragging = true;
            dragStartWindow = this.Position;
            dragStartMouseScreen = this.PointToScreen(e.GetPosition(this));
            Cursor = new Cursor(StandardCursorType.Hand);
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (dragging)
            {
                var mouseScreenNow = this.PointToScreen(e.GetPosition(this));
                var dx = (int)(mouseScreenNow.X - dragStartMouseScreen.X);
                var dy = (int)(mouseScreenNow.Y - dragStartMouseScreen.Y);
                Position = new PixelPoint(dragStartWindow.X + dx, dragStartWindow.Y + dy);
            }
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            dragging = false;
            this.Cursor = new Cursor(StandardCursorType.Arrow);

            var screenX = Position.X + (int)(Width / 2);
            var screenY = Position.Y + (int)(Height / 2);
            PositionUpdated?.Invoke(StepIndex, screenX, screenY);
        }

        public void UpdateStepNumber(int newStepNumber)
        {
            NumeroPasso.Text = newStepNumber.ToString();
        }

        public int CenterScreenX() => Position.X + (int)(Width / 2);
        public int CenterScreenY() => Position.Y + (int)(Height / 2);

        #region Click-Through Support (WinAPI)
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hwnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int nIndex, int dwNewLong);

        const int GWL_EXSTYLE = -20;
        const int WS_EX_LAYERED = 0x80000;
        const int WS_EX_TRANSPARENT = 0x20;

        public void SetClickThrough(bool enable)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var handle = this.TryGetPlatformHandle();
                if (handle != null)
                {
                    IntPtr hwnd = handle.Handle;
                    int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

                    if (enable)
                        exStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT;
                    else
                        exStyle &= ~WS_EX_TRANSPARENT;

                    SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
                }
            }
        }

        private void EnableTransparency()
        {
            TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
            Background = Brushes.Transparent;
            SystemDecorations = SystemDecorations.None;
            Topmost = true;
        }
        #endregion
    }
}