using System;
using System.Runtime.InteropServices;

namespace XSHARK
{
    public static class XShark_I
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 =
            new IntPtr(-4);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr value);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        private static extern uint GetDpiForSystem();

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private const int SM_CXVIRTUALSCREEN = 78;
        private const int SM_CYVIRTUALSCREEN = 79;
        private const int SM_XVIRTUALSCREEN = 76;
        private const int SM_YVIRTUALSCREEN = 77;

        private const int VK_LBUTTON = 0x01;
        private const int VK_RBUTTON = 0x02;
        private const int VK_MBUTTON = 0x04; // ← AGREGADO
        private const int VK_XBUTTON1 = 0x05;
        private const int VK_XBUTTON2 = 0x06;

        private static bool _initialized;
        private static readonly object _initLock = new object();

        private static float _dpiScale = 1f;

        private static int _lastX;
        private static int _lastY;

        public struct InputState
        {
            public int MouseX;
            public int MouseY;
            public int DeltaX;
            public int DeltaY;
            public int VirtualLeft;
            public int VirtualTop;
            public int VirtualWidth;
            public int VirtualHeight;
            public bool IsMouseClamped;
            public float DpiScale;
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
                return;

            lock (_initLock)
            {
                if (_initialized)
                    return;

                try
                {
                    if (!SetProcessDpiAwarenessContext(
                        DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2))
                    {
                        SetProcessDPIAware();
                    }
                }
                catch
                {
                    try { SetProcessDPIAware(); }
                    catch { }
                }

                try
                {
                    uint dpi = GetDpiForSystem();
                    _dpiScale = dpi / 96f;
                }
                catch
                {
                    _dpiScale = 1f;
                }

                if (GetCursorPos(out POINT p))
                {
                    _lastX = p.X;
                    _lastY = p.Y;
                }

                _initialized = true;
            }
        }

        public static InputState CaptureState()
        {
            EnsureInitialized();

            if (!GetCursorPos(out POINT p))
                return default;

            int virtualLeft = GetSystemMetrics(SM_XVIRTUALSCREEN);
            int virtualTop = GetSystemMetrics(SM_YVIRTUALSCREEN);
            int virtualWidth = GetSystemMetrics(SM_CXVIRTUALSCREEN);
            int virtualHeight = GetSystemMetrics(SM_CYVIRTUALSCREEN);

            int deltaX = p.X - _lastX;
            int deltaY = p.Y - _lastY;

            if (deltaX > 10000) deltaX = 0;
            if (deltaX < -10000) deltaX = 0;
            if (deltaY > 10000) deltaY = 0;
            if (deltaY < -10000) deltaY = 0;

            _lastX = p.X;
            _lastY = p.Y;

            bool clamped =
                p.X <= virtualLeft ||
                p.X >= virtualLeft + virtualWidth - 1 ||
                p.Y <= virtualTop ||
                p.Y >= virtualTop + virtualHeight - 1;

            return new InputState
            {
                MouseX = p.X,
                MouseY = p.Y,
                DeltaX = deltaX,
                DeltaY = deltaY,
                VirtualLeft = virtualLeft,
                VirtualTop = virtualTop,
                VirtualWidth = virtualWidth,
                VirtualHeight = virtualHeight,
                IsMouseClamped = clamped,
                DpiScale = _dpiScale
            };
        }

        public static void MoveCursorX(int newX)
        {
            if (!GetCursorPos(out POINT p))
                return;

            SetCursorPos(newX, p.Y);
        }

        // -------------------------
        // BOTONES
        // -------------------------

        public static bool IsLeftClickPressed()
            => (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;

        public static bool IsRightClickPressed()
            => (GetAsyncKeyState(VK_RBUTTON) & 0x8000) != 0;

        public static bool IsMiddleClickPressed()
            => (GetAsyncKeyState(VK_MBUTTON) & 0x8000) != 0;

        public static bool IsMouseButton4Pressed()
            => (GetAsyncKeyState(VK_XBUTTON1) & 0x8000) != 0;

        public static bool IsMouseButton5Pressed()
            => (GetAsyncKeyState(VK_XBUTTON2) & 0x8000) != 0;

        // -------------------------
        // NUEVO: CENTRAR MOUSE
        // -------------------------

        public static void CenterMouseToVirtualCenter()
        {
            EnsureInitialized();

            int virtualLeft = GetSystemMetrics(SM_XVIRTUALSCREEN);
            int virtualTop = GetSystemMetrics(SM_YVIRTUALSCREEN);
            int virtualWidth = GetSystemMetrics(SM_CXVIRTUALSCREEN);
            int virtualHeight = GetSystemMetrics(SM_CYVIRTUALSCREEN);

            int centerX = virtualLeft + virtualWidth / 2;
            int centerY = virtualTop + virtualHeight / 2;

            SetCursorPos(centerX, centerY);

            _lastX = centerX;
            _lastY = centerY;
        }
    }
}