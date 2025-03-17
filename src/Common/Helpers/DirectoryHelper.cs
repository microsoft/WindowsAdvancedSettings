// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading;
using Serilog;

namespace WindowsAdvancedSettings.Common.Helpers;

public static class DirectoryHelper
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(DirectoryHelper));

    // Attempt to delete a directory with retries and an increasing backoff delay between retry attempts.
    // This is useful when the directory may be temporarily in use by another process and the deletion may fail.
    public static void DeleteDirectoryWithRetries(string directoryPath, bool recursive = true, int maxRetries = 3, int initialRetryDelayMs = 100, bool throwOnFailure = true)
    {
        ArgumentException.ThrowIfNullOrEmpty(directoryPath);
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);
        ArgumentOutOfRangeException.ThrowIfNegative(initialRetryDelayMs);

        var retryDelay = initialRetryDelayMs;
        for (var i = 0; i <= maxRetries; ++i)
        {
            try
            {
                if (Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, recursive);
                }

                return;
            }
            catch (Exception ex)
            {
                if (i == maxRetries)
                {
                    _log.Error(ex, $"Failed to delete directory {directoryPath} on attempt {i + 1}.");
                    if (throwOnFailure)
                    {
                        throw;
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    _log.Information(ex, $"Failed to delete directory {directoryPath} on attempt {i + 1}. Retrying up to {maxRetries - i} more times.");
                }
            }

            Thread.Sleep(retryDelay);
            retryDelay *= 2;
        }
    }
}
