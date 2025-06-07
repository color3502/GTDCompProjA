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

        private double Scaling => this.RenderScaling;

        public MacroOverlay(int stepIndex, int? x = null, int? y = null)
        {
            InitializeComponent();

            StepIndex = stepIndex;
            NumeroPasso.Text = (stepIndex + 1).ToString();

            PointerPressed += OnPointerPressed;
            PointerReleased += OnPointerReleased;
            PointerMoved += OnPointerMoved;

            // Habilita transparência e esconde do ALT+TAB somente após a janela
            // estar aberta, garantindo que o handle já exista.
            Opened += (_, _) => EnableTransparency();

            // Se vier x/y absolutos, posicione o centro do overlay em x/y quando abrir.
            // Caso contrário, centraliza na tela para facilitar o arraste inicial.
            if (x.HasValue && y.HasValue)
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                Opened += (_, _) => SetCenterPosition(x.Value, y.Value);
            }
            else
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }

        public void SetCenterPosition(int screenX, int screenY)
        {
            int px = (int)(screenX - (Width * Scaling) / 2);
            int py = (int)(screenY - (Height * Scaling) / 2);
            Position = new PixelPoint(px, py);
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

            var center = this.PointToScreen(new Point(Width / 2, Height / 2));
            PositionUpdated?.Invoke(StepIndex, (int)center.X, (int)center.Y);
        }

        public void UpdateStepNumber(int newStepNumber)
        {
            NumeroPasso.Text = newStepNumber.ToString();
            StepIndex = newStepNumber - 1;
        }

        public int CenterScreenX() => (int)this.PointToScreen(new Point(Width / 2, Height / 2)).X;
        public int CenterScreenY() => (int)this.PointToScreen(new Point(Width / 2, Height / 2)).Y;

        #region Click-Through Support (WinAPI)
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hwnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int nIndex, int dwNewLong);

        const int GWL_EXSTYLE = -20;
        const int WS_EX_LAYERED = 0x80000;
        const int WS_EX_TRANSPARENT = 0x20;
        const int WS_EX_TOOLWINDOW = 0x80;

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

                    exStyle |= WS_EX_TOOLWINDOW;

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

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var handle = this.TryGetPlatformHandle();
                if (handle != null)
                {
                    IntPtr hwnd = handle.Handle;
                    int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                    exStyle |= WS_EX_TOOLWINDOW;
                    SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
                }
            }
        }
        #endregion
    }
}