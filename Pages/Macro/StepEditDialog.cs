using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using GTDCompanion.Helpers;
using System;

namespace GTDCompanion.Pages
{
    public class StepEditDialog : Window
    {
        private MacroStep step;
        private TextBox tbDelay, tbRep, tbCliques, tbX, tbY;
        private ComboBox? cbBotao;
        private readonly MacroOverlay? overlay;

        public bool Deleted { get; private set; }

        public StepEditDialog(MacroStep step, MacroOverlay? overlay = null)
        {
            this.step = step;
            this.overlay = overlay;

            Title = "Editar Passo";
            Width = 350;
            Height = 360;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var sp = new StackPanel { Margin = new Thickness(20), Spacing = 8 };
            sp.Children.Add(new TextBlock { Text = $"Tipo: {step.Tipo}", FontWeight = Avalonia.Media.FontWeight.Bold });

            if (step.Tipo == "Clique")
            {
                sp.Children.Add(new TextBlock { Text = "Botão:" });
                cbBotao = new ComboBox
                {
                    ItemsSource = new[] { "Left", "Right", "Middle" },
                    SelectedItem = step.Botao,
                    Width = 120
                };
                sp.Children.Add(cbBotao);

                sp.Children.Add(new TextBlock { Text = "Cliques:" });
                tbCliques = new TextBox { Text = step.Cliques.ToString(), Width = 80 };
                sp.Children.Add(tbCliques);

                sp.Children.Add(new TextBlock { Text = "Posição X:" });
                tbX = new TextBox { Text = step.X.ToString(), Width = 100 };
                sp.Children.Add(tbX);

                sp.Children.Add(new TextBlock { Text = "Posição Y:" });
                tbY = new TextBox { Text = step.Y.ToString(), Width = 100 };
                sp.Children.Add(tbY);
            }
            else
            {
                sp.Children.Add(new TextBlock { Text = $"Tecla/Combo: {step.Teclas}", Margin = new Thickness(0, 4, 0, 4) });
            }

            sp.Children.Add(new TextBlock { Text = "Delay após passo (s):" });
            tbDelay = new TextBox { Text = step.Delay.ToString("F2"), Width = 80 };
            sp.Children.Add(tbDelay);

            sp.Children.Add(new TextBlock { Text = "Repetições:" });
            tbRep = new TextBox { Text = step.Repeticoes.ToString(), Width = 80 };
            sp.Children.Add(tbRep);

            var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, HorizontalAlignment = HorizontalAlignment.Center };
            var btnDelete = new Button { Content = "Deletar", Width = 90 };
            var btnOk = new Button { Content = "OK", Width = 90 };
            var btnCancel = new Button { Content = "Cancelar", Width = 90 };
            panel.Children.Add(btnDelete);
            panel.Children.Add(btnOk);
            panel.Children.Add(btnCancel);
            sp.Children.Add(panel);

            if (overlay != null)
            {
                overlay.PositionUpdated += Overlay_PositionUpdated;
            }

            btnOk.Click += (s, e) =>
            {
                double.TryParse(tbDelay.Text, out var delay);
                int.TryParse(tbRep.Text, out var rep);
                step.Delay = delay;
                step.Repeticoes = rep;

                if (step.Tipo == "Clique")
                {
                    step.Botao = cbBotao?.SelectedItem?.ToString() ?? "Left";
                    int.TryParse(tbCliques.Text, out var c);
                    int.TryParse(tbX.Text, out var x);
                    int.TryParse(tbY.Text, out var y);
                    step.Cliques = c;
                    step.X = x;
                    step.Y = y;
                }
                Close();
            };

            btnDelete.Click += (s, e) =>
            {
                Deleted = true;
                Close();
            };

            btnCancel.Click += (s, e) => Close();

            Content = sp;
        }

        private void Overlay_PositionUpdated(int idx, int x, int y)
        {
            tbX.Text = x.ToString();
            tbY.Text = y.ToString();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (overlay != null)
            {
                overlay.PositionUpdated -= Overlay_PositionUpdated;
            }
            base.OnClosed(e);
        }
    }
}