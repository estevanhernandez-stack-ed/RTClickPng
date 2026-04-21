using System.Runtime.InteropServices;

namespace RTClickPng.Engine.Interop;

/// <summary>
/// P/Invoke bindings to libwebp.dll (bundled in build/native).
/// We use the simple decode path: WebPDecodeRGBA returns a heap-allocated RGBA buffer we must free via WebPFree.
/// </summary>
internal static partial class LibWebp
{
    private const string Lib = "libwebp";

    /// <summary>
    /// Decode WebP bytes to a freshly allocated RGBA buffer.  Caller must free via <see cref="WebPFree"/>.
    /// Returns NULL on failure.
    /// </summary>
    [LibraryImport(Lib, EntryPoint = "WebPDecodeRGBA")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial IntPtr WebPDecodeRGBA(IntPtr data, nuint data_size, out int width, out int height);

    /// <summary>Free a buffer returned by any WebPDecode* function.</summary>
    [LibraryImport(Lib, EntryPoint = "WebPFree")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial void WebPFree(IntPtr ptr);

    /// <summary>Quick header probe — returns non-zero on success.</summary>
    [LibraryImport(Lib, EntryPoint = "WebPGetInfo")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial int WebPGetInfo(IntPtr data, nuint data_size, out int width, out int height);
}
