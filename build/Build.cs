using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
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
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.GitHub.ChangeLogExtensions;
using static Nuke.GitHub.GitHubTasks;
using static Nuke.WebDocu.WebDocuTasks;
using static Nuke.Common.Tools.Docker.DockerTasks;
using Nuke.Common.Tools.Docker;
using Nuke.Common.Tools.AzureKeyVault;

class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter] readonly string KeyVaultBaseUrl;
    [Parameter] readonly string KeyVaultClientId;
    [Parameter] readonly string KeyVaultClientSecret;
    [Parameter] readonly string KeyVaultTenantId;
    [GitVersion(Framework = "net5.0")] readonly GitVersion GitVersion;
    [GitRepository] readonly GitRepository GitRepository;
    [AzureKeyVault] readonly AzureKeyVault KeyVault;

    [AzureKeyVaultConfiguration(
        BaseUrlParameterName = nameof(KeyVaultBaseUrl),
        ClientIdParameterName = nameof(KeyVaultClientId),
        ClientSecretParameterName = nameof(KeyVaultClientSecret),
        TenantIdParameterName = nameof(KeyVaultTenantId))]
    private readonly AzureKeyVaultConfiguration KeyVaultSettings;

    [Parameter] readonly string Configuration = IsLocalBuild ? "Debug" : "Release";

    [AzureKeyVaultSecret] private readonly string DocuBaseUrl;
    [AzureKeyVaultSecret] readonly string GitHubAuthenticationToken;
    [AzureKeyVaultSecret] string DanglPublicFeedSource;
    [AzureKeyVaultSecret] string FeedzAccessToken;
    [AzureKeyVaultSecret] readonly string NuGetApiKey;
    [AzureKeyVaultSecret("DanglAspNetCoreFileHandling-DocuApiKey")] readonly string DocuApiKey;

    [Solution("Dangl.AspNetCore.FileHandling.sln")] readonly Solution Solution;
    AbsolutePath SolutionDirectory => Solution.Directory;
    AbsolutePath OutputDirectory => SolutionDirectory / "output";
    AbsolutePath SourceDirectory => SolutionDirectory / "src";

    string DocFxFile => SolutionDirectory / "docfx.json";
    string ChangeLogFile => RootDirectory / "CHANGELOG.md";

    Target Clean => _ => _
            .Executes(() =>
            {
                SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(d => d.DeleteDirectory());
                (SolutionDirectory / "test").GlobDirectories("**/bin", "**/obj").ForEach(d => d.DeleteDirectory());
                OutputDirectory.CreateOrCleanDirectory();
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
            var testProjects = (RootDirectory / "test").GlobFiles("**/*.csproj").ToList();
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
        var testResults = OutputDirectory.GlobFiles("*testresults*.xml").ToList();
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
        testResults.ForEach(f => f.DeleteFile());
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
        .Requires(() => DanglPublicFeedSource)
        .Requires(() => FeedzAccessToken)
        .Requires(() => NuGetApiKey)
        .Requires(() => Configuration.EqualsOrdinalIgnoreCase("Release"))
        .OnlyWhenDynamic(() => IsOnBranch("master") || IsOnBranch("develop"))
        .Executes(() =>
        {
            var packages = OutputDirectory.GlobFiles("*.nupkg")
                .Select(f => f.ToString())
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
                        .SetSource(DanglPublicFeedSource)
                        .SetApiKey(FeedzAccessToken));

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
            var environmentVariables = EnvironmentInfo.Variables.ToDictionary();
            environmentVariables.Add("DOCFX_SOURCE_BRANCH_NAME", GitVersion.BranchName);
            var docFxPath = NuGetToolPathResolver.GetPackageExecutable("docfx", "tools/net8.0/any/docfx.dll");
            DotNet($"{docFxPath} metadata {DocFxFile}", environmentVariables: environmentVariables);
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

            var environmentVariables = EnvironmentInfo.Variables.ToDictionary();
            environmentVariables.Add("DOCFX_SOURCE_BRANCH_NAME", GitVersion.BranchName);
            var docFxPath = NuGetToolPathResolver.GetPackageExecutable("docfx", "tools/net8.0/any/docfx.dll");
            DotNet($"{docFxPath} {DocFxFile}", environmentVariables: environmentVariables);

            File.Delete(SolutionDirectory / "index.md");
            Directory.Delete(SolutionDirectory / "api", true);
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
            var nuGetPackages = OutputDirectory.GlobFiles("*.nupkg").ToArray();
            Assert.NotEmpty(nuGetPackages);

            PublishRelease(x => x
                .SetArtifactPaths(nuGetPackages.Select(a => a.ToString()).ToArray())
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
