using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System.Threading.Tasks;

namespace GTDCompanion.Pages
{
    public partial class StickerNotesPage : UserControl
    {
        private static readonly StickerNoteWindow?[] windows = new StickerNoteWindow?[10];
        private readonly IBrush?[] defaultBackgrounds = new IBrush?[10];
        private readonly IBrush openBrush = new SolidColorBrush(Color.Parse("#FE6A0A"));

        private void UpdateButtonStates()
        {
            for (int i = 0; i < windows.Length; i++)
            {
                var btn = GetButton(i);
                btn.Background = (windows[i] != null && windows[i]!.IsVisible) ? openBrush : defaultBackgrounds[i];
            }
        }

        public StickerNotesPage()
        {
            InitializeComponent();
            var buttons = new Button[]
            {
                Note1Button, Note2Button, Note3Button, Note4Button, Note5Button,
                Note6Button, Note7Button, Note8Button, Note9Button, Note10Button
            };
            var configButtons = new Button[]
            {
                Config1Button, Config2Button, Config3Button, Config4Button, Config5Button,
                Config6Button, Config7Button, Config8Button, Config9Button, Config10Button
            };

            for (int i = 0; i < buttons.Length; i++)
            {
                int idx = i;
                defaultBackgrounds[i] = buttons[i].Background;
                buttons[i].Click += (_, __) => ToggleWindow(idx);
                var data = StickerNoteStorage.Load(idx + 1);
                buttons[i].Content = string.IsNullOrWhiteSpace(data.Title) ? $"Nota {idx + 1}" : data.Title;
                configButtons[i].Click += async (_, __) => await OpenConfig(idx);
            }

            UpdateButtonStates();
        }

        private Button GetButton(int idx) => idx switch
        {
            0 => Note1Button,
            1 => Note2Button,
            2 => Note3Button,
            3 => Note4Button,
            4 => Note5Button,
            5 => Note6Button,
            6 => Note7Button,
            7 => Note8Button,
            8 => Note9Button,
            _ => Note10Button,
        };

        private void ToggleWindow(int idx)
        {
            if (windows[idx] == null || !windows[idx]!.IsVisible)
            {
                var win = new StickerNoteWindow(idx + 1);
                windows[idx] = win;
                win.Closed += (_, __) =>
                {
                    windows[idx] = null;
                    UpdateButtonStates();
                };
                win.Show();
            }
            else
            {
                windows[idx]!.Close();
                windows[idx] = null;
            }

            UpdateButtonStates();
        }

        private async Task OpenConfig(int idx)
        {
            var data = StickerNoteStorage.Load(idx + 1);
            var dlg = new NoteNameDialog(string.IsNullOrWhiteSpace(data.Title) ? $"Nota {idx + 1}" : data.Title);
            await dlg.ShowDialog((Window)VisualRoot!);

            if (dlg.IsCleared)
            {
                data.Title = string.Empty;
                data.Text = string.Empty;
                StickerNoteStorage.Save(idx + 1, data);
                GetButton(idx).Content = $"Nota {idx + 1}";
                windows[idx]?.SetTitle($"Nota {idx + 1}");
            }
            else if (dlg.ResultName != null)
            {
                data.Title = dlg.ResultName;
                StickerNoteStorage.Save(idx + 1, data);
                GetButton(idx).Content = string.IsNullOrWhiteSpace(data.Title) ? $"Nota {idx + 1}" : data.Title;
                windows[idx]?.SetTitle(string.IsNullOrWhiteSpace(data.Title) ? $"Nota {idx + 1}" : data.Title);
            }
        }
    }
}
