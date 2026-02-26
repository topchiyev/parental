using System;
using System.Runtime.InteropServices;

namespace Parental.WinServiceHelper;

public static class Program
{
    public static void Main(string[] args)
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("This application is designed to run on Windows.");
            return;
        }

        LockWorkStation();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool LockWorkStation();
}