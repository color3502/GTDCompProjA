using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia;
using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Input;

namespace GTDCompanion.Pages
{
    public partial class TranslatorPage : UserControl
    {
        private static readonly Dictionary<string, string> LanguageMap = new()
        {
            { "PT-BR", "Português (BR)" },
            { "EN", "Inglês" },
            { "ES", "Espanhol" },
            { "IT", "Italiano" },
            { "FR", "Francês" },
            { "DE", "Alemão" },
            { "JA", "Japonês" },
            { "ZH", "Chinês" },
            { "RU", "Russo" },
            { "AR", "Árabe" },
            { "KO", "Coreano" },
            { "HI", "Hindi" },
            { "TR", "Turco" },
            { "NL", "Holandês" },
            { "PL", "Polonês" },
            { "SV", "Sueco" },
            { "DA", "Dinamarquês" },
            { "NO", "Norueguês" },
            { "FI", "Finlandês" }
        };

        public TranslatorPage()
        {
            InitializeComponent();
            InitCombos();
            CopyOnTranslateCheck.IsChecked = GTDConfigHelper.GetBool("Translator", "copy_on_translate", true);

           

            CopyOnTranslateCheck.IsCheckedChanged += (s, e) =>
            {
                GTDConfigHelper.Set("Translator", "copy_on_translate", CopyOnTranslateCheck.IsChecked == true ? "true" : "false");
            };

            TranslateButton.Click += TranslateButton_Click;
            PasteAndTranslateButton.Click += PasteAndTranslateButton_Click;
            SwapLangsButton.Click += SwapLangsButton_Click;
            OpenOverlayButton.Click += OpenOverlayButton_Click;

            InputTextBox.KeyDown += InputTextBox_KeyDown;

            // Handler único para ambos ComboBox
            FromLangCombo.SelectionChanged += LangCombo_SelectionChanged;
            ToLangCombo.SelectionChanged += LangCombo_SelectionChanged;
        }

        private void InitCombos()
        {
            FromLangCombo.ItemsSource = LanguageMap;
            ToLangCombo.ItemsSource = LanguageMap;
            FromLangCombo.SelectedIndex = 0; // PT-BR
            ToLangCombo.SelectedIndex = 1;   // EN

            // Dispara o handler para garantir que o default seja salvo
            LangCombo_SelectionChanged(null, null);
        }

        // Handler único para alteração dos idiomas
        private void LangCombo_SelectionChanged(object? sender, SelectionChangedEventArgs? e)
        {
            var fromItem = FromLangCombo.SelectedItem as KeyValuePair<string, string>? ?? default;
            var toItem = ToLangCombo.SelectedItem as KeyValuePair<string, string>? ?? default;

            string from = fromItem.Key != null ? fromItem.Key : "PT-BR";
            string to = toItem.Key != null ? toItem.Key : "EN";

            GTDConfigHelper.Set("Translator", "set_from_lang", from);
            GTDConfigHelper.Set("Translator", "set_to_lang", to);

            // 🚩 Notifica quem quiser ouvir (ex: overlays abertas)
            GTDTranslatorEvents.NotificarIdiomaAlterado();
        }

        private void SwapLangsButton_Click(object? sender, RoutedEventArgs e)
        {
            var fromIdx = FromLangCombo.SelectedIndex;
            var toIdx = ToLangCombo.SelectedIndex;
            FromLangCombo.SelectedIndex = toIdx;
            ToLangCombo.SelectedIndex = fromIdx;
            // Não precisa salvar aqui, o SelectionChanged já será disparado!
        }

        private async void TranslateButton_Click(object? sender, RoutedEventArgs e)
        {
            await TranslateAndShowAsync();
        }

        private async Task TranslateAndShowAsync()
        {
            string text = InputTextBox.Text ?? "";
            if (string.IsNullOrWhiteSpace(text)) return;

            var fromItem = FromLangCombo.SelectedItem as KeyValuePair<string, string>? ?? default;
            var toItem = ToLangCombo.SelectedItem as KeyValuePair<string, string>? ?? default;

            string from = fromItem.Key != null ? fromItem.Key : "PT-BR";
            string to = toItem.Key != null ? toItem.Key : "EN";

            OutputTextBox.Text = "Traduzindo...";
            string translated = await TranslateAsync(text, to);
            OutputTextBox.Text = translated;

            if (CopyOnTranslateCheck.IsChecked == true)
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel?.Clipboard != null)
                    await topLevel.Clipboard.SetTextAsync(translated ?? "");
            }
        }

        private async Task<string> TranslateAsync(string text, string to)
        {
            using var client = new HttpClient();
            var values = new Dictionary<string, string>
            {
                { "app_id", "app_id__LEs7L2inoyalVASnhQ0y9h" },
                { "app_secret", "app_secret__8WatGRDlgI2ZA7oDWS67l07FFzA1BMGIGtsfaDjp" },
                { "project_api_id", "project_api_id__nPpGaZ0gFBQliQ8" },
                { "text", text },
                { "to", to }
            };
            var content = new FormUrlEncodedContent(values);
            var response = await client.PostAsync("https://tools.nextexperience.com.br/api/lili/tools/translater-string", content);
            string result = await response.Content.ReadAsStringAsync();

            try
            {
                using var doc = JsonDocument.Parse(result);
                return doc.RootElement.GetProperty("return").GetString() ?? "Erro ao traduzir.";
            }
            catch { return "Erro ao traduzir."; }
        }

        private async void PasteAndTranslateButton_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.Clipboard != null)
            {
                InputTextBox.Text = await topLevel.Clipboard.GetTextAsync();
                await TranslateAndShowAsync();
            }
        }

        private TranslatorOverlay? _overlay;

        private void OpenOverlayButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_overlay == null || !_overlay.IsVisible)
            {
                _overlay = new TranslatorOverlay();
                _overlay.Closed += (_, __) => _overlay = null;
                _overlay.Show();
            }
            else
            {
                _overlay.Activate(); // Traz para frente
            }
        }

        private void InputTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.Enter)
            {
                _ = TranslateAndShowAsync();
            }
        }
    }
}
