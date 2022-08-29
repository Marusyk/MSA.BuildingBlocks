var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var nugetApiKey = EnvironmentVariable("NUGET_API_KEY");
var nugetApiUrl = EnvironmentVariable("NUGET_API_URL");

DirectoryPath artifactsDir = Directory("./artifacts/");

var solutionFile = File("./src/MSA.BuildingBlocks.sln");

Task("Clean")
    .Does(() =>
{
    CleanDirectory(artifactsDir);
    CleanDirectories(GetDirectories("./**/obj") + GetDirectories("./**/bin"));
});

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetRestore(solutionFile);
});

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
{
    var settings = new DotNetBuildSettings
    { 
        Configuration = configuration,
        NoRestore = true,
        NoLogo = true
    };
    DotNetBuild(solutionFile, settings);
});

Task("NuGetPack")
    .IsDependentOn("Build")
    .Does(() =>
{
    DotNetPack(solutionFile, new DotNetCorePackSettings
    {
        Configuration = configuration,
        NoRestore = true,
        NoBuild = true,
        NoLogo = true,
        OutputDirectory = artifactsDir
    });
});

Task("NuGetPush")
    .IsDependentOn("NuGetPack")
    .WithCriteria(!string.IsNullOrWhiteSpace(nugetApiUrl))
    .WithCriteria(!string.IsNullOrWhiteSpace(nugetApiKey))
    .Does(() =>
{
    var packages = GetFiles(string.Concat(artifactsDir, "/", "*.nupkg"));
    foreach(var package in packages)
    {
        DotNetNuGetPush(package.FullPath, new DotNetCoreNuGetPushSettings
        {
            Source = nugetApiUrl,
            SkipDuplicate = true,
            ApiKey = nugetApiKey
        });
     }
});

Task("Default")
    .IsDependentOn("Build");

RunTarget(target);
