// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using FileExplorerGitIntegration.Models;
using LibGit2Sharp;
using WindowsAdvancedSettings.Common.Helpers;

namespace FileExplorerGitIntegration.UnitTest;

[TestClass]
public class GitSubmoduleUnitTests
{
    private const string RepoUrl = "https://github.com/libgit2/TestGitRepository.git";
    private const string FolderStatusProp = "System.VersionControl.CurrentFolderStatus";
    private const string StatusProp = "System.VersionControl.Status";
    private const string ShaProp = "System.VersionControl.LastChangeID";

    private static GitLocalRepository? _repo;
    private static string? _repoPath;

    public enum CommitHashState
    {
        Base,
        Previous,
        New,
        NewInSubmodule,
        Missing,
    }

    private static readonly Dictionary<CommitHashState, string> _commits = [];

    [ClassInitialize]
#pragma warning disable SA1313
    public static void ClassInitialize(TestContext _)
#pragma warning restore SA1313
    {
        _commits.Clear();
        _repoPath = Directory.CreateTempSubdirectory("GitSubmoduleUnitTests").FullName;
        Repository.Clone(RepoUrl, _repoPath);

        GitDetect gitDetector = new();
        gitDetector.DetectGit();
        var gitPath = gitDetector.GitConfiguration.ReadInstallPath();

        // Set identity for git commits
        GitExecute.ExecuteGitCommand(gitPath, _repoPath, $"config user.email test@GitSubmoduleUnitTests");
        GitExecute.ExecuteGitCommand(gitPath, _repoPath, $"config user.name Test GitSubmoduleUnitTests");

        // Get the base and previous commit SHAs
        {
            var result = GitExecute.ExecuteGitCommand(gitPath, _repoPath, "log -n 2 --pretty=format:%H -- .");
            Assert.AreEqual(Microsoft.Windows.DevHome.SDK.ProviderOperationStatus.Success, result.Status);
            Assert.IsNotNull(result.Output);
            var parts = result.Output.Split('\n');
            Assert.AreEqual(2, parts.Length);
            _commits[CommitHashState.Base] = parts[0];
            _commits[CommitHashState.Previous] = parts[1];
            Assert.AreNotEqual(_commits[CommitHashState.Base], _commits[CommitHashState.Previous]);
        }

        // Create a bunch of submodule baselines
        GitExecute.ExecuteGitCommand(gitPath, _repoPath, $"submodule add -- {RepoUrl} sm_changed_file");
        GitExecute.ExecuteGitCommand(gitPath, _repoPath, $"submodule add -- {RepoUrl} sm_changed_head");
        GitExecute.ExecuteGitCommand(gitPath, _repoPath, $"submodule add -- {RepoUrl} sm_changed_index");
        GitExecute.ExecuteGitCommand(gitPath, _repoPath, $"submodule add -- {RepoUrl} sm_changed_untracked_file");
        GitExecute.ExecuteGitCommand(gitPath, _repoPath, $"submodule add -- {RepoUrl} sm_missing_commits");
        GitExecute.ExecuteGitCommand(gitPath, _repoPath, $"submodule add -- {RepoUrl} sm_unchanged");
        GitExecute.ExecuteGitCommand(gitPath, _repoPath, $"submodule add -- {RepoUrl} sm_unchanged_detached");
        GitExecute.ExecuteGitCommand(gitPath, Path.Combine(_repoPath, "sm_unchanged_detached"), $"checkout {_commits[CommitHashState.Base]}");

        // Commit and get the new SHA
        GitExecute.ExecuteGitCommand(gitPath, _repoPath, "commit -m \"Adding submodules\"");
        {
            var result = GitExecute.ExecuteGitCommand(gitPath, _repoPath, "log -n 1 --pretty=format:%H -- .");
            Assert.AreEqual(Microsoft.Windows.DevHome.SDK.ProviderOperationStatus.Success, result.Status);
            Assert.IsNotNull(result.Output);
            var parts = result.Output.Split('\n');
            Assert.AreEqual(1, parts.Length);
            _commits[CommitHashState.New] = parts[0];
            Assert.AreNotEqual(_commits[CommitHashState.New], _commits[CommitHashState.Base]);
        }

        // Add and stage (but not commit) new submodule
        GitExecute.ExecuteGitCommand(gitPath, _repoPath, $"submodule add -- {RepoUrl} sm_added_and_uncommitted");

        // Modify submodules
        File.AppendAllText(Path.Combine(_repoPath, "sm_changed_file/master.txt"), "In this submodule, the file is changed in the working directory.");
        GitExecute.ExecuteGitCommand(gitPath, Path.Combine(_repoPath, "sm_changed_head"), $"config user.email test@GitSubmoduleUnitTests");
        GitExecute.ExecuteGitCommand(gitPath, Path.Combine(_repoPath, "sm_changed_head"), $"config user.name Test GitSubmoduleUnitTests");
        File.AppendAllText(Path.Combine(_repoPath, "sm_changed_head/master.txt"), "In this submodule, the file is changed and the change is committed to HEAD.");
        GitExecute.ExecuteGitCommand(gitPath, Path.Combine(_repoPath, "sm_changed_head"), "commit --all --message \"Committing a change in the submodule.\"");
        File.AppendAllText(Path.Combine(_repoPath, "sm_changed_index/master.txt"), "In this submodule, the file is changed and the change is committed to HEAD.");
        GitExecute.ExecuteGitCommand(gitPath, Path.Combine(_repoPath, "sm_changed_index"), "stage --all");
        File.AppendAllText(Path.Combine(_repoPath, "sm_changed_untracked_file/untracked_file.txt"), "In this submodule, we've added an untracked file.");
        GitExecute.ExecuteGitCommand(gitPath, Path.Combine(_repoPath, "sm_missing_commits"), $"checkout {_commits[CommitHashState.Previous]}");
        File.AppendAllLines(
            Path.Combine(_repoPath, ".gitmodules"),
            ["[submodule \"sm_gitmodules_only\"]", "\tpath = sm_gitmodules_only", "\turl = ..\\\\submodules_target"]);

        // Get the new commit SHA in sm_changed_head
        {
            var result = GitExecute.ExecuteGitCommand(gitPath, Path.Combine(_repoPath, "sm_changed_head"), "log -n 1 --pretty=format:%H -- .");
            Assert.AreEqual(Microsoft.Windows.DevHome.SDK.ProviderOperationStatus.Success, result.Status);
            Assert.IsNotNull(result.Output);
            var parts = result.Output.Split('\n');
            Assert.AreEqual(1, parts.Length);
            _commits[CommitHashState.NewInSubmodule] = parts[0];
            Assert.AreNotEqual(_commits[CommitHashState.NewInSubmodule], _commits[CommitHashState.Base]);
        }

        _repo = new GitLocalRepository(_repoPath);
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _repo = null;
        GC.Collect(2);
        if (_repoPath is not null)
        {
            DirectoryHelper.DeleteDirectoryWithRetries(_repoPath, true, 5, 100, false);
        }
    }

