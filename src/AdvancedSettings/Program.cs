// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using FileExplorerSourceControlIntegration;
using Microsoft.Extensions.Configuration;
using Microsoft.Internal.Windows.DevHome.Helpers.FileExplorer;
using Microsoft.Windows.AppLifecycle;
using Serilog;
using WindowsAdvancedSettings.Common.Helpers;

namespace WindowsAdvancedSettings;

public sealed class Program
{
    [MTAThread]
    public static async Task Main([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] string[] args)
    {
        // Set up Logging
        Environment.SetEnvironmentVariable("WINDOWSADVANCEDSETTINGS_LOG_ROOT", Path.Join(Logging.LogFolderRoot, "WindowsAdvancedSettings"));
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
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

        Setup.ConfigureRegistry();
        Log.CloseAndFlush();
#if DEBUG
        Console.ReadLine();
#endif
    }

    private static void AppActivationRedirected(object? sender, Microsoft.Windows.AppLifecycle.AppActivationArguments activationArgs)
    {
        Log.Information($"Redirected with kind: {activationArgs.Kind}");
    }
}
