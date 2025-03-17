// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Foundation.Collections;

namespace FileExplorerGitIntegration.Models;

[ComVisible(false)]
[ClassInterface(ClassInterfaceType.None)]
public sealed class GitLocalRepository : ILocalRepository
{
    private readonly RepositoryCache? _repositoryCache;

    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(GitLocalRepository));

    public string RootFolder
    {
        get; init;
    }

    public GitLocalRepository(string rootFolder)
    {
        RootFolder = rootFolder;
    }

    internal GitLocalRepository(string rootFolder, RepositoryCache? cache)
    {
        RootFolder = rootFolder;
        _repositoryCache = cache;

        try
        {
            // Rather than open the repo from scratch as validation, try to retrieve it from the cache
            OpenRepository();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Exception thrown in OpenRepository");
            throw;
        }
    }

    private RepositoryWrapper OpenRepository()
    {
        if (_repositoryCache != null)
        {
            return _repositoryCache.GetRepository(RootFolder);
        }

        return new RepositoryWrapper(RootFolder);
    }

    IPropertySet ILocalRepository.GetProperties(string[] properties, string relativePath)
    {
        relativePath = relativePath.Replace('\\', '/');
        var result = new ValueSet();

        (CommitWrapper? commit, bool alreadyFetched) latestCommit = (null, false);

        var repository = OpenRepository();

        if (repository is null)
        {
            _log.Debug("GetProperties: Repository object is null");
            return result;
        }

        // If this repo wasn't fetched from the cache, we'll need to dispose of it at the end of the method.
        using var repositoryCleanup = (_repositoryCache is null) ? repository : null;

        foreach (var propName in properties)
        {
            switch (propName)
            {
                case "System.VersionControl.LastChangeMessage":
                case "System.VersionControl.LastChangeAuthorName":
                case "System.VersionControl.LastChangeDate":
                case "System.VersionControl.LastChangeAuthorEmail":
                case "System.VersionControl.LastChangeID":
                    AddLatestCommitProperty(result, relativePath, propName, repository, ref latestCommit);
                    break;

                case "System.VersionControl.Status":
                    result.Add(propName, GetStatus(relativePath, repository));
                    break;

                case "System.VersionControl.CurrentFolderStatus":
                    var folderStatus = GetFolderStatus(relativePath, repository);
                    if (folderStatus is not null)
                    {
                        result.Add(propName, folderStatus);
                    }

                    break;
            }
        }

        _log.Debug("Returning source control properties from git source control extension");
        return result;
    }

    private void AddLatestCommitProperty(ValueSet result, string relativePath, string propName, RepositoryWrapper repository, ref (CommitWrapper? commit, bool alreadyFetched) latestCommit)
    {
        if (!latestCommit.alreadyFetched)
        {
            latestCommit.commit = FindLatestCommit(relativePath, repository);
            latestCommit.alreadyFetched = true;
        }

        if (latestCommit.commit is not null)
        {
            switch (propName)
            {
                case "System.VersionControl.LastChangeMessage":
                    result.Add(propName, latestCommit.commit.MessageShort);
                    break;
                case "System.VersionControl.LastChangeAuthorName":
                    result.Add(propName, latestCommit.commit.AuthorName);
                    break;
                case "System.VersionControl.LastChangeDate":
                    result.Add(propName, latestCommit.commit.AuthorWhen);
                    break;
                case "System.VersionControl.LastChangeAuthorEmail":
                    result.Add(propName, latestCommit.commit.AuthorEmail);
                    break;
                case "System.VersionControl.LastChangeID":
                    result.Add(propName, latestCommit.commit.Sha);
                    break;
            }
        }
    }

    public IPropertySet GetProperties(string[] properties, string relativePath)
    {
        return ((ILocalRepository)this).GetProperties(properties, relativePath);
    }

    private string? GetFolderStatus(string relativePath, RepositoryWrapper repository)
    {
        try
        {
            return repository.GetRepoStatus(relativePath);
        }
        catch
        {
            return null;
        }
    }

    private string? GetStatus(string relativePath, RepositoryWrapper repository)
    {
        try
        {
            return repository.GetFileStatus(relativePath);
        }
        catch
        {
            return null;
        }
    }

    private CommitWrapper? FindLatestCommit(string relativePath, RepositoryWrapper repository)
    {
        try
        {
            return repository.FindLastCommit(relativePath);
        }
        catch
        {
            return null;
        }
    }
}
