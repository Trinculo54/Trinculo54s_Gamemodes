#addin "nuget:?package=SharpZipLib&Version=1.3.0"
#addin "nuget:?package=Cake.Compression&Version=0.34.1"
#addin "nuget:?package=Cake.FileHelpers&Version=3.3.0"

var buildId = EnvironmentVariable("APPVEYOR_BUILD_VERSION") ?? "0";
var buildDir = MakeAbsolute(Directory("./build"));

private void Infection() {
    var projBuildDir = buildDir.Combine("Infection");
    var projBuildName = "Infection_" + buildId;

    DotNetCorePublish("./Infection/Infection.csproj", new DotNetCorePublishSettings {
        Configuration = Argument("configuration", "Release"),
        SelfContained = true,
        OutputDirectory = projBuildDir
    });

    MoveFile(projBuildDir + "/Infection.dll", buildDir + "/Infection.dll");
}

Task("Build")
    .Does(() => {
        if (DirectoryExists(buildDir)) {
            DeleteDirectory(buildDir, new DeleteDirectorySettings {
                Recursive = true
            });
        }
        DotNetCoreRestore("Infection.sln");
        Infection();
        Information("Finished building.");
    });

RunTarget(Argument("target", "Build"));