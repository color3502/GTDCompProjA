using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace GTDCompanion.Pages
{
    public partial class TranslatorOverlay : Window
    {
        private bool dragging = false;
        private PixelPoint dragOffset;
        private bool _collapsed = false;
        private double _originalHeight = 0;

        private void OnIdiomaAlterado()
        {
            OverlayFromLangCombo.Text = GTDConfigHelper.GetString("Translator", "set_from_lang");
            OverlayToLangCombo.Text   = GTDConfigHelper.GetString("Translator", "set_to_lang");            
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            GTDTranslatorEvents.IdiomaAlterado -= OnIdiomaAlterado;
        }

        public TranslatorOverlay()
        {
            InitializeComponent();

            AppConfig.PopulateEnvironment();

            GTDTranslatorEvents.IdiomaAlterado += OnIdiomaAlterado;

            OverlayCopyOnTranslateCheck.IsChecked = GTDConfigHelper.GetBool("Translator", "copy_on_translate", true);

            OverlayFromLangCombo.Text = GTDConfigHelper.GetString("Translator", "set_from_lang");
            OverlayToLangCombo.Text = GTDConfigHelper.GetString("Translator", "set_to_lang");

            double op = GTDConfigHelper.GetDouble("Translator", "overlay_opacity", 0.9);
            Opacity = op;
            OverlayTransparencySlider.Value = op;
            OverlayTransparencySlider.PropertyChanged += (s, e) =>
            {
                if (e.Property.Name == "Value")
                {
                    double value = OverlayTransparencySlider.Value;
                    double rounded = Math.Round(value, 2);

                    Opacity = rounded;
                    GTDConfigHelper.Set("Translator", "overlay_opacity", rounded.ToString(CultureInfo.InvariantCulture));
                }
            };

            OverlayCopyOnTranslateCheck.IsCheckedChanged += (s, e) =>
            {
                if (OverlayCopyOnTranslateCheck.IsChecked == true)
                    GTDConfigHelper.Set("Translator", "copy_on_translate", "true");
                else
                    GTDConfigHelper.Set("Translator", "copy_on_translate", "false");
            };

            OverlayTranslateButton.Click += OverlayTranslateButton_Click;
            OverlayPasteAndTranslateButton.Click += OverlayPasteAndTranslateButton_Click;
            OverlaySwapLangsButton.Click += OverlaySwapLangsButton_Click;

            OverlayInputTextBox.AddHandler(KeyDownEvent, OverlayInputTextBox_PreviewKeyDown, RoutingStrategies.Tunnel);

            // --- Autoajuste dos TextBoxes e da janela
            OverlayInputTextBox.GetObservable(TextBox.TextProperty).Subscribe(_ => AdjustTextBoxAndWindowHeight());
            OverlayOutputTextBox.GetObservable(TextBox.TextProperty).Subscribe(_ => AdjustTextBoxAndWindowHeight());
            AdjustTextBoxAndWindowHeight();

            // Drag overlay: usar toda a janela
            PointerPressed += OnPointerPressed;
            PointerReleased += OnPointerReleased;
            PointerMoved += OnPointerMoved;

            // Botão custom de fechar
            if (this.FindControl<Button>("CloseButton") is Button closeBtn)
            {
                closeBtn.Click += (s, e) => this.Close();
            }

            _originalHeight = Height;
            this.PointerPressed += OnPointerPressedTitleBar;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var handle = this.TryGetPlatformHandle();
                if (handle != null)
                {
                    IntPtr hwnd = handle.Handle;
                    int style = GetWindowLong(hwnd, GWL_EXSTYLE);
                    style |= WS_EX_TOOLWINDOW;
                    SetWindowLong(hwnd, GWL_EXSTYLE, style);
                }
            }
        }

        private void AdjustTextBoxAndWindowHeight()
        {
            // Limites de linhas
            int inputLines = (OverlayInputTextBox.Text ?? "").Split('\n').Length;
            int outputLines = (OverlayOutputTextBox.Text ?? "").Split('\n').Length;

            int visibleInputLines = Math.Min(Math.Max(inputLines, 1), 3);
            int visibleOutputLines = Math.Min(Math.Max(outputLines, 1), 3);

            double lineHeight = 24.0; // ajuste se quiser mais compacto

            // Ajusta TextBox individualmente
            OverlayInputTextBox.Height = visibleInputLines * lineHeight;
            OverlayOutputTextBox.Height = visibleOutputLines * lineHeight;

            // Altura extra da janela (tudo fora os TextBoxes)
            double fixedHeight = 250 - 32 - 32; // altura base - altura default dos 2 TextBoxes

            // Soma do crescimento dos dois textboxes (até 3 linhas cada)
            double extraInput = (visibleInputLines - 1) * lineHeight;
            double extraOutput = (visibleOutputLines - 1) * lineHeight;

            this.Height = fixedHeight + (visibleInputLines * lineHeight) + (visibleOutputLines * lineHeight);
        }

        // Minimizar/restaurar com duplo clique na barra de título
        private void OnPointerPressedTitleBar(object? sender, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                return;
            if (e.ClickCount == 2)
            {
                var pos = e.GetPosition(this);
                if (pos.Y <= 37)
                    ToggleCollapse();
            }
        }

        private void ToggleCollapse()
        {
            if (!_collapsed)
            {
                _originalHeight = this.Height;
                this.Height = 37;
                _collapsed = true;
            }
            else
            {
                this.Height = _originalHeight;
                _collapsed = false;
            }
        }

        private void OverlaySwapLangsButton_Click(object? sender, RoutedEventArgs e)
        {
            var fromText = OverlayFromLangCombo.Text;
            var toText = OverlayToLangCombo.Text;
            OverlayFromLangCombo.Text = toText;
            OverlayToLangCombo.Text = fromText;
        }

        private async void OverlayTranslateButton_Click(object? sender, RoutedEventArgs e)
        {
            await TranslateAndShowAsync();
        }

        private async Task TranslateAndShowAsync()
        {
            string text = OverlayInputTextBox.Text ?? "";
            if (string.IsNullOrWhiteSpace(text)) return;

            string from = OverlayFromLangCombo.Text ?? "PT-BR";
            string to   = OverlayToLangCombo.Text ?? "EN";

            OverlayOutputTextBox.Text = "Traduzindo...";
            string translated = await TranslateAsync(text, to);
            OverlayOutputTextBox.Text = translated;

            if (OverlayCopyOnTranslateCheck.IsChecked == true && this.Clipboard != null)
                await this.Clipboard.SetTextAsync(translated ?? "");
        }

        private async Task<string> TranslateAsync(string text, string to)
        {
            var nextApiEndpoint = Environment.GetEnvironmentVariable("NEXTAPI_ENDPOINT") ?? "";
            var nextApiAppId = Environment.GetEnvironmentVariable("NEXTAPI_APPID") ?? "";
            var nextApiAppSecret = Environment.GetEnvironmentVariable("NEXTAPI_APPSECRET") ?? "";
            var nextApiProjectApiId = Environment.GetEnvironmentVariable("NEXTAPI_PROJECTID") ?? "";
            using var client = new HttpClient();
            var values = new Dictionary<string, string>
            {
                { "app_id", nextApiAppId },
                { "app_secret", nextApiAppSecret },
                { "project_api_id", nextApiProjectApiId },
                { "text", text },
                { "to", to }
            };
            var content = new FormUrlEncodedContent(values);
            var response = await client.PostAsync(nextApiEndpoint + "/lili/tools/translater-string", content);
            string result = await response.Content.ReadAsStringAsync();

            try
            {
                using var doc = JsonDocument.Parse(result);
                return doc.RootElement.GetProperty("return").GetString() ?? string.Empty;
            }
            catch { return "Erro ao traduzir."; }
        }

        private async void OverlayPasteAndTranslateButton_Click(object? sender, RoutedEventArgs e)
        {
            if (this.Clipboard != null)
                OverlayInputTextBox.Text = await this.Clipboard.GetTextAsync();
            await TranslateAndShowAsync();
        }

        private void OverlayInputTextBox_PreviewKeyDown(object? sender, KeyEventArgs e)
        {
            // Shift+Enter: nova linha
            if (e.Key == Key.Enter && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                return; // comportamento normal

            // Enter sozinho: traduz e impede quebra de linha
            if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                _ = TranslateAndShowAsync();
                e.Handled = true; // Isso agora FUNCIONA!
            }
        }

        // Drag com mouse "colado"
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

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x80;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    }
}
