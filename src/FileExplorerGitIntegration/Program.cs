// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using FileExplorerGitIntegration.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Windows.AppLifecycle;
using Serilog;
using Windows.ApplicationModel.Activation;
using WindowsAdvancedSettings.Common.Helpers;

namespace FileExplorerGitIntegration;

public sealed class Program
{
    [MTAThread]
    public static async Task Main([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] string[] args)
    {
        // Set up Logging
        Environment.SetEnvironmentVariable("WINDOWSADVANCEDSETTINGS_LOG_ROOT", Path.Join(Logging.LogFolderRoot, "FileExplorerGitIntegration"));
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings_FileExplorerGitIntegration.json")
            .Build();
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        Log.Information($"Launched with args: {string.Join(' ', args.ToArray())}");

        // Force the app to be single instanced
        // Get or register the main instance
        var mainInstance = AppInstance.FindOrRegisterForKey("mainInstance");
        var activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();

        // If the main instance isn't this current instance
        if (!mainInstance.IsCurrent)
        {
            Log.Information($"Not main instance, redirecting.");
            await mainInstance.RedirectActivationToAsync(activationArgs);
            Log.CloseAndFlush();
            return;
        }

        // Otherwise, we're in the main instance
        // Register for activation redirection
        AppInstance.GetCurrent().Activated += AppActivationRedirected;

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

    private static void AppActivationRedirected(object? sender, Microsoft.Windows.AppLifecycle.AppActivationArguments activationArgs)
    {
        Log.Information($"Redirected with kind: {activationArgs.Kind}");

        // Handle COM server
        if (activationArgs.Kind == ExtendedActivationKind.Launch)
        {
            var d = activationArgs.Data as ILaunchActivatedEventArgs;
            var args = d?.Arguments.Split();

            if (args?.Length > 1 && args[1] == "-RegisterProcessAsComServer")
            {
                Log.Information($"Activation COM Registration Redirect: {string.Join(' ', args.ToList())}");
                HandleCOMServerActivation();
            }
        }
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

        RepositoryCache cache = new RepositoryCache();
        using var gitLocalRepositoryProviderServer = new GitLocalRepositoryProviderServer();
        var gitLocalRepositoryProviderInstance = new GitLocalRepositoryProviderFactory(cache);
        gitLocalRepositoryProviderServer.RegisterGitRepositoryProviderServer(() => gitLocalRepositoryProviderInstance);
        gitLocalRepositoryProviderServer.Run();
    }
}
