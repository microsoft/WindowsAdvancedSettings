// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsAdvancedSettings.Models;

public enum GitDetectStatus
{
    // git.exe was not found on the system
    NotFound,

    // In the PATH environment variable
    PathEnvironmentVariable,

    // Probed well-known registry keys to find a Git install location
    RegistryProbe,

    // Probed well-known folders under Program Files [(x86)]
    ProgramFiles,
}
