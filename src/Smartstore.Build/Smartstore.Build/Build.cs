using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution]
    readonly Solution Solution;

    [Parameter]
    readonly string Edition = "Community";

    [Parameter]
    readonly string Runtime = "win-x64";

    [GitRepository]
    readonly GitRepository GitRepository;

    [GitVersion]
    readonly GitVersion GitVersion;

    readonly string Version = "6.3.0";

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "test";
    AbsolutePath ArtifactsDirectory => RootDirectory / "build" / "artifacts";
    AbsolutePath ToolsDirectory => RootDirectory / "build" / ".tools";
    AbsolutePath SbomToolPath => ToolsDirectory / (EnvironmentInfo.Platform == PlatformFamily.Windows ? "sbom-tool.exe" : "sbom-tool");
    const string SbomToolPackage = "Microsoft.Sbom.DotNetTool";
    const string SbomToolVersion = "4.1.4";

    string GetPublishName()
    {
        var name = Edition + '.' + Version;
        if (string.IsNullOrEmpty(Runtime))
        {
            return name;
        }

        return name + '.' + Runtime;
    }

    //////////////////////////////////////////////////////////////////////
    // TARGETS
    //////////////////////////////////////////////////////////////////////

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            Log.Information("Cleaning bin, obj and Modules directories...");

            DotNetClean(s => s
                .SetProject(Solution)
                .SetVerbosity(DotNetVerbosity.minimal));

            (SourceDirectory / "Smartstore.Web/Modules").CreateOrCleanDirectory();
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetVerbosity(DotNetVerbosity.minimal)
                //.SetAssemblyVersion(GitVersion.AssemblySemVer)
                //.SetFileVersion(GitVersion.AssemblySemFileVer)
                //.SetInformationalVersion(GitVersion.InformationalVersion)
                .EnableNoRestore());
        });

    Target Deploy => _ => _
        .DependsOn(Compile)
        .Triggers(Zip, GenerateSbom)
        .Executes(() =>
        {
            var publishName = GetPublishName();
            AbsolutePath outputDir = ArtifactsDirectory / publishName;

            if (outputDir.DirectoryExists())
            {
                Log.Information($"Deleting {publishName}...");
                outputDir.CreateOrCleanDirectory();
            }

            Log.Information($"Publishing Smartstore {publishName}...");

            DotNetPublish(s => s
                .SetProject("src/Smartstore.Web/Smartstore.Web.csproj")
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .SetRuntime(Runtime)
                .SetSelfContained(true)
                //.SetPublishTrimmed(true)
                //.SetPublishSingleFile(true)
                //.SetNoBuild(true)
                .SetOutput(outputDir));
        });

    Target GenerateSbom => _ => _
        .Executes(() =>
        {
            EnsureSbomTool();

            var publishName = GetPublishName();
            AbsolutePath publishDirectory = ArtifactsDirectory / publishName;
            AbsolutePath buildComponentDirectory = SourceDirectory;

            if (!publishDirectory.DirectoryExists())
            {
                throw new Exception($"Published output for {publishName} not found in {ArtifactsDirectory}.");
            }

            // Build root == ArtifactsDirectory / publishName
            AbsolutePath buildRootDirectory = publishDirectory;

            // Manifest directory "sbom" under publish directory (temp)
            AbsolutePath manifestDirectory = publishDirectory / "sbom";

            // sbom-tool will write to: <manifestDirectory>/_manifest/spdx_2.2/manifest.spdx.json
            AbsolutePath generatedManifestDirectory = manifestDirectory / "_manifest" / "spdx_2.2";
            AbsolutePath generatedManifest = generatedManifestDirectory / "manifest.spdx.json";

            // Final SBOM location in build root
            AbsolutePath finalSbomFile = buildRootDirectory / "manifest.spdx.json";

            Log.Information($"Generating SBOM for {publishName} using Microsoft SBOM Tool...");

            manifestDirectory.CreateOrCleanDirectory();

            // Create file list according to include/exclude rules
            var fileListPath = CreateSbomFileList(publishDirectory, manifestDirectory);

            var arguments = string.Join(" ", new string[]
            {
                "generate",
                "-b", publishDirectory,
                "-bc", buildComponentDirectory,
                "-ps", "Smartstore",
                "-nsb", "https://smartstore.com",
                "-pn", "Smartstore",
                "-pv", Version,
                "-m", manifestDirectory,
                "-bl", fileListPath
            }.Select(x => $"\"{x}\""));

            var sbomToolExecutable = GetSbomToolExecutable();

            ProcessTasks.StartProcess(sbomToolExecutable, arguments)
                .AssertZeroExitCode();

            if (!File.Exists(generatedManifest))
            {
                throw new Exception($"SBOM manifest not found at {generatedManifest}.");
            }

            // Copy SBOM to build root (ArtifactsDirectory/{publishName})
            File.Copy(generatedManifest, finalSbomFile, overwrite: true);

            // Clean up temp manifest directory ("sbom")
            if (Directory.Exists(manifestDirectory))
            {
                Directory.Delete(manifestDirectory, recursive: true);
            }
        });

    Target Zip => _ => _
        .After(GenerateSbom)
        .Executes(() =>
        {
            var publishName = GetPublishName();
            Log.Information($"Zipping {publishName}...");

            AbsolutePath rootPath = ArtifactsDirectory / publishName;
            if (!rootPath.DirectoryExists())
            {
                throw new Exception($"Path '{publishName}' does not exist. Please build the {publishName} solution before packing.");
            }

            AbsolutePath zipPath = ArtifactsDirectory / $"Smartstore.{publishName}.zip";

            rootPath.ZipTo(
                zipPath,
                filter: null,
                compressionLevel: CompressionLevel.SmallestSize,
                fileMode: FileMode.Create);
        });

    Target Test => _ => _
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration));
        });

    void EnsureSbomTool()
    {
        ToolsDirectory.CreateDirectory();

        if (File.Exists(SbomToolPath))
        {
            // Local sbom-tool already available
            return;
        }

        Log.Information("Ensuring Microsoft SBOM Tool dotnet tool is installed locally...");

        try
        {
            DotNet(
                $"tool install --tool-path \"{ToolsDirectory}\" {SbomToolPackage} --version {SbomToolVersion}");
        }
        catch (Exception ex)
        {
            // Most likely already installed globally or some permission issue.
            // We will try to use a global installation as fallback.
            Log.Warning(ex, "Local SBOM tool install failed, will try to use a globally installed tool.");
        }
    }

    string GetSbomToolExecutable()
    {
        // 1. Local tool in .tools
        if (File.Exists(SbomToolPath))
        {
            return SbomToolPath;
        }

        // 2. Global dotnet tool in %USERPROFILE%\.dotnet\tools\sbom-tool.exe
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var globalToolPath = Path.Combine(userProfile, ".dotnet", "tools",
            EnvironmentInfo.Platform == PlatformFamily.Windows ? "sbom-tool.exe" : "sbom-tool");

        if (File.Exists(globalToolPath))
        {
            return globalToolPath;
        }

        throw new Exception(
            "Could not locate sbom-tool. " +
            "Tried local tools directory and the global dotnet tools path (%USERPROFILE%/.dotnet/tools). " +
            "Please install with either:\n" +
            "  dotnet tool install --tool-path \"build/.tools\" Microsoft.Sbom.DotNetTool --version 4.1.4\n" +
            "or\n" +
            "  dotnet tool install -g Microsoft.Sbom.DotNetTool --version 4.1.4");
    }

    AbsolutePath CreateSbomFileList(AbsolutePath publishDirectory, AbsolutePath manifestDirectory)
    {
        var fileListPath = manifestDirectory / "filelist.txt";

        // Allowed file extensions
        var includedExtensions = new[] { ".dll", ".exe", ".js" };

        var filesToInclude = Directory
            .GetFiles(publishDirectory, "*", SearchOption.AllDirectories)
            .Where(f =>
            {
                // Normalize to forward slashes for easier matching
                var relative = Path.GetRelativePath(publishDirectory, f)
                    .Replace(Path.DirectorySeparatorChar, '/');

                // Exclude folders: refs/*, i18n/*, lang/*
                if (relative.StartsWith("refs/", StringComparison.OrdinalIgnoreCase) ||
                    relative.StartsWith("i18n/", StringComparison.OrdinalIgnoreCase) ||
                    relative.StartsWith("lang/", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                var ext = Path.GetExtension(f);

                // Include only the desired extensions
                if (!includedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }

                // Exclude *.resources.dll
                if (ext.Equals(".dll", StringComparison.OrdinalIgnoreCase) &&
                    relative.EndsWith(".resources.dll", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                return true;
            })
            .ToList();

        if (filesToInclude.Count == 0)
        {
            throw new Exception($"No files found to include for SBOM in '{publishDirectory}'.");
        }

        File.WriteAllLines(fileListPath, filesToInclude);

        return fileListPath;
    }
}