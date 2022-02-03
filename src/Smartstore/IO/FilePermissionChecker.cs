using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using Smartstore.Data;
using Smartstore.Engine;

namespace Smartstore.IO
{
    public class FilePermissionChecker : IFilePermissionChecker
    {
        /// <summary>
        /// Gets the relative paths of all directories that require write access.
        /// </summary>
        public static readonly string[] WrittenDirectories = new[]
        {
            "wwwroot/exchange",
            "App_Data",
            $"App_Data/Tenants/{DataSettings.Instance.TenantName}",
            $"App_Data/Tenants/{DataSettings.Instance.TenantName}/Media",
            "Modules",
            $"runtimes/{RuntimeInfo.GetRuntimeIdentifier()}/native"
        };

        /// <summary>
        /// Gets the relative paths of all files that require write access.
        /// </summary>
        public static readonly string[] WrittenFiles = new[]
        {
            $"App_Data/Tenants/{DataSettings.Instance.TenantName}/InstalledPlugins.txt",
            $"App_Data/Tenants/{DataSettings.Instance.TenantName}/Settings.txt",
        };

        private readonly IOSIdentity _osIdentity;

        public FilePermissionChecker(IOSIdentity osIdentity)
        {
            _osIdentity = osIdentity;
        }

        public virtual bool CanAccess(FileSystemInfo entry, FileEntryRights rights)
        {
            Guard.NotNull(entry, nameof(entry));

            if (!entry.Exists)
            {
                throw new InvalidOperationException($"For file permission checks given entry must be an existing local/physical file or directory. Entry: '{entry.FullName}'");
            }

            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                    if (OperatingSystem.IsWindows())
                        return CanAccessOnWindows(entry, rights);
                    break;
                case PlatformID.Unix:
                    return CanAccessOnLinux(entry, rights);
            }

            return false;
        }

        [SupportedOSPlatform("windows")]
        protected virtual bool CanAccessOnWindows(FileSystemInfo entry, FileEntryRights rights)
        {
            var canAccess = true;
            var identity = WindowsIdentity.GetCurrent();

            var denyRead = false;
            var denyWrite = false;
            var denyModify = false;
            var denyDelete = false;

            var allowRead = false;
            var allowWrite = false;
            var allowModify = false;
            var allowDelete = false;

            try
            {
                var rules = GetAccessControl(entry).GetAccessRules(true, true, typeof(SecurityIdentifier))
                    .Cast<FileSystemAccessRule>()
                    .ToList();

                foreach (var rule in rules.Where(rule => identity.User?.Equals(rule.IdentityReference) ?? false))
                {
                    CheckRule(rule);
                }

                if (identity.Groups != null)
                {
                    foreach (var reference in identity.Groups)
                    {
                        foreach (var rule in rules.Where(rule => reference.Equals(rule.IdentityReference)))
                        {
                            CheckRule(rule);
                        }
                    }
                }

                allowDelete = !denyDelete && allowDelete;
                allowModify = !denyModify && allowModify;
                allowRead = !denyRead && allowRead;
                allowWrite = !denyWrite && allowWrite;

                if (rights.HasFlag(FileEntryRights.Read))
                    canAccess = allowRead;

                if (rights.HasFlag(FileEntryRights.Write))
                    canAccess = canAccess && allowWrite;

                if (rights.HasFlag(FileEntryRights.Modify))
                    canAccess = canAccess && allowModify;

                if (rights.HasFlag(FileEntryRights.Delete))
                    canAccess = canAccess && allowDelete;
            }
            catch (IOException)
            {
                return false;
            }
            catch
            {
                return true;
            }

            return canAccess;

            static FileSystemSecurity GetAccessControl(FileSystemInfo entry)
            {
                if (entry is DirectoryInfo dir)
                {
                    return dir.GetAccessControl();
                }
                else if (entry is FileInfo file)
                {
                    return file.GetAccessControl();
                }

                throw new InvalidOperationException($"Cannot get access control list for entry '{entry.FullName}'");
            }

            void CheckRule(FileSystemAccessRule rule)
            {
                switch (rule.AccessControlType)
                {
                    case AccessControlType.Deny:
                        if (CheckRuleLocal(rule, FileSystemRights.Delete))
                            denyDelete = true;
                        if (CheckRuleLocal(rule, FileSystemRights.Modify))
                            denyModify = true;
                        if (CheckRuleLocal(rule, FileSystemRights.Read))
                            denyRead = true;
                        if (CheckRuleLocal(rule, FileSystemRights.Write))
                            denyWrite = true;
                        return;
                    case AccessControlType.Allow:
                        if (CheckRuleLocal(rule, FileSystemRights.Delete))
                            allowDelete = true;
                        if (CheckRuleLocal(rule, FileSystemRights.Modify))
                            allowModify = true;
                        if (CheckRuleLocal(rule, FileSystemRights.Read))
                            allowRead = true;
                        if (CheckRuleLocal(rule, FileSystemRights.Write))
                            allowWrite = true;
                        break;
                }
            }

            static bool CheckRuleLocal(FileSystemAccessRule accessRule, FileSystemRights fileSystemRights)
            {
                return (fileSystemRights & accessRule.FileSystemRights) == fileSystemRights;
            }
        }

        protected virtual bool CanAccessOnLinux(FileSystemInfo entry, FileEntryRights rights)
        {
            // MacOSX file permission check differs slightly from linux
            var arguments = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? $"-c \"stat -f '%A %u %g' {entry.FullName}\""
                : $"-c \"stat -c '%a %u %g' {entry.FullName}\"";

            try
            {
                // Create bash command like
                // sh -c "stat -c '%a %u %g' <file>"
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        FileName = "/bin/sh",
                        Arguments = arguments
                    }
                };
                process.Start();
                process.WaitForExit();

                // Result looks like: 555 1111 2222
                // Where 555 - file permissions, 1111 - file owner ID, 2222 - file group ID
                var result = process.StandardOutput.ReadToEnd().Trim('\n').Split(' ');

                var filePermissions = result[0].Select(p => (int)char.GetNumericValue(p)).ToArray();
                var isOwner = _osIdentity.UserId == result[1];
                var isInGroup = _osIdentity.Groups.Contains(result[2]);

                var filePermission =
                    isOwner ? filePermissions[0] : (isInGroup ? filePermissions[1] : filePermissions[2]);

                return CheckUserFilePermissions(filePermission);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FilePermissionChecker error: {ex.Message}");
                return false;
            }

            bool CheckUserFilePermissions(int userFilePermission)
            {
                var readPermissions = new[] { 5, 6, 7 };
                var writePermissions = new[] { 2, 3, 6, 7 };
                var checkRead = rights.HasFlag(FileEntryRights.Read);
                var checkWrite = rights.HasFlag(FileEntryRights.Write);
                var checkModify = rights.HasFlag(FileEntryRights.Modify);
                var checkDelete = rights.HasFlag(FileEntryRights.Delete);

                if (checkRead & readPermissions.Contains(userFilePermission))
                    return true;

                return (checkWrite || checkModify || checkDelete) & writePermissions.Contains(userFilePermission);
            }
        }
    }
}
