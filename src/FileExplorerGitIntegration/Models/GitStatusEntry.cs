// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Globalization;
using LibGit2Sharp;

namespace FileExplorerGitIntegration.Models;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
internal sealed class GitStatusEntry
{
    public GitStatusEntry(string path, FileStatus status, string? renameOldPath = null)
    {
        Path = path;
        Status = status;
        RenameOldPath = renameOldPath;
    }

    public string Path { get; set; }

    public FileStatus Status { get; set; }

    public string? RenameOldPath { get; set; }

    public string? RenameNewPath { get; set; }

    private string DebuggerDisplay
    {
        get
        {
            if (Status.HasFlag(FileStatus.RenamedInIndex) || Status.HasFlag(FileStatus.RenamedInWorkdir))
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}: {1} -> {2}", Status, RenameOldPath, Path);
            }

            return string.Format(CultureInfo.InvariantCulture, "{0}: {1}", Status, Path);
        }
    }
}
