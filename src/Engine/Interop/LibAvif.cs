using System.Runtime.InteropServices;

namespace RTClickPng.Engine.Interop;

/// <summary>
/// P/Invoke bindings to avif.dll (libavif, bundled in build/native).
/// We use: create decoder, read from memory, parse, next-image, allocate RGBA, yuv→rgb, copy out.
/// </summary>
internal static partial class LibAvif
{
    private const string Lib = "avif";

    // avifResult (enough of it — we only check ==0 for success).
    internal const int AVIF_RESULT_OK = 0;

    // avifRGBFormat
    internal const int AVIF_RGB_FORMAT_RGBA = 1;

    /// <summary>
    /// Partial mirror of <c>avifDecoder</c> up to and including the <c>image</c> pointer field.
    /// libavif does not expose <c>avifDecoderGetImage</c>; callers read <c>decoder-&gt;image</c> directly.
    /// Layout pinned to libavif 1.4.x.  If we ever bump to a version that reorders these prefix fields,
    /// this struct has to move with it.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct avifDecoder_PrefixUpToImage
    {
        public int codecChoice;
        public int maxThreads;
        public int requestedSource;
        public int allowProgressive;
        public int allowIncremental;
        public int ignoreExif;
        public int ignoreXMP;
        public uint imageSizeLimit;
        public uint imageDimensionLimit;
        public uint imageCountLimit;
        public uint strictFlags;
        public IntPtr image;  // avifImage*
    }

    /// <summary>
    /// Mirror of the head of <c>avifImage</c> (width/height/depth), which appear as the first fields.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct avifImage_Head
    {
        public uint width;
        public uint height;
        public uint depth;
        // (rest omitted)
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct avifRGBImage
    {
        public uint width;
        public uint height;
        public uint depth;
        public int format;              // avifRGBFormat
        public int chromaUpsampling;    // avifChromaUpsampling
        public int chromaDownsampling;  // avifChromaDownsampling
        public int avoidLibYUV;         // avifBool — added in libavif 1.1 (must be present or pixels offset is 4 bytes off)
        public int ignoreAlpha;
        public int alphaPremultiplied;
        public int isFloat;
        public int maxThreads;
        public IntPtr pixels;           // uint8_t*
        public uint rowBytes;
    }

    [LibraryImport(Lib, EntryPoint = "avifDecoderCreate")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial IntPtr avifDecoderCreate();

    [LibraryImport(Lib, EntryPoint = "avifDecoderDestroy")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial void avifDecoderDestroy(IntPtr decoder);

    [LibraryImport(Lib, EntryPoint = "avifDecoderSetIOMemory")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial int avifDecoderSetIOMemory(IntPtr decoder, IntPtr data, nuint size);

    [LibraryImport(Lib, EntryPoint = "avifDecoderParse")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial int avifDecoderParse(IntPtr decoder);

    [LibraryImport(Lib, EntryPoint = "avifDecoderNextImage")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial int avifDecoderNextImage(IntPtr decoder);

    [LibraryImport(Lib, EntryPoint = "avifImageCreateEmpty")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial IntPtr avifImageCreateEmpty();

    [LibraryImport(Lib, EntryPoint = "avifImageDestroy")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial void avifImageDestroy(IntPtr image);

    [LibraryImport(Lib, EntryPoint = "avifDecoderReadMemory")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial int avifDecoderReadMemory(IntPtr decoder, IntPtr image, IntPtr data, nuint size);

    [LibraryImport(Lib, EntryPoint = "avifRGBImageSetDefaults")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial void avifRGBImageSetDefaults(ref avifRGBImage rgb, IntPtr image);

    [LibraryImport(Lib, EntryPoint = "avifRGBImageAllocatePixels")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial int avifRGBImageAllocatePixels(ref avifRGBImage rgb);

    [LibraryImport(Lib, EntryPoint = "avifRGBImageFreePixels")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial void avifRGBImageFreePixels(ref avifRGBImage rgb);

    [LibraryImport(Lib, EntryPoint = "avifImageYUVToRGB")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial int avifImageYUVToRGB(IntPtr image, ref avifRGBImage rgb);
}
