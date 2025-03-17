// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using FileExplorerSourceControlIntegration;
using Microsoft.Internal.Windows.DevHome.Helpers.FileExplorer;
using Serilog;
using Windows.Storage;

namespace AdvancedSettings.Tester;

public enum ProviderType
{
    Dev,
    Release,
}

internal class ConfigureFolderPath
{
    private static readonly string _releaseProviderGuidString = "1212F95B-257E-414e-B44F-F26634BD2627";
    private static readonly string _devProviderGuidString = "40FE4D6E-C9A0-48B4-A83E-AAA1D002C0D5";
    private static readonly Guid _releaseProvider = new(_releaseProviderGuidString);
    private static readonly Guid _devProvider = new(_devProviderGuidString);

    public static void DisplayStatus()
    {
        foreach (var folderInfo in ExtraFolderPropertiesWrapper.GetRegisteredFolderInfos())
        {
            var providerName = GetProviderName(folderInfo.HandlerClsid);
            Console.WriteLine($"{providerName}: {folderInfo.RootFolderPath}  {{{folderInfo.HandlerClsid}}}  {folderInfo.AppId}");
        }
    }

    public static void AddPath(string provider, string path)
    {
        try
        {
            var providerType = (ProviderType)Enum.Parse(typeof(ProviderType), provider, true);
            AddPath(providerType, path);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Invalid provider: {provider}");
        }
    }

    public static void AddPath(ProviderType providerType, string path)
    {
        var provider = GetProvider(providerType);
        Console.WriteLine($"Registering source folder: {path} for provider {providerType}");
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

    private static Guid GetProvider(ProviderType providerType)
    {
        return providerType switch
        {
            ProviderType.Release => _releaseProvider,
            _ => _devProvider,
        };
    }

    private static string GetProviderName(Guid guid)
    {
        if (guid.Equals(_devProvider))
        {
            return "DEV";
        }
        else if (guid.Equals(_releaseProvider))
        {
            return "REL";
        }
        else
        {
            return "UNK";
        }
    }
}
