var target = Argument("target", "Build");
var configuration = Argument("configuration", "Release");
var solution = "./" + Argument("solution", "Smartstore") + ".sln";
var edition = Argument("edition", "Community");
var runtime = Argument("runtime", "win-x64");
var version = "5.0.0";

public string GetPublishName() 
{
    var name = edition + '.' + version;
	if (string.IsNullOrEmpty(runtime))
    {
    	return name;
    }
    
	return name + '.' + runtime;
}

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
    var publishName = GetPublishName();
    var outputDir = "./artifacts/" + publishName;
    	
    if (DirectoryExists(outputDir)) 
    {
        Information($"Deleting {publishName}...");
	    DeleteDirectory(outputDir, new DeleteDirectorySettings 
        {
            Recursive = true,
            Force = true
        });
    }
    
    Information($"Publishing Smartstore {publishName}...");
    DotNetPublish("./src/Smartstore.Web/Smartstore.Web.csproj", new DotNetPublishSettings
    {
        Configuration = configuration,
        OutputDirectory = outputDir,
        Runtime = runtime,
        SelfContained = true,
        //PublishTrimmed = true,
        //PublishSingleFile = true
        // Whether to not to build the project before publishing. This makes build faster, but requires build to be done before publish is executed.
        //NoBuild = true
    });
    
});

Task("Zip").Does(() =>
{
    var publishName = GetPublishName();
    
    Information($"Zipping {publishName}...");
    
    var rootPath = new DirectoryPath("./artifacts/" + publishName);
    if (!DirectoryExists(rootPath)) 
    {
        throw new Exception($"Path '{publishName}' does not exist. Please build the {publishName} solution before packing.");
    }

    var zipPath = new FilePath($"./artifacts/Smartstore.{publishName}.zip");

    Zip(rootPath, zipPath);
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