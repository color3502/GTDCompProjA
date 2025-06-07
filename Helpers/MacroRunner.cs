using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InputSimulatorStandard;
using InputSimulatorStandard.Native;
using System.Threading;
using Avalonia.Controls;

namespace GTDCompanion.Helpers
{
    public class MacroRunner
    {
        private readonly List<MacroStep> _steps;
        private readonly InputSimulator _inputSimulator;

        public MacroRunner(List<MacroStep> steps)
        {
            _steps = steps;
            _inputSimulator = new InputSimulator();
        }

        public async Task ExecuteAsync(int repetitions, double delayBetweenRuns, CancellationToken token)
        {
            if (repetitions <= 0) repetitions = 1;

            for (int run = 0; run < repetitions && !token.IsCancellationRequested; run++)
            {
                foreach (var step in _steps)
                {
                    for (int r = 0; r < step.Repeticoes && !token.IsCancellationRequested; r++)
                    {
                        if (step.Tipo == "Clique")
                            ExecuteMouseClick(step);
                        else if (step.Tipo == "Tecla")
                            ExecuteKeyPress(step);

                        await Task.Delay(TimeSpan.FromSeconds(step.Delay), token);
                    }
                }

                if (run < repetitions - 1)
                    await Task.Delay(TimeSpan.FromSeconds(delayBetweenRuns), token);
            }
        }

        private void ExecuteMouseClick(MacroStep step)
        {
            _inputSimulator.Mouse.MoveMouseToPositionOnVirtualDesktop(
                NormalizeCoordinate(step.X, true), 
                NormalizeCoordinate(step.Y, false));

            for (int c = 0; c < step.Cliques; c++)
            {
                switch (step.Botao.ToLower())
                {
                    case "left":
                        _inputSimulator.Mouse.LeftButtonClick();
                        break;
                    case "right":
                        _inputSimulator.Mouse.RightButtonClick();
                        break;
                    case "middle":
                        _inputSimulator.Mouse.MiddleButtonClick();
                        break;
                }
                Thread.Sleep(50);
            }
        }

        private void ExecuteKeyPress(MacroStep step)
        {
            var keys = step.Teclas.Split('+', StringSplitOptions.RemoveEmptyEntries);
            foreach (var key in keys)
            {
                var vk = (VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), "VK_" + key.ToUpper());
                _inputSimulator.Keyboard.KeyDown(vk);
            }

            Thread.Sleep(50);

            foreach (var key in keys)
            {
                var vk = (VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), "VK_" + key.ToUpper());
                _inputSimulator.Keyboard.KeyUp(vk);
            }
        }

        private static double NormalizeCoordinate(int coordinate, bool isWidth)
        {
            // Use Avalonia API to get the primary screen size via TopLevel
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            var screen = topLevel?.Screens?.Primary ?? topLevel?.Screens?.All[0];
            if (screen == null)
                throw new InvalidOperationException("Unable to determine screen size.");

            var screenDimension = isWidth
                ? screen.Bounds.Width
                : screen.Bounds.Height;

            return coordinate * (65535.0 / (screenDimension - 1));
        }
    }
}