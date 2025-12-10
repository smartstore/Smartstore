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
        .Triggers(/*GenerateSbom, */Zip)
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
        .DependsOn(Deploy)
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

            AbsolutePath manifestRootDirectory = publishDirectory;
            AbsolutePath manifestDirectory = manifestRootDirectory / "_manifest";
            AbsolutePath generatedManifestDirectory = manifestDirectory / "spdx_2.2";
            AbsolutePath generatedManifest = generatedManifestDirectory / "manifest.spdx.json";
            AbsolutePath sbomDirectory = publishDirectory / "sbom";
            AbsolutePath sbomFile = sbomDirectory / "manifest.spdx.json";
            AbsolutePath fileListPath = manifestDirectory / "filelist.txt";

            Log.Information($"Generating SBOM for {publishName} using Microsoft SBOM Tool...");

            // Ensure manifest directory exists and is clean
            manifestDirectory.CreateOrCleanDirectory();

            // Extensions to include in the files section
            var includedExtensions = new[]
            {
                ".dll",
                ".exe",
                ".js"
            };

            // Build whitelist of files to include
            var filesToInclude = Directory
                .GetFiles(publishDirectory, "*", SearchOption.AllDirectories)
                .Where(f =>
                {
                    var ext = Path.GetExtension(f);
                    return includedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
                })
                .ToList();

            if (filesToInclude.Count == 0)
            {
                throw new Exception($"No files found to include for SBOM in '{publishDirectory}'.");
            }

            // Write file list for -bl (one file per line, absolute paths)
            File.WriteAllLines(fileListPath, filesToInclude);

            var arguments = string.Join(" ", new string[]
            {
                "generate",
                "-b", publishDirectory,
                "-bc", buildComponentDirectory,
                "-ps", "Smartstore",
                "-nsb", "https://smartstore.com",
                "-pn", "Smartstore",
                "-pv", Version,
                "-m", manifestRootDirectory,
                "-bl", fileListPath
            }.Select(x => $"\"{x}\""));

            ProcessTasks.StartProcess(SbomToolPath, arguments)
                .AssertZeroExitCode();

            if (!File.Exists(generatedManifest))
            {
                throw new Exception($"SBOM manifest not found at {generatedManifest}.");
            }

            sbomDirectory.CreateOrCleanDirectory();
            File.Copy(generatedManifest, sbomFile, overwrite: true);

            if (Directory.Exists(generatedManifestDirectory))
            {
                Directory.Delete(generatedManifestDirectory, recursive: true);
            }
        });


    Target Zip => _ => _
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
            return;
        }

        Log.Information("Installing Microsoft SBOM Tool dotnet tool...");
        DotNet($"tool install --tool-path \"{ToolsDirectory}\" {SbomToolPackage} --version {SbomToolVersion}");
    }

}