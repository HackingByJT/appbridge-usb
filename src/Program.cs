using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AppBridgeUsb
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            try { SetProcessDpiAwarenessContext(new IntPtr(-4)); }
            catch (EntryPointNotFoundException) { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new AppBridgeForm());
        }

        [DllImport("user32.dll")]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr value);
    }
}
