using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

internal static class AppBridgeControls
{
    private const int KeyboardHook = 13;
    private const int KeyDown = 0x0100, KeyUp = 0x0101;
    private const int SysKeyDown = 0x0104, SysKeyUp = 0x0105;
    private const int Escape = 0x1B, F11 = 0x7A, F12 = 0x7B;
    private static readonly HookCallback Callback = OnKey;
    private static readonly Stopwatch Clock = Stopwatch.StartNew();
    private static IntPtr hook, requestedWindow;
    private static Process player;
    private static int playerId, exitCode;
    private static bool f12Held, escapeHeld, waitingForToggle, expectedState;
    private static bool? requestedState;
    private static long toggleSentAt;

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            SetProcessDpiAwarenessContext(new IntPtr(-4));
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length < 1 || !File.Exists(args[0]) ||
                !String.Equals(Path.GetFileName(args[0]), "scrcpy.exe", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The screen engine was not found. Prepare components in AppBridge USB.");

            hook = SetWindowsHookEx(KeyboardHook, Callback, GetModuleHandle(null), 0);
            if (hook == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "F12 and Esc could not be enabled.");

            string executable = Path.GetFullPath(args[0]);
            StringBuilder arguments = new StringBuilder();
            for (int i = 1; i < args.Length; i++)
            {
                if (i > 1) arguments.Append(' ');
                arguments.Append(QuoteArgument(args[i]));
            }
            ProcessStartInfo start = new ProcessStartInfo(executable, arguments.ToString());
            start.WorkingDirectory = Path.GetDirectoryName(executable);
            start.UseShellExecute = false;
            start.CreateNoWindow = true;
            // Route keys through SDL's window messages so captured Esc cannot reach Android as raw input.
            start.EnvironmentVariables["SDL_WINDOWS_RAW_KEYBOARD"] = "0";
            player = Process.Start(start);
            if (player == null) throw new InvalidOperationException("AppBridge could not open the Android window.");
            playerId = player.Id;

            using (System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer())
            {
                timer.Interval = 25;
                timer.Tick += OnTick;
                timer.Start();
                Application.Run();
            }
            if (exitCode != 0)
                MessageBox.Show("The Android connection closed. Check the USB cable, unlock the phone and start a new session in AppBridge USB.",
                    "AppBridge USB", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return exitCode;
        }
        catch (Exception error)
        {
            MessageBox.Show(error.Message, "AppBridge USB", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
        finally
        {
            playerId = 0;
            if (hook != IntPtr.Zero) UnhookWindowsHookEx(hook);
            if (player != null)
            {
                try
                {
                    if (!player.HasExited)
                    {
                        bool closing = player.CloseMainWindow();
                        if (!closing || !player.WaitForExit(2000)) player.Kill();
                    }
                }
                catch (InvalidOperationException) { /* The owned process already exited. */ }
                catch (Win32Exception)
                {
                    if (!player.HasExited)
                        MessageBox.Show("Close the Android window to end the USB session.", "AppBridge USB");
                }
                finally { player.Dispose(); }
            }
        }
    }

    private static IntPtr OnKey(int code, IntPtr message, IntPtr data)
    {
        if (code < 0 || data == IntPtr.Zero) return CallNextHookEx(hook, code, message, data);
        int key = Marshal.ReadInt32(data);
        if (key != F12 && key != Escape) return CallNextHookEx(hook, code, message, data);
        if ((Marshal.ReadInt32(data, 8) & 0x10) != 0) return CallNextHookEx(hook, code, message, data);

        int kind = message.ToInt32();
        bool down = kind == KeyDown || kind == SysKeyDown;
        bool up = kind == KeyUp || kind == SysKeyUp;
        if (!down && !up) return CallNextHookEx(hook, code, message, data);
        bool held = key == F12 ? f12Held : escapeHeld;
        if (held)
        {
            if (up)
            {
                if (key == F12) f12Held = false; else escapeHeld = false;
            }
            // Pair key-up with its captured key-down and suppress autorepeat.
            return new IntPtr(1);
        }

        IntPtr window = GetForegroundWindow();
        uint processId;
        GetWindowThreadProcessId(window, out processId);
        if (!down || playerId == 0 || processId != (uint)playerId || HasModifiers())
            return CallNextHookEx(hook, code, message, data);

        if (key == F12) f12Held = true; else escapeHeld = true;
        requestedWindow = window;
        requestedState = key == F12;
        return new IntPtr(1);
    }

    private static void OnTick(object sender, EventArgs args)
    {
        if (player.HasExited)
        {
            exitCode = player.ExitCode;
            playerId = 0;
            Application.ExitThread();
            return;
        }
        if (!requestedState.HasValue) return;
        if (GetForegroundWindow() != requestedWindow || !IsWindow(requestedWindow))
        {
            ClearRequest();
            return;
        }
        uint currentProcessId;
        GetWindowThreadProcessId(requestedWindow, out currentProcessId);
        if (currentProcessId != (uint)playerId) { ClearRequest(); return; }
        if (HasModifiers() || (GetAsyncKeyState(F11) & 0x8000) != 0) return;
        bool fullscreen = IsFullscreen(requestedWindow);
        if (waitingForToggle)
        {
            if (fullscreen == expectedState) waitingForToggle = false;
            else
            {
                if (Clock.ElapsedMilliseconds - toggleSentAt > 2000) ClearRequest();
                return;
            }
        }
        if (fullscreen == requestedState.Value)
        {
            ClearRequest();
            return;
        }
        // Zero scancode deliberately selects SDL 3.4.18's virtual-key path.
        // Messages target this exact window; no input is injected into other apps.
        bool posted = PostMessage(requestedWindow, KeyDown, new IntPtr(F11), new IntPtr(1));
        bool released = PostMessage(requestedWindow, KeyUp, new IntPtr(F11), new IntPtr(0xC0000001L));
        if (!posted || !released) { ClearRequest(); return; }
        expectedState = !fullscreen;
        waitingForToggle = true;
        toggleSentAt = Clock.ElapsedMilliseconds;
    }

    private static void ClearRequest()
    {
        requestedState = null;
        requestedWindow = IntPtr.Zero;
        waitingForToggle = false;
    }

    private static bool HasModifiers()
    {
        return (GetAsyncKeyState(0x10) & 0x8000) != 0 || (GetAsyncKeyState(0x11) & 0x8000) != 0 ||
               (GetAsyncKeyState(0x12) & 0x8000) != 0 || (GetAsyncKeyState(0x5B) & 0x8000) != 0 ||
               (GetAsyncKeyState(0x5C) & 0x8000) != 0;
    }

    private static bool IsFullscreen(IntPtr window)
    {
        const long Caption = 0x00C00000;
        if ((GetWindowLongPtr(window, -16).ToInt64() & Caption) != 0) return false;
        Rect bounds;
        MonitorInfo monitor = new MonitorInfo();
        monitor.Size = Marshal.SizeOf(typeof(MonitorInfo));
        if (!GetWindowRect(window, out bounds) || !GetMonitorInfo(MonitorFromWindow(window, 2), ref monitor))
            return false;
        return Math.Abs(bounds.Left - monitor.Monitor.Left) <= 2 &&
               Math.Abs(bounds.Top - monitor.Monitor.Top) <= 2 &&
               Math.Abs(bounds.Right - monitor.Monitor.Right) <= 2 &&
               Math.Abs(bounds.Bottom - monitor.Monitor.Bottom) <= 2;
    }

    private static string QuoteArgument(string value)
    {
        if (value.Length > 0 && value.IndexOfAny(new char[] { ' ', '\t', '\n', '\r', '"' }) < 0) return value;
        StringBuilder result = new StringBuilder("\"");
        int slashes = 0;
        foreach (char c in value)
        {
            if (c == '\\') { slashes++; continue; }
            if (c == '"') result.Append('\\', slashes * 2 + 1).Append('"');
            else result.Append('\\', slashes).Append(c);
            slashes = 0;
        }
        return result.Append('\\', slashes * 2).Append('"').ToString();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo { public int Size; public Rect Monitor, Work; public uint Flags; }
    private delegate IntPtr HookCallback(int code, IntPtr message, IntPtr data);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int type, HookCallback callback, IntPtr module, uint thread);
    [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr handle);
    [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr handle, int code, IntPtr message, IntPtr data);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr GetModuleHandle(string name);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern bool SetProcessDpiAwarenessContext(IntPtr context);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, out Rect bounds);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool PostMessage(IntPtr window, int message, IntPtr key, IntPtr data);
}
