// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Diagnostics;
using Serilog;

namespace FileExplorerGitIntegration.Models;

public sealed class RepositoryCache : IDisposable
{
    private readonly ConcurrentDictionary<string, RepositoryWrapper> _repositoryCache = new();
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(RepositoryCache));
    private bool _disposedValue;

    public RepositoryWrapper GetRepository(string rootFolder)
    {
        if (_repositoryCache.TryGetValue(rootFolder, out RepositoryWrapper? repo))
        {
            return repo;
        }

        var tempRepo = new RepositoryWrapper(rootFolder);
        try
        {
            if (!_repositoryCache.TryAdd(rootFolder, tempRepo))
            {
                // Another thread beat us here. Dispose of the repo we just created and get the one from the cache.
                tempRepo.Dispose();
                var result = _repositoryCache.TryGetValue(rootFolder, out repo);
                Debug.Assert(result, "Failed to get newly added repo from cache");
                Debug.Assert(repo != null, "Repo from cache should not be null");
            }
            else
            {
                repo = tempRepo;
                tempRepo = null;
            }
        }
        finally
        {
            if (tempRepo != null)
            {
                tempRepo.Dispose();
            }
        }

        return repo;
    }

    internal void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                foreach (var repo in _repositoryCache.Values)
                {
                    repo.Dispose();
                }
            }

            _repositoryCache.Clear();
            _disposedValue = true;
        }
    }

    void IDisposable.Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
