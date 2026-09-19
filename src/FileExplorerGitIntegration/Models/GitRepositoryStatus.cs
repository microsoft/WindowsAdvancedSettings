// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using LibGit2Sharp;

namespace FileExplorerGitIntegration.Models;

internal sealed class GitRepositoryStatus
{
    private readonly Dictionary<string, GitStatusEntry> _fileEntries = new();
    private readonly Dictionary<string, SubmoduleStatus> _submoduleEntries = new();
    private readonly Dictionary<FileStatus, List<GitStatusEntry>> _statusEntries = new();

    public string BranchName { get; set; } = string.Empty;

    public bool IsHeadDetached { get; set; }

    public string UpstreamBranch { get; set; } = string.Empty;

    public int AheadBy { get; set; }

    public int BehindBy { get; set; }

    public string Sha { get; set; } = string.Empty;

    public GitRepositoryStatus()
    {
        _statusEntries.Add(FileStatus.NewInIndex, new List<GitStatusEntry>());
        _statusEntries.Add(FileStatus.ModifiedInIndex, new List<GitStatusEntry>());
        _statusEntries.Add(FileStatus.DeletedFromIndex, new List<GitStatusEntry>());
        _statusEntries.Add(FileStatus.NewInWorkdir, new List<GitStatusEntry>());
        _statusEntries.Add(FileStatus.ModifiedInWorkdir, new List<GitStatusEntry>());
        _statusEntries.Add(FileStatus.DeletedFromWorkdir, new List<GitStatusEntry>());
        _statusEntries.Add(FileStatus.RenamedInIndex, new List<GitStatusEntry>());
        _statusEntries.Add(FileStatus.RenamedInWorkdir, new List<GitStatusEntry>());
        _statusEntries.Add(FileStatus.Conflicted, new List<GitStatusEntry>());
    }

    public void Add(string path, GitStatusEntry status)
    {
        _fileEntries.Add(path, status);

        if ((status.Status & FileStatus.NewInIndex) != 0)
        {
            _statusEntries[FileStatus.NewInIndex].Add(status);
        }

        if ((status.Status & FileStatus.ModifiedInIndex) != 0)
        {
            _statusEntries[FileStatus.ModifiedInIndex].Add(status);
        }

        if ((status.Status & FileStatus.DeletedFromIndex) != 0)
        {
            _statusEntries[FileStatus.DeletedFromIndex].Add(status);
        }

        if ((status.Status & FileStatus.NewInWorkdir) != 0)
        {
            _statusEntries[FileStatus.NewInWorkdir].Add(status);
        }

        if ((status.Status & FileStatus.ModifiedInWorkdir) != 0)
        {
            _statusEntries[FileStatus.ModifiedInWorkdir].Add(status);
        }

        if ((status.Status & FileStatus.DeletedFromWorkdir) != 0)
        {
            _statusEntries[FileStatus.DeletedFromWorkdir].Add(status);
        }

        if ((status.Status & FileStatus.RenamedInIndex) != 0)
        {
            _statusEntries[FileStatus.RenamedInIndex].Add(status);
        }

        if ((status.Status & FileStatus.RenamedInWorkdir) != 0)
        {
            _statusEntries[FileStatus.RenamedInWorkdir].Add(status);
        }

        if ((status.Status & FileStatus.Conflicted) != 0)
        {
            _statusEntries[FileStatus.Conflicted].Add(status);
        }
    }

    public bool TryAdd(string path, SubmoduleStatus status)
    {
        return _submoduleEntries.TryAdd(path, status);
    }

    public Dictionary<string, GitStatusEntry> FileEntries => _fileEntries;

    public List<GitStatusEntry> Added => _statusEntries[FileStatus.NewInIndex];

    public List<GitStatusEntry> Staged => _statusEntries[FileStatus.ModifiedInIndex];

    public List<GitStatusEntry> Removed => _statusEntries[FileStatus.DeletedFromIndex];

    public List<GitStatusEntry> Untracked => _statusEntries[FileStatus.NewInWorkdir];

    public List<GitStatusEntry> Modified => _statusEntries[FileStatus.ModifiedInWorkdir];

    public List<GitStatusEntry> Missing => _statusEntries[FileStatus.DeletedFromWorkdir];

    public List<GitStatusEntry> RenamedInIndex => _statusEntries[FileStatus.RenamedInIndex];

    public List<GitStatusEntry> RenamedInWorkDir => _statusEntries[FileStatus.RenamedInWorkdir];

    public List<GitStatusEntry> Conflicted => _statusEntries[FileStatus.Conflicted];

    public Dictionary<string, SubmoduleStatus> SubmoduleEntries => _submoduleEntries;
}
