using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace XSHARK
{
    public sealed class XShark_U
    {
        private readonly string[] _menu =
        {
            "Emulation",
            "Deadzone",
            "Smoothing",
            "Response Curve",
            "AutoCenter",
            "AC Strength",
            "AC Delay",
            "AC Damping",
            "Primary Buttons",
            "Game Controllers",
            "Exit"
        };

        private int _selected;
        private bool _running = true;

        private string[] _lastRendered = Array.Empty<string>();

        private const int WINDOW_WIDTH = 60;
        private const int WINDOW_HEIGHT = 24;

        private const int BOX_WIDTH = 42;
        private const int BOX_HEIGHT = 16;

        private const int LABEL_WIDTH = 18;
        private const int VALUE_WIDTH = 8;

        private const int TARGET_UI_FPS = 60;
        private const int UI_REFRESH_MS = 1000 / TARGET_UI_FPS;

        private const int GWL_STYLE = -16;
        private const int WS_MAXIMIZEBOX = 0x00010000;
        private const int WS_SIZEBOX = 0x00040000;

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public void Initialize()
        {
            Console.CursorVisible = false;

            TrySetupConsole();
            Console.Clear();

            _lastRendered = new string[_menu.Length];

            DrawBorder();
            DrawInfoLine();
        }

        // 🔥 SETUP SEGURO (NO CRASHEA)
        private void TrySetupConsole()
        {
            try
            {
                if (Console.LargestWindowWidth >= WINDOW_WIDTH &&
                    Console.LargestWindowHeight >= WINDOW_HEIGHT)
                {
                    Console.SetWindowSize(WINDOW_WIDTH, WINDOW_HEIGHT);
                    Console.SetBufferSize(WINDOW_WIDTH, WINDOW_HEIGHT);
                }
            }
            catch
            {
                // Ignoramos si no se puede redimensionar
            }

            try
            {
                DisableResize();
            }
            catch
            {
                // Ignoramos si no se puede modificar estilo
            }
        }

        private void DisableResize()
        {
            IntPtr handle = GetConsoleWindow();
            if (handle == IntPtr.Zero)
                return;

            int style = GetWindowLong(handle, GWL_STYLE);

            style &= ~WS_MAXIMIZEBOX;
            style &= ~WS_SIZEBOX;

            SetWindowLong(handle, GWL_STYLE, style);
        }

        public void Run(XShark_C core)
        {
            var stopwatch = Stopwatch.StartNew();

            while (_running)
            {
                double frameStart = stopwatch.Elapsed.TotalMilliseconds;

                Render(core);
                HandleInput(core);

                double frameTime = stopwatch.Elapsed.TotalMilliseconds - frameStart;
                double remaining = UI_REFRESH_MS - frameTime;

                if (remaining > 0)
                    Thread.Sleep((int)remaining);
            }

            Console.CursorVisible = true;
            Console.Clear();
        }

        private void DrawBorder()
        {
            int startX = (Console.WindowWidth - BOX_WIDTH) / 2;
            int startY = (Console.WindowHeight - BOX_HEIGHT) / 2;

            if (startX < 0 || startY < 0)
                return;

            Console.SetCursorPosition(startX, startY);
            Console.Write("┌" + new string('─', BOX_WIDTH - 2) + "┐");

            for (int i = 1; i < BOX_HEIGHT - 1; i++)
            {
                Console.SetCursorPosition(startX, startY + i);
                Console.Write("│");

                Console.SetCursorPosition(startX + BOX_WIDTH - 1, startY + i);
                Console.Write("│");
            }

            Console.SetCursorPosition(startX, startY + BOX_HEIGHT - 1);
            Console.Write("└" + new string('─', BOX_WIDTH - 2) + "┘");
        }

        private void DrawInfoLine()
        {
            string line1 = "A/B/Y/X - Mouse Buttons";
            string line2 = "Center Mouse - Middle Button";

            int borderStartY = (Console.WindowHeight - BOX_HEIGHT) / 2;
            int infoY = borderStartY + BOX_HEIGHT + 1;

            if (infoY + 1 >= Console.WindowHeight)
                return;

            DrawCentered(line1, infoY);
            DrawCentered(line2, infoY + 1);
        }

        private void DrawCentered(string text, int y)
        {
            if (y < 0 || y >= Console.WindowHeight)
                return;

            int x = (Console.WindowWidth / 2) - (text.Length / 2);
            if (x < 0) x = 0;

            Console.SetCursorPosition(x, y);
            Console.Write(text);
        }

        private void Render(XShark_C core)
        {
            int startX = (Console.WindowWidth - BOX_WIDTH) / 2 + 2;
            int startY = (Console.WindowHeight - BOX_HEIGHT) / 2 + 2;

            if (startX < 0 || startY < 0)
                return;

            for (int i = 0; i < _menu.Length; i++)
            {
                string line = BuildLine(i, core);

                if (_lastRendered[i] == line)
                    continue;

                Console.SetCursorPosition(startX, startY + i);
                Console.Write(line.PadRight(BOX_WIDTH - 4));
                _lastRendered[i] = line;
            }
        }

        private string BuildLine(int index, XShark_C core)
        {
            if (_menu[index] == "Exit")
                return $"{(_selected == index ? ">" : " ")} Exit";

            if (_menu[index] == "Game Controllers")
                return $"{(_selected == index ? ">" : " ")} Game Controllers";

            string label = _menu[index].PadRight(LABEL_WIDTH);
            string value = GetValue(index, core).PadLeft(VALUE_WIDTH);

            return $"{(_selected == index ? ">" : " ")} {label}{value}";
        }

        private string GetValue(int index, XShark_C core)
        {
            return index switch
            {
                0 => core.IsActive ? "ON" : "OFF",
                1 => core.Deadzone.ToString("F2"),
                2 => core.Smoothing.ToString("F2"),
                3 => core.ResponseCurve.ToString("F2"),
                4 => core.AutoCenterEnabled ? "ON" : "OFF",
                5 => core.AutoCenterStrength.ToString("F1"),
                6 => core.AutoCenterDelayMs.ToString(),
                7 => core.AutoCenterDamping.ToString("F2"),
                8 => core.PrimaryButtonsEnabled ? "ON" : "OFF",
                _ => ""
            };
        }

        private void HandleInput(XShark_C core)
        {
            while (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        _selected = (_selected - 1 + _menu.Length) % _menu.Length;
                        break;

                    case ConsoleKey.DownArrow:
                        _selected = (_selected + 1) % _menu.Length;
                        break;

                    case ConsoleKey.LeftArrow:
                        Adjust(core, -1);
                        break;

                    case ConsoleKey.RightArrow:
                        Adjust(core, 1);
                        break;

                    case ConsoleKey.Enter:
                        Activate(core);
                        break;

                    case ConsoleKey.Escape:
                        _running = false;
                        break;
                }
            }
        }

        private void Activate(XShark_C core)
        {
            if (_selected == 9)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "joy.cpl",
                        UseShellExecute = true
                    });
                }
                catch { }
            }

            if (_selected == 10)
                _running = false;
        }

        private void Adjust(XShark_C core, int dir)
        {
            switch (_selected)
            {
                case 0: core.ToggleEmulation(); break;
                case 1: core.AdjustDeadzone(0.01f * dir); break;
                case 2: core.AdjustSmoothing(0.05f * dir); break;
                case 3: core.AdjustCurve(0.1f * dir); break;
                case 4: core.ToggleAutoCenter(); break;
                case 5: core.AdjustAutoCenterStrength(0.5f * dir); break;
                case 6: core.AdjustAutoCenterDelay(20 * dir); break;
                case 7: core.AdjustAutoCenterDamping(0.02f * dir); break;
                case 8: core.TogglePrimaryButtons(); break;
            }
        }
    }
}