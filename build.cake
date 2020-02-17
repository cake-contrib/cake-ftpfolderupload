#addin nuget:?package=Cake.Incubator&version=5.1.0
///////////////////////////////////////////////////////////////////////////////
using System.Text.RegularExpressions;
using System.Xml.Linq;
private readonly FilePath CsProjPath = "./src/Cake.FtpFolderupload/Cake.FtpFolderUpload.csproj";
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var pushNuget = Argument("nugetPush", false);
///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////
Setup(ctx =>
{
   // Executed BEFORE the first task.
   Information("Running tasks...");
});
Teardown(ctx =>
{
   // Executed AFTER the last task.
   Information("Finished running tasks.");
});
///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////
Task("Default")
.IsDependentOn("UpdateVersion")
.IsDependentOn("PackAndDeploy")
.Does(() => {
      
});
Task("UpdateVersion")
   .Does(() => {
      
      var file = CsProjPath;
      Information(file.FullPath);
      var path = file.FullPath;
      
      var result = XElement.Load(path);
      var propertyGroup = result.Element("PropertyGroup");
      var currentVersion = propertyGroup.Element("Version").Value;
      Information($"Current version {currentVersion}");      
      
      Information("Enter version");
      var version = System.Console.ReadLine();    
      ValidateVersion(version);
      EnsureVersion(version,currentVersion);
      Information(version);
      Information("Enter release notes");
      var description = System.Console.ReadLine();          
      propertyGroup.SetElementValue("Version",version);
      propertyGroup.SetElementValue("AssemblyVersion",$"{version}.0");
      propertyGroup.SetElementValue("FileVersion",$"{version}.0");
      propertyGroup.SetElementValue("PackageReleaseNotes", description);
      
      System.IO.File.WriteAllText(path, result.ToString());      
   });
Task("Pack")
   .Does(() => {      
      DotNetBuild(CsProjPath, x => x.SetConfiguration("Release"));
       DotNetCorePack(CsProjPath.FullPath, new DotNetCorePackSettings(){
         Configuration = "Release",         
      });
   });
Task("PackAndDeploy")
.IsDependentOn("Pack")
.Does(() => {
   var result = XElement.Load(CsProjPath.FullPath);
   var propertyGroup = result.Element("PropertyGroup");
   var currentVersion = propertyGroup.Element("Version").Value;
   var nugetApiKey = EnvironmentVariable("NugetApiKey");
   var nugetDestination = EnvironmentVariable("NugetDestination") ?? "https://api.nuget.org/v3/index.json";
      
   Information($"Pushing version {currentVersion} to nuget");
   NuGetPush($"./src/Cake.FtpFolderupload/bin/Release/Cake.FtpFolderUpload.{currentVersion}.nupkg", new NuGetPushSettings()
   {
      ApiKey = nugetApiKey,
      Source = nugetDestination
   });
});
   
RunTarget(target);
private static void ValidateVersion(string version){
   var regex = new Regex(@"\d+\.\d+\.\d+");
   if (!regex.IsMatch(version))
    {
       throw new InvalidOperationException($"The version is in an incorrect format");
    }
}
private static void EnsureVersion(string version, string currentVersion)
 {
    var comparison = version.Split('.').Zip(currentVersion.Split('.'),(left,right) => new { Left = int.Parse(left), Right = int.Parse(right)});
    foreach (var position in comparison)
    {
       if (position.Left > position.Right){
          return; 
       }
       if (position.Left < position.Right){
          throw new InvalidOperationException("New version is lower than existing");
       }
    }    
 }
