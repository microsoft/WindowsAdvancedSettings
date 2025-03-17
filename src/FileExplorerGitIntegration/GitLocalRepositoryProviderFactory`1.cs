// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using FileExplorerGitIntegration.Models;
using WinRT;

namespace FileExplorerGitIntegration;

[ComVisible(true)]
public class GitLocalRepositoryProviderFactory<T> : IClassFactory
where T : GitLocalRepositoryProviderFactory
{
    private readonly Func<T> _createGitLocalRepositoryProvider;

    public GitLocalRepositoryProviderFactory(Func<T> createGitLocalRepositoryProvider)
    {
        _createGitLocalRepositoryProvider = createGitLocalRepositoryProvider;
    }

    public int CreateInstance(nint pUnkOuter, ref Guid riid, out nint ppvObject)
    {
        ppvObject = nint.Zero;

        if (pUnkOuter != nint.Zero)
        {
            Marshal.ThrowExceptionForHR(CLASSENOAGGREGATION);
        }

        if (riid == typeof(T).GUID || riid == Guid.Parse(Guids.IUnknown))
        {
            // Create the instance of the .NET object
            ppvObject = MarshalInspectable<GitLocalRepositoryProviderFactory>.FromManaged(_createGitLocalRepositoryProvider());
        }
        else
        {
            // The object that ppvObject points to does not support the
            // interface identified by riid.
            Marshal.ThrowExceptionForHR(ENOINTERFACE);
        }

        return 0;
    }

    int IClassFactory.LockServer(bool fLock)
    {
        return 0;
    }

    private const int CLASSENOAGGREGATION = unchecked((int)0x80040110);
    private const int ENOINTERFACE = unchecked((int)0x80004002);
}

internal static class Guids
{
    public const string IClassFactory = "00000001-0000-0000-C000-000000000046";
    public const string IUnknown = "00000000-0000-0000-C000-000000000046";
}

// IClassFactory declaration
[ComImport]
[ComVisible(false)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid(Guids.IClassFactory)]
internal interface IClassFactory
{
    [PreserveSig]
    int CreateInstance(nint pUnkOuter, ref Guid riid, out nint ppvObject);

    [PreserveSig]
    int LockServer(bool fLock);
}
