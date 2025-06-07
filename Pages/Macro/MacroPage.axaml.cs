using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using GTDCompanion.Helpers;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

namespace GTDCompanion.Pages
{
    public partial class MacroPage : UserControl
    {
        private readonly List<MacroStep> macroSteps = new();
        private readonly Dictionary<int, MacroOverlay> overlays = new();

        public MacroPage()
        {
            InitializeComponent();

            AddStepButton.Click += AddStep;
            RemoveStepButton.Click += RemoveStep;
            MoveUpButton.Click += MoveStepUp;
            MoveDownButton.Click += MoveStepDown;
            SaveMacroButton.Click += SaveMacro;
            LoadMacroButton.Click += LoadMacro;
            ExecuteMacroButton.Click += ExecuteMacro;

            StepListBox.DoubleTapped += StepListBox_DoubleTapped;
        }

        private void AddStep(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (StepTypeCombo.SelectedIndex == 0) // Clique
            {
                var idx = macroSteps.Count;
                var overlay = new MacroOverlay(idx);
                overlay.PositionUpdated += UpdateOverlayPosition;
                overlay.Show();

                // Posição inicial do overlay
                var screenX = overlay.CenterScreenX();
                var screenY = overlay.CenterScreenY();

                macroSteps.Add(new MacroStep { Tipo = "Clique", Botao = "Left", Cliques = 1, X = screenX, Y = screenY });
                overlays[idx] = overlay;
                RefreshStepList();
                UpdateOverlaysOrder();
            }
            else // Tecla/Combo
            {
                var popup = new KeyCapturePopup();
                popup.KeyComboCaptured += combo =>
                {
                    macroSteps.Add(new MacroStep { Tipo = "Tecla", Teclas = combo });
                    RefreshStepList();
                    UpdateOverlaysOrder();
                };
                popup.ShowDialog(GetWindow());
            }
        }

        private void RemoveStep(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var idx = StepListBox.SelectedIndex;
            if (idx < 0 || idx >= macroSteps.Count) return;

            if (overlays.ContainsKey(idx))
            {
                overlays[idx].Close();
                overlays.Remove(idx);
            }
            macroSteps.RemoveAt(idx);

            // Reindex overlays
            var newOverlays = new Dictionary<int, MacroOverlay>();
            int j = 0;
            foreach (var kv in overlays.OrderBy(x => x.Key))
            {
                if (kv.Key == idx) continue;
                newOverlays[j] = kv.Value;
                kv.Value.UpdateStepNumber(j + 1);
                j++;
            }
            overlays.Clear();
            foreach (var kv in newOverlays)
                overlays[kv.Key] = kv.Value;

            RefreshStepList();
            UpdateOverlaysOrder();
        }

        private void MoveStepUp(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var idx = StepListBox.SelectedIndex;
            if (idx <= 0) return;

            (macroSteps[idx - 1], macroSteps[idx]) = (macroSteps[idx], macroSteps[idx - 1]);

            if (overlays.ContainsKey(idx) && overlays.ContainsKey(idx - 1))
            {
                (overlays[idx - 1], overlays[idx]) = (overlays[idx], overlays[idx - 1]);
            }

            RefreshStepList();
            UpdateOverlaysOrder();
            StepListBox.SelectedIndex = idx - 1;
        }

        private void MoveStepDown(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var idx = StepListBox.SelectedIndex;
            if (idx < 0 || idx >= macroSteps.Count - 1) return;

            (macroSteps[idx + 1], macroSteps[idx]) = (macroSteps[idx], macroSteps[idx + 1]);

            if (overlays.ContainsKey(idx) && overlays.ContainsKey(idx + 1))
            {
                (overlays[idx + 1], overlays[idx]) = (overlays[idx], overlays[idx + 1]);
            }

            RefreshStepList();
            UpdateOverlaysOrder();
            StepListBox.SelectedIndex = idx + 1;
        }

        private void RefreshStepList()
        {
            var items = new List<string>();
            for (int i = 0; i < macroSteps.Count; i++)
            {
                var step = macroSteps[i];
                items.Add(step.Tipo == "Clique"
                    ? $"{i + 1}. Clique ({step.Botao}) x{step.Cliques} (Delay: {step.Delay}s, Rep: {step.Repeticoes}) [{step.X},{step.Y}]"
                    : $"{i + 1}. Tecla/Combo [{step.Teclas}] (Delay: {step.Delay}s, Rep: {step.Repeticoes})");
            }
            StepListBox.ItemsSource = items;
        }

        private async void SaveMacro(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var storage = TopLevel.GetTopLevel(this)?.StorageProvider;
            if (storage is null) return;

            var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                SuggestedFileName = "macro.json",
                FileTypeChoices = [new FilePickerFileType("JSON") { Patterns = ["*.json"] }]
            });

            if (file != null)
            {
                await using var stream = await file.OpenWriteAsync();
                await JsonSerializer.SerializeAsync(stream, macroSteps);
            }
        }

        private async void LoadMacro(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var storage = TopLevel.GetTopLevel(this)?.StorageProvider;
            if (storage is null) return;

            var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("JSON") { Patterns = ["*.json"] }]
            });

            if (files.Count > 0)
            {
                var file = files[0];
                await using var stream = await file.OpenReadAsync();
                macroSteps.Clear();
                var loadedSteps = await JsonSerializer.DeserializeAsync<List<MacroStep>>(stream);
                if (loadedSteps != null)
                {
                    macroSteps.AddRange(loadedSteps);
                    RefreshStepList();

                    // Fecha overlays antigos
                    foreach (var ov in overlays.Values) ov.Close();
                    overlays.Clear();

                    // Reabre overlays nas posições certas
                    for (int i = 0; i < macroSteps.Count; i++)
                    {
                        if (macroSteps[i].Tipo == "Clique")
                        {
                            var ov = new MacroOverlay(i, macroSteps[i].X, macroSteps[i].Y);
                            ov.PositionUpdated += UpdateOverlayPosition;
                            overlays[i] = ov;
                            ov.Show();
                        }
                    }
                    UpdateOverlaysOrder();
                }
            }
        }

        private void ExecuteMacro(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var runner = new MacroRunner(macroSteps);
            runner.Execute();
        }

        private void UpdateOverlayPosition(int idx, int x, int y)
        {
            macroSteps[idx].X = x;
            macroSteps[idx].Y = y;
        }

        private Window GetWindow() => (Window)VisualRoot!;

        private void UpdateOverlaysOrder()
        {
            // Atualiza o número de cada overlay conforme ordem
            for (int i = 0; i < macroSteps.Count; i++)
            {
                if (macroSteps[i].Tipo == "Clique" && overlays.ContainsKey(i))
                {
                    overlays[i].UpdateStepNumber(i + 1);
                }
            }
        }

        private async void StepListBox_DoubleTapped(object? sender, TappedEventArgs e)
        {
            var idx = StepListBox.SelectedIndex;
            if (idx < 0 || idx >= macroSteps.Count) return;
            var step = macroSteps[idx];
            var dlg = new StepEditDialog(step);
            var win = GetWindow();
            await dlg.ShowDialog(win);

            // Se mudou a posição pelo dialog, atualize overlay
            if (step.Tipo == "Clique" && overlays.ContainsKey(idx))
            {
                overlays[idx].Position = new PixelPoint(step.X - (int)(overlays[idx].Width / 2), step.Y - (int)(overlays[idx].Height / 2));
            }

            RefreshStepList();
        }
    }
}