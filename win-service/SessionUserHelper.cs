using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Parental.WinService;

public static class SessionUserHelper
{
    public sealed class ActiveUserInfo
    {
        public bool IsLoggedIn { get; init; }
        public int SessionId { get; init; } = -1;
        public string UserName { get; init; }
        public string Domain { get; init; }
    }

    public static ActiveUserInfo GetActiveConsoleUserInfo(ILogger logger)
    {
        if (!OperatingSystem.IsWindows())
        {
            logger.LogError("GetActiveConsoleUserInfo is only supported on Windows.");
            return null;
        }

        int sessionId = WTSGetActiveConsoleSessionId();
        if (sessionId == -1)
        {
            return new ActiveUserInfo
            {
                IsLoggedIn = false,
                SessionId = -1
            };
        }

        string user = QuerySessionString(sessionId, WTS_INFO_CLASS.WTSUserName, logger);
        string domain = QuerySessionString(sessionId, WTS_INFO_CLASS.WTSDomainName, logger);

        if (string.IsNullOrWhiteSpace(user))
        {
            return new ActiveUserInfo
            {
                IsLoggedIn = false,
                SessionId = sessionId,
                UserName = null,
                Domain = null,
            };
        }

        return new ActiveUserInfo
        {
            IsLoggedIn = true,
            SessionId = sessionId,
            UserName = user,
            Domain = domain
        };
    }

    private static string QuerySessionString(int sessionId, WTS_INFO_CLASS infoClass, ILogger logger)
    {
        if (!OperatingSystem.IsWindows())
        {
            logger.LogError("QuerySessionString is only supported on Windows.");
            return null;
        }

        IntPtr buffer = IntPtr.Zero;
        int bytes = 0;

        try
        {
            bool ok = WTSQuerySessionInformation(
                IntPtr.Zero, // WTS_CURRENT_SERVER_HANDLE
                sessionId,
                infoClass,
                out buffer,
                out bytes);

            if (!ok || buffer == IntPtr.Zero || bytes <= 1)
                return null;

            // WTS returns a null-terminated Unicode string.
            string s = Marshal.PtrToStringUni(buffer);
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }
        finally
        {
            if (buffer != IntPtr.Zero)
                WTSFreeMemory(buffer);
        }
    }

    public static bool LockSession(ILogger logger, int sessionId)
    {
        var fileName = "wsprnsvchlp.exe";

        var path1 = Path.Combine(AppContext.BaseDirectory, fileName);
        var currentProcessInfo = Process.GetCurrentProcess();
        var path2 = currentProcessInfo.MainModule?.FileName;
        path2 = Path.GetDirectoryName(path2);
        path2 = Path.Combine(path2, fileName);

        string path;

        if (File.Exists(path1))
        {
            path = path1;
        }
        else if (File.Exists(path2))
        {
            path = path2;
        }
        else
        {
            logger.LogError($"LockSession failed: helper executable not found at '{path1}' or '{path2}'.");
            return false;
        }
        
        try
        {
            var started = ProcessHelper.CreateProcessAsUser(path, string.Empty, logger, sessionId);
            if (!started)
                return false;

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to lock session: {ex}");
            return false;
        }
    }

    public static bool DisconnectSession(ILogger logger, int sessionId)
    {
        if (!OperatingSystem.IsWindows())
        {
            logger.LogError("DisconnectSession is only supported on Windows.");
            return false;
        }

        bool ok = WTSDisconnectSession(IntPtr.Zero, sessionId, false);
        if (!ok)
        {
            int error = Marshal.GetLastWin32Error();
            string errorMessage = new System.ComponentModel.Win32Exception(error).Message;
            logger.LogError($"WTSDisconnectSession failed. sessionId={sessionId}, error={error}, message={errorMessage}");
            return false;
        }

        return true;
    }

    #region P/Invoke

    [DllImport("kernel32.dll")]
    private static extern int WTSGetActiveConsoleSessionId();

    [DllImport("Wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSQueryUserToken(uint SessionId, out IntPtr phToken);

    [DllImport("Wtsapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool WTSQuerySessionInformation(
        IntPtr hServer,
        int sessionId,
        WTS_INFO_CLASS wtsInfoClass,
        out IntPtr ppBuffer,
        out int pBytesReturned);

    [DllImport("Wtsapi32.dll")]
    private static extern void WTSFreeMemory(IntPtr pMemory);

    [DllImport("Wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSDisconnectSession(IntPtr hServer, int sessionId, bool bWait);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool DuplicateToken(
        IntPtr ExistingTokenHandle,
        SECURITY_IMPERSONATION_LEVEL ImpersonationLevel,
        out IntPtr DuplicateTokenHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    #endregion

    private enum WTS_INFO_CLASS
    {
        WTSInitialProgram = 0,
        WTSApplicationName = 1,
        WTSWorkingDirectory = 2,
        WTSOEMId = 3,
        WTSSessionId = 4,
        WTSUserName = 5,
        WTSWinStationName = 6,
        WTSDomainName = 7,
        WTSConnectState = 8,
        WTSClientBuildNumber = 9,
        WTSClientName = 10,
        WTSClientDirectory = 11,
        WTSClientProductId = 12,
        WTSClientHardwareId = 13,
        WTSClientAddress = 14,
        WTSClientDisplay = 15,
        WTSClientProtocolType = 16
    }

    private enum SECURITY_IMPERSONATION_LEVEL
    {
        SecurityAnonymous,
        SecurityIdentification,
        SecurityImpersonation,
        SecurityDelegation
    }
}
