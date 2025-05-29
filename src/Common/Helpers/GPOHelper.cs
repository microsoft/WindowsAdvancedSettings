// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Win32;
using Serilog;

namespace WindowsAdvancedSettings.Common.Helpers;

public class GPOHelper
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(GPOHelper));

    private enum GpoRuleConfigured
    {
        WrongValue = -3, // The policy is set to an unrecognized value
        Unavailable = -2, // Couldn't access registry
        NotConfigured = -1, // Policy is not configured
        Disabled = 0, // Policy is disabled
        Enabled = 1, // Policy is enabled
    }

    // Registry path where gpo policy values are stored
    private const string PoliciesScopeMachine = "HKEY_LOCAL_MACHINE";
    private const string PoliciesPath = @"\SOFTWARE\Policies\Microsoft\Windows\WindowsAdvancedSettings";

    // Registry value names
    private const string PolicyConfigureEnabledWindowsAdvancedSettings = "ConfigureEnabledWindowsAdvancedSettings";

    private static GpoRuleConfigured GetConfiguredValue(string registryValueName)
    {
        try
        {
            var rawValue = Registry.GetValue(
                keyName: PoliciesScopeMachine + PoliciesPath,
                valueName: registryValueName,
                defaultValue: GpoRuleConfigured.NotConfigured);

            _log.Debug($"Registry value {registryValueName} set to {rawValue}");

            // Value will be null if the subkey specified by keyName does not exist.
            if (rawValue == null)
            {
                return GpoRuleConfigured.NotConfigured;
            }
            else if (rawValue is not int && rawValue is not GpoRuleConfigured)
            {
                return GpoRuleConfigured.WrongValue;
            }
            else
            {
                return (GpoRuleConfigured)rawValue;
            }
        }
        catch (System.Security.SecurityException)
        {
            // The user does not have the permissions required to read from the registry key.
            return GpoRuleConfigured.Unavailable;
        }
        catch (System.IO.IOException)
        {
            // The RegistryKey that contains the specified value has been marked for deletion.
            return GpoRuleConfigured.Unavailable;
        }
        catch (System.ArgumentException)
        {
            // keyName does not begin with a valid registry root.
            return GpoRuleConfigured.NotConfigured;
        }
    }

    private static bool EvaluateConfiguredValue(string registryValueName, GpoRuleConfigured defaultValue)
    {
        var configuredValue = GetConfiguredValue(registryValueName);
        if (configuredValue < 0)
        {
            if (configuredValue != GpoRuleConfigured.NotConfigured)
            {
                // Only log an error if a configuration was attempted but was incorrect. NotConfigured is expected state.
                _log.Error($"Registry value {registryValueName} set to {configuredValue}, using default {defaultValue} instead.");
            }

            configuredValue = defaultValue;
        }

        return configuredValue == GpoRuleConfigured.Enabled;
    }

    public static bool GetConfiguredEnabledWindowsAdvancedSettingsValue()
    {
        var defaultValue = GpoRuleConfigured.Enabled;
        return EvaluateConfiguredValue(PolicyConfigureEnabledWindowsAdvancedSettings, defaultValue);
    }
}
