using Avalonia.Controls;
using Avalonia.Interactivity;

namespace GTDCompanion.Pages
{
    public partial class StickerNotesPage : UserControl
    {
        private readonly StickerNoteWindow?[] windows = new StickerNoteWindow?[5];

        public StickerNotesPage()
        {
            InitializeComponent();
            Note1Button.Click += (_, __) => ToggleWindow(0);
            Note2Button.Click += (_, __) => ToggleWindow(1);
            Note3Button.Click += (_, __) => ToggleWindow(2);
            Note4Button.Click += (_, __) => ToggleWindow(3);
            Note5Button.Click += (_, __) => ToggleWindow(4);
        }

        private void ToggleWindow(int idx)
        {
            if (windows[idx] == null || !windows[idx]!.IsVisible)
            {
                var win = new StickerNoteWindow(idx + 1);
                windows[idx] = win;
                win.Closed += (_, __) => windows[idx] = null;
                win.Show();
            }
            else
            {
                windows[idx]!.Close();
                windows[idx] = null;
            }
        }
    }
}
