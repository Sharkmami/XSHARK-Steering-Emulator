using System;

namespace XSHARK
{
    public sealed class XShark_A
    {
        private float _velocity;
        private float _idleTimer;
        private int _lastMouseX;
        private bool _initialized;

        private const float MOVE_THRESHOLD = 1.0f;
        private const float SNAP_EPSILON = 0.0002f;
        private const float MAX_VELOCITY = 15f;

        private void Initialize(int mouseX)
        {
            if (_initialized)
                return;

            _lastMouseX = mouseX;
            _velocity = 0f;
            _idleTimer = 0f;
            _initialized = true;
        }

        public float Process(
            float currentSteering,
            int mouseX,
            int virtualLeft,
            int screenWidth,
            bool isMouseClamped,
            float deltaTime)
        {
            if (!XShark_cfg.AutoCenterEnabled)
            {
                Reset();
                return currentSteering;
            }

            if (deltaTime <= 0f)
                return currentSteering;

            Initialize(mouseX);

            float mouseDelta = Math.Abs(mouseX - _lastMouseX);
            _lastMouseX = mouseX;

            bool userMoved = mouseDelta > MOVE_THRESHOLD;

            if (userMoved)
            {
                _idleTimer = 0f;
                _velocity = 0f;
                return currentSteering;
            }

            _idleTimer += deltaTime;

            if (_idleTimer < XShark_cfg.AutoCenterDelayMs / 1000f)
                return currentSteering;

            float stiffness = XShark_cfg.AutoCenterStrength;
            float dampingModifier = XShark_cfg.AutoCenterDamping;

            float critical = 2f * MathF.Sqrt(stiffness);
            float damping = critical * dampingModifier;

            float acceleration =
                -stiffness * currentSteering
                - damping * _velocity;

            _velocity += acceleration * deltaTime;
            _velocity = Math.Clamp(_velocity, -MAX_VELOCITY, MAX_VELOCITY);

            float newSteering =
                currentSteering + _velocity * deltaTime;

            if (Math.Abs(newSteering) < SNAP_EPSILON &&
                Math.Abs(_velocity) < SNAP_EPSILON)
            {
                newSteering = 0f;
                _velocity = 0f;
            }

            newSteering = Math.Clamp(newSteering, -1f, 1f);

            if (isMouseClamped)
                _velocity *= 0.4f;

            int newMouseX = XShark_M.SteeringToMouseX(
                newSteering,
                virtualLeft,
                screenWidth,
                XShark_cfg.CurrentDeadzone,
                XShark_cfg.CurrentCurve
            );

            XShark_I.MoveCursorX(newMouseX);

            return newSteering;
        }

        public void Reset()
        {
            _velocity = 0f;
            _idleTimer = 0f;
            _initialized = false;
        }
    }
}