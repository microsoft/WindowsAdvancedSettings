// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Windows.AppLifecycle;
using Serilog;
using Windows.ApplicationModel.Activation;
using WindowsAdvancedSettings.Common.Helpers;

namespace FileExplorerSourceControlIntegration;

public sealed class Program
{
    [MTAThread]
    public static async Task Main([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] string[] args)
    {
        // Set up Logging
        Environment.SetEnvironmentVariable("WINDOWSADVANCEDSETTINGS_LOG_ROOT", Path.Join(Logging.LogFolderRoot, "FileExplorerSourceControlIntegration"));
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings_FileExplorerSourceControl.json")
            .Build();
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        Log.Information($"Launched with args: {string.Join(' ', args.ToArray())}");

        // Force the app to be single instanced
        // Get or register the main instance
        var mainInstance = AppInstance.FindOrRegisterForKey("mainInstance");
        var activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();

        if (!mainInstance.IsCurrent)
        {
            Log.Information($"Not main instance, redirecting.");
            await mainInstance.RedirectActivationToAsync(activationArgs);
            Log.CloseAndFlush();
            return;
        }

        if (args.Length > 0 && args[0] == "-RegisterProcessAsComServer")
        {
            HandleCOMServerActivation();
        }
        else
        {
            Log.Warning("Not being launched as a ComServer... exiting.");
        }

        Log.CloseAndFlush();
    }

    private static void HandleCOMServerActivation()
    {
        var gpoPolicyEnabled = GPOHelper.GetConfiguredEnabledWindowsAdvancedSettingsValue();
        if (!gpoPolicyEnabled)
        {
            Log.Information($"Windows Advanced Settings is disabled by policy, exiting.");
            return;
        }

        Log.Information($"Activating COM Server");
        using var sourceControlProviderServer = new SourceControlProviderServer();
        var sourceControlProviderInstance = new SourceControlProvider();
        var wrapper = new Microsoft.Internal.Windows.DevHome.Helpers.FileExplorer.ExtraFolderPropertiesWrapper(sourceControlProviderInstance, sourceControlProviderInstance);
        sourceControlProviderServer.RegisterSourceControlProviderServer(() => wrapper);
        sourceControlProviderServer.Run();
    }
}
