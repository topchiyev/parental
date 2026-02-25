using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Parental.WinService;

public static class SessionUserHelper
{
    public sealed class ActiveUserInfo
    {
        public bool IsLoggedIn { get; init; }
        public int SessionId { get; init; } = -1;
        public string UserName { get; init; }
        public string Domain { get; init; }
        public bool? IsAdministrator { get; init; } // null if unknown
    }

    public static ActiveUserInfo GetActiveConsoleUserInfo()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("SessionUserHelper is only supported on Windows.");
        }

        int sessionId = WTSGetActiveConsoleSessionId();
        if (sessionId == -1)
        {
            return new ActiveUserInfo
            {
                IsLoggedIn = false,
                SessionId = -1,
                IsAdministrator = null
            };
        }

        string user = QuerySessionString(sessionId, WTS_INFO_CLASS.WTSUserName);
        string domain = QuerySessionString(sessionId, WTS_INFO_CLASS.WTSDomainName);

        if (string.IsNullOrWhiteSpace(user))
        {
            return new ActiveUserInfo
            {
                IsLoggedIn = false,
                SessionId = sessionId,
                UserName = null,
                Domain = null,
                IsAdministrator = null
            };
        }

        bool? isAdmin = null;
        try
        {
            isAdmin = TryIsSessionUserAdministrator(sessionId);
        }
        catch
        {
            // Keep null if token query fails; don't crash your service loop.
            isAdmin = null;
        }

        return new ActiveUserInfo
        {
            IsLoggedIn = true,
            SessionId = sessionId,
            UserName = user,
            Domain = domain,
            IsAdministrator = isAdmin
        };
    }

    private static bool? TryIsSessionUserAdministrator(int sessionId)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("SessionUserHelper is only supported on Windows.");
        }

        IntPtr userToken = IntPtr.Zero;
        IntPtr duplicatedToken = IntPtr.Zero;

        try
        {
            if (!WTSQueryUserToken((uint)sessionId, out userToken))
            {
                int err = Marshal.GetLastWin32Error();
                Console.Error.WriteLine($"WTSQueryUserToken failed. sessionId={sessionId}, error={err}");
                return null; // unknown (not false)
            }

            if (!DuplicateToken(userToken, SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation, out duplicatedToken))
            {
                int err = Marshal.GetLastWin32Error();
                Console.Error.WriteLine($"DuplicateToken failed. error={err}");
                return null;
            }

            using var identity = new WindowsIdentity(duplicatedToken);
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        finally
        {
            if (duplicatedToken != IntPtr.Zero) CloseHandle(duplicatedToken);
            if (userToken != IntPtr.Zero) CloseHandle(userToken);
        }
    }

    private static string QuerySessionString(int sessionId, WTS_INFO_CLASS infoClass)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("SessionUserHelper is only supported on Windows.");
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

    public static void LockSession()
    {
        var success = false;
        try
        {
            success = LockWorkStation();
        }
        catch { }

        if (success)
            return;

        try {
            int sessionId = WTSGetActiveConsoleSessionId();
            if (sessionId == -1)
                return;

            WTSLogoffSession(IntPtr.Zero, sessionId, false);
        }
        catch { }
    }

    // ---------- P/Invoke ----------

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

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool DuplicateToken(
        IntPtr ExistingTokenHandle,
        SECURITY_IMPERSONATION_LEVEL ImpersonationLevel,
        out IntPtr DuplicateTokenHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool LockWorkStation();

    [DllImport("Wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSLogoffSession(IntPtr hServer, int sessionId, bool bWait);

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