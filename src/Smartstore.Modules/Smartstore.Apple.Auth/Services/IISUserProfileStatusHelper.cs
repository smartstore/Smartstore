#nullable enable

using System;
using System.Security.Principal;
using Microsoft.Win32;

namespace Smartstore.Apple.Auth.Services
{
    public enum UserProfileStatus
    {
        Enabled,        // User profile appears to be loaded (Windows)
        Disabled,       // User profile appears not loaded (Windows)
        Indeterminate,  // Signals are mixed/unclear (Windows)
        NotApplicable   // Non-Windows platforms (no IIS flag)
    }

    /// <summary>
    /// Fast, read-only heuristic to infer IIS "Load User Profile" for the current app pool.
    /// </summary>
    public static class IISUserProfileStatusHelper
    {
        public static UserProfileStatus GetStatus()
        {
            // Non-Windows: IIS flag does not exist.
            if (!OperatingSystem.IsWindows())
                return UserProfileStatus.NotApplicable;

            try
            {
                var sid = WindowsIdentity.GetCurrent().User?.Value;
                if (string.IsNullOrWhiteSpace(sid))
                    return UserProfileStatus.Indeterminate;

                using var hive = Registry.Users.OpenSubKey(sid, writable: false);
                return hive != null ? UserProfileStatus.Enabled : UserProfileStatus.Disabled;
            }
            catch
            {
                return UserProfileStatus.Indeterminate;
            }
        }
    }   
}