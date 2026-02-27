using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Parental.WinServiceHelper;

public static class Program
{
    public static int Main(string[] args)
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("This application is designed to run on Windows.");
            return 2;
        }

        // Retry briefly to survive transient session transitions (for example after wake).
        const int attempts = 3;
        for (var i = 0; i < attempts; i++)
        {
            if (LockWorkStation())
                return 0;

            Thread.Sleep(200);
        }

        return 1;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool LockWorkStation();
}
