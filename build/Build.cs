using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.AzureKeyVault;
using Nuke.Common.Tools.AzureKeyVault.Attributes;
using Nuke.Common.Tools.DocFX;
using Nuke.Common.Tools.DotCover;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.ReportGenerator;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using Nuke.GitHub;
using Nuke.WebDocu;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using static Nuke.Common.ChangeLog.ChangelogTasks;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.IO.XmlTasks;
using static Nuke.Common.Tools.DocFX.DocFXTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.GitHub.ChangeLogExtensions;
using static Nuke.GitHub.GitHubTasks;
using static Nuke.WebDocu.WebDocuTasks;
using static Nuke.Common.Tools.Docker.DockerTasks;
using Nuke.Common.Tools.Docker;

class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter] readonly string KeyVaultBaseUrl;
    [Parameter] readonly string KeyVaultClientId;
    [Parameter] readonly string KeyVaultClientSecret;
    [GitVersion(Framework = "net5.0")] readonly GitVersion GitVersion;
    [GitRepository] readonly GitRepository GitRepository;
    [KeyVault] readonly KeyVault KeyVault;

    [KeyVaultSettings(
        BaseUrlParameterName = nameof(KeyVaultBaseUrl),
        ClientIdParameterName = nameof(KeyVaultClientId),
        ClientSecretParameterName = nameof(KeyVaultClientSecret))]
    private readonly KeyVaultSettings KeyVaultSettings;

    [Parameter] readonly string Configuration = IsLocalBuild ? "Debug" : "Release";

    [KeyVaultSecret] private readonly string DocuBaseUrl;
    [KeyVaultSecret] readonly string GitHubAuthenticationToken;
    [KeyVaultSecret] readonly string PublicMyGetSource;
    [KeyVaultSecret] readonly string PublicMyGetApiKey;
    [KeyVaultSecret] readonly string NuGetApiKey;
    [KeyVaultSecret("DanglAspNetCoreFileHandling-DocuApiKey")] readonly string DocuApiKey;

    [Solution("Dangl.AspNetCore.FileHandling.sln")] readonly Solution Solution;
    AbsolutePath SolutionDirectory => Solution.Directory;
    AbsolutePath OutputDirectory => SolutionDirectory / "output";
    AbsolutePath SourceDirectory => SolutionDirectory / "src";

    string DocFxFile => SolutionDirectory / "docfx.json";
    string ChangeLogFile => RootDirectory / "CHANGELOG.md";

    Target Clean => _ => _
            .Executes(() =>
            {
                GlobDirectories(SourceDirectory, "**/bin", "**/obj").ForEach(DeleteDirectory);
                GlobDirectories(SolutionDirectory / "test", "**/bin", "**/obj").ForEach(DeleteDirectory);
                EnsureCleanDirectory(OutputDirectory);
            });

    Target Restore => _ => _
            .DependsOn(Clean)
            .Executes(() =>
            {
                DotNetRestore();
            });

    Target Compile => _ => _
            .DependsOn(Restore)
            .Executes(() =>
            {
                DotNetBuild(x => x
                    .SetConfiguration(Configuration)
                    .EnableNoRestore()
                    .SetFileVersion(GitVersion.AssemblySemFileVer)
                    .SetAssemblyVersion(GitVersion.AssemblySemVer)
                    .SetInformationalVersion(GitVersion.InformationalVersion));
            });

    Target Pack => _ => _
            .DependsOn(Compile)
            .Executes(() =>
            {
                var changeLog = GetCompleteChangeLog(ChangeLogFile)
                    .EscapeStringPropertyForMsBuild();

                DotNetPack(x => x
                    .SetConfiguration(Configuration)
                    .SetPackageReleaseNotes(changeLog)
                    .SetDescription("Dangl.AspNetCore.FileHandling www.dangl-it.com")
                    .SetTitle("Dangl.AspNetCore.FileHandling www.dangl-it.com")
                    .EnableNoBuild()
                    .SetOutputDirectory(OutputDirectory)
                    .SetVersion(GitVersion.NuGetVersion));
            });

    Target Tests => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var testProjects = GlobFiles(RootDirectory / "test", "**/*.csproj").ToList();
            var dotnetPath = ToolPathResolver.GetPathExecutable("dotnet");
            var snapshotIndex = 0;

            // That's required since the Azure integration tests depend on that image
            DockerPull(c => c.SetName("mcr.microsoft.com/azure-storage/azurite"));

            DotNetTest(c => c
                .SetNoBuild(true)
                .SetTestAdapterPath(".")
                    .CombineWith(cc => testProjects.SelectMany(testProject =>
                    {
                        var projectDirectory = Path.GetDirectoryName(testProject);
                        var targetFrameworks = GetTestFrameworksForProjectFile(testProject);
                        return targetFrameworks.Select(targetFramework =>
                        {
                            snapshotIndex++;
                            return cc
                                .SetProcessWorkingDirectory(projectDirectory)
                                .SetFramework(targetFramework)
                                .SetLoggers($"xunit;LogFilePath={OutputDirectory}/{snapshotIndex}_testresults-{targetFramework}.xml");
                        });
                    })), degreeOfParallelism: System.Environment.ProcessorCount);

            PrependFrameworkToTestresults();
        });

    private IEnumerable<string> GetTestFrameworksForProjectFile(string projectFile)
    {
        var targetFrameworks = XmlPeek(projectFile, "//Project/PropertyGroup//TargetFrameworks")
            .Concat(XmlPeek(projectFile, "//Project/PropertyGroup//TargetFramework"))
            .Distinct()
            .SelectMany(f => f.Split(';'))
            .Distinct();
        return targetFrameworks;
    }

    private void PrependFrameworkToTestresults()
    {
        var testResults = GlobFiles(OutputDirectory, "*testresults*.xml").ToList();
        Serilog.Log.Information($"Found {testResults.Count} test result files on which to append the framework.");
        foreach (var testResultFile in testResults)
        {
            var frameworkName = GetFrameworkNameFromFilename(testResultFile);
            var xDoc = XDocument.Load(testResultFile);

            foreach (var testType in ((IEnumerable)xDoc.XPathEvaluate("//test/@type")).OfType<XAttribute>())
            {
                testType.Value = frameworkName + "+" + testType.Value;
            }

            foreach (var testName in ((IEnumerable)xDoc.XPathEvaluate("//test/@name")).OfType<XAttribute>())
            {
                testName.Value = frameworkName + "+" + testName.Value;
            }

            xDoc.Save(testResultFile);
        }

        // Merge all the results to a single file
        // The "run-time" attributes of the single assemblies is ensured to be unique for each single assembly by this test,
        // since in Jenkins, the format is internally converted to JUnit. Aterwards, results with the same timestamps are
        // ignored. See here for how the code is translated to JUnit format by the Jenkins plugin:
        // https://github.com/jenkinsci/xunit-plugin/blob/d970c50a0501f59b303cffbfb9230ba977ce2d5a/src/main/resources/org/jenkinsci/plugins/xunit/types/xunitdotnet-2.0-to-junit.xsl#L75-L79
        Serilog.Log.Information("Updating \"run-time\" attributes in assembly entries to prevent Jenkins to treat them as duplicates");
        var firstXdoc = XDocument.Load(testResults[0]);
        var runtime = DateTime.Now;
        var firstAssemblyNodes = firstXdoc.Root.Elements().Where(e => e.Name.LocalName == "assembly");
        foreach (var assemblyNode in firstAssemblyNodes)
        {
            assemblyNode.SetAttributeValue("run-time", $"{runtime:HH:mm:ss}");
            runtime = runtime.AddSeconds(1);
        }
        for (var i = 1; i < testResults.Count; i++)
        {
            var xDoc = XDocument.Load(testResults[i]);
            var assemblyNodes = xDoc.Root.Elements().Where(e => e.Name.LocalName == "assembly");
            foreach (var assemblyNode in assemblyNodes)
            {
                assemblyNode.SetAttributeValue("run-time", $"{runtime:HH:mm:ss}");
                runtime = runtime.AddSeconds(1);
            }
            firstXdoc.Root.Add(assemblyNodes);
        }

        firstXdoc.Save(OutputDirectory / "testresults.xml");
        testResults.ForEach(DeleteFile);
    }

    private string GetFrameworkNameFromFilename(string filename)
    {
        var name = Path.GetFileName(filename);
        name = name.Substring(0, name.Length - ".xml".Length);
        var startIndex = name.LastIndexOf('-');
        name = name.Substring(startIndex + 1);
        return name;
    }

    Target Push => _ => _
        .DependsOn(Pack)
        .Requires(() => PublicMyGetSource)
        .Requires(() => PublicMyGetApiKey)
        .Requires(() => NuGetApiKey)
        .Requires(() => Configuration.EqualsOrdinalIgnoreCase("Release"))
        .OnlyWhenDynamic(() => IsOnBranch("master") || IsOnBranch("develop"))
        .Executes(() =>
        {
            var packages = GlobFiles(OutputDirectory, "*.nupkg")
                .Where(x => !x.EndsWith("symbols.nupkg"))
                .ToList();
            Assert.NotEmpty(packages);

            packages
                .ForEach(x =>
                {
                    DotNetNuGetPush(s => s
                        // Need to set it here, otherwise it takes the one from NUKEs .tmp directory
                        .SetProcessToolPath(ToolPathResolver.GetPathExecutable("dotnet"))
                        .SetTargetPath(x)
                        .SetSource(PublicMyGetSource)
                        .SetApiKey(PublicMyGetApiKey));

                    if (GitVersion.BranchName.Equals("master") || GitVersion.BranchName.Equals("origin/master"))
                    {
                        // Stable releases are published to NuGet
                        DotNetNuGetPush(s => s
                            // Need to set it here, otherwise it takes the one from NUKEs .tmp directory
                            .SetProcessToolPath(ToolPathResolver.GetPathExecutable("dotnet"))
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
            DocFXMetadata(x => x
                .SetProcessEnvironmentVariable("DOCFX_SOURCE_BRANCH_NAME", GitVersion.BranchName)
                .SetProjects(DocFxFile));
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

            DocFXBuild(x => x
                .SetProcessEnvironmentVariable("DOCFX_SOURCE_BRANCH_NAME", GitVersion.BranchName)
                .SetConfigFile(DocFxFile));

            File.Delete(SolutionDirectory / "index.md");
            Directory.Delete(SolutionDirectory / "api", true);
            Directory.Delete(SolutionDirectory / "obj", true);
        });

    Target UploadDocumentation => _ => _
        .DependsOn(Push) // To have a relation between pushed package version and published docs version
        .DependsOn(BuildDocumentation)
        .Requires(() => DocuApiKey)
        .Requires(() => DocuBaseUrl)
        .OnlyWhenDynamic(() => IsOnBranch("master") || IsOnBranch("develop"))
        .Executes(() =>
        {
            var changeLog = GetCompleteChangeLog(ChangeLogFile);

            WebDocu(s => s
                .SetDocuBaseUrl(DocuBaseUrl)
                .SetDocuApiKey(DocuApiKey)
                .SetMarkdownChangelog(changeLog)
                .SetSourceDirectory(OutputDirectory)
                .SetVersion(GitVersion.NuGetVersion)
            );
        });

    Target PublishGitHubRelease => _ => _
        .DependsOn(Pack)
        .Requires(() => GitHubAuthenticationToken)
        .OnlyWhenDynamic(() => IsOnBranch("master"))
        .Executes(() =>
        {
            var releaseTag = $"v{GitVersion.MajorMinorPatch}";

            var changeLogSectionEntries = ExtractChangelogSectionNotes(ChangeLogFile);
            var latestChangeLog = changeLogSectionEntries
                .Aggregate((c, n) => c + Environment.NewLine + n);
            var completeChangeLog = $"## {releaseTag}" + Environment.NewLine + latestChangeLog;

            var repositoryInfo = GetGitHubRepositoryInfo(GitRepository);
            var nuGetPackages = GlobFiles(OutputDirectory, "*.nupkg").ToArray();
            Assert.NotEmpty(nuGetPackages);

            PublishRelease(x => x
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

    private bool IsOnBranch(string branchName)
    {
        return GitVersion.BranchName.Equals(branchName) || GitVersion.BranchName.Equals($"origin/{branchName}");
    }
}
