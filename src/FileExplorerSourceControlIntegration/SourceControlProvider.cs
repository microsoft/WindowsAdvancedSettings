// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using FileExplorerGitIntegration.Models;
using Microsoft.Internal.Windows.DevHome.Helpers.FileExplorer;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Foundation.Collections;
using Windows.Win32;
using Windows.Win32.System.Com;
using WinRT;

namespace FileExplorerSourceControlIntegration;

#nullable enable
[ComVisible(true)]
#if !DEBUG
[Guid("1212F95B-257E-414e-B44F-F26634BD2627")]
#else
[Guid("40FE4D6E-C9A0-48B4-A83E-AAA1D002C0D5")]
#endif
public class SourceControlProvider :
    Microsoft.Internal.Windows.DevHome.Helpers.FileExplorer.IExtraFolderPropertiesHandler,
    Microsoft.Internal.Windows.DevHome.Helpers.FileExplorer.IPerFolderRootSelector
{
    public static readonly Guid GitSourceControlGuid = typeof(GitLocalRepositoryProviderFactory).GUID;

    private readonly Serilog.ILogger _log = Log.ForContext("SourceContext", nameof(SourceControlProvider));

    public SourceControlProvider()
    {
    }

    public Microsoft.Internal.Windows.DevHome.Helpers.FileExplorer.IPerFolderRootPropertyProvider? GetProvider(string rootPath)
    {
        var localRepositoryProvider = GetLocalProvider(rootPath);
        var result = localRepositoryProvider.GetRepository(rootPath);
        if (result.Result.Status == ProviderOperationStatus.Failure)
        {
            _log.Information("Could not open local repository.");
            _log.Information(result.Result.DisplayMessage);
            return null;
        }

        return new RootFolderPropertyProvider(result.Repository);
    }

    internal ILocalRepositoryProvider GetLocalProvider(string rootPath)
    {
        ILocalRepositoryProvider? provider = null;
        var providerPtr = IntPtr.Zero;
        try
        {
            var hr = PInvoke.CoCreateInstance(GitSourceControlGuid, null, CLSCTX.CLSCTX_LOCAL_SERVER, typeof(ILocalRepositoryProvider).GUID, out var extensionObj);
            providerPtr = Marshal.GetIUnknownForObject(extensionObj);
            if (hr < 0)
            {
                Log.Debug("Failure occurred while creating instance of repository provider");
                Marshal.ThrowExceptionForHR(hr);
            }

            provider = MarshalInterface<ILocalRepositoryProvider>.FromAbi(providerPtr);
        }
        finally
        {
            if (providerPtr != IntPtr.Zero)
            {
                Marshal.Release(providerPtr);
            }
        }

        Log.Debug("GetLocalProvider succeeded");
        return provider;
    }

    IDictionary<string, object> IExtraFolderPropertiesHandler.GetProperties(string[] propertyStrings, string rootFolderPath, string relativePath)
    {
        var localProvider = GetLocalProvider(rootFolderPath);
        var localProviderResult = localProvider.GetRepository(rootFolderPath);
        if (localProviderResult.Result.Status == ProviderOperationStatus.Failure)
        {
            _log.Warning("Could not open local repository.");
            _log.Warning(localProviderResult.Result.DisplayMessage);
            throw new ArgumentException(localProviderResult.Result.DisplayMessage);
        }

        return GetProperties(propertyStrings, relativePath, localProviderResult.Repository);
    }

    internal static IPropertySet GetProperties(string[] properties, string relativePath, ILocalRepository repository)
    {
        var repositoryStatusPropertyString = "System.VersionControl.CurrentFolderStatus";
        var isFileExplorerVersionControlEnabled = true;
        var showFileExplorerVersionControlColumnData = true;
        var showRepositoryStatus = true;

        if (!isFileExplorerVersionControlEnabled || (!showFileExplorerVersionControlColumnData && !showRepositoryStatus))
        {
            return new PropertySet();
        }

        if (showFileExplorerVersionControlColumnData && !showRepositoryStatus)
        {
            var filteredPropertyStrings = properties.Where(s => s != repositoryStatusPropertyString).ToArray();
            properties = filteredPropertyStrings;
        }
        else if (!showFileExplorerVersionControlColumnData && showRepositoryStatus)
        {
            properties = [repositoryStatusPropertyString];
        }

        // Trim any string properties to 80 characters
        var result = repository.GetProperties(properties, relativePath);
        foreach (var key in result.Keys.ToList())
        {
            if (result[key] is string str)
            {
                if (str.Length > 80)
                {
                    result[key] = str[..80];
                }
            }
        }

        return result;
    }
}

internal sealed class RootFolderPropertyProvider : Microsoft.Internal.Windows.DevHome.Helpers.FileExplorer.IPerFolderRootPropertyProvider
{
    public RootFolderPropertyProvider(ILocalRepository repository)
    {
        _repository = repository;
    }

    public IPropertySet GetProperties(string[] properties, string relativePath)
    {
        return SourceControlProvider.GetProperties(properties, relativePath, _repository);
    }

    private readonly ILocalRepository _repository;
}
