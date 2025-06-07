using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using GTDCompanion.Helpers;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace GTDCompanion.Pages
{
    public partial class MacroPage : UserControl
    {
        private readonly List<MacroStep> macroSteps = new();
        private readonly Dictionary<int, MacroOverlay> overlays = new();
        private GlobalHotkey? globalHotkey;

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

            this.AttachedToVisualTree += (_, _) =>
            {
                var win = GetWindow();
                if (win != null)
                    win.KeyDown += OnWindowKeyDown;
                globalHotkey = new GlobalHotkey(() => ExecuteMacro(null, new Avalonia.Interactivity.RoutedEventArgs()));
                globalHotkey.Register();
            };

            this.DetachedFromVisualTree += (_, _) =>
            {
                var win = GetWindow();
                if (win != null)
                    win.KeyDown -= OnWindowKeyDown;
                globalHotkey?.Dispose();
            };
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
                if (step.Tipo == "Clique")
                {
                    items.Add($"{i + 1}. Clique ({step.Botao}) x{step.Cliques} (Delay: {step.Delay}s, Rep: {step.Repeticoes}) [{step.X},{step.Y}]");
                }
                else
                {
                    var label = step.Teclas.Contains('+') ? "Combo" : "Tecla";
                    items.Add($"{i + 1}. {label} [{step.Teclas}] (Delay: {step.Delay}s, Rep: {step.Repeticoes})");
                }
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

        private CancellationTokenSource? macroCts;
        private bool isRunning = false;

        private async void ExecuteMacro(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (isRunning)
            {
                macroCts?.Cancel();
                return;
            }

            int.TryParse(RepeatCountTextBox.Text, out var repeats);
            if (repeats <= 0) repeats = 1;
            double.TryParse(RepeatDelayTextBox.Text, out var repeatDelay);

            isRunning = true;
            ExecuteMacroButton.Content = "Parar Macro";
            foreach (var ov in overlays.Values)
                ov.SetClickThrough(true);

            macroCts = new CancellationTokenSource();
            var runner = new MacroRunner(macroSteps);
            try
            {
                await runner.ExecuteAsync(repeats, repeatDelay, macroCts.Token);
            }
            catch (TaskCanceledException) { }

            foreach (var ov in overlays.Values)
                ov.SetClickThrough(false);
            isRunning = false;
            ExecuteMacroButton.Content = "Executar Macro";
        }

        private void UpdateOverlayPosition(int idx, int x, int y)
        {
            macroSteps[idx].X = x;
            macroSteps[idx].Y = y;
            RefreshStepList();
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
            overlays.TryGetValue(idx, out var overlay);
            var dlg = new StepEditDialog(step, overlay);
            var win = GetWindow();
            await dlg.ShowDialog(win);

            if (dlg.Deleted)
            {
                StepListBox.SelectedIndex = idx;
                RemoveStep(this, new Avalonia.Interactivity.RoutedEventArgs());
                return;
            }

            // Se mudou a posição pelo dialog, atualize overlay
            if (step.Tipo == "Clique" && overlays.ContainsKey(idx))
            {
                overlays[idx].SetCenterPosition(step.X, step.Y);
            }

            RefreshStepList();
        }

        private void OnWindowKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.F8)
            {
                ExecuteMacro(null, new Avalonia.Interactivity.RoutedEventArgs());
            }
        }
    }
}