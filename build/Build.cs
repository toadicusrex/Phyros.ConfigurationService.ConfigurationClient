using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[GitHubActions("ci",
GitHubActionsImage.UbuntuLatest,
On = new[] { GitHubActionsTrigger.Push, GitHubActionsTrigger.WorkflowDispatch, GitHubActionsTrigger.PullRequest },
InvokedTargets = new[] { nameof(Publish) },
AutoGenerate = true,
FetchDepth = 0,
EnableGitHubToken = true,
ImportSecrets = [ nameof(NuGetPackageSourceCredentials_PhyrosGitHub)]
)]
[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild
{
	/// Support plugins are available for:
	/// - JetBrains ReSharper https://nuke.build/resharper 
	/// - JetBrains Rider https://nuke.build/rider 
	/// - Microsoft VisualStudio https://nuke.build/visualstudio 
	/// - Microsoft VSCode https://nuke.build/vscode 

	public static int Main() => Execute<Build>(x => x.Publish);

	[Parameter] [Secret] readonly string NuGetPackageSourceCredentials_PhyrosGitHub;
	[Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
	readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

	[Solution] readonly Solution Solution;
	[GitRepository] readonly GitRepository GitRepository;
	[GitVersion] readonly GitVersion GitVersion;
	[PathVariable("git")] readonly Tool Git;
	
	AbsolutePath SourceDirectory => RootDirectory / "src";
	AbsolutePath TestsDirectory => RootDirectory / "tests";
	AbsolutePath OutputDirectory => RootDirectory / "output";
	string nugetFeed = "https://nuget.pkg.github.com/phyros-corp/index.json";

	Target Clean => _ => _
		.Before(Restore)
		.Executes(() =>
		{
			SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(x => x.DeleteDirectory());
			TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(x => x.DeleteDirectory());
			OutputDirectory.CreateOrCleanDirectory();
		});

	//Target AddSource => _ => _
	//	.Executes(() =>
	//	{
	//		if (GitHubActions.Instance != null)
	//		{
	//			try
	//			{
	//				DotNetNuGetAddSource(s => s
	//					.SetName("github")
	//					.SetUsername(GitHubActions.Instance.RepositoryOwner)
	//					.SetPassword(GitHubActions.Instance.Token)
	//					.EnableStorePasswordInClearText()
	//					.SetSource($"https://nuget.pkg.github.com/{GitHubActions.Instance.RepositoryOwner}/index.json"));
	//			}
	//			catch
	//			{
	//				Log.Information("Source already added");
	//			}
	//		}
	//		else
	//		{
	//			Log.Information("Not running in GitHub Actions; relying on existing nuget package sources on the build machine.");
	//		}
	//	});

	Target Restore => _ => _
		.Executes(() =>
		{
			DotNetRestore(s =>
				s
					.SetProjectFile(Solution)
				);
		});

	Target Compile => _ => _
		.DependsOn(Restore)
		.Executes(() =>
		{
			DotNetBuild(s => s
				.SetProjectFile(Solution)
				.SetConfiguration(Configuration)
				.SetSymbolPackageFormat(DotNetSymbolPackageFormat.snupkg)
				.SetAssemblyVersion(GitVersion.AssemblySemVer)
				.SetFileVersion(GitVersion.AssemblySemFileVer)
				.SetInformationalVersion(GitVersion.InformationalVersion)
			);
		});

	Target Test => _ => _
		.DependsOn(Compile)
		.Executes(() =>
		{
			var projects = Solution.GetAllProjects("*.Test");

			foreach (var project in projects)
			{
				DotNetTest(_ => _
					.SetProjectFile(project.Path)
					.SetConfiguration(Configuration)
					.EnableNoBuild()
				);
			}
		});

	Target Pack => _ => _
		.DependsOn(Test)
		.Produces(OutputDirectory)
		.Executes(() =>
		{
			DotNetPack(s => s
				.SetProject(Solution)
				.SetVersion(GitVersion.SemVer)
				.SetSymbolPackageFormat(DotNetSymbolPackageFormat.snupkg)
				.SetIncludeSymbols(true)
				.SetOutputDirectory(OutputDirectory));

			var outputPackages = OutputDirectory.GlobFiles("*.nupkg").ToList();
			outputPackages.AddRange(OutputDirectory.GlobFiles("*.snupkg"));
			if (!outputPackages.Any())
			{
				Log.Error($"No files were found in the output directory '{RootDirectory}'.");
				throw new Exception("Artifacts not found to publish.");
			}
			else
			{
				foreach (var package in outputPackages)
				{
					Log.Information($"Package produced in output location: {package.Name}");
				}
			}
		});


	Target Tag => _ => _
		.DependsOn(Pack)
		.Requires(() => GitHubActions.Instance != null)
		.Executes(() =>
		{
			Log.Information($"Access Token: {GitHubActions.Instance.Token}");

			Git($"config --global user.email \"development@phyros.com\"");
			Git($"config --global user.name \"Phyros Development\"");

			Git($"tag -a {GitVersion.FullSemVer} -m \"Setting git tag on commit to '{GitVersion.FullSemVer}'\"");
			Git($"push origin {GitVersion.FullSemVer}", logger: (t, s) =>
			{
				t = OutputType.Std;
				s = $"Tagging commit with {GitVersion.FullSemVer}.";
			});
		});

	Target Publish => _ => _
		.DependsOn(Tag)
		.Requires(() => GitHubActions.Instance != null)
		.Consumes(Pack, OutputDirectory)
		.Executes(() =>
		{
			if (GitHubActions.Instance != null)
			{
				var outputPackages = OutputDirectory.GlobFiles("*.nupkg").ToList();
				outputPackages.AddRange(OutputDirectory.GlobFiles("*.snupkg"));
				if (!outputPackages.Any())
				{
					Log.Error($"No files were found in the output directory '{OutputDirectory}'.");
					throw new Exception("Artifacts not found to publish.");
				}
				else
				{
					foreach (var package in outputPackages)
					{
						Log.Information($"Package produced in output location: {package.Name}");
					}

					DotNetNuGetPush(settings => settings
						.SetTargetPath(OutputDirectory / "*.nupkg")
						.SetSource(nugetFeed)
						.SetApiKey(GitHubActions.Instance.Token));
				}
			}
			else
			{
				Log.Information($"Pushing to the phyros nuget feed is allowed only from the build pipeline.");
				var outputPackages = OutputDirectory.GlobFiles("*.nupkg").ToList();
				outputPackages.AddRange(OutputDirectory.GlobFiles("*.snupkg"));
				foreach (var package in outputPackages)
				{
					Log.Information($"Package Name: {OutputDirectory}\\{package.Name}");
				}
			}
		});
}
