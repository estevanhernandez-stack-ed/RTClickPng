using System.Runtime.InteropServices;

namespace RTClickPng.Engine.Interop;

/// <summary>
/// P/Invoke bindings to turbojpeg.dll (libjpeg-turbo's TurboJPEG API, bundled in build/native).
/// We only use the encode path — PNG is our output target for decode.  JPEG encoding is optional
/// per the settings "Show JPEG variants" toggle.
/// </summary>
internal static partial class LibJpegTurbo
{
    private const string Lib = "turbojpeg";

    // TJPF_RGBA pixel format (see turbojpeg.h)
    internal const int TJPF_RGBA = 7;

    // TJSAMP chroma subsampling
    internal const int TJSAMP_444 = 0;
    internal const int TJSAMP_422 = 1;
    internal const int TJSAMP_420 = 2;

    [LibraryImport(Lib, EntryPoint = "tjInitCompress")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial IntPtr tjInitCompress();

    [LibraryImport(Lib, EntryPoint = "tjDestroy")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial int tjDestroy(IntPtr handle);

    [LibraryImport(Lib, EntryPoint = "tjCompress2")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial int tjCompress2(
        IntPtr handle,
        IntPtr srcBuf,
        int width,
        int pitch,
        int height,
        int pixelFormat,
        ref IntPtr jpegBuf,
        ref nuint jpegSize,
        int jpegSubsamp,
        int jpegQual,
        int flags);

    [LibraryImport(Lib, EntryPoint = "tjFree")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial void tjFree(IntPtr buffer);
}
