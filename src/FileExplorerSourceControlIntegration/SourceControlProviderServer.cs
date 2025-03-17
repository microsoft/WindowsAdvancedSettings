// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using COM;
using Serilog;
using Windows.Win32;
using Windows.Win32.System.Com;

namespace FileExplorerSourceControlIntegration;

public sealed class SourceControlProviderServer : IDisposable
{
    private readonly HashSet<uint> _registrationCookies = new();
    private readonly Serilog.ILogger _log = Log.ForContext("SourceContext", nameof(SourceControlProviderServer));

    [UnconditionalSuppressMessage(
        "ReflectionAnalysis",
        "IL2050:COMCorrectness",
        Justification = "SourceControlProviderFactory and all the interfaces it implements are defined in an assembly that is not marked trimmable which means the relevant interfaces won't be trimmed.")]
    public void RegisterSourceControlProviderServer<T>(Func<T> createSourceControlProviderServer)
    {
        var clsid = typeof(SourceControlProvider).GUID;

        _log.Debug($"Registering class object:");
        _log.Debug($"CLSID: {clsid:B}");
        _log.Debug($"Type: {typeof(T)}");

        uint cookie;
        var hr = PInvoke.CoRegisterClassObject(
            clsid,
            new SourceControlProviderFactory<T>(createSourceControlProviderServer),
            CLSCTX.CLSCTX_LOCAL_SERVER,
            Ole32.REGCLS_MULTIPLEUSE | Ole32.REGCLS_SUSPENDED,
            out cookie);

        if (hr < 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        _registrationCookies.Add(cookie);
        _log.Debug($"Cookie: {cookie}");
        hr = PInvoke.CoResumeClassObjects();
        if (hr < 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }
    }

    public void Run()
    {
        // TODO : We need to handle lifetime management of the server.
        // For details around ref counting and locking of out-of-proc COM servers, see
        // https://docs.microsoft.com/windows/win32/com/out-of-process-server-implementation-helpers
        // https://github.com/microsoft/devhome/issues/645
        Console.ReadLine();
        var disposedEvent = new ManualResetEvent(false);
        disposedEvent.WaitOne();
    }

    public void Dispose()
    {
        _log.Debug($"Revoking class object registrations:");
        foreach (var cookie in _registrationCookies)
        {
            _log.Debug($"Cookie: {cookie}");
            var hr = PInvoke.CoRevokeClassObject(cookie);
            Debug.Assert(hr >= 0, $"CoRevokeClassObject failed ({hr:x}). Cookie: {cookie}");
        }
    }

    private sealed class Ole32
    {
#pragma warning disable SA1310 // Field names should not contain underscore
        // https://docs.microsoft.com/windows/win32/api/combaseapi/ne-combaseapi-regcls
        public const REGCLS REGCLS_MULTIPLEUSE = (REGCLS)1;
        public const REGCLS REGCLS_SUSPENDED = (REGCLS)4;
#pragma warning restore SA1310 // Field names should not contain underscore
    }
}
