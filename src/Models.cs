using System;
using System.Collections.Generic;
using System.Text;

namespace AppBridgeUsb
{
    internal sealed class UsbDevice
    {
        internal string Serial { get; set; }
        internal string Model { get; set; }
        internal string State { get; set; }
        internal string Transport { get; set; }
        internal bool IsReady { get { return String.Equals(State, "device", StringComparison.Ordinal); } }

        public override string ToString()
        {
            string state = IsReady ? "Ready" : State == "unauthorized" ? "Allow USB debugging" : "Offline";
            return Model + "  /  " + state + "  /  USB " + Transport;
        }
    }

    internal sealed class DisplayMetrics
    {
        internal int Width { get; set; }
        internal int Height { get; set; }
        internal int Density { get; set; }

        internal string ToScrcpyValue()
        {
            // Start with a portrait mobile layout. Apps may request landscape later.
            return Math.Min(Width, Height) + "x" + Math.Max(Width, Height) + "/" + Density;
        }
    }

    internal sealed class SessionDetails
    {
        internal int ApiLevel { get; set; }
        internal DisplayMetrics Display { get; set; }
    }

    internal sealed class LaunchOptions
    {
        internal string PackageName { get; set; }
        internal bool Fullscreen { get; set; }
        internal bool ScreenOff { get; set; }
        internal bool Audio { get; set; }
        internal bool RestartApp { get; set; }
    }

    internal static class WindowsArguments
    {
        internal static string Join(IEnumerable<string> arguments)
        {
            StringBuilder result = new StringBuilder();
            foreach (string argument in arguments)
            {
                if (result.Length > 0) result.Append(' ');
                result.Append(Quote(argument));
            }
            return result.ToString();
        }

        internal static string Quote(string value)
        {
            if (value == null) throw new ArgumentNullException("value");
            StringBuilder result = new StringBuilder("\"");
            int backslashes = 0;
            foreach (char character in value)
            {
                if (character == '\\') { backslashes++; continue; }
                if (character == '"')
                    result.Append('\\', backslashes * 2 + 1).Append('"');
                else
                    result.Append('\\', backslashes).Append(character);
                backslashes = 0;
            }
            return result.Append('\\', backslashes * 2).Append('"').ToString();
        }
    }
}
