// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using WinRT;

namespace COM;

[ComVisible(true)]
public class SourceControlProviderFactory<T> : IClassFactory
{
    private readonly Func<T> _createSourceControlProvider;

    public SourceControlProviderFactory(Func<T> createSourceControlProvider)
    {
        _createSourceControlProvider = createSourceControlProvider;
    }

    public int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject)
    {
        ppvObject = IntPtr.Zero;

        if (pUnkOuter != IntPtr.Zero)
        {
            Marshal.ThrowExceptionForHR(CLASSENOAGGREGATION);
        }

        // TODO: How to detect between WinRT/IInspectable and COM/IUnknown interfaces
        ppvObject = MarshalInspectable<T>.CreateMarshaler2(_createSourceControlProvider(), riid, true).Detach();

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
[Guid(COM.Guids.IClassFactory)]
internal interface IClassFactory
{
    [PreserveSig]
    int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject);

    [PreserveSig]
    int LockServer(bool fLock);
}
