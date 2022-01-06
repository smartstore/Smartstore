var target = Argument("target", "Build");
var configuration = Argument("configuration", "Release");
var solution = "./" + Argument("solution", "Smartstore") + ".sln";
var edition = Argument("edition", "Community");
var version = "5.0.0";

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean").Does(() =>
{
    Information("Cleaning bin, obj and Modules directories...");
    DotNetClean(solution);
    CleanDirectory($"./src/Smartstore.Web/bin/{configuration}");
    CleanDirectory($"./src/Smartstore.Web/obj/{configuration}");
    CleanDirectory($"./src/Smartstore.Web/Modules");
});

Task("Build").Does(() =>
{
    Information($"Building Smartstore {edition}...");

    CleanDirectory($"./src/Smartstore.Web/Modules");

    DotNetBuild(solution, new DotNetBuildSettings
    {
        Configuration = configuration,
        Verbosity = DotNetVerbosity.Minimal,
        // Whether to mark the build as unsafe for incrementality. This turns off incremental compilation and forces a clean rebuild of the project dependency graph.
        //NoIncremental = false,
        // Whether to not do implicit NuGet package restore. This makes build faster, but requires restore to be done before build is executed
        //NoRestore = false
    });
    
});

Task("Deploy")
    .IsDependentOn("Build")
    .Does(() =>
{
    var outputDir = "./artifacts/" + edition;
    	
    if (DirectoryExists(outputDir)) 
    {
        Information($"Deleting {outputDir}...");
	    DeleteDirectory(outputDir, new DeleteDirectorySettings 
        {
            Recursive = true,
            Force = true
        });
    }
    
    Information($"Publishing Smartstore {edition}...");
    DotNetPublish("./src/Smartstore.Web/Smartstore.Web.csproj", new DotNetPublishSettings
    {
        Configuration = configuration,
        OutputDirectory = outputDir,
        // Whether to not to build the project before publishing. This makes build faster, but requires build to be done before publish is executed.
        NoBuild = true
    });
    
});

Task("Zip").Does(() =>
{
    Information("Zipping '" + edition + "'...");
    
    var rootPath = new DirectoryPath("./artifacts/" + edition);
    if (!DirectoryExists(rootPath)) 
    {
        throw new Exception($"Path '{edition}' does not exist. Please build the {edition} solution before packing.");
    }

    var zipPath = new FilePath($"./artifacts/Smartstore.{edition}.{version}.zip");

    Zip(rootPath, zipPath);

    /*SevenZip(new SevenZipSettings 
    {
        Command = new AddCommand
        {
            DirectoryContents = new DirectoryPathCollection(new[] { rootPath }),
            Archive = zipPath,
            ArchiveType = SwitchArchiveType.Zip,
            CompressionMethod = new SwitchCompressionMethod { Level = 3 }
        }
    });*/
});

Task("Test").Does(() =>
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