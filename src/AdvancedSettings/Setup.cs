// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using FileExplorerSourceControlIntegration;
using Microsoft.Win32;
using Serilog;

namespace WindowsAdvancedSettings;

public static class Setup
{
    private static readonly string _advancedSettingsRegistryKeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\\AdvancedSettings";
    private static readonly string _sourceControlProviderValueName = @"SourceControlProvider";
    private static readonly string _protocolLaunchValueName = @"Protocol";

#if DEBUG
    private static readonly string _protocolLaunchValue = @"ms-advancedsettingsdev://";
#else
    private static readonly string _protocolLaunchValue = @"ms-advancedsettings://";
#endif

    public static void ConfigureRegistry()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKey(_advancedSettingsRegistryKeyPath, false);
            using var key = Registry.CurrentUser.CreateSubKey(_advancedSettingsRegistryKeyPath, true);
            key.SetValue(_sourceControlProviderValueName, typeof(SourceControlProvider).GUID.ToString());
            key.SetValue(_protocolLaunchValueName, _protocolLaunchValue);
            key.Close();
            Log.Debug($"Registry Key under {_advancedSettingsRegistryKeyPath}");
            Log.Debug($"  {_sourceControlProviderValueName} = {typeof(SourceControlProvider).GUID}");
            Log.Debug($"  {_protocolLaunchValueName} = {_protocolLaunchValue}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed configuring the registry.");
        }
    }
}
