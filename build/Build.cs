using System;
using System.IO;
using System.Linq;
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.GitVersion;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.DocFx.DocFxTasks;
using Nuke.Common.Tools.DocFx;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.ChangeLog.ChangelogTasks;
using static Nuke.GitHub.ChangeLogExtensions;
using Nuke.CoberturaConverter;
using static Nuke.CoberturaConverter.CoberturaConverterTasks;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using static Nuke.GitHub.GitHubTasks;
using Nuke.GitHub;
using Nuke.WebDocu;
using static Nuke.WebDocu.WebDocuTasks;
using Nuke.Azure.KeyVault;

[KeyVaultSettings(
    VaultBaseUrlParameterName = nameof(KeyVaultBaseUrl),
    ClientIdParameterName = nameof(KeyVaultClientId),
    ClientSecretParameterName = nameof(KeyVaultClientSecret))]
class Build : NukeBuild
{
    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter] string KeyVaultBaseUrl;
    [Parameter] string KeyVaultClientId;
    [Parameter] string KeyVaultClientSecret;
    [GitVersion] readonly GitVersion GitVersion;
    [GitRepository] readonly GitRepository GitRepository;
    [KeyVault] KeyVault KeyVault;

    [KeyVaultSecret] string DocuApiEndpoint;
    [KeyVaultSecret] string GitHubAuthenticationToken;
    [KeyVaultSecret] readonly string PublicMyGetSource;
    [KeyVaultSecret] readonly string PublicMyGetApiKey;
    [KeyVaultSecret] readonly string NuGetApiKey;
    [KeyVaultSecret("DanglAspNetCoreFileHandling-DocuApiKey")] readonly string DocuApiKey;

    string DocFxFile => SolutionDirectory / "docfx.json";
    string ChangeLogFile => RootDirectory / "CHANGELOG.md";
    string DocFxDotNetSdkVersion = "2.1.4";

    Target Clean => _ => _
            .Executes(() =>
            {
                DeleteDirectories(GlobDirectories(SourceDirectory, "**/bin", "**/obj"));
                DeleteDirectories(GlobDirectories(SolutionDirectory / "test", "**/bin", "**/obj"));
                EnsureCleanDirectory(OutputDirectory);
            });

    Target Restore => _ => _
            .DependsOn(Clean)
            .Executes(() =>
            {
                DotNetRestore(s => DefaultDotNetRestore
                    // Need to set it here, otherwise it takes the one from NUKEs .tmp directory
                    .SetToolPath(ToolPathResolver.GetPathExecutable("dotnet")));
            });

    Target Compile => _ => _
            .DependsOn(Restore)
            .Executes(() =>
            {
                DotNetBuild(s => DefaultDotNetBuild
                    // Need to set it here, otherwise it takes the one from NUKEs .tmp directory
                    .SetToolPath(ToolPathResolver.GetPathExecutable("dotnet"))
                    .SetFileVersion(GitVersion.GetNormalizedFileVersion())
                    .SetAssemblyVersion($"{GitVersion.Major}.{GitVersion.Minor}.{GitVersion.Patch}.0"));
            });

    Target Pack => _ => _
            .DependsOn(Compile)
            .Executes(() =>
            {
                var changeLog = GetCompleteChangeLog(ChangeLogFile)
                    .EscapeStringPropertyForMsBuild();
                DotNetPack(s => DefaultDotNetPack
                    // Need to set it here, otherwise it takes the one from NUKEs .tmp directory
                    .SetToolPath(ToolPathResolver.GetPathExecutable("dotnet"))
                    .SetPackageReleaseNotes(changeLog)
                    .SetDescription("Dangl.AspNetCore.FileHandling www.dangl-it.com"));
            });

    Target Coverage => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var testProjects = GlobFiles(SolutionDirectory / "test", "*.csproj").ToList();
            var dotnetPath = ToolPathResolver.GetPathExecutable("dotnet");
            for (var i = 0; i < testProjects.Count; i++)
            {
                var testProject = testProjects[i];
                var projectName = Path.GetFileNameWithoutExtension(testProject);

                /* DotCover */
                var projectDirectory = Path.GetDirectoryName(testProject);
                var snapshotIndex = i;
                var toolSettings = new ToolSettings()
                    .SetToolPath(ToolPathResolver.GetPackageExecutable("JetBrains.dotCover.CommandLineTools",
                        "tools/dotCover.exe"))
                    .SetArgumentConfigurator(a => a
                        .Add("cover")
                        .Add($"/TargetExecutable=\"{dotnetPath}\"")
                        .Add($"/TargetWorkingDir=\"{projectDirectory}\"")
                        .Add($"/TargetArguments=\"test --no-build --test-adapter-path:. \\\"--logger:xunit;LogFilePath={OutputDirectory}/{snapshotIndex}_testresults.xml\\\"\"")
                        .Add("/Filters=\"+:Dangl.AspNetCore.FileHandling\"")
                        .Add("/AttributeFilters=\"System.CodeDom.Compiler.GeneratedCodeAttribute\"")
                        .Add($"/Output=\"{OutputDirectory / $"coverage{snapshotIndex:00}.snapshot"}\""));
                ProcessTasks.StartProcess(toolSettings)
                    .AssertZeroExitCode();
            }

            var snapshots = testProjects.Select((t, i) => OutputDirectory / $"coverage{i:00}.snapshot")
                .Select(p => p.ToString())
                .Aggregate((c, n) => c + ";" + n);

            var mergeSettings = new ToolSettings()
                .SetToolPath(ToolPathResolver.GetPackageExecutable("JetBrains.dotCover.CommandLineTools",
                    "tools/dotCover.exe"))
                .SetArgumentConfigurator(a => a
                    .Add("merge")
                    .Add($"/Source=\"{snapshots}\"")
                    .Add($"/Output=\"{OutputDirectory / "coverage.snapshot"}\""));
            ProcessTasks.StartProcess(mergeSettings)
                .AssertZeroExitCode();

            var reportSettings = new ToolSettings()
                .SetToolPath(ToolPathResolver.GetPackageExecutable("JetBrains.dotCover.CommandLineTools",
                    "tools/dotCover.exe"))
                .SetArgumentConfigurator(a => a
                    .Add("report")
                    .Add($"/Source=\"{OutputDirectory / "coverage.snapshot"}\"")
                    .Add($"/Output=\"{OutputDirectory / "coverage.xml"}\"")
                    .Add("/ReportType=\"DetailedXML\""));
            ProcessTasks.StartProcess(reportSettings)
                .AssertZeroExitCode();

            var reportGeneratorSettings = new ToolSettings()
                .SetToolPath(ToolPathResolver.GetPackageExecutable("ReportGenerator", "tools/ReportGenerator.exe"))
                .SetArgumentConfigurator(a => a
                    .Add($"-reports:\"{OutputDirectory / "coverage.xml"}\"")
                    .Add($"-targetdir:\"{OutputDirectory / "CoverageReport"}\""));
            ProcessTasks.StartProcess(reportGeneratorSettings)
                .AssertZeroExitCode();

            // This is the report in Cobertura format that integrates so nice in Jenkins
            // dashboard and allows to extract more metrics and set build health based
            // on coverage readings
            DotCoverToCobertura(s => s
                    .SetInputFile(OutputDirectory / "coverage.xml")
                    .SetOutputFile(OutputDirectory / "cobertura_coverage.xml"))
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        });

    Target Push => _ => _
        .DependsOn(Pack)
        .Requires(() => PublicMyGetSource)
        .Requires(() => PublicMyGetApiKey)
        .Requires(() => NuGetApiKey)
        .Requires(() => Configuration.EqualsOrdinalIgnoreCase("Release"))
        .Executes(() =>
        {
            GlobFiles(OutputDirectory, "*.nupkg").NotEmpty()
                .Where(x => !x.EndsWith("symbols.nupkg"))
                .ForEach(x =>
                {
                    DotNetNuGetPush(s => s
                        // Need to set it here, otherwise it takes the one from NUKEs .tmp directory
                        .SetToolPath(ToolPathResolver.GetPathExecutable("dotnet"))
                        .SetTargetPath(x)
                        .SetSource(PublicMyGetSource)
                        .SetApiKey(PublicMyGetApiKey));

                    if (GitVersion.BranchName.Equals("master") || GitVersion.BranchName.Equals("origin/master"))
                    {
                        // Stable releases are published to NuGet
                        DotNetNuGetPush(s => s
                            // Need to set it here, otherwise it takes the one from NUKEs .tmp directory
                            .SetToolPath(ToolPathResolver.GetPathExecutable("dotnet"))
                            .SetTargetPath(x)
                            .SetSource("https://api.nuget.org/v3/index.json")
                            .SetApiKey(NuGetApiKey));
                    }
                });
        });

    Target BuildDocFxMetadata => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            // So it uses a fixed, known version of MsBuild to generate the metadata. Otherwise,
            // updates of dotnet or Visual Studio could introduce incompatibilities and generation failures
            var dotnetPath = Path.GetDirectoryName(ToolPathResolver.GetPathExecutable("dotnet.exe"));
            var msBuildPath = Path.Combine(dotnetPath, "sdk", DocFxDotNetSdkVersion, "MSBuild.dll");
            SetVariable("MSBUILD_EXE_PATH", msBuildPath);
            DocFxMetadata(DocFxFile, s => s.SetLogLevel(DocFxLogLevel.Info));
        });

    Target BuildDocumentation => _ => _
        .DependsOn(Clean)
        .DependsOn(BuildDocFxMetadata)
        .Executes(() =>
        {
            // Using README.md as index.md
            if (File.Exists(SolutionDirectory / "index.md"))
            {
                File.Delete(SolutionDirectory / "index.md");
            }

            File.Copy(SolutionDirectory / "README.md", SolutionDirectory / "index.md");


            DocFxBuild(DocFxFile, s => s
                .ClearXRefMaps()
                .SetLogLevel(DocFxLogLevel.Info));

            File.Delete(SolutionDirectory / "index.md");
            Directory.Delete(SolutionDirectory / "api", true);
            Directory.Delete(SolutionDirectory / "obj", true);
        });

    Target UploadDocumentation => _ => _
        .DependsOn(Push) // To have a relation between pushed package version and published docs version
        .DependsOn(BuildDocumentation)
        .Requires(() => DocuApiKey)
        .Requires(() => DocuApiEndpoint)
        .Executes(() =>
        {
            WebDocu(s => s
                .SetDocuApiEndpoint(DocuApiEndpoint)
                .SetDocuApiKey(DocuApiKey)
                .SetSourceDirectory(OutputDirectory)
                .SetVersion(GitVersion.NuGetVersion)
            );
        });

    Target PublishGitHubRelease => _ => _
        .DependsOn(Pack)
        .Requires(() => GitHubAuthenticationToken)
        .OnlyWhen(() => GitVersion.BranchName.Equals("master") || GitVersion.BranchName.Equals("origin/master"))
        .Executes(() =>
        {
            var releaseTag = $"v{GitVersion.MajorMinorPatch}";

            var changeLogSectionEntries = ExtractChangelogSectionNotes(ChangeLogFile);
            var latestChangeLog = changeLogSectionEntries
                .Aggregate((c, n) => c + Environment.NewLine + n);
            var completeChangeLog = $"## {releaseTag}" + Environment.NewLine + latestChangeLog;

            var repositoryInfo = GetGitHubRepositoryInfo(GitRepository);
            var nuGetPackages = GlobFiles(OutputDirectory, "*.nupkg").NotEmpty().ToArray();

            PublishRelease(new GitHubReleaseSettings()
                .SetArtifactPaths(nuGetPackages)
                .SetCommitSha(GitVersion.Sha)
                .SetReleaseNotes(completeChangeLog)
                .SetRepositoryName(repositoryInfo.repositoryName)
                .SetRepositoryOwner(repositoryInfo.gitHubOwner)
                .SetTag(releaseTag)
                .SetToken(GitHubAuthenticationToken))
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        });
}
