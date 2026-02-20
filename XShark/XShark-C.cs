using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace XSHARK
{
    public sealed class XShark_C : IDisposable
    {
        private XShark_E? _emulator;
        private CancellationTokenSource? _cts;
        private Task? _loopTask;

        private readonly XShark_A _autoCenter = new XShark_A();
        private readonly Stopwatch _clock = new Stopwatch();

        private float _previousSteering;
        private bool _previousMiddleState;

        private bool _configSavePending;
        private double _configSaveTimer;

        private double _lastReconnectAttempt;

        private const int TARGET_FPS = 60;
        private const float MAX_DELTA = 0.05f;
        private const float RECONNECT_INTERVAL = 2.0f;

        public float CurrentSteering { get; private set; }

        public bool IsActive => XShark_cfg.IsEmulationActive;
        public bool AutoCenterEnabled => XShark_cfg.AutoCenterEnabled;
        public bool PrimaryButtonsEnabled => XShark_cfg.EnablePrimaryButtons;

        public float Deadzone => XShark_cfg.CurrentDeadzone;
        public float Smoothing => XShark_cfg.CurrentSmoothing;
        public float ResponseCurve => XShark_cfg.CurrentCurve;

        public float AutoCenterStrength => XShark_cfg.AutoCenterStrength;
        public int AutoCenterDelayMs => XShark_cfg.AutoCenterDelayMs;
        public float AutoCenterDamping => XShark_cfg.AutoCenterDamping;

        public bool Start()
        {
            if (_loopTask != null)
                return true;

            XShark_cfg.Load();

            _emulator = new XShark_E();
            _emulator.Start();

            _cts = new CancellationTokenSource();
            _clock.Restart();

            _loopTask = Task.Run(() => MainLoop(_cts.Token));

            return true;
        }

        public void Stop()
        {
            if (_cts == null)
                return;

            _cts.Cancel();

            try { _loopTask?.Wait(); } catch { }

            try
            {
                XShark_cfg.Save();
            }
            catch { }

            _emulator?.Dispose();
            _emulator = null;

            _cts.Dispose();
            _cts = null;
            _loopTask = null;
        }

        public void Dispose() => Stop();

        private async Task MainLoop(CancellationToken token)
        {
            double lastTime = _clock.Elapsed.TotalSeconds;
            double targetFrameTime = 1.0 / TARGET_FPS;

            while (!token.IsCancellationRequested)
            {
                double frameStart = _clock.Elapsed.TotalSeconds;

                float deltaTime = (float)(frameStart - lastTime);
                lastTime = frameStart;

                deltaTime = Math.Clamp(deltaTime, 0f, MAX_DELTA);

                ProcessFrame(deltaTime);
                HandleDeferredConfigSave(deltaTime);
                HandleReconnect(frameStart);

                double frameDuration = _clock.Elapsed.TotalSeconds - frameStart;
                double remaining = targetFrameTime - frameDuration;

                if (remaining > 0)
                    await Task.Delay(TimeSpan.FromSeconds(remaining), token);
            }
        }

        private void ProcessFrame(float deltaTime)
        {
            var input = XShark_I.CaptureState();

            bool currentMiddle = XShark_I.IsMiddleClickPressed();

            if (currentMiddle && !_previousMiddleState)
            {
                XShark_I.CenterMouseToVirtualCenter();
            }

            _previousMiddleState = currentMiddle;
            // ================================

            float targetSteering = 0f;

            if (XShark_cfg.IsEmulationActive)
            {
                targetSteering = XShark_M.CalculateSteering(
                    input.MouseX,
                    input.VirtualLeft,
                    input.VirtualWidth,
                    XShark_cfg.CurrentDeadzone,
                    XShark_cfg.CurrentCurve,
                    input.DpiScale
                );

                float smooth = XShark_cfg.CurrentSmoothing;

                if (smooth <= 0f)
                {
                    CurrentSteering = targetSteering;
                }
                else
                {
                    float responsiveness = 1f - smooth;
                    float speed = responsiveness * 20f;

                    CurrentSteering = XShark_M.ExpSmoothing(
                        _previousSteering,
                        targetSteering,
                        speed,
                        deltaTime
                    );
                }

                CurrentSteering = _autoCenter.Process(
                    CurrentSteering,
                    input.MouseX,
                    input.VirtualLeft,
                    input.VirtualWidth,
                    input.IsMouseClamped,
                    deltaTime
                );
            }
            else
            {
                CurrentSteering = XShark_M.ExpSmoothing(
                    _previousSteering,
                    0f,
                    5f,
                    deltaTime
                );

                _autoCenter.Reset();
            }

            _previousSteering = CurrentSteering;

            try { _emulator?.UpdateFrame(CurrentSteering); } catch { }
        }

        private void HandleReconnect(double now)
        {
            if (_emulator == null) return;
            if (_emulator.IsConnected) return;
            if (now - _lastReconnectAttempt < RECONNECT_INTERVAL) return;

            _lastReconnectAttempt = now;

            _emulator.Dispose();
            _emulator = new XShark_E();
            _emulator.Start();
        }

        private void HandleDeferredConfigSave(float deltaTime)
        {
            if (!_configSavePending) return;

            _configSaveTimer += deltaTime;

            if (_configSaveTimer >= 0.5f)
            {
                XShark_cfg.Save();
                _configSavePending = false;
                _configSaveTimer = 0;
            }
        }

        private void RequestConfigSave()
        {
            _configSavePending = true;
            _configSaveTimer = 0;
        }

        public void ToggleEmulation()
        {
            XShark_cfg.IsEmulationActive = !XShark_cfg.IsEmulationActive;
            _autoCenter.Reset();
            RequestConfigSave();
        }

        public void ToggleAutoCenter()
        {
            XShark_cfg.AutoCenterEnabled = !XShark_cfg.AutoCenterEnabled;
            _autoCenter.Reset();
            RequestConfigSave();
        }

        public void TogglePrimaryButtons()
        {
            XShark_cfg.EnablePrimaryButtons = !XShark_cfg.EnablePrimaryButtons;
            RequestConfigSave();
        }

        public void AdjustDeadzone(float delta)
        {
            XShark_cfg.CurrentDeadzone += delta;
            RequestConfigSave();
        }

        public void AdjustSmoothing(float delta)
        {
            XShark_cfg.CurrentSmoothing += delta;
            RequestConfigSave();
        }

        public void AdjustCurve(float delta)
        {
            XShark_cfg.CurrentCurve += delta;
            RequestConfigSave();
        }

        public void AdjustAutoCenterStrength(float delta)
        {
            XShark_cfg.AutoCenterStrength += delta;
            RequestConfigSave();
        }

        public void AdjustAutoCenterDelay(int delta)
        {
            XShark_cfg.AutoCenterDelayMs += delta;
            RequestConfigSave();
        }

        public void AdjustAutoCenterDamping(float delta)
        {
            XShark_cfg.AutoCenterDamping += delta;
            RequestConfigSave();
        }


        private const int STD_INPUT_HANDLE = -10;
        private const uint ENABLE_QUICK_EDIT = 0x0040;
        private const uint ENABLE_EXTENDED_FLAGS = 0x0080;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        private static void DisableQuickEdit()
        {
            try
            {
                IntPtr handle = GetStdHandle(STD_INPUT_HANDLE);
                if (!GetConsoleMode(handle, out uint mode)) return;

                mode &= ~ENABLE_QUICK_EDIT;
                mode |= ENABLE_EXTENDED_FLAGS;

                SetConsoleMode(handle, mode);
            }
            catch { }
        }

        private const int STD_OUTPUT_HANDLE = -11;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

        private static void EnableVirtualTerminal()
        {
            try
            {
                IntPtr handle = GetStdHandle(STD_OUTPUT_HANDLE);
                if (!GetConsoleMode(handle, out uint mode)) return;

                mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
                SetConsoleMode(handle, mode);
            }
            catch { }
        }

        static void Main(string[] args)
        {
            DisableQuickEdit();
            EnableVirtualTerminal();

            Console.Title = "XShark";

            XShark_cfg.Load();

            using var core = new XShark_C();

            if (!core.Start())
                return;

            var ui = new XShark_U();
            ui.Initialize();
            ui.Run(core);

            core.Stop();
        }
    }
}