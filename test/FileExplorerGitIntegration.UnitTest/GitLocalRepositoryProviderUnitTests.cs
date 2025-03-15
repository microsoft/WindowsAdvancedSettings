// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using FileExplorerGitIntegration.Models;
using LibGit2Sharp;
using WindowsAdvancedSettings.Common.Helpers;

namespace FileExplorerGitIntegration.UnitTest;

[TestClass]
public class GitLocalRepositoryProviderUnitTests
{
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
    public void OpenRepository()
    {
        _ = new GitLocalRepository(RepoPath);
        GitLocalRepositoryProviderFactory gitProvider = new GitLocalRepositoryProviderFactory();
        var result = gitProvider.GetRepository(RepoPath);
        Assert.IsNotNull(result.Repository);
    }

    [TestMethod]
    public void GetProperties()
    {
        var properties = new string[]
        {
            "System.VersionControl.LastChangeMessage",
            "System.VersionControl.LastChangeAuthorName",
            "System.VersionControl.LastChangeDate",
            "System.VersionControl.LastChangeAuthorEmail",
            "System.VersionControl.LastChangeID",
            "System.VersionControl.Status",
            "System.VersionControl.CurrentFolderStatus",
        };

        var relativePath = Path.Join("a", "a1");
        GitLocalRepository repo = new GitLocalRepository(RepoPath);
        var result = repo.GetProperties(properties, relativePath);
        Assert.IsNotNull(result);

        // Oddity: When sorting the full commit graph by time, the HEAD is a merge with two parents:
        // 1. Third a/a1; f73b9567; Apr 7 15:31:13 2005 -0700
        // 2. Fourth a/a1; d0114ab8; Apr 7 15:30:13 2005 -0700
        // It would appear that commit is older, but for some reason, this is the one that was found by LibGit2Sharp.
        // Commit 1. is "the most recent", and is the commit shown by "git log" and on the GitHub repo.
        Assert.AreEqual(result["System.VersionControl.Status"], string.Empty);
        Assert.AreEqual(result["System.VersionControl.LastChangeDate"], new System.DateTimeOffset(2005, 4, 7, 15, 30, 13, new System.TimeSpan(-7, 0, 0)));
        Assert.AreEqual(result["System.VersionControl.LastChangeMessage"], "Third a/a1");
        Assert.AreEqual(result["System.VersionControl.LastChangeAuthorEmail"], "a.u.thor@example.com");
        Assert.AreEqual(result["System.VersionControl.LastChangeAuthorName"], "A U Thor");
        Assert.AreEqual(result["System.VersionControl.LastChangeID"], "f73b95671f326616d66b2afb3bdfcdbbce110b44");
        Assert.AreEqual(result["System.VersionControl.CurrentFolderStatus"], "FolderStatusBranch ≡ | +0 ~0 -0 | +0 ~0 -0");
    }

    [TestMethod]
    public void GitStatus()
    {
        const string repoStatusProperty = "System.VersionControl.CurrentFolderStatus";
        var properties = new string[]
        {
            repoStatusProperty,
        };
        var localRepo = new GitLocalRepository(RepoPath);
        var result = localRepo.GetProperties(properties, ".");
        Assert.AreEqual(result[repoStatusProperty], "FolderStatusBranch ≡ | +0 ~0 -0 | +0 ~0 -0");

        // Add a file
        var newFileName = "newfile.txt";
        var newFilePath = Path.Join(RepoPath, newFileName);
        File.WriteAllText(newFilePath, "Initial content");
        result = localRepo.GetProperties(properties, ".");
        Assert.AreEqual(result[repoStatusProperty], "FolderStatusBranch ≡ | +0 ~0 -0 | +1 ~0 -0");

        // Stage that add
        var modifiedRepo = new Repository(RepoPath);
        modifiedRepo.Index.Add(newFileName);
        modifiedRepo.Index.Write();
        result = localRepo.GetProperties(properties, ".");
        Assert.AreEqual(result[repoStatusProperty], "FolderStatusBranch ≡ | +1 ~0 -0 | +0 ~0 -0");

        // Re-modify the staged file
        File.WriteAllText(newFilePath, "New content");
        result = localRepo.GetProperties(properties, ".");
        Assert.AreEqual(result[repoStatusProperty], "FolderStatusBranch ≡ | +1 ~0 -0 | +0 ~1 -0");

        // Delete the file, the index still shows the add
        File.Delete(newFilePath);
        result = localRepo.GetProperties(properties, ".");
        Assert.AreEqual(result[repoStatusProperty], "FolderStatusBranch ≡ | +1 ~0 -0 | +0 ~0 -1");

        // Remove from index, back to clean state
        modifiedRepo.Index.Remove(newFileName);
        modifiedRepo.Index.Write();
        result = localRepo.GetProperties(properties, ".");
        Assert.AreEqual(result[repoStatusProperty], "FolderStatusBranch ≡ | +0 ~0 -0 | +0 ~0 -0");
    }

    [TestMethod]
    public void DetectFileRename()
    {
        const string statusProperty = "System.VersionControl.Status";
        const string lastChangeMessageProperty = "System.VersionControl.LastChangeMessage";
        var properties = new string[]
        {
            statusProperty,
            lastChangeMessageProperty,
        };
        var localRepo = new GitLocalRepository(RepoPath);
        var relativeFromPath = Path.Join("a", "a1");
        var relativeToPath = Path.Join("a", "a1_renamed");

        // Get initial properties
        var result = localRepo.GetProperties(properties, relativeFromPath);
        Assert.AreEqual(result[statusProperty], string.Empty);
        Assert.AreEqual(result[lastChangeMessageProperty], "Third a/a1");
        result = localRepo.GetProperties(properties, relativeToPath);
        Assert.AreEqual(result[statusProperty], string.Empty);
        result.TryGetValue(lastChangeMessageProperty, out var lastChangeMessage);
        Assert.IsNull(lastChangeMessage);

        // Rename
        var modifiedRepo = new Repository(RepoPath);
        Commands.Move(modifiedRepo, relativeFromPath, relativeToPath);
        Commands.Stage(modifiedRepo, relativeFromPath);
        Commands.Stage(modifiedRepo, relativeToPath);

        // Get old and new properties
        result = localRepo.GetProperties(properties, relativeFromPath);
        Assert.AreEqual(result[statusProperty], string.Empty);
        Assert.AreEqual(result[lastChangeMessageProperty], "Third a/a1");
        result = localRepo.GetProperties(properties, relativeToPath);

        // This test is comparing a localized string from the PRI file
        Assert.AreEqual(result[statusProperty], "FileStatusStagedRenamed");
        Assert.AreEqual(result[lastChangeMessageProperty], "Third a/a1");

        // Reset
        modifiedRepo.Reset(ResetMode.Hard);

        // Get old and new properties
        result = localRepo.GetProperties(properties, relativeFromPath);
        Assert.AreEqual(result[statusProperty], string.Empty);
        Assert.AreEqual(result[lastChangeMessageProperty], "Third a/a1");
        result = localRepo.GetProperties(properties, relativeToPath);
        Assert.AreEqual(result[statusProperty], string.Empty);
        result.TryGetValue(lastChangeMessageProperty, out lastChangeMessage);
        Assert.IsNull(lastChangeMessage);
    }
}
