// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AdvancedSettings.Tester;
using Microsoft.Extensions.Configuration;
using Serilog;
using Windows.Storage;

internal sealed class Program
{
    private static void Main([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArray] string[] args)
    {
        Environment.SetEnvironmentVariable("WINDOWSADVANCEDSETTINGSTESTER_LOG_ROOT", ApplicationData.Current.TemporaryFolder.Path);
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings_tester.json")
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        if (args.Length == 0)
        {
            ConfigureFolderPath.DisplayStatus();
        }
        else if (args.Length > 2 && args[0].Equals("add", StringComparison.OrdinalIgnoreCase))
        {
            ConfigureFolderPath.AddPath(args[1], args[2]);
            ConfigureFolderPath.DisplayStatus();
        }
        else if (args.Length > 1 && args[0].Equals("remove", StringComparison.OrdinalIgnoreCase))
        {
            ConfigureFolderPath.RemovePath(args[1]);
            ConfigureFolderPath.DisplayStatus();
        }
        else
        {
            DisplayHelp();
        }

        Log.CloseAndFlush();
    }

    private static void DisplayHelp()
    {
        var help = "WASTester Usage:\n" +
        "   WASTester.exe add <dev|release> <path>\n" +
        "   WASTester.exe remove <path>\n" +
        "   WASTester.exe";
        Console.WriteLine(help);
    }
}
