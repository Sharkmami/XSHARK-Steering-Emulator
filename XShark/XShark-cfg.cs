using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;

namespace XSHARK
{
    internal sealed class ConfigModel
    {
        public float Deadzone = 0.05f;
        public float Smoothing = 0.2f;
        public float Curve = 1.0f;

        public bool AutoCenterEnabled = true;
        public float AutoCenterStrength = 6f;
        public int AutoCenterDelay = 150;
        public float AutoCenterDamping = 0.90f;

        public bool EmulationActive = true;
        public bool EnablePrimaryButtons = false;

        public void Validate()
        {
            Deadzone = Math.Clamp(Deadzone, 0f, 0.5f);
            Smoothing = Math.Clamp(Smoothing, 0f, 1f);
            Curve = Math.Clamp(Curve, 0.1f, 3f);

            AutoCenterStrength = Math.Clamp(AutoCenterStrength, 0.5f, 20f);
            AutoCenterDelay = Math.Clamp(AutoCenterDelay, 0, 2000);
            AutoCenterDamping = Math.Clamp(AutoCenterDamping, 0.5f, 0.999f);
        }
    }

    public static class XShark_cfg
    {
        private static readonly string BASE_PATH =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "XShark");

        private static readonly string FILE =
            Path.Combine(BASE_PATH, "xshark.cfg");

        private const int VERSION = 2;

        private static ConfigModel _data = new ConfigModel();
        private static readonly object _lock = new object();

        static XShark_cfg()
        {
            Directory.CreateDirectory(BASE_PATH);
        }

        public static float CurrentDeadzone
        {
            get => _data.Deadzone;
            set { _data.Deadzone = value; _data.Validate(); }
        }

        public static float CurrentSmoothing
        {
            get => _data.Smoothing;
            set { _data.Smoothing = value; _data.Validate(); }
        }

        public static float CurrentCurve
        {
            get => _data.Curve;
            set { _data.Curve = value; _data.Validate(); }
        }

        public static bool AutoCenterEnabled
        {
            get => _data.AutoCenterEnabled;
            set => _data.AutoCenterEnabled = value;
        }

        public static float AutoCenterStrength
        {
            get => _data.AutoCenterStrength;
            set { _data.AutoCenterStrength = value; _data.Validate(); }
        }

        public static int AutoCenterDelayMs
        {
            get => _data.AutoCenterDelay;
            set { _data.AutoCenterDelay = value; _data.Validate(); }
        }

        public static float AutoCenterDamping
        {
            get => _data.AutoCenterDamping;
            set { _data.AutoCenterDamping = value; _data.Validate(); }
        }

        public static bool IsEmulationActive
        {
            get => _data.EmulationActive;
            set => _data.EmulationActive = value;
        }

        public static bool EnablePrimaryButtons
        {
            get => _data.EnablePrimaryButtons;
            set => _data.EnablePrimaryButtons = value;
        }

        public static void Load()
        {
            lock (_lock)
            {
                if (!File.Exists(FILE))
                {
                    Save();
                    return;
                }

                try
                {
                    var lines = File.ReadAllLines(FILE);
                    var map = Parse(lines);
                    Apply(map);
                    _data.Validate();
                }
                catch
                {
                    // Si se corrompe el archivo lo regeneramos
                    Save();
                }
            }
        }

        public static void Save()
        {
            lock (_lock)
            {
                try
                {
                    using var fs = new FileStream(
                        FILE,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None);

                    using var writer = new StreamWriter(fs);

                    writer.WriteLine($"Version={VERSION}");
                    writer.WriteLine($"Deadzone={_data.Deadzone.ToString(CultureInfo.InvariantCulture)}");
                    writer.WriteLine($"Smoothing={_data.Smoothing.ToString(CultureInfo.InvariantCulture)}");
                    writer.WriteLine($"Curve={_data.Curve.ToString(CultureInfo.InvariantCulture)}");
                    writer.WriteLine($"AutoCenterEnabled={_data.AutoCenterEnabled}");
                    writer.WriteLine($"AutoCenterStrength={_data.AutoCenterStrength.ToString(CultureInfo.InvariantCulture)}");
                    writer.WriteLine($"AutoCenterDelay={_data.AutoCenterDelay}");
                    writer.WriteLine($"AutoCenterDamping={_data.AutoCenterDamping.ToString(CultureInfo.InvariantCulture)}");
                    writer.WriteLine($"EmulationActive={_data.EmulationActive}");
                    writer.WriteLine($"EnablePrimaryButtons={_data.EnablePrimaryButtons}");

                    writer.Flush();
                }
                catch
                {
                    // No rompemos UI
                }
            }
        }

        private static Dictionary<string, string> Parse(string[] lines)
        {
            var dict = new Dictionary<string, string>();

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!line.Contains('=')) continue;

                var parts = line.Split('=', 2);
                dict[parts[0].Trim()] = parts[1].Trim();
            }

            return dict;
        }

        private static void Apply(Dictionary<string, string> map)
        {
            TryFloat(map, "Deadzone", ref _data.Deadzone);
            TryFloat(map, "Smoothing", ref _data.Smoothing);
            TryFloat(map, "Curve", ref _data.Curve);

            TryBool(map, "AutoCenterEnabled", ref _data.AutoCenterEnabled);
            TryFloat(map, "AutoCenterStrength", ref _data.AutoCenterStrength);
            TryInt(map, "AutoCenterDelay", ref _data.AutoCenterDelay);
            TryFloat(map, "AutoCenterDamping", ref _data.AutoCenterDamping);

            TryBool(map, "EmulationActive", ref _data.EmulationActive);
            TryBool(map, "EnablePrimaryButtons", ref _data.EnablePrimaryButtons);
        }

        private static void TryFloat(Dictionary<string, string> m, string k, ref float f)
        {
            if (m.TryGetValue(k, out var v) &&
                float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var r))
                f = r;
        }

        private static void TryInt(Dictionary<string, string> m, string k, ref int f)
        {
            if (m.TryGetValue(k, out var v) &&
                int.TryParse(v, out var r))
                f = r;
        }

        private static void TryBool(Dictionary<string, string> m, string k, ref bool f)
        {
            if (m.TryGetValue(k, out var v) &&
                bool.TryParse(v, out var r))
                f = r;
        }
    }
}