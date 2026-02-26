using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Security.Principal;
using System.IO;

namespace Parental.WinService;

public static class Program
{
    public static void Main(string[] args)
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("This application is designed to run as a Windows Service.");
            return;
        }

        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        var isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);

        if (!isAdmin)
        {
            Console.WriteLine("This application must be run with administrator privileges.");
            return;
        }

        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddWindowsService(options =>
        {
            options.ServiceName = "wsprnsvc";
        });

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            if (OperatingSystem.IsWindows())
            {
                builder.AddEventLog(options =>
                {
                    if (OperatingSystem.IsWindows())
                    {
                        options.LogName = "Application";
                        options.SourceName = "wsprnsvc";
                    }
                });
            }
        });

        var logger = loggerFactory.CreateLogger("wsprnsvc");
        logger.LogInformation("Example log message");

        builder.Services.AddHostedService<Worker>();

        var host = builder.Build();
        host.Run();
    }
}
