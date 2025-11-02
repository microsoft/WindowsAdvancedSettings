// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Win32;
using Serilog;
using System;

namespace WindowsAdvancedSettings.Common.Helpers;

public class GPOHelper
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(GPOHelper));

    private enum GpoRuleConfigured
    {
        WrongValue = -3,      // The policy is set to an unrecognized value
        Unavailable = -2,     // Couldn't access registry
        NotConfigured = -1,   // Policy is not configured
        Disabled = 0,         // Policy is disabled
        Enabled = 1,          // Policy is enabled
    }

    private const string PoliciesScopeMachine = "HKEY_LOCAL_MACHINE";
    private const string PoliciesRegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsAdvancedSettings";
    private const string PolicyConfigureEnabledWindowsAdvancedSettings = "ConfigureEnabledWindowsAdvancedSettings";

    private static GpoRuleConfigured GetConfiguredValue(string registryValueName)
    {
        ArgumentNullException.ThrowIfNull(registryValueName, nameof(registryValueName));

        try
        {
            var rawValue = Registry.GetValue(
                keyName: PoliciesScopeMachine + @"\SOFTWARE\Policies\Microsoft\Windows\WindowsAdvancedSettings",
                valueName: registryValueName,
                defaultValue: null);

            _log.Debug("Retrieved registry value {RegistryValueName} with result {@RawValue}", 
                registryValueName, rawValue);

            // Try to convert the registry value to our enum
            var convertedValue = ConvertToGpoRuleConfigured(rawValue);
            return convertedValue;
        }
        catch (System.Security.SecurityException ex)
        {
            _log.Warning(ex, "Access denied when reading registry value {RegistryValueName}", registryValueName);
            return GpoRuleConfigured.Unavailable;
        }
        catch (System.IO.IOException ex)
        {
            _log.Warning(ex, "IO error when accessing registry value {RegistryValueName}", registryValueName);
            return GpoRuleConfigured.Unavailable;
        }
        catch (System.ArgumentException ex)
        {
            _log.Warning(ex, "Invalid registry path for value {RegistryValueName}", registryValueName);
            return GpoRuleConfigured.NotConfigured;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Unexpected error reading registry value {RegistryValueName}", registryValueName);
            return GpoRuleConfigured.Unavailable;
        }
    }

    private static GpoRuleConfigured ConvertToGpoRuleConfigured(object? rawValue)
    {
        // Use pattern matching for cleaner type checking
        return rawValue switch
        {
            null => GpoRuleConfigured.NotConfigured,
            int intValue => Enum.IsDefined(typeof(GpoRuleConfigured), intValue)
                ? (GpoRuleConfigured)intValue
                : GpoRuleConfigured.WrongValue,
            GpoRuleConfigured enumValue => enumValue,
            long longValue => TryConvertLongToGpo(longValue),
            string stringValue => TryConvertStringToGpo(stringValue),
            _ => GpoRuleConfigured.WrongValue
        };
    }

    private static GpoRuleConfigured TryConvertLongToGpo(long value)
    {
        try
        {
            var intValue = Convert.ToInt32(value);
            return Enum.IsDefined(typeof(GpoRuleConfigured), intValue) 
                ? (GpoRuleConfigured)intValue 
                : GpoRuleConfigured.WrongValue;
        }
        catch (OverflowException)
        {
            return GpoRuleConfigured.WrongValue;
        }
    }

    private static GpoRuleConfigured TryConvertStringToGpo(string value)
    {
        if (int.TryParse(value, out int intValue))
        {
            return Enum.IsDefined(typeof(GpoRuleConfigured), intValue) 
                ? (GpoRuleConfigured)intValue 
                : GpoRuleConfigured.WrongValue;
        }

        return GpoRuleConfigured.WrongValue;
    }

    private static bool EvaluateConfiguredValue(string registryValueName, GpoRuleConfigured defaultValue)
    {
        ArgumentNullException.ThrowIfNull(registryValueName, nameof(registryValueName));

        var configuredValue = GetConfiguredValue(registryValueName);

        // If we got an error state (negative value) and it's not NotConfigured, log it
        if (configuredValue < 0 && configuredValue != GpoRuleConfigured.NotConfigured)
        {
            _log.Error("Registry value {RegistryValueName} is in invalid state {State}, using default {DefaultValue}", 
                registryValueName, configuredValue, defaultValue);
            configuredValue = defaultValue;
        }
        else if (configuredValue < 0)
        {
            // NotConfigured is normal - use default without error logging
            configuredValue = defaultValue;
        }

        return configuredValue == GpoRuleConfigured.Enabled;
    }

    public static bool GetConfiguredEnabledWindowsAdvancedSettingsValue()
    {
        return EvaluateConfiguredValue(
            PolicyConfigureEnabledWindowsAdvancedSettings, 
            GpoRuleConfigured.Enabled);
    }
}