using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Parental.WinService.Models.Entity;

namespace Parental.WinService;

public class Worker : BackgroundService
{
    private const string RegistryPath = @"HKEY_LOCAL_MACHINE\Software\Parental";
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows())
        {
            _logger.LogCritical("This application is designed to run as a Windows Service.");
            return;
        }

        var deviceJson = Registry.GetValue(RegistryPath, "Device", string.Empty) as string;
        var device = string.IsNullOrWhiteSpace(deviceJson) ? null : JsonSerializer.Deserialize<Device>(deviceJson);

        using (var client = new HttpClient())
        {
            bool? lastLockedState = null;
            bool isDisconnected = false;

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(5000, cancellationToken);

                try
                {
                    var serverAddress = Registry.GetValue(RegistryPath, "ServerAddress", string.Empty) as string;
                    var deviceId = Registry.GetValue(RegistryPath, "DeviceID", string.Empty) as string;
                    if (!string.IsNullOrWhiteSpace(serverAddress) && !string.IsNullOrWhiteSpace(deviceId))
                    {
                        var uri = new Uri($"{serverAddress}/api/Devices/get?id={deviceId}&handshake=true");
                        var newDevice = await client.GetFromJsonAsync<Device>(uri, cancellationToken);
                        if (newDevice != null)
                        {
                            deviceJson = JsonSerializer.Serialize(newDevice);
                            device = newDevice;
                            Registry.SetValue(RegistryPath, "Device", deviceJson, RegistryValueKind.String);
                            isDisconnected = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    isDisconnected = true;
                    _logger.LogError(ex, "An error occurred while sending the heartbeat.");
                }

                if (device == null)
                {
                    _logger.LogWarning("Device information is not available.");
                    continue;
                }

                var sessionInfo = SessionUserHelper.GetActiveConsoleUserInfo(_logger);
                if (sessionInfo == null)
                {
                    _logger.LogWarning("Could not retrieve active console user info.");
                    continue;
                }

                if (!sessionInfo.IsLoggedIn)
                {
                    continue;
                }

                var shouldLock = device.IsLocked();

                if (isDisconnected && device.IsLockedWhileDisconnected)
                {
                    shouldLock = true;
                }

                if (device.AllowedUsernames.Any(username => username.Equals(sessionInfo.UserName, StringComparison.OrdinalIgnoreCase)))
                {
                    shouldLock = false;
                }
                
                if (!lastLockedState.HasValue || lastLockedState.Value != shouldLock)
                {
                    lastLockedState = shouldLock;
                    _logger.LogInformation("Device lock state changed. ShouldLock={ShouldLock}", shouldLock);
                }

                if (!shouldLock)
                {
                    continue;
                }

                var lockResult = SessionUserHelper.LockSession(_logger, sessionInfo.SessionId);
                if (!lockResult)
                {
                    _logger.LogWarning("Lock requested but helper could not be started. SessionId={SessionId}", sessionInfo.SessionId);
                    var disconnected = SessionUserHelper.DisconnectSession(_logger, sessionInfo.SessionId);
                    if (!disconnected)
                        _logger.LogWarning("Fallback disconnect also failed. SessionId={SessionId}", sessionInfo.SessionId);
                }
            }
        }
    }
}
