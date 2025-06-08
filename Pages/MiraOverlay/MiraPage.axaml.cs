using GTDCompanion; // para enxergar MiraConfig
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Input;
using System;
using Avalonia;
using GTDCompanion.Helpers;

namespace GTDCompanion.Pages
{
    public partial class MiraPage : UserControl
    {
        private static OverlayWindow? overlayWin;
        public static MiraPage? Instance { get; private set; }
        private bool isLoaded = false; // Evita trigger duplo ao carregar

        private void UpdateToggleButton()
        {
            ToggleBtn.Content = (overlayWin != null && overlayWin.IsVisible)
                ? "Ocultar Mira" : "Mostrar Mira";
        }

        public static void ToggleOverlayGlobal()
        {
            if (overlayWin == null || !overlayWin.IsVisible)
            {
                overlayWin = new OverlayWindow();
                overlayWin.Closed += (_, __) => overlayWin = null;
                overlayWin.Show();
                overlayWin.UpdateMira(GTDConfigHelper.LoadMiraConfig());
            }
            else
            {
                overlayWin.Close();
                overlayWin = null;
            }

            Instance?.UpdateToggleButton();
        }

        public MiraPage()
        {
            InitializeComponent();

            // Carrega configuração salva e aplica nos controles
            var config = GTDConfigHelper.LoadMiraConfig();
            ModeloCombo.SelectedIndex = config.Modelo;
            TamanhoSlider.Value = config.Tamanho;
            EspessuraSlider.Value = config.Espessura;
            AlphaSlider.Value = config.Alpha;

            // Seleciona cor no ComboBox, se existir
            for (int i = 0; i < CorCombo.Items.Count; i++)
            {
                if (CorCombo.Items[i] is ComboBoxItem item && item.Tag?.ToString()?.Equals(config.Cor.ToString(), StringComparison.OrdinalIgnoreCase) == true)
                {
                    CorCombo.SelectedIndex = i;
                    break;
                }
            }
            // Se não achou a cor exata, seleciona o primeiro
            if (CorCombo.SelectedIndex == -1)
                CorCombo.SelectedIndex = 0;

            ModeloCombo.SelectionChanged += OnConfigChanged;
            CorCombo.SelectionChanged += OnConfigChanged;
            TamanhoSlider.PropertyChanged += (_, __) => OnConfigChanged(null, null);
            EspessuraSlider.PropertyChanged += (_, __) => OnConfigChanged(null, null);
            AlphaSlider.PropertyChanged += (_, __) => OnConfigChanged(null, null);

            isLoaded = true;

            this.AttachedToVisualTree += (_, _) =>
            {
                Instance = this;
                var win = GetWindow();
                if (win != null)
                    win.KeyDown += OnWindowKeyDown;
            };

            this.DetachedFromVisualTree += (_, _) =>
            {
                if (Instance == this)
                    Instance = null;
                var win = GetWindow();
                if (win != null)
                    win.KeyDown -= OnWindowKeyDown;
            };

            UpdateToggleButton();
        }

        private void ToggleMira_Click(object? sender, RoutedEventArgs e)
        {
            ToggleOverlayGlobal();
        }

        private void OnConfigChanged(object? sender, EventArgs? e)
        {
            if (!isLoaded) return; // Evita salvar ao carregar controles

            if (overlayWin != null && overlayWin.IsVisible)
            {
                overlayWin.UpdateMira(GetCurrentConfig());
            }
            // Salva sempre que muda
            GTDConfigHelper.SaveMiraConfig(GetCurrentConfig());
        }

        private MiraConfig GetCurrentConfig()
        {
            var corCombo = CorCombo.SelectedItem as ComboBoxItem;
            var colorString = corCombo?.Tag?.ToString() ?? "#FF3232";
            var color = Color.Parse(colorString);

            return new MiraConfig
            {
                Modelo = ModeloCombo.SelectedIndex,
                Cor = color,
                Tamanho = (int)TamanhoSlider.Value,
                Espessura = (int)EspessuraSlider.Value,
                Alpha = (byte)AlphaSlider.Value
            };
        }

        private Window GetWindow() => (Window)VisualRoot!;

        private void OnWindowKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.F7)
            {
                ToggleOverlayGlobal();
            }
        }
    }
}
