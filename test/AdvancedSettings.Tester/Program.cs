// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using FileExplorerSourceControlIntegration;
using Microsoft.Extensions.Configuration;
using Microsoft.Internal.Windows.DevHome.Helpers.FileExplorer;
using Microsoft.Windows.DevHome.SDK;
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
            DisplayStatus();
        }
        else if (args.Length > 2 && args[0].Equals("add", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"Registering source folder: {args[1]} for provider {args[2]}");
            ConfigureFolderPath.AddPath(args[1], args[2]);
            DisplayStatus();
        }
        else if (args.Length > 1 && args[0].Equals("remove", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"Removing path {args[1]}");
            ConfigureFolderPath.RemovePath(args[1]);
            DisplayStatus();
        }
        else if (args.Length > 1 && args[0].Equals("removeall", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"Removing all folders for provider: {args[1]}");
            ConfigureFolderPath.RemoveAllForProvider(args[1]);
        }
        else
        {
            DisplayHelp();
        }

        Log.CloseAndFlush();
    }

    private static void DisplayStatus()
    {
        foreach (var folderInfo in ExtraFolderPropertiesWrapper.GetRegisteredFolderInfos())
        {
            var providerName = GetProviderName(folderInfo.HandlerClsid);
            Console.WriteLine($"{providerName}: {folderInfo.RootFolderPath}  {{{folderInfo.HandlerClsid}}}  {folderInfo.AppId}");
        }
    }

    private static string GetProviderName(Guid guid)
    {
        if (guid.Equals(ConfigureFolderPath.DevProvider))
        {
            return "DEV";
        }
        else if (guid.Equals(ConfigureFolderPath.ReleaseProvider))
        {
            return "REL";
        }
        else
        {
            return "UNK";
        }
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
