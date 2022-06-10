using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Principal;

namespace Smartstore.Engine
{
    internal class GenericOSIdentity : IOSIdentity
    {
        public GenericOSIdentity()
        {
            Name = Environment.UserName;
            Domain = Environment.UserDomainName;

            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                    if (OperatingSystem.IsWindows())
                    {
                        PopulateWindowsUser(this);
                    }
                    break;
                case PlatformID.Unix:
                    PopulateLinuxUser(this);
                    break;
                default:
                    UserId = Name;
                    Groups = Array.Empty<string>();
                    break;
            }
        }

        [SupportedOSPlatform("windows")]
        private static void PopulateWindowsUser(GenericOSIdentity identity)
        {
            identity.Groups = WindowsIdentity.GetCurrent().Groups?.Select(p => p.Value).AsReadOnly();
            identity.UserId = identity.Name;
        }

        private static void PopulateLinuxUser(GenericOSIdentity identity)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    FileName = "sh",
                    Arguments = "-c \" id -u ; id -G \""
                }
            };

            process.Start();
            process.WaitForExit();

            var res = process.StandardOutput.ReadToEnd();

            var respars = res.Split("\n");

            identity.UserId = respars[0];
            identity.Groups = respars[1].Split(" ").AsReadOnly();
        }

        public string Name { get; }

        public string Domain { get; }

        public string FullName { get; }

        public IReadOnlyCollection<string> Groups { get; private set; }

        public string UserId { get; private set; }
    }
}
