using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GTDCompanion.Pages
{
    public partial class KeyCapturePopup : Window
    {
        private readonly HashSet<Key> pressedKeys = new();
        public event Action<string>? KeyComboCaptured;

        public KeyCapturePopup()
        {
            InitializeComponent();

            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
            CancelButton.Click += (s, e) => Close();
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            pressedKeys.Add(e.Key);
            UpdateInstructionText();
        }

        private void OnKeyUp(object? sender, KeyEventArgs e)
        {
            // Ao soltar todas as teclas, finaliza captura
            pressedKeys.Remove(e.Key);

            if (!pressedKeys.Any())
            {
                var combo = FormatKeyCombo(e.KeyModifiers, e.Key);
                KeyComboCaptured?.Invoke(combo);
                Close();
            }
        }

        private void UpdateInstructionText()
        {
            var keys = pressedKeys.Select(k => k.ToString()).OrderBy(k => k);
            InstructionText.Text = $"Capturando: {string.Join(" + ", keys)}";
        }

        private string FormatKeyCombo(KeyModifiers modifiers, Key key)
        {
            var parts = new List<string>();

            if (modifiers.HasFlag(KeyModifiers.Control))
                parts.Add("Ctrl");
            if (modifiers.HasFlag(KeyModifiers.Shift))
                parts.Add("Shift");
            if (modifiers.HasFlag(KeyModifiers.Alt))
                parts.Add("Alt");
            if (modifiers.HasFlag(KeyModifiers.Meta))
                parts.Add("Win");

            // Adiciona a tecla principal
            if (!IsModifierKey(key))
                parts.Add(key.ToString());

            return string.Join("+", parts);
        }

        private bool IsModifierKey(Key key)
        {
            return key is Key.LeftCtrl or Key.RightCtrl
                or Key.LeftShift or Key.RightShift
                or Key.LeftAlt or Key.RightAlt
                or Key.LWin or Key.RWin;
        }
    }
}
