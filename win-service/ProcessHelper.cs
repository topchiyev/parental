using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

public class ProcessHelper
{
    public static bool CreateProcessAsUser(string filename, string args, ILogger logger, int sessionId)
    {
        if (!OperatingSystem.IsWindows())
        {
            logger.LogError("CreateProcessAsUser is only supported on Windows.");
            return false;
        }

        if (!WTSQueryUserToken((uint)sessionId, out IntPtr hToken))
        {
            int error = Marshal.GetLastWin32Error();
            string errorMessage = new System.ComponentModel.Win32Exception(error).Message;
            logger.LogError($"WTSQueryUserToken failed. error={error}, message={errorMessage}");
            return false;
        }

        IntPtr hDupedToken = IntPtr.Zero;

        PROCESS_INFORMATION pi = new PROCESS_INFORMATION();

        try
        {
            SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();
            sa.Length = Marshal.SizeOf(sa);

            bool result = DuplicateTokenEx(
                hToken,
                GENERIC_ALL_ACCESS,
                ref sa,
                (int)SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                (int)TOKEN_TYPE.TokenPrimary,
                ref hDupedToken
            );

            if (!result)
            {
                int error = Marshal.GetLastWin32Error();
                string errorMessage = new System.ComponentModel.Win32Exception(error).Message;
                logger.LogError($"DuplicateTokenEx failed. error={error}, message={errorMessage}");
                return false;
            }


            STARTUPINFO si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
            si.lpDesktop = @"winsta0\default";
            si.dwFlags = STARTF_USESHOWWINDOW;
            si.wShowWindow = SW_HIDE;

            int dwCreationFlags = CREATE_NO_WINDOW;

            result = CreateProcessAsUser(
                hDupedToken,
                filename,
                args,
                ref sa, ref sa,
                false, dwCreationFlags, IntPtr.Zero,
                Path.GetDirectoryName(filename), ref si, ref pi
            );

            if (!result)
            {
                int error = Marshal.GetLastWin32Error();
                string errorMessage = new System.ComponentModel.Win32Exception(error).Message;
                logger.LogError($"CreateProcessAsUser failed. error={error}, message={errorMessage}");
                return false;
            }

            if (pi.hProcess == IntPtr.Zero)
            {
                logger.LogError("CreateProcessAsUser returned success but process handle is null.");
                return false;
            }

            uint waitResult = WaitForSingleObject(pi.hProcess, HelperExitWaitTimeoutMs);
            if (waitResult == WAIT_TIMEOUT)
            {
                logger.LogWarning("Helper process timed out after {TimeoutMs}ms. Terminating stale instance.", HelperExitWaitTimeoutMs);
                TerminateProcess(pi.hProcess, 1);
                return false;
            }

            if (waitResult == WAIT_FAILED)
            {
                int error = Marshal.GetLastWin32Error();
                string errorMessage = new System.ComponentModel.Win32Exception(error).Message;
                logger.LogError($"WaitForSingleObject failed. error={error}, message={errorMessage}");
                return false;
            }

            if (!GetExitCodeProcess(pi.hProcess, out uint exitCode))
            {
                int error = Marshal.GetLastWin32Error();
                string errorMessage = new System.ComponentModel.Win32Exception(error).Message;
                logger.LogError($"GetExitCodeProcess failed. error={error}, message={errorMessage}");
                return false;
            }

            if (exitCode != 0)
            {
                logger.LogWarning("Helper process exited with non-zero code: {ExitCode}", exitCode);
                return false;
            }

            return true;
        }
        finally
        {
            if (pi.hProcess != IntPtr.Zero)
                CloseHandle(pi.hProcess);
            if (pi.hThread != IntPtr.Zero)
                CloseHandle(pi.hThread);
            if (hDupedToken != IntPtr.Zero)
                CloseHandle(hDupedToken);
            if (hToken != IntPtr.Zero)
                CloseHandle(hToken);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct STARTUPINFO
    {
        public int cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public int dwX;
        public int dwY;
        public int dwXSize;
        public int dwXCountChars;
        public int dwYCountChars;
        public int dwFillAttribute;
        public int dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public int dwProcessID;
        public int dwThreadID;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SECURITY_ATTRIBUTES
    {
        public int Length;
        public IntPtr lpSecurityDescriptor;
        public bool bInheritHandle;
    }

    private enum SECURITY_IMPERSONATION_LEVEL
    {
        SecurityAnonymous,
        SecurityIdentification,
        SecurityImpersonation,
        SecurityDelegation
    }

    private enum TOKEN_TYPE
    {
        TokenPrimary = 1,
        TokenImpersonation
    }

    private const int GENERIC_ALL_ACCESS = 0x10000000;
    private const int CREATE_NO_WINDOW = 0x08000000;
    private const int STARTF_USESHOWWINDOW = 0x00000001;
    private const int SW_HIDE = 0;
    private const uint WAIT_TIMEOUT = 0x00000102;
    private const uint WAIT_FAILED = 0xFFFFFFFF;
    private const uint HelperExitWaitTimeoutMs = 3000;

    [DllImport("kernel32.dll",
        EntryPoint = "CloseHandle", SetLastError = true,
        CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall
    )]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

    [DllImport("advapi32.dll",
        EntryPoint = "CreateProcessAsUser", SetLastError = true,
        CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall
    )]
    private static extern bool CreateProcessAsUser(
        IntPtr hToken, string lpApplicationName, string lpCommandLine,
        ref SECURITY_ATTRIBUTES lpProcessAttributes, ref SECURITY_ATTRIBUTES lpThreadAttributes,
        bool bInheritHandle, int dwCreationFlags, IntPtr lpEnvrionment,
        string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo,
        ref PROCESS_INFORMATION lpProcessInformation
    );

    [DllImport("advapi32.dll",
        EntryPoint = "DuplicateTokenEx", SetLastError = true,
        CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall
    )]
    private static extern bool DuplicateTokenEx(
        IntPtr hExistingToken, int dwDesiredAccess,
        ref SECURITY_ATTRIBUTES lpThreadAttributes,
        int ImpersonationLevel, int dwTokenType,
        ref IntPtr phNewToken
    );

    [DllImport("Wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSQueryUserToken(uint SessionId, out IntPtr phToken);
}
