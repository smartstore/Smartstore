using System;
using System.IO;
using NuGet.Versioning;
using NUnit.Framework;
using Smartstore.Engine.Runtimes;

namespace Smartstore.Tests.Engine.Runtimes;

[TestFixture]
public class NativeLibraryManagerTests
{
    [Test]
    public void Can_resolve_unix_system_executable_without_process_main_module()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Ignore("Unix executable resolution is only used on Linux and macOS.");
        }

        var path = NativeLibraryManager.ResolveUnixExecutablePath("sh");

        Assert.That(path, Is.Not.Null);
        Assert.That(File.Exists(path), Is.True);
        Assert.That(NativeLibraryManager.ResolveUnixExecutablePath(path), Is.EqualTo(path));
        Assert.That(NativeLibraryManager.ResolveUnixExecutablePath("smartstore-missing-tool-" + Guid.NewGuid().ToString("N")), Is.Null);
    }

    [TestCase("3.0.3", true)]
    [TestCase("3.1.0", true)]
    [TestCase("3.0.2", false)]
    [TestCase("invalid", false)]
    public void Can_check_native_package_version_sidecar(string installedVersion, bool expected)
    {
        var path = Path.GetTempFileName();
        var versionPath = NativeLibraryManager.GetVersionFilePath(path);

        try
        {
            File.WriteAllText(versionPath, installedVersion);

            var result = NativeLibraryManager.IsVersionCompatible(
                new FileInfo(path),
                VersionRange.Parse("[3.0.3,)"));

            Assert.That(result, Is.EqualTo(expected));
        }
        finally
        {
            File.Delete(path);
            File.Delete(versionPath);
        }
    }
}
