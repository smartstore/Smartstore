#addin nuget:?package=Cake.7zip&version=2.0.0

var target = Argument("target", "Build");
var configuration = Argument("configuration", "Release");
var solution = "./" + Argument("solution", "Smartstore") + ".sln";
var publishDir = Argument("publishdir", "Core");

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    Information("Cleaning bin, obj and Modules directories...");
    DotNetClean(solution);
    CleanDirectory($"./src/Smartstore.Web/bin/{configuration}");
    CleanDirectory($"./src/Smartstore.Web/obj/{configuration}");
    CleanDirectory($"./src/Smartstore.Web/Modules");
});

Task("Build")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetBuild(solution, new DotNetBuildSettings
    {
        Configuration = configuration,
        Verbosity = DotNetVerbosity.Minimal,
        // Whether to mark the build as unsafe for incrementality. This turns off incremental compilation and forces a clean rebuild of the project dependency graph.
        NoIncremental = false,
        // Whether to not do implicit NuGet package restore. This makes build faster, but requires restore to be done before build is executed
        NoRestore = false
    });
    
});

Task("Deploy")
    //.IsDependentOn("Build")
    .Does(() =>
{
    Information("Deploying to '" + publishDir + "'...");
    DotNetPublish("./src/Smartstore.Web/Smartstore.Web.csproj", new DotNetPublishSettings
    {
        Configuration = configuration,
        OutputDirectory = "./artifacts/" + publishDir,
        // Whether to not to build the project before publishing. This makes build faster, but requires build to be done before publish is executed.
        NoBuild = true
    });
    
});

Task("Zip")
    .IsDependentOn("Deploy")
    .Does(() =>
{
    Information("Zipping '" + publishDir + "'...");
    SevenZip(new SevenZipSettings 
    {
        Command = new AddCommand
        {
            DirectoryContents = new DirectoryPathCollection(new[] { new DirectoryPath("./artifacts/" + publishDir) }),
            Archive = new FilePath($"./artifacts/{publishDir}.zip"),
            ArchiveType = SwitchArchiveType.Zip,
            CompressionMethod = new SwitchCompressionMethod { Level = 3 }
        }
    });
});

Task("Test")
    .Does(() =>
{
    DotNetTest(solution, new DotNetTestSettings
    {
        Configuration = configuration
    });
});

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);