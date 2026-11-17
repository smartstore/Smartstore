using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Principal;

namespace Smartstore.Engine;

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
        var result = Process.RunAndCaptureText("sh", ["-c", "id -u && id -G"]);
        var lines = result.StandardOutput.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (result.ExitStatus.ExitCode != 0 || result.ExitStatus.Signal != null || lines.Length != 2)
        {
            throw new InvalidOperationException($"Could not determine Unix user identity. Exit status: {result.ExitStatus}. {result.StandardError.Trim()}");
        }

        identity.UserId = lines[0];
        identity.Groups = lines[1].Split(' ', StringSplitOptions.RemoveEmptyEntries).AsReadOnly();
    }

    public string Name { get; }

    public string Domain { get; }

    public string FullName { get; }

    public IReadOnlyCollection<string> Groups { get; private set; }

    public string UserId { get; private set; }
}