    [TestMethod]
    [DataRow("", FolderStatusProp, "FolderStatusBranch ↑1 | +1 ~1 -0 | +0 ~6 -0")]
    [DataRow(".gitmodules", StatusProp, "FileStatusStagedModified")]
    [DataRow("README.txt", StatusProp, "")]
    [DataRow("sm_added_and_uncommitted", StatusProp, "SubmoduleStatusAdded")]
    [DataRow("sm_changed_file", StatusProp, "SubmoduleStatusDirty")]
    [DataRow("sm_changed_head", StatusProp, "SubmoduleStatusChanged")]
    [DataRow("sm_changed_index", StatusProp, "SubmoduleStatusDirty")]
    [DataRow("sm_changed_untracked_file", StatusProp, "SubmoduleStatusDirty")]
    [DataRow("sm_missing_commits", StatusProp, "SubmoduleStatusChanged")]
    [DataRow("sm_unchanged", StatusProp, "")]
    [DataRow("sm_unchanged_detached", StatusProp, "")]
    public void RootFolderStatus(string path, string property, string value)
    {
        Assert.IsNotNull(_repo);
        var result = _repo.GetProperties([property], path);
        Assert.IsNotNull(result);
        Assert.AreEqual(value, result[property]);
    }

    [TestMethod]
    [DataRow("", ShaProp, CommitHashState.New)]
    [DataRow(".gitmodules", ShaProp, CommitHashState.New)]
    [DataRow("sm_added_and_uncommitted", ShaProp, CommitHashState.Missing)]
    [DataRow("sm_changed_file", ShaProp, CommitHashState.New)]
    [DataRow("sm_changed_head", ShaProp, CommitHashState.New)]
    [DataRow("sm_changed_index", ShaProp, CommitHashState.New)]
    [DataRow("sm_changed_untracked_file", ShaProp, CommitHashState.New)]
    [DataRow("sm_missing_commits", ShaProp, CommitHashState.New)]
    [DataRow("sm_unchanged", ShaProp, CommitHashState.New)]
    [DataRow("sm_unchanged_detached", ShaProp, CommitHashState.New)]
    public void RootFolderCommit(string path, string property, CommitHashState state)
    {
        Assert.IsNotNull(_repo);
        var result = _repo.GetProperties([property], path);
        Assert.IsNotNull(result);
        if (result.TryGetValue(property, out var actual))
        {
            Assert.AreEqual(_commits[state], actual);
        }
        else
        {
            Assert.AreEqual(CommitHashState.Missing, state);
        }
    }

    [TestMethod]
    [DataRow("sm_added_and_uncommitted\\.", ShaProp, CommitHashState.Base)]
    [DataRow("sm_changed_file\\.", ShaProp, CommitHashState.Base)]
    [DataRow("sm_changed_head\\.", ShaProp, CommitHashState.NewInSubmodule)]
    [DataRow("sm_changed_head\\master.txt", ShaProp, CommitHashState.NewInSubmodule)]
    [DataRow("sm_changed_index\\.", ShaProp, CommitHashState.Base)]
    [DataRow("sm_changed_untracked_file\\.", ShaProp, CommitHashState.Base)]
    [DataRow("sm_missing_commits\\.", ShaProp, CommitHashState.Previous)]
    [DataRow("sm_unchanged\\.", ShaProp, CommitHashState.Base)]
    [DataRow("sm_unchanged_detached\\.", ShaProp, CommitHashState.Base)]
    public void SubmoduleFilesCommit(string path, string property, CommitHashState state)
    {
        Assert.IsNotNull(_repo);
        var result = _repo.GetProperties([property], path);
        Assert.IsNotNull(result);
        if (result.TryGetValue(property, out var actual))
        {
            Assert.AreEqual(_commits[state], actual);
        }
        else
        {
            Assert.AreEqual(CommitHashState.Missing, state);
        }
    }
}
