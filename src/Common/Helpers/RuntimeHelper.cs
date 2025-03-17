// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Windows.Win32;
using Windows.Win32.Foundation;

namespace WindowsAdvancedSettings.Common.Helpers;

public static class RuntimeHelper
{
    public static bool IsPackaged
    {
        get
        {
            uint length = 0;
            return PInvoke.GetCurrentPackageFullName(ref length, null) != WIN32_ERROR.APPMODEL_ERROR_NO_PACKAGE;
        }
    }
}
