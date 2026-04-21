using System.Runtime.InteropServices;

namespace RTClickPng.Engine.Interop;

/// <summary>
/// P/Invoke bindings to heif.dll (libheif, bundled in build/native).
/// Flow: alloc context → read from memory → get primary image handle → decode to RGB interleaved →
/// copy pixel plane out → release everything in reverse order.
/// </summary>
internal static partial class LibHeif
{
    private const string Lib = "heif";

    // heif_colorspace
    internal const int HEIF_COLORSPACE_RGB = 1;

    // heif_chroma — interleaved RGBA keeps everything in one plane for us.
    internal const int HEIF_CHROMA_INTERLEAVED_RGBA = 11;

    // heif_channel
    internal const int HEIF_CHANNEL_INTERLEAVED = 10;

    [StructLayout(LayoutKind.Sequential)]
    internal struct heif_error
    {
        public int code;
        public int subcode;
        public IntPtr message;  // const char*
    }

    [LibraryImport(Lib, EntryPoint = "heif_context_alloc")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial IntPtr heif_context_alloc();

    [LibraryImport(Lib, EntryPoint = "heif_context_free")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial void heif_context_free(IntPtr ctx);

    [LibraryImport(Lib, EntryPoint = "heif_context_read_from_memory_without_copy")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial heif_error heif_context_read_from_memory_without_copy(
        IntPtr ctx, IntPtr mem, nuint size, IntPtr options);

    [LibraryImport(Lib, EntryPoint = "heif_context_get_primary_image_handle")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial heif_error heif_context_get_primary_image_handle(IntPtr ctx, out IntPtr handle);

    [LibraryImport(Lib, EntryPoint = "heif_image_handle_release")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial void heif_image_handle_release(IntPtr handle);

    [LibraryImport(Lib, EntryPoint = "heif_decode_image")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial heif_error heif_decode_image(
        IntPtr handle, out IntPtr outImage, int colorspace, int chroma, IntPtr decodeOptions);

    [LibraryImport(Lib, EntryPoint = "heif_image_get_width")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial int heif_image_get_width(IntPtr img, int channel);

    [LibraryImport(Lib, EntryPoint = "heif_image_get_height")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial int heif_image_get_height(IntPtr img, int channel);

    [LibraryImport(Lib, EntryPoint = "heif_image_get_plane_readonly")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial IntPtr heif_image_get_plane_readonly(IntPtr img, int channel, out int outStride);

    [LibraryImport(Lib, EntryPoint = "heif_image_release")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial void heif_image_release(IntPtr img);
}
