// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using WindowsAdvancedSettings.Common.Helpers;

namespace FileExplorerGitIntegration.Models;

public class GitExecute
{
    public static GitCommandRunnerResultInfo ExecuteGitCommand(string gitApplication, string repositoryDirectory, string arguments)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo();
            if (!WslIntegrator.IsWSLRepo(repositoryDirectory))
            {
                processStartInfo.FileName = gitApplication;
                processStartInfo.Arguments = arguments;
                processStartInfo.WorkingDirectory = repositoryDirectory;
            }
            else
            {
                Log.Information("Wsl.exe will be invoked to obtain property information from git");
                processStartInfo.FileName = "wsl";
                processStartInfo.Arguments = string.Concat(WslIntegrator.GetArgumentPrefixForWsl(repositoryDirectory), arguments);
                processStartInfo.WorkingDirectory = WslIntegrator.GetWorkingDirectory(repositoryDirectory);
            }

            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.UseShellExecute = false;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;

            using var process = Process.Start(processStartInfo);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd();

                // Add timeout for 1 minute
                process.WaitForExit(TimeSpan.FromMinutes(1));

                if (process.ExitCode != 0)
                {
                    Log.Error("Execute Git process exited unsuccessfully with exit code {ExitCode}", process.ExitCode);
                    return new GitCommandRunnerResultInfo(ProviderOperationStatus.Failure, output, "Execute Git process exited unsuccessfully", string.Empty, null, arguments, process.ExitCode);
                }

                return new GitCommandRunnerResultInfo(ProviderOperationStatus.Success, output);
            }
            else
            {
                Log.Error("Failed to start the Git process: process is null");
                return new GitCommandRunnerResultInfo(ProviderOperationStatus.Failure, null, "Git process is null", string.Empty, new InvalidOperationException("Failed to start the Git process: process is null"), null, null);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to invoke Git with arguments: {Argument}", arguments);
            return new GitCommandRunnerResultInfo(ProviderOperationStatus.Failure, null, "Failed to invoke Git with arguments", string.Empty, ex, arguments, null);
        }
    }
}
