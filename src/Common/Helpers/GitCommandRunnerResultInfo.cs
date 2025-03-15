// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Windows.DevHome.SDK;

namespace WindowsAdvancedSettings.Common.Helpers;

public class GitCommandRunnerResultInfo
{
    public ProviderOperationStatus Status { get; set; }

    public string? Output { get; set; }

    public string? DisplayMessage { get; set; }

    public string? DiagnosticText { get; set; }

    public Exception? Ex { get; set; }

    public string? Arguments { get; set; }

    public int? ProcessExitCode { get; set; }

    public GitCommandRunnerResultInfo(ProviderOperationStatus status, string? output)
    {
        Status = status;
        Output = output;
    }

    public GitCommandRunnerResultInfo(ProviderOperationStatus status, string? output, string? displayMessage, string? diagnosticText, Exception? ex, string? args, int? processExitCode)
    {
        Status = status;
        Output = output;
        DisplayMessage = displayMessage;
        DiagnosticText = diagnosticText;
        Ex = ex;
        Arguments = args;
        ProcessExitCode = processExitCode;
    }
}
