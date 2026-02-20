using System;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace XSHARK
{
    public sealed class XShark_E : IDisposable
    {
        private ViGEmClient? _client;
        private IXbox360Controller? _controller;

        private readonly object _lock = new object();

        private short _lastLX;
        private bool _lastA;
        private bool _lastB;
        private bool _lastX;
        private bool _lastY;

        private bool _disposed;

        public bool IsConnected
        {
            get
            {
                lock (_lock)
                    return _controller != null;
            }
        }

        public bool Start()
        {
            lock (_lock)
            {
                if (_disposed)
                    return false;

                if (_controller != null)
                    return true;

                try
                {
                    _client = new ViGEmClient();
                    _controller = _client.CreateXbox360Controller();
                    _controller.Connect();

                    ResetCache();

                    return true;
                }
                catch
                {
                    Cleanup();
                    return false;
                }
            }
        }

        public void UpdateFrame(float steering)
        {
            lock (_lock)
            {
                if (_disposed)
                    return;

                if (_controller == null)
                    return;

                try
                {
                    UpdateAxis(steering);
                    UpdateButtons();
                    _controller.SubmitReport();
                }
                catch
                {
                    Cleanup();
                }
            }
        }

        private void UpdateAxis(float steering)
        {
            short value = XShark_M.ToThumbstick(steering);

            if (value == _lastLX)
                return;

            _controller!.SetAxisValue(Xbox360Axis.LeftThumbX, value);
            _lastLX = value;
        }

        private void UpdateButtons()
        {
            if (!XShark_cfg.EnablePrimaryButtons)
            {
                SetButton(Xbox360Button.A, false, ref _lastA);
                SetButton(Xbox360Button.B, false, ref _lastB);
                SetButton(Xbox360Button.X, false, ref _lastX);
                SetButton(Xbox360Button.Y, false, ref _lastY);
                return;
            }

            bool a = XShark_I.IsLeftClickPressed();
            bool b = XShark_I.IsRightClickPressed();
            bool x = XShark_I.IsMouseButton4Pressed();
            bool y = XShark_I.IsMouseButton5Pressed();

            SetButton(Xbox360Button.A, a, ref _lastA);
            SetButton(Xbox360Button.B, b, ref _lastB);
            SetButton(Xbox360Button.X, x, ref _lastX);
            SetButton(Xbox360Button.Y, y, ref _lastY);
        }

        private void SetButton(
            Xbox360Button button,
            bool newState,
            ref bool cache)
        {
            if (newState == cache)
                return;

            _controller!.SetButtonState(button, newState);
            cache = newState;
        }

        private void ResetCache()
        {
            _lastLX = 0;
            _lastA = false;
            _lastB = false;
            _lastX = false;
            _lastY = false;
        }

        private void Cleanup()
        {
            try { _controller?.Disconnect(); } catch { }
            try { _client?.Dispose(); } catch { }

            _controller = null;
            _client = null;

            ResetCache();
        }

        public void Stop()
        {
            lock (_lock)
            {
                Cleanup();
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed)
                    return;

                Cleanup();
                _disposed = true;
            }
        }
    }
}