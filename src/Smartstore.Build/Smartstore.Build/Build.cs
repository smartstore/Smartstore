using System;
using System.IO;
using System.IO.Compression;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Serilog;
using static Nuke.Common.IO.FileSystemTasks;
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

    readonly string Version = "5.1.0";

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "test";
    AbsolutePath ArtifactsDirectory => RootDirectory / "build" / "artifacts";

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
                .SetVerbosity(DotNetVerbosity.Minimal));

            EnsureCleanDirectory(SourceDirectory / "Smartstore.Web/Modules");
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
                .SetVerbosity(DotNetVerbosity.Minimal)
                //.SetAssemblyVersion(GitVersion.AssemblySemVer)
                //.SetFileVersion(GitVersion.AssemblySemFileVer)
                //.SetInformationalVersion(GitVersion.InformationalVersion)
                .EnableNoRestore());
        });

    Target Deploy => _ => _
        .DependsOn(Compile)
        .Triggers(Zip)
        .Executes(() =>
        {
            var publishName = GetPublishName();
            AbsolutePath outputDir = ArtifactsDirectory / publishName;

            if (outputDir.Exists())
            {
                Log.Information($"Deleting {publishName}...");
                EnsureCleanDirectory(outputDir);
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

    Target Zip => _ => _
        .Executes(() =>
        {
            var publishName = GetPublishName();
            Log.Information($"Zipping {publishName}...");

            AbsolutePath rootPath = ArtifactsDirectory / publishName;
            if (!rootPath.Exists())
            {
                throw new Exception($"Path '{publishName}' does not exist. Please build the {publishName} solution before packing.");
            }

            AbsolutePath zipPath = ArtifactsDirectory / $"Smartstore.{publishName}.zip";

            CompressionTasks.CompressZip(
                directory: rootPath,
                archiveFile: zipPath,
                filter: null,
                compressionLevel: CompressionLevel.Optimal,
                fileMode: FileMode.Create);
        });

    Target Test => _ => _
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration));
        });

}
