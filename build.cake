var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var nugetApiKey = EnvironmentVariable("NUGET_API_KEY");
var nugetApiUrl = EnvironmentVariable("NUGET_API_URL");

DirectoryPath artifactsDir = Directory("./artifacts/");

var projectFile = File("./src/MSA.BuildingBlocks.Mapping/MSA.BuildingBlocks.Mapping.csproj");

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
    DotNetRestore(projectFile);
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
    DotNetBuild(projectFile, settings);
});

Task("NuGetPack")
    .IsDependentOn("Build")
    .Does(() =>
{
    DotNetPack(projectFile, new DotNetCorePackSettings
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
    DotNetNuGetPush(packages.First(), new DotNetCoreNuGetPushSettings
    {
        Source = nugetApiUrl,
        ApiKey = nugetApiKey
    });
});

Task("Default")
    .IsDependentOn("Build");

RunTarget(target);
