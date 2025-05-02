// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using FileExplorerGitIntegration.Helpers;
using LibGit2Sharp;
using Microsoft.Windows.DevHome.SDK;
using Serilog;

namespace FileExplorerGitIntegration.Models;

public sealed class RepositoryWrapper : IDisposable
{
    private readonly GitDetect _gitDetect = new();
    private readonly ReaderWriterLockSlim _repoLock = new();

    private readonly string _workingDirectory;

    private readonly StatusCache _statusCache;

    private readonly string _folderStatusBranch;
    private readonly string _folderStatusDetached;
    private readonly string _fileStatusMergeConflict;
    private readonly string _fileStatusUntracked;
    private readonly string _fileStatusAdded;
    private readonly string _fileStatusAddedModified;
    private readonly string _fileStatusStaged;
    private readonly string _fileStatusStagedRenamed;
    private readonly string _fileStatusStagedModified;
    private readonly string _fileStatusStagedRenamedModified;
    private readonly string _fileStatusModified;
    private readonly string _fileStatusRenamedModified;
    private readonly string _submoduleStatusAdded;
    private readonly string _submoduleStatusChanged;
    private readonly string _submoduleStatusDirty;
    private readonly string _submoduleStatusStaged;
    private readonly string _submoduleStatusUntracked;

    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(RepositoryWrapper));

    private string? _head;
    private CommitLogCache? _commits;

    private bool _disposedValue;

    public RepositoryWrapper(string rootFolder)
    {
        _gitDetect.DetectGit();
        ValidateGitRepositoryRootPath(rootFolder);
        _workingDirectory = string.Concat(rootFolder, Path.DirectorySeparatorChar.ToString());
        _statusCache = new StatusCache(rootFolder);

        _folderStatusBranch = Resources.GetResource("FolderStatusBranch", _log);
        _folderStatusDetached = Resources.GetResource("FolderStatusDetached", _log);
        _fileStatusMergeConflict = Resources.GetResource("FileStatusMergeConflict", _log);
        _fileStatusUntracked = Resources.GetResource("FileStatusUntracked", _log);
        _fileStatusAdded = Resources.GetResource("FileStatusAdded", _log);
        _fileStatusAddedModified = Resources.GetResource("FileStatusAddedModified", _log);
        _fileStatusStaged = Resources.GetResource("FileStatusStaged", _log);
        _fileStatusStagedRenamed = Resources.GetResource("FileStatusStagedRenamed", _log);
        _fileStatusStagedModified = Resources.GetResource("FileStatusStagedModified", _log);
        _fileStatusStagedRenamedModified = Resources.GetResource("FileStatusStagedRenamedModified", _log);
        _fileStatusModified = Resources.GetResource("FileStatusModified", _log);
        _fileStatusRenamedModified = Resources.GetResource("FileStatusRenamedModified", _log);
        _submoduleStatusAdded = Resources.GetResource("SubmoduleStatusAdded", _log);
        _submoduleStatusChanged = Resources.GetResource("SubmoduleStatusChanged", _log);
        _submoduleStatusDirty = Resources.GetResource("SubmoduleStatusDirty", _log);
        _submoduleStatusStaged = Resources.GetResource("SubmoduleStatusStaged", _log);
        _submoduleStatusUntracked = Resources.GetResource("SubmoduleStatusUntracked", _log);
    }

    public void ValidateGitRepositoryRootPath(string rootFolder)
    {
        var validateGitRootRepo = GitExecute.ExecuteGitCommand(_gitDetect.GitConfiguration.ReadInstallPath(), rootFolder, "rev-parse --show-toplevel");
        var output = validateGitRootRepo.Output;
        if (validateGitRootRepo.Status != ProviderOperationStatus.Success || output is null || output.Contains("fatal: not a git repository"))
        {
            _log.Error(validateGitRootRepo.Ex, $"Failed to validate the git root repository using GitExecute. RootFolder: {rootFolder} Git output: {output} Process Error Code: {validateGitRootRepo.ProcessExitCode}");
            throw validateGitRootRepo.Ex ?? new ArgumentException($"Not a valid git repository root path:  RootFolder: {rootFolder} Git output: {output}");
        }

        if (WslIntegrator.IsWSLRepo(rootFolder))
        {
            var normalizedLinuxPath = WslIntegrator.GetNormalizedLinuxPath(rootFolder);
            if (output.TrimEnd('\n') != normalizedLinuxPath)
            {
                _log.Error($"Not a valid WSL git repository root path: {rootFolder}");
                throw new ArgumentException($"Not a valid WSL git repository root path: {rootFolder}");
            }

            return;
        }

        var normalizedRootFolderPath = rootFolder.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!output.TrimEnd('\n').Equals(normalizedRootFolderPath, StringComparison.OrdinalIgnoreCase))
        {
            _log.Error($"Not a valid git repository root path: {rootFolder}");
            throw new ArgumentException($"Not a valid git repository root path: {rootFolder}");
        }
    }

    public CommitWrapper? FindLastCommit(string relativePath)
    {
        // Fetching the most recent status to check if the file is renamed
        // should be much less expensive than getting the most recent commit.
        // So, preemtively check for a rename here.
        var commitLog = GetCommitLogCache();
        return commitLog.FindLastCommit(GetOriginalPath(relativePath));
    }

    private CommitLogCache GetCommitLogCache()
    {
        var result = GitExecute.ExecuteGitCommand(_gitDetect.GitConfiguration.ReadInstallPath(), _workingDirectory, "rev-parse HEAD");
        if (result.Status != ProviderOperationStatus.Success)
        {
            throw result.Ex ?? new InvalidOperationException(result.ProcessExitCode?.ToString(CultureInfo.InvariantCulture) ?? "Unknown error while obtaining HEAD commit");
        }

        string? head = result.Output?.Trim();
        if (string.IsNullOrEmpty(head))
        {
            throw new InvalidOperationException("Git command output is null or the repository has no commits");
        }

        if (_head != null && _commits != null)
        {
            _repoLock.EnterReadLock();
            try
            {
                if (head == _head)
                {
                    return _commits;
                }
            }
            finally
            {
                _repoLock?.ExitReadLock();
            }
        }

        // Either the commit log hasn't been created yet, or it's out of date
        _repoLock.EnterWriteLock();
        try
        {
            if (_head == null || _commits == null || head != _head)
            {
                _commits = new CommitLogCache(_workingDirectory);
                _head = head;
            }
        }
        finally
        {
            _repoLock.ExitWriteLock();
        }

        return _commits;
    }

    public string GetRepoStatus(string relativePath)
    {
        var repoStatus = _statusCache.Status;

        string branchName;
        var branchStatus = string.Empty;
        try
        {
            _repoLock.EnterWriteLock();
            branchName = repoStatus.IsHeadDetached ?
                string.Format(CultureInfo.CurrentCulture, _folderStatusDetached, repoStatus.Sha[..7]) :
                string.Format(CultureInfo.CurrentCulture, _folderStatusBranch, repoStatus.BranchName);
            if (repoStatus.UpstreamBranch != string.Empty)
            {
                var behind = repoStatus.BehindBy;
                var ahead = repoStatus.AheadBy;
                if (behind == 0 && ahead == 0)
                {
                    branchStatus = " ≡";
                }
                else if (behind > 0 && ahead > 0)
                {
                    branchStatus = " ↓" + behind + " ↑" + ahead;
                }
                else if (behind > 0)
                {
                    branchStatus = " ↓" + behind;
                }
                else if (ahead > 0)
                {
                    branchStatus = " ↑" + ahead;
                }
            }
        }
        finally
        {
            _repoLock.ExitWriteLock();
        }

        var fileStatus = $"| +{repoStatus.Added.Count} ~{repoStatus.Staged.Count + repoStatus.RenamedInIndex.Count} -{repoStatus.Removed.Count} | +{repoStatus.Untracked.Count} ~{repoStatus.Modified.Count + repoStatus.RenamedInWorkDir.Count} -{repoStatus.Missing.Count}";
        var conflicted = repoStatus.Conflicted.Count;

        if (conflicted > 0)
        {
            fileStatus += $" !{conflicted}";
        }

        return branchName + branchStatus + " " + fileStatus;
    }

    public string GetFileStatus(string relativePath)
    {
        if (_statusCache.Status.SubmoduleEntries.TryGetValue(relativePath, out var subStatus))
        {
            return ToString(subStatus);
        }
        else if (_statusCache.Status.FileEntries.TryGetValue(relativePath, out var status))
        {
            return ToString(status);
        }

        return string.Empty;
    }

    private string ToString(GitStatusEntry status)
    {
        if (status.Status == FileStatus.Unaltered || status.Status.HasFlag(FileStatus.Nonexistent | FileStatus.Ignored))
        {
            return string.Empty;
        }
        else if (status.Status.HasFlag(FileStatus.Conflicted))
        {
            return _fileStatusMergeConflict;
        }
        else if (status.Status.HasFlag(FileStatus.NewInWorkdir))
        {
            return _fileStatusUntracked;
        }

        var statusString = string.Empty;
        var added = status.Status.HasFlag(FileStatus.NewInIndex);
        var staged = status.Status.HasFlag(FileStatus.ModifiedInIndex) || status.Status.HasFlag(FileStatus.RenamedInIndex) || status.Status.HasFlag(FileStatus.TypeChangeInIndex);
        var modified = status.Status.HasFlag(FileStatus.ModifiedInWorkdir) || status.Status.HasFlag(FileStatus.TypeChangeInWorkdir);
        var renamed = status.Status.HasFlag(FileStatus.RenamedInIndex) || status.Status.HasFlag(FileStatus.RenamedInWorkdir);

        if (staged)
        {
            if (renamed && modified)
            {
                statusString = _fileStatusStagedRenamedModified;
            }
            else if (renamed)
            {
                statusString = _fileStatusStagedRenamed;
            }
            else if (modified)
            {
                statusString = _fileStatusStagedModified;
            }
            else
            {
                statusString = _fileStatusStaged;
            }
        }
        else if (added)
        {
            if (modified)
            {
                statusString = _fileStatusAddedModified;
            }
            else
            {
                statusString = _fileStatusAdded;
            }
        }
        else if (modified)
        {
            if (renamed)
            {
                statusString = _fileStatusRenamedModified;
            }
            else
            {
                statusString = _fileStatusModified;
            }
        }

        return statusString;
    }

    private string ToString(SubmoduleStatus status)
    {
        if (status.HasFlag(SubmoduleStatus.WorkDirFilesModified) || status.HasFlag(SubmoduleStatus.WorkDirFilesUntracked) || status.HasFlag(SubmoduleStatus.WorkDirFilesIndexDirty))
        {
            return _submoduleStatusDirty;
        }
        else if (status.HasFlag(SubmoduleStatus.WorkDirModified))
        {
            return _submoduleStatusChanged;
        }
        else if (status.HasFlag(SubmoduleStatus.WorkDirAdded))
        {
            return _submoduleStatusUntracked;
        }
        else if (status.HasFlag(SubmoduleStatus.IndexAdded))
        {
            return _submoduleStatusAdded;
        }
        else if (status.HasFlag(SubmoduleStatus.IndexModified))
        {
            return _submoduleStatusStaged;
        }

        return string.Empty;
    }

    // Detect uncommitted renames and return the original path.
    // This allows us to get the commit history, because the new path doesn't exist yet.
    private string GetOriginalPath(string relativePath)
    {
        _statusCache.Status.FileEntries.TryGetValue(relativePath, out var status);
        if (status is null)
        {
            return relativePath;
        }

        if (status.Status.HasFlag(FileStatus.RenamedInIndex) || status.Status.HasFlag(FileStatus.RenamedInWorkdir))
        {
            return status.RenameOldPath ?? relativePath;
        }

        return relativePath;
    }

    internal void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _repoLock.Dispose();
                _statusCache.Dispose();
            }
        }

        _disposedValue = true;
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
