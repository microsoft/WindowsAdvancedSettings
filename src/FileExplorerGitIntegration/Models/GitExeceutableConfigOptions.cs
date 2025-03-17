// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace FileExplorerGitIntegration.Models;

public partial class GitExecutableConfigOptions
{
    private const string GitExecutableConfigFileNameDefault = "GitConfiguration.json";

    public string GitExecutableConfigFileName { get; set; } = GitExecutableConfigFileNameDefault;

    private readonly string _gitExecutableConfigFolderPathDefault = Path.Combine(Path.GetTempPath(), "FileExplorerGitIntegration");

    private string? _gitExecutableConfigFolderPath;

    public string GitExecutableConfigFolderPath
    {
        get => _gitExecutableConfigFolderPath is null ? _gitExecutableConfigFolderPathDefault : _gitExecutableConfigFolderPath;
        set => _gitExecutableConfigFolderPath = string.IsNullOrEmpty(value) ? _gitExecutableConfigFolderPathDefault : value;
    }
}
