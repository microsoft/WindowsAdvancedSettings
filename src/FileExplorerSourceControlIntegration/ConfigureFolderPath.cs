// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Internal.Windows.DevHome.Helpers.FileExplorer;
using Serilog;

namespace FileExplorerSourceControlIntegration;

public enum ProviderType
{
    Dev,
    Release,
}

public class ConfigureFolderPath
{
    private static readonly string _releaseProviderGuidString = "1212F95B-257E-414e-B44F-F26634BD2627";
    private static readonly string _devProviderGuidString = "40FE4D6E-C9A0-48B4-A83E-AAA1D002C0D5";
    public static readonly Guid ReleaseProvider = new(_releaseProviderGuidString);
    public static readonly Guid DevProvider = new(_devProviderGuidString);
    public static readonly Guid CurrentProvider = typeof(SourceControlProvider).GUID;

    public static void AddPath(string provider, string path)
    {
        try
        {
            var providerType = (ProviderType)Enum.Parse(typeof(ProviderType), provider, true);
            var providerGuid = GetProvider(providerType);
            AddPath(providerGuid, path);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Invalid provider: {provider}");
        }
    }

    public static void AddPath(Guid provider, string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                throw new ArgumentException($"Not a valid directory path: {path}");
            }

            var wrapperResult = ExtraFolderPropertiesWrapper.Register(path, provider);
            if (!wrapperResult.Succeeded)
            {
                Log.Error(wrapperResult.ExtendedError, "Failed to register folder for source control integration");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"An exception occurred while registering folder {path}");
        }
    }

    public static void RemovePath(string path)
    {
        Log.Information($"Removing source folder: {path}");
        try
        {
            ExtraFolderPropertiesWrapper.Unregister(path);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"An exception occurred while removing the registration of folder {path}");
        }
    }

    public static Guid GetProvider(ProviderType providerType)
    {
        return providerType switch
        {
            ProviderType.Release => ReleaseProvider,
            _ => DevProvider,
        };
    }

    public static void RemoveAllForProvider(string provider)
    {
        try
        {
            var providerType = (ProviderType)Enum.Parse(typeof(ProviderType), provider, true);
            var providerGuid = GetProvider(providerType);
            RemoveAllForProvider(providerGuid);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Invalid provider: {provider}");
        }
    }

    public static void RemoveAllForProvider(Guid provider)
    {
        Log.Information($"Removing all registered folders for provider: {provider}");
        try
        {
            foreach (var folderInfo in ExtraFolderPropertiesWrapper.GetRegisteredFolderInfos())
            {
                if (folderInfo.HandlerClsid.Equals(provider))
                {
                    RemovePath(folderInfo.RootFolderPath);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"An exception occurred while enumerating the folder properties.");
        }
    }

    public static void RemoveAllForCurrentProvider()
    {
        RemoveAllForProvider(typeof(SourceControlProvider).GUID);
    }
}
