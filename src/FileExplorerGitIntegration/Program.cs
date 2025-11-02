// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using FileExplorerGitIntegration.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Windows.AppLifecycle;
using Serilog;
using Serilog.Core;
using Windows.ApplicationModel.Activation;
using WindowsAdvancedSettings.Common.Helpers;

namespace FileExplorerGitIntegration;

/// <summary>
/// Entry point for the File Explorer Git Integration application.
/// Manages single-instance enforcement and COM server activation.
/// </summary>
public sealed class Program
{
    private const string MainInstanceKey = "FileExplorerGitIntegration_MainInstance";
    private const string ComServerArgument = "-RegisterProcessAsComServer";
    private const string LogSubDirectory = "FileExplorerGitIntegration";
    
    private static Logger? _logger;

    [MTAThread]
    public static async Task<int> Main(string[] args)
    {
        try
        {
            InitializeLogging();
            _logger?.Information("Application started with arguments: {Args}", string.Join(" ", args));

            // Enforce single-instance behavior
            if (!await EnsureSingleInstanceAsync())
            {
                return 0;
            }

            // Handle COM server activation or exit
            return HandleActivation(args);
        }
        catch (Exception ex)
        {
            _logger?.Fatal(ex, "Fatal error in application entry point");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    /// <summary>
    /// Initializes Serilog configuration and sets up the logger instance.
    /// </summary>
    private static void InitializeLogging()
    {
        try
        {
            Environment.SetEnvironmentVariable(
                "WINDOWSADVANCEDSETTINGS_LOG_ROOT",
                Path.Join(Logging.LogFolderRoot, LogSubDirectory));

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings_FileExplorerGitIntegration.json")
                .Build();

            _logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .CreateLogger();

            Log.Logger = _logger;
        }
        catch (Exception ex)
        {
            // Fallback logging if configuration fails
            Console.Error.WriteLine($"Failed to initialize logging: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Ensures only one instance of the application is running.
    /// If another instance exists, redirects the activation to it.
    /// </summary>
    /// <returns>True if this is the main instance, false if activation was redirected.</returns>
    private static async Task<bool> EnsureSingleInstanceAsync()
    {
        try
        {
            var mainInstance = AppInstance.FindOrRegisterForKey(MainInstanceKey);
            var activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();

            if (mainInstance.IsCurrent)
            {
                _logger?.Information("Main instance registered successfully");
                AppInstance.GetCurrent().Activated += OnAppActivationRedirected;
                return true;
            }

            _logger?.Information("Not main instance; redirecting activation");
            await mainInstance.RedirectActivationToAsync(activationArgs);
            return false;
        }
        catch (Exception ex)
        {
            _logger?.Error(ex, "Error ensuring single instance");
            throw;
        }
    }

    /// <summary>
    /// Handles application activation and determines the appropriate action.
    /// </summary>
    private static int HandleActivation(string[] args)
    {
        if (args.Length > 0 && args[0].Equals(ComServerArgument, StringComparison.OrdinalIgnoreCase))
        {
            return ActivateComServer();
        }

        _logger?.Warning("Application not launched as COM server; exiting");
        return 1;
    }

    /// <summary>
    /// Handles redirected activation events from secondary instances.
    /// </summary>
    private static void OnAppActivationRedirected(object? sender, AppActivationArguments activationArgs)
    {
        try
        {
            _logger?.Information("Activation redirected with kind: {ActivationKind}", activationArgs.Kind);

            if (activationArgs.Kind == ExtendedActivationKind.Launch)
            {
                var launchArgs = activationArgs.Data as ILaunchActivatedEventArgs;
                var args = launchArgs?.Arguments.Split();

                if (args?.Length > 1 && args[1].Equals(ComServerArgument, StringComparison.OrdinalIgnoreCase))
                {
                    _logger?.Information("Received COM registration request via activation");
                    ActivateComServer();
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.Error(ex, "Error handling activation redirect");
        }
    }

    /// <summary>
    /// Activates the COM server if it's enabled by policy.
    /// </summary>
    /// <returns>Exit code indicating success or failure.</returns>
    private static int ActivateComServer()
    {
        try
        {
            var gpoPolicyEnabled = GPOHelper.GetConfiguredEnabledWindowsAdvancedSettingsValue();
            if (!gpoPolicyEnabled)
            {
                _logger?.Information("Windows Advanced Settings disabled by policy; exiting");
                return 1;
            }

            _logger?.Information("Activating COM server");

            var cache = new RepositoryCache();
            using var gitLocalRepositoryProviderServer = new GitLocalRepositoryProviderServer();
            var gitLocalRepositoryProviderInstance = new GitLocalRepositoryProviderFactory(cache);
            
            gitLocalRepositoryProviderServer.RegisterGitRepositoryProviderServer(
                () => gitLocalRepositoryProviderInstance);
            
            gitLocalRepositoryProviderServer.Run();
            
            _logger?.Information("COM server completed execution");
            return 0;
        }
        catch (Exception ex)
        {
            _logger?.Error(ex, "Fatal error in COM server activation");
            return 1;
        }
    }
}
