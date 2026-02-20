using System;

namespace XSHARK
{
    public static class XShark_M
    {
        private const float EPSILON = 0.000001f;

        private static float Clamp(float v)
            => Math.Clamp(v, -1f, 1f);

        private static float Normalize(
            int mouseX,
            int virtualLeft,
            int width)
        {
            if (width <= 0)
                return 0f;

            float center = virtualLeft + width * 0.5f;
            float half = width * 0.5f;

            return Clamp((mouseX - center) / half);
        }

        private static float ApplyDeadzone(float value, float deadzone)
        {
            float abs = Math.Abs(value);
            if (abs <= deadzone)
                return 0f;

            float sign = Math.Sign(value);
            float scaled = (abs - deadzone) / (1f - deadzone);

            return sign * Math.Clamp(scaled, 0f, 1f);
        }

        private static float ApplyCurve(float value, float curve)
        {
            if (Math.Abs(curve - 1f) < 0.0001f)
                return value;

            float sign = Math.Sign(value);
            float abs = Math.Abs(value);

            return sign * MathF.Pow(abs, curve);
        }

        private static float RemoveCurve(float value, float curve)
        {
            if (Math.Abs(curve - 1f) < 0.0001f)
                return value;

            float sign = Math.Sign(value);
            float abs = Math.Abs(value);

            return sign * MathF.Pow(abs, 1f / curve);
        }

        public static float CalculateSteering(
            int mouseX,
            int virtualLeft,
            int screenWidth,
            float deadzone,
            float curve,
            float dpiScale)
        {
            float normalized = Normalize(mouseX, virtualLeft, screenWidth);

            if (dpiScale > EPSILON)
                normalized *= 1f / dpiScale;

            normalized = Clamp(normalized);

            normalized = ApplyDeadzone(normalized, deadzone);
            normalized = ApplyCurve(normalized, curve);

            return Clamp(normalized);
        }

        public static int SteeringToMouseX(
            float steering,
            int virtualLeft,
            int screenWidth,
            float deadzone,
            float curve)
        {
            steering = Clamp(steering);

            float linear = RemoveCurve(steering, curve);

            float sign = Math.Sign(linear);
            float abs = Math.Abs(linear);

            float withDeadzone = abs * (1f - deadzone) + deadzone;

            float normalized = sign * withDeadzone;

            float center = virtualLeft + screenWidth * 0.5f;
            float half = screenWidth * 0.5f;

            float mouse = center + normalized * half;

            return (int)Math.Clamp(
                mouse,
                virtualLeft,
                virtualLeft + screenWidth - 1);
        }

        public static short ToThumbstick(float steering)
        {
            steering = Clamp(steering);
            return (short)MathF.Round(steering * 32767f);
        }

        public static float Lerp(float a, float b, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return a + (b - a) * t;
        }

        public static float ExpSmoothing(
            float current,
            float target,
            float speed,
            float deltaTime)
        {
            if (deltaTime <= 0f)
                return current;

            float factor = 1f - MathF.Exp(-speed * deltaTime);
            return Lerp(current, target, factor);
        }

        public static float CalculateRelativeSteering(
            float deltaX,
            float sensitivity,
            float curve)
        {
            float normalized = Clamp(deltaX * sensitivity);

            float sign = Math.Sign(normalized);
            float abs = Math.Abs(normalized);

            abs = MathF.Pow(abs, curve);

            return sign * abs;
        }

        public static float ApplyCubicBlend(float value, float blend)
        {
            value = Clamp(value);

            float cubic = value * value * value;

            return Lerp(value, cubic, blend);
        }
    }
}