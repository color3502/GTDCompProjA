using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace GTDCompanion.Pages
{
    public partial class StickerNotesPage : UserControl
    {
        private static readonly StickerNoteWindow?[] windows = new StickerNoteWindow?[5];
        private readonly IBrush?[] defaultBackgrounds = new IBrush?[5];
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
            defaultBackgrounds[0] = Note1Button.Background;
            defaultBackgrounds[1] = Note2Button.Background;
            defaultBackgrounds[2] = Note3Button.Background;
            defaultBackgrounds[3] = Note4Button.Background;
            defaultBackgrounds[4] = Note5Button.Background;
            Note1Button.Click += (_, __) => ToggleWindow(0);
            Note2Button.Click += (_, __) => ToggleWindow(1);
            Note3Button.Click += (_, __) => ToggleWindow(2);
            Note4Button.Click += (_, __) => ToggleWindow(3);
            Note5Button.Click += (_, __) => ToggleWindow(4);

            UpdateButtonStates();
        }

        private Button GetButton(int idx) => idx switch
        {
            0 => Note1Button,
            1 => Note2Button,
            2 => Note3Button,
            3 => Note4Button,
            _ => Note5Button,
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
    }
}
