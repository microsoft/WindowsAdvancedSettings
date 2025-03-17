// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Windows.ApplicationModel.Resources;
using Serilog;

namespace WindowsAdvancedSettings.Common.Helpers;

public static class Resources
{
    private static ResourceLoader? _resourceLoader;

    public static string GetResource(string identifier, ILogger? log = null)
    {
        try
        {
            if (_resourceLoader == null)
            {
                if (RuntimeHelper.IsPackaged)
                {
                    // Packaged resource map will be in the merged PRI.
                    _resourceLoader = new ResourceLoader(ResourceLoader.GetDefaultResourceFilePath(), "Resources");
                }
                else
                {
                    // Unpackaged will not be merged and will instead be in the named PRI.
                    _resourceLoader = new ResourceLoader("FileExplorerGitIntegration.pri", "FileExplorerGitIntegration/Resources");
                }
            }

            return _resourceLoader.GetString(identifier);
        }
        catch (Exception ex)
        {
            log?.Error(ex, $"Failed loading resource: {identifier}");

            // If we fail, load the original identifier so it is obvious which resource is missing.
            return identifier;
        }
    }
}
