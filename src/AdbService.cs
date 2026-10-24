using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AppBridgeUsb
{
    internal sealed class AdbService
    {
        private const int QueryTimeoutMilliseconds = 15000;
        private readonly string executable;
        private static readonly Regex PackagePattern = new Regex(
            @"^[A-Za-z][A-Za-z0-9_]*(?:\.[A-Za-z0-9_]+)+$", RegexOptions.CultureInvariant);

        internal AdbService(string adbPath)
        {
            executable = Path.GetFullPath(adbPath);
        }

        internal static bool IsPackageName(string value)
        {
            return !String.IsNullOrEmpty(value) && value.Length <= 255 && PackagePattern.IsMatch(value);
        }

        internal static void UseLocalServer(ProcessStartInfo start)
        {
            // In particular, ANDROID_SERIAL must not override -d during USB verification.
            foreach (string name in new[] { "ANDROID_SERIAL", "ADB_SERVER_SOCKET", "ANDROID_ADB_SERVER_ADDRESS", "ANDROID_ADB_SERVER_PORT", "ADB_TRACE" })
                start.EnvironmentVariables.Remove(name);
        }

        internal async Task<List<UsbDevice>> GetUsbDevicesAsync(CancellationToken cancellationToken)
        {
            string output = await RunAsync(new[] { "devices", "-l" }, "find USB devices", cancellationToken).ConfigureAwait(false);
            List<UsbDevice> devices = new List<UsbDevice>();
            List<UsbDevice> unverified = new List<UsbDevice>();
            foreach (string line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] fields = Regex.Split(line.Trim(), @"\s+");
                if (fields.Length < 2 || fields[0] == "List" || fields[0].StartsWith("*", StringComparison.Ordinal)) continue;
                string transport = FindField(fields, "usb:");
                if (fields[0].StartsWith("emulator-", StringComparison.Ordinal) ||
                    fields[0].IndexOf(':') >= 0 || fields[0].IndexOf("_adb-tls-", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (fields[1] != "device" && fields[1] != "unauthorized" && fields[1] != "offline") continue;
                string model = FindField(fields, "model:");
                UsbDevice device = new UsbDevice {
                    Serial = fields[0], State = fields[1], Transport = transport,
                    Model = String.IsNullOrEmpty(model) ? "Android device" : model.Replace('_', ' ')
                };
                if (String.IsNullOrEmpty(transport)) unverified.Add(device);
                else devices.Add(device);
            }
            if (unverified.Count > 0)
            {
                // Windows/libusb may omit usb: in the long listing. -d asks ADB itself
                // to select a USB transport; never infer USB merely from a serial's shape.
                string singleUsbSerial = null;
                try
                {
                    singleUsbSerial = (await RunAsync(new[] { "-d", "get-serialno" },
                        "verify the USB transport", cancellationToken).ConfigureAwait(false)).Trim();
                }
                catch (InvalidOperationException) { /* Ambiguous or not authorized; keep unverified devices excluded. */ }
                UsbDevice verified = unverified.FirstOrDefault(device => String.Equals(device.Serial, singleUsbSerial, StringComparison.Ordinal));
                if (verified != null)
                {
                    verified.Transport = "verified";
                    devices.Add(verified);
                }
                if (devices.Count == 0)
                    throw new InvalidOperationException("ADB could not verify a physical USB phone. Connect one USB phone, unlock it and allow USB debugging, then refresh. Disconnect any other USB phones while authorizing it.");
            }
            return devices.OrderBy(device => device.Model, StringComparer.OrdinalIgnoreCase)
                .ThenBy(device => device.Transport, StringComparer.Ordinal).ToList();
        }

        internal async Task<List<string>> ListPackagesAsync(string serial, CancellationToken cancellationToken)
        {
            await RequireUsbDeviceAsync(serial, cancellationToken).ConfigureAwait(false);
            string output = await RunDeviceAsync(serial, new[] { "shell", "pm", "list", "packages", "--user", "current" },
                "list installed apps", cancellationToken).ConfigureAwait(false);
            return output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(line => line.StartsWith("package:", StringComparison.Ordinal))
                .Select(line => line.Substring(8).Trim()).Where(IsPackageName)
                .Distinct(StringComparer.Ordinal).OrderBy(package => package, StringComparer.OrdinalIgnoreCase).ToList();
        }

        internal async Task<SessionDetails> ReadSessionDetailsAsync(string serial, string packageName, CancellationToken cancellationToken)
        {
            if (!IsPackageName(packageName)) throw new InvalidOperationException("Enter a valid Android package, for example org.videolan.vlc.");
            await RequireUsbDeviceAsync(serial, cancellationToken).ConfigureAwait(false);
            string apiText = await RunDeviceAsync(serial, new[] { "shell", "getprop", "ro.build.version.sdk" },
                "read the Android version", cancellationToken).ConfigureAwait(false);
            int apiLevel;
            if (!Int32.TryParse(apiText.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out apiLevel))
                throw new InvalidOperationException("The Android version could not be read. Unlock the phone and try again.");
            if (apiLevel < 31)
                throw new InvalidOperationException("This beta requires Android 12 or newer (API 31). Choose a compatible USB device.");

            string packageOutput = await RunDeviceAsync(serial,
                new[] { "shell", "pm", "path", "--user", "current", packageName }, "check the selected app", cancellationToken).ConfigureAwait(false);
            if (!packageOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Any(line => line.StartsWith("package:", StringComparison.Ordinal)))
                throw new InvalidOperationException("That package is not installed for the current Android user. Select List apps and choose an installed package.");

            Task<string> size = RunDeviceAsync(serial, new[] { "shell", "wm", "size" }, "read the display size", cancellationToken);
            Task<string> density = RunDeviceAsync(serial, new[] { "shell", "wm", "density" }, "read the display density", cancellationToken);
            await Task.WhenAll(size, density).ConfigureAwait(false);
            return new SessionDetails { ApiLevel = apiLevel, Display = ParseDisplay(size.Result, density.Result) };
        }

        private async Task RequireUsbDeviceAsync(string serial, CancellationToken cancellationToken)
        {
            List<UsbDevice> devices = await GetUsbDevicesAsync(cancellationToken).ConfigureAwait(false);
            UsbDevice device = devices.FirstOrDefault(item => String.Equals(item.Serial, serial, StringComparison.Ordinal));
            if (device == null) throw new InvalidOperationException("The selected USB device is no longer connected. Reconnect the cable and select Refresh devices.");
            if (!device.IsReady)
                throw new InvalidOperationException(device.State == "unauthorized"
                    ? "Unlock the phone and allow USB debugging, then select Refresh devices."
                    : "The selected USB device is offline. Unlock it, reconnect the cable and select Refresh devices.");
        }

        private static DisplayMetrics ParseDisplay(string sizeOutput, string densityOutput)
        {
            Match size = SelectMetric(sizeOutput, @"size:\s*(\d+)\s*x\s*(\d+)");
            Match density = SelectMetric(densityOutput, @"density:\s*(\d+)");
            int width, height, dpi;
            if (!size.Success || !density.Success ||
                !Int32.TryParse(size.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out width) ||
                !Int32.TryParse(size.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out height) ||
                !Int32.TryParse(density.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out dpi) ||
                width < 1 || height < 1 || dpi < 1)
                throw new InvalidOperationException("The phone's display size or density could not be read. A virtual display cannot be started safely. Unlock the phone and try again.");
            return new DisplayMetrics { Width = width, Height = height, Density = dpi };
        }

        private static Match SelectMetric(string output, string expression)
        {
            // Respect an existing user override. These queries never change display 0.
            Match selected = Regex.Match(output, @"Override\s+" + expression, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            return selected.Success ? selected : Regex.Match(output, @"Physical\s+" + expression, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        private static string FindField(string[] fields, string prefix)
        {
            foreach (string field in fields)
                if (field.StartsWith(prefix, StringComparison.Ordinal)) return field.Substring(prefix.Length);
            return null;
        }

        private Task<string> RunDeviceAsync(string serial, string[] command, string action, CancellationToken cancellationToken)
        {
            List<string> arguments = new List<string> { "-s", serial };
            arguments.AddRange(command);
            return RunAsync(arguments.ToArray(), action, cancellationToken);
        }

        private async Task<string> RunAsync(string[] arguments, string action, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ProcessStartInfo start = new ProcessStartInfo(executable, WindowsArguments.Join(arguments));
            start.WorkingDirectory = Path.GetDirectoryName(executable);
            start.UseShellExecute = false;
            start.CreateNoWindow = true;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            start.StandardOutputEncoding = Encoding.UTF8;
            start.StandardErrorEncoding = Encoding.UTF8;
            UseLocalServer(start);
            using (Process process = new Process())
            {
                process.StartInfo = start;
                bool started = false;
                try
                {
                    if (!process.Start()) throw new InvalidOperationException("ADB could not start. Prepare the components again.");
                    started = true;
                    Task<string> output = process.StandardOutput.ReadToEndAsync();
                    Task<string> errors = process.StandardError.ReadToEndAsync();
                    await Task.Run(delegate {
                        Stopwatch elapsed = Stopwatch.StartNew();
                        while (!process.WaitForExit(100))
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (elapsed.ElapsedMilliseconds >= QueryTimeoutMilliseconds)
                                throw new TimeoutException("ADB timed out while trying to " + action + ". Check the USB cable and unlock the phone.");
                        }
                    }).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();
                    Task streams = Task.WhenAll(output, errors);
                    if (await Task.WhenAny(streams, Task.Delay(2000, cancellationToken)).ConfigureAwait(false) != streams)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        throw new TimeoutException("ADB did not finish its response. Reconnect the USB cable and try again.");
                    }
                    await streams.ConfigureAwait(false);
                    if (process.ExitCode != 0)
                        throw new InvalidOperationException("ADB could not " + action + ". Unlock the phone, allow USB debugging and try again.");
                    return output.Result;
                }
                finally
                {
                    if (started)
                    {
                        try { if (!process.HasExited) process.Kill(); }
                        catch (InvalidOperationException) { }
                        catch (System.ComponentModel.Win32Exception) { }
                    }
                }
            }
        }
    }
}
