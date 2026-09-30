using Microsoft.Win32;
using NLog;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Micser.DriverUtility
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class UacHelper
    {
        private const string UacRegistryKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";
        private const string UacRegistryValue = "EnableLUA";
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private static readonly uint STANDARD_RIGHTS_READ = 0x00020000;
        private static readonly uint TOKEN_QUERY = 0x0008;
        private static readonly uint TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);

        public enum TOKEN_ELEVATION_TYPE
        {
            TokenElevationTypeDefault = 1,
            TokenElevationTypeFull,
            TokenElevationTypeLimited
        }

        public enum TOKEN_INFORMATION_CLASS
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin,
            TokenElevationType,
            TokenLinkedToken,
            TokenElevation,
            TokenHasRestrictions,
            TokenAccessInformation,
            TokenVirtualizationAllowed,
            TokenVirtualizationEnabled,
            TokenIntegrityLevel,
            TokenUIAccess,
            TokenMandatoryPolicy,
            TokenLogonSid,
            MaxTokenInfoClass
        }

        public static bool IsProcessElevated
        {
            get
            {
                if (IsUacEnabled)
                {
                    if (!NativeMethods.OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_READ, out var tokenHandle))
                    {
                        Logger.Error("Could not get process token. Win32 Error Code: " + Marshal.GetLastWin32Error());
                        return false;
                    }

                    try
                    {
                        var elevationResultSize = Marshal.SizeOf(typeof(int));

                        var elevationTypePtr = Marshal.AllocHGlobal(elevationResultSize);
                        try
                        {
                            var success = NativeMethods.GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenElevationType, elevationTypePtr, (uint)elevationResultSize, out _);
                            if (success)
                            {
                                var elevationResult = (TOKEN_ELEVATION_TYPE)Marshal.ReadInt32(elevationTypePtr);
                                return elevationResult == TOKEN_ELEVATION_TYPE.TokenElevationTypeFull;
                            }
                            else
                            {
                                Logger.Error($"GetTokenInformation returned false. Win32 Error Code: " + Marshal.GetLastWin32Error());
                                return false;
                            }
                        }
                        finally
                        {
                            if (elevationTypePtr != IntPtr.Zero)
                            {
                                Marshal.FreeHGlobal(elevationTypePtr);
                            }
                        }
                    }
                    finally
                    {
                        if (tokenHandle != IntPtr.Zero)
                        {
                            NativeMethods.CloseHandle(tokenHandle);
                        }
                    }
                }

                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                var result = principal.IsInRole(WindowsBuiltInRole.Administrator)
                             || principal.IsInRole(0x200); // Domain Administrator
                return result;
            }
        }

        public static bool IsUacEnabled
        {
            get
            {
                using (var uacKey = Registry.LocalMachine.OpenSubKey(UacRegistryKey, false))
                {
                    return uacKey != null && uacKey.GetValue(UacRegistryValue).Equals(1);
                }
            }
        }

        private static class NativeMethods
        {
            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CloseHandle(IntPtr hObject);

            [DllImport("advapi32.dll", SetLastError = true)]
            internal static extern bool GetTokenInformation(IntPtr TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, IntPtr TokenInformation, uint TokenInformationLength, out uint ReturnLength);

            [DllImport("advapi32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);
        }
    }
}