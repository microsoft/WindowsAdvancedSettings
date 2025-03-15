// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using FileExplorerGitIntegration.Models;
using LibGit2Sharp;
using WindowsAdvancedSettings.Common.Helpers;

namespace FileExplorerGitIntegration.UnitTest;

[TestClass]
public class GitCommandRunnerTests
{
    private GitDetect GitDetector { get; set; } = new();

    private static string RepoPath => Path.Combine(Path.GetTempPath(), "GitTestRepository");

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        Debug.WriteLine("ClassInitialize");
        const string url = "http://github.com/libgit2/TestGitRepository";
        try
        {
            _ = Repository.Clone(url, RepoPath);
        }
        catch (NameConflictException)
        {
            // Clean stale test state and try again
            if (Directory.Exists(RepoPath))
            {
                // Cloning the repo leads to files that are hidden and readonly (such as under the .git directory).
                // Therefore, change the attribute so they can be deleted
                var repoDirectory = new DirectoryInfo(RepoPath)
                {
                    Attributes = System.IO.FileAttributes.Normal,
                };

                foreach (var dirInfo in repoDirectory.GetFileSystemInfos("*", SearchOption.AllDirectories))
                {
                    dirInfo.Attributes = System.IO.FileAttributes.Normal;
                }

                Directory.Delete(RepoPath, true);
            }

            _ = Repository.Clone(url, RepoPath);
        }
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        Debug.WriteLine("ClassCleanup");
        if (Directory.Exists(RepoPath))
        {
            // Cloning the repo leads to files that are hidden and readonly (such as under the .git directory).
            // Therefore, change the attribute so they can be deleted
            var repoDirectory = new DirectoryInfo(RepoPath)
            {
                Attributes = FileAttributes.Normal,
            };

            foreach (var dirInfo in repoDirectory.GetFileSystemInfos("*", SearchOption.AllDirectories))
            {
                dirInfo.Attributes = FileAttributes.Normal;
            }

            DirectoryHelper.DeleteDirectoryWithRetries(RepoPath, true, 5, 100, false);
        }
    }

    [TestMethod]
    public void TestBasicInvokeGitFunctionality()
    {
        var isGitInstalled = GitDetector.DetectGit();
        if (!isGitInstalled)
        {
            Assert.Inconclusive("Git is not installed. Test cannot run in this case.");
            return;
        }

        var result = GitExecute.ExecuteGitCommand(GitDetector.GitConfiguration.ReadInstallPath(), RepoPath, "--version");
        Assert.IsNotNull(result.Output);
        Assert.IsTrue(result.Output.Contains("git version"));
    }

    [TestMethod]
    public void TestInvokeGitFunctionalityForRawStatus()
    {
        var isGitInstalled = GitDetector.DetectGit();
        if (!isGitInstalled)
        {
            Assert.Inconclusive("Git is not installed. Test cannot run in this case.");
            return;
        }

        var result = GitExecute.ExecuteGitCommand(GitDetector.GitConfiguration.ReadInstallPath(), RepoPath, "status");
        Assert.IsNotNull(result.Output);
        Assert.IsTrue(result.Output.Contains("On branch"));
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (File.Exists(Path.Combine(Path.GetTempPath(), "GitConfiguration.json")))
        {
            File.Delete(Path.Combine(Path.GetTempPath(), "GitConfiguration.json"));
        }
    }
}
