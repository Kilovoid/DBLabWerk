using Avalonia;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace SQLWerk;

sealed class Program
{
    private const string MutexName = "com.sqlwerk.app.singleinstance";

    [STAThread]
    public static void Main(string[] args)
    {
        using var mutex = new Mutex(false, MutexName);

        if (!mutex.WaitOne(0))
        {
            ActivateExistingInstance();
            return;
        }

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();

    private static void ActivateExistingInstance()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var current = Process.GetCurrentProcess();
        foreach (var p in Process.GetProcessesByName(current.ProcessName))
        {
            if (p.Id == current.Id || p.MainWindowHandle == IntPtr.Zero)
                continue;

            NativeMethods.ShowWindow(p.MainWindowHandle, NativeMethods.SW_RESTORE);
            NativeMethods.SetForegroundWindow(p.MainWindowHandle);
            break;
        }
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public const int SW_RESTORE = 9;
    }
}
