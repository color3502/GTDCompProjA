using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Controls.Shapes;
using System;
using System.Runtime.InteropServices;

namespace GTDCompanion.Pages
{
    public partial class OverlayWindow : Window
    {
        public OverlayWindow()
        {
            InitializeComponent();

            this.Opened += (_, __) => MakeClickThrough();

            // Centraliza no monitor principal
            var screen = Screens.Primary?.WorkingArea ?? new PixelRect(0, 0, 1920, 1080);
            this.Position = new PixelPoint(
                screen.X + screen.Width / 2 - (int)(this.Width / 2),
                screen.Y + screen.Height / 2 - (int)(this.Height / 2));
        }

        public void UpdateMira(MiraConfig config)
        {
            OverlayCanvas.Children.Clear();

            double cx = this.Width / 2;
            double cy = this.Height / 2;

            var color = new Color(config.Alpha, config.Cor.R, config.Cor.G, config.Cor.B);
            var brush = new SolidColorBrush(color);

            double size = config.Tamanho;
            double thick = config.Espessura;

            switch (config.Modelo)
            {
                case 0: // Cruz simples
                    OverlayCanvas.Children.Add(new Line
                    {
                        StartPoint = new Point(cx, cy - size / 2),
                        EndPoint = new Point(cx, cy + size / 2),
                        Stroke = brush,
                        StrokeThickness = thick
                    });
                    OverlayCanvas.Children.Add(new Line
                    {
                        StartPoint = new Point(cx - size / 2, cy),
                        EndPoint = new Point(cx + size / 2, cy),
                        Stroke = brush,
                        StrokeThickness = thick
                    });
                    break;
                case 1: // Círculo
                    OverlayCanvas.Children.Add(new Ellipse
                    {
                        Width = size,
                        Height = size,
                        Stroke = brush,
                        StrokeThickness = thick,
                        Fill = null,
                        [Canvas.LeftProperty] = cx - size / 2,
                        [Canvas.TopProperty] = cy - size / 2
                    });
                    break;
                case 2: // Ponto
                    OverlayCanvas.Children.Add(new Ellipse
                    {
                        Width = thick * 2,
                        Height = thick * 2,
                        Fill = brush,
                        Stroke = null,
                        [Canvas.LeftProperty] = cx - thick,
                        [Canvas.TopProperty] = cy - thick
                    });
                    break;
                case 3: // X
                    OverlayCanvas.Children.Add(new Line
                    {
                        StartPoint = new Point(cx - size / 2, cy - size / 2),
                        EndPoint = new Point(cx + size / 2, cy + size / 2),
                        Stroke = brush,
                        StrokeThickness = thick
                    });
                    OverlayCanvas.Children.Add(new Line
                    {
                        StartPoint = new Point(cx - size / 2, cy + size / 2),
                        EndPoint = new Point(cx + size / 2, cy - size / 2),
                        Stroke = brush,
                        StrokeThickness = thick
                    });
                    break;
                case 4: // H
                    OverlayCanvas.Children.Add(new Line
                    {
                        StartPoint = new Point(cx - size / 2, cy - size / 2),
                        EndPoint = new Point(cx - size / 2, cy + size / 2),
                        Stroke = brush,
                        StrokeThickness = thick
                    });
                    OverlayCanvas.Children.Add(new Line
                    {
                        StartPoint = new Point(cx + size / 2, cy - size / 2),
                        EndPoint = new Point(cx + size / 2, cy + size / 2),
                        Stroke = brush,
                        StrokeThickness = thick
                    });
                    OverlayCanvas.Children.Add(new Line
                    {
                        StartPoint = new Point(cx - size / 2, cy),
                        EndPoint = new Point(cx + size / 2, cy),
                        Stroke = brush,
                        StrokeThickness = thick
                    });
                    break;
                case 5: // T invertido
                    OverlayCanvas.Children.Add(new Line
                    {
                        StartPoint = new Point(cx, cy - size / 2),
                        EndPoint = new Point(cx, cy + size / 2),
                        Stroke = brush,
                        StrokeThickness = thick
                    });
                    OverlayCanvas.Children.Add(new Line
                    {
                        StartPoint = new Point(cx - size / 2, cy + size / 2),
                        EndPoint = new Point(cx + size / 2, cy + size / 2),
                        Stroke = brush,
                        StrokeThickness = thick
                    });
                    break;
                case 6: // Retângulo
                    OverlayCanvas.Children.Add(new Rectangle
                    {
                        Width = size,
                        Height = size,
                        Stroke = brush,
                        StrokeThickness = thick,
                        Fill = null,
                        [Canvas.LeftProperty] = cx - size / 2,
                        [Canvas.TopProperty] = cy - size / 2
                    });
                    break;
                case 7: // Triângulo
                    var points = new Avalonia.Collections.AvaloniaList<Point>
                    {
                        new Point(cx, cy - size / 2),
                        new Point(cx - size / 2, cy + size / 2),
                        new Point(cx + size / 2, cy + size / 2)
                    };
                    OverlayCanvas.Children.Add(new Polyline
                    {
                        Points = points,
                        Stroke = brush,
                        StrokeThickness = thick,
                        Fill = null
                    });
                    break;
                case 8: // Sniper 1 - círculo com cruz
                    OverlayCanvas.Children.Add(new Ellipse
                    {
                        Width = size,
                        Height = size,
                        Stroke = brush,
                        StrokeThickness = thick,
                        Fill = null,
                        [Canvas.LeftProperty] = cx - size / 2,
                        [Canvas.TopProperty] = cy - size / 2
                    });
                    OverlayCanvas.Children.Add(new Line
                    {
                        StartPoint = new Point(cx, cy - size / 2),
                        EndPoint = new Point(cx, cy + size / 2),
                        Stroke = brush,
                        StrokeThickness = thick
                    });
                    OverlayCanvas.Children.Add(new Line
                    {
                        StartPoint = new Point(cx - size / 2, cy),
                        EndPoint = new Point(cx + size / 2, cy),
                        Stroke = brush,
                        StrokeThickness = thick
                    });
                    break;
                case 9: // Sniper 2 - círculo, cruz e ponto central
                    OverlayCanvas.Children.Add(new Ellipse
                    {
                        Width = size,
                        Height = size,
                        Stroke = brush,
                        StrokeThickness = thick,
                        Fill = null,
                        [Canvas.LeftProperty] = cx - size / 2,
                        [Canvas.TopProperty] = cy - size / 2
                    });
                    OverlayCanvas.Children.Add(new Ellipse
                    {
                        Width = thick * 2,
                        Height = thick * 2,
                        Fill = brush,
                        Stroke = null,
                        [Canvas.LeftProperty] = cx - thick,
                        [Canvas.TopProperty] = cy - thick
                    });
                    OverlayCanvas.Children.Add(new Line
                    {
                        StartPoint = new Point(cx, cy - size / 2),
                        EndPoint = new Point(cx, cy + size / 2),
                        Stroke = brush,
                        StrokeThickness = thick
                    });
                    OverlayCanvas.Children.Add(new Line
                    {
                        StartPoint = new Point(cx - size / 2, cy),
                        EndPoint = new Point(cx + size / 2, cy),
                        Stroke = brush,
                        StrokeThickness = thick
                    });
                    break;
                case 10: // Sniper 3 - cruz com espaço central
                    double gap = size * 0.2;
                    OverlayCanvas.Children.Add(new Line
                    {
                        StartPoint = new Point(cx, cy - size / 2),
                        EndPoint = new Point(cx, cy - gap),
                        Stroke = brush,
                        StrokeThickness = thick
                    });
                    OverlayCanvas.Children.Add(new Line
                    {
                        StartPoint = new Point(cx, cy + gap),
                        EndPoint = new Point(cx, cy + size / 2),
                        Stroke = brush,
                        StrokeThickness = thick
                    });
                    OverlayCanvas.Children.Add(new Line
                    {
                        StartPoint = new Point(cx - size / 2, cy),
                        EndPoint = new Point(cx - gap, cy),
                        Stroke = brush,
                        StrokeThickness = thick
                    });
                    OverlayCanvas.Children.Add(new Line
                    {
                        StartPoint = new Point(cx + gap, cy),
                        EndPoint = new Point(cx + size / 2, cy),
                        Stroke = brush,
                        StrokeThickness = thick
                    });
                    break;
                case 11: // Quatro cantos
                    double corner = size / 2;
                    double length = size / 4;
                    // Canto superior esquerdo
                    OverlayCanvas.Children.Add(new Line
                    {
                        StartPoint = new Point(cx - corner, cy - corner),
                        EndPoint = new Point(cx - corner + length, cy - corner),
                        Stroke = brush,
                        StrokeThickness = thick
                    });
                    OverlayCanvas.Children.Add(new Line
                    {
                        StartPoint = new Point(cx - corner, cy - corner),
                        EndPoint = new Point(cx - corner, cy - corner + length),
                        Stroke = brush,
                        StrokeThickness = thick
                    });
                    // Canto superior direito
                    OverlayCanvas.Children.Add(new Line
                    {
                        StartPoint = new Point(cx + corner, cy - corner),
                        EndPoint = new Point(cx + corner - length, cy - corner),
                        Stroke = brush,
                        StrokeThickness = thick
                    });
                    OverlayCanvas.Children.Add(new Line
                    {
                        StartPoint = new Point(cx + corner, cy - corner),
                        EndPoint = new Point(cx + corner, cy - corner + length),
                        Stroke = brush,
                        StrokeThickness = thick
                    });
                    // Canto inferior esquerdo
                    OverlayCanvas.Children.Add(new Line
                    {
                        StartPoint = new Point(cx - corner, cy + corner),
                        EndPoint = new Point(cx - corner + length, cy + corner),
                        Stroke = brush,
                        StrokeThickness = thick
                    });
                    OverlayCanvas.Children.Add(new Line
                    {
                        StartPoint = new Point(cx - corner, cy + corner),
                        EndPoint = new Point(cx - corner, cy + corner - length),
                        Stroke = brush,
                        StrokeThickness = thick
                    });
                    // Canto inferior direito
                    OverlayCanvas.Children.Add(new Line
                    {
                        StartPoint = new Point(cx + corner, cy + corner),
                        EndPoint = new Point(cx + corner - length, cy + corner),
                        Stroke = brush,
                        StrokeThickness = thick
                    });
                    OverlayCanvas.Children.Add(new Line
                    {
                        StartPoint = new Point(cx + corner, cy + corner),
                        EndPoint = new Point(cx + corner, cy + corner - length),
                        Stroke = brush,
                        StrokeThickness = thick
                    });
                    break;
                case 12: // Estrela
                    // cruz
                    OverlayCanvas.Children.Add(new Line
                    {
                        StartPoint = new Point(cx, cy - size / 2),
                        EndPoint = new Point(cx, cy + size / 2),
                        Stroke = brush,
                        StrokeThickness = thick
                    });
                    OverlayCanvas.Children.Add(new Line
                    {
                        StartPoint = new Point(cx - size / 2, cy),
                        EndPoint = new Point(cx + size / 2, cy),
                        Stroke = brush,
                        StrokeThickness = thick
                    });
                    // diagonais
                    OverlayCanvas.Children.Add(new Line
                    {
                        StartPoint = new Point(cx - size / 2, cy - size / 2),
                        EndPoint = new Point(cx + size / 2, cy + size / 2),
                        Stroke = brush,
                        StrokeThickness = thick
                    });
                    OverlayCanvas.Children.Add(new Line
                    {
                        StartPoint = new Point(cx - size / 2, cy + size / 2),
                        EndPoint = new Point(cx + size / 2, cy - size / 2),
                        Stroke = brush,
                        StrokeThickness = thick
                    });
                    break;
                case 13: // Diamante
                    var dpts = new Avalonia.Collections.AvaloniaList<Point>
                    {
                        new Point(cx, cy - size / 2),
                        new Point(cx + size / 2, cy),
                        new Point(cx, cy + size / 2),
                        new Point(cx - size / 2, cy)
                    };
                    OverlayCanvas.Children.Add(new Polyline
                    {
                        Points = dpts,
                        Stroke = brush,
                        StrokeThickness = thick,
                        Fill = null
                    });
                    break;
            }
        }

        private void MakeClickThrough()
        {
            if (OperatingSystem.IsWindows())
            {
                var hwnd = this.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
                if (hwnd != IntPtr.Zero)
                {
                    int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                    exStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW;
                    SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
                }
            }
        }

        const int GWL_EXSTYLE = -20;
        const int WS_EX_LAYERED = 0x80000;
        const int WS_EX_TRANSPARENT = 0x20;
        const int WS_EX_TOOLWINDOW = 0x80;

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hwnd, int index);
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
    }
}
