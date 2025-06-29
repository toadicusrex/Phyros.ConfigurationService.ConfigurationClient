using System;
using System.IO;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.PathConstruction;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.Tools.DotNet;

class Build : NukeBuild
{
	/// Support plugins are available for:
	///   - JetBrains ReSharper        https://nuke.build/resharper
	///   - JetBrains Rider            https://nuke.build/rider
	///   - Microsoft VisualStudio     https://nuke.build/visualstudio
	///   - Microsoft VSCode           https://nuke.build/vscode

	[Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
	readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

	readonly string NuGetApiKey = Environment.GetEnvironmentVariable("PACKAGES_API_KEY");
	readonly string NuGetSource = "https://nuget.pkg.github.com/toadicusrex/index.json";

	[Solution("src/Phyros.OrganizationalUnits.sln")] readonly Solution Solution;
	[GitVersion] readonly GitVersion GitVersion;
	[Parameter("NuGet version from GitVersion")] readonly string GitVersionNuGetVersion;

	Target Clean => _ => _
			.Before(Restore)
			.Executes(() =>
			{
				// Clean bin/obj from all projects and artifacts directory
				var directoriesToClean = Solution.AllProjects.SelectMany(p => new[]
					{
								p.Directory / "bin",
								p.Directory / "obj"
					}).Concat(new[] { RootDirectory / "artifacts" });
				foreach (var dir in directoriesToClean)
				{
					if (Directory.Exists(dir))
						Directory.Delete(dir, true);
				}
			});

	Target Restore => _ => _
			.Executes(() =>
			{
				DotNetTasks.DotNetRestore(s => s
					.SetProjectFile(Solution));
			});

	Target Compile => _ => _
			.DependsOn(Restore)
			.Executes(() =>
			{
				DotNetTasks.DotNetBuild(s => s
							.SetProjectFile(Solution)
							.SetConfiguration(Configuration)
							.EnableNoRestore());
			});

	Target Pack => _ => _
		.DependsOn(Compile)
		.Executes(() =>
		{
			var project = Solution.Projects.FirstOrDefault(proj => !proj.Name.EndsWith("Test", StringComparison.InvariantCultureIgnoreCase) && !proj.Name.Equals("_build"));
			if (!File.Exists(project.Path))
				throw new Exception($"Could not find main project at '{project.Path}'.");
			var version = GitVersion?.NuGetVersion ?? GitVersionNuGetVersion ?? "1.0.0";
			Console.WriteLine($"[Pack] Using version: {version}");
			DotNetTasks.DotNetPack(s => s
				.SetProject(project.Path)
				.SetConfiguration(Configuration)
				.SetVersion(version)
				.SetOutputDirectory(RootDirectory / "artifacts")
				.EnableIncludeSymbols()
				.SetSymbolPackageFormat(DotNetSymbolPackageFormat.snupkg));
		});

	Target Publish => _ => _
		.DependsOn(Pack)
		.OnlyWhenStatic(() => IsServerBuild)
		.Executes(() =>
		{
			var artifactsDir = Path.Combine(RootDirectory, "artifacts");
			if (!Directory.Exists(artifactsDir))
			{
				Console.WriteLine($"[Publish] Artifacts directory '{artifactsDir}' does not exist. Skipping publish.");
				return;
			}
			var packageFiles = Directory.GetFiles(artifactsDir, "*.nupkg");
			foreach (var packageFile in packageFiles)
			{
				DotNetTasks.DotNetNuGetPush(s => s
					.SetTargetPath(packageFile)
					.SetSource(NuGetSource)
					.SetApiKey(NuGetApiKey)
					.SetProcessAdditionalArguments(new[] { "--skip-duplicate" }));
			}
		});

	public static int Main() => Execute<Build>(x => IsServerBuild ? x.Publish : x.Pack);
}
