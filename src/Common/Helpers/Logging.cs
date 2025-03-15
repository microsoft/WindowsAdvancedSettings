// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Serilog;
using Windows.Storage;

namespace WindowsAdvancedSettings.Common.Helpers;

public class Logging
{
    public static readonly string LogExtension = ".waslog";

    public static readonly string LogFolderName = "Logs";

    public static readonly string DefaultLogFileName = "was";

    private static readonly Lazy<string> _logFolderRoot = new(() => Path.Combine(ApplicationData.Current.TemporaryFolder.Path, LogFolderName));

    public static readonly string LogFolderRoot = _logFolderRoot.Value;

    public static void SetupLogging(string jsonFileName, string appName)
    {
        Environment.SetEnvironmentVariable("WINDOWSADVANCEDSETTINGS_LOG_ROOT", Path.Join(LogFolderRoot, appName));
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(jsonFileName)
            .Build();
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();
    }
}
