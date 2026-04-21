using System.Runtime.InteropServices;

namespace RTClickPng.Engine.Interop;

/// <summary>
/// P/Invoke bindings to spng.dll (libspng, bundled in build/native).
/// We use libspng for both PNG encode (output) and PNG decode (source is already PNG).
/// </summary>
internal static partial class LibSpng
{
    private const string Lib = "spng";

    // spng_ctx_flags (from spng.h enum spng_ctx_flags)
    internal const int SPNG_CTX_IGNORE_ADLER32 = 1;
    internal const int SPNG_CTX_ENCODER        = 2;

    // spng_format values (from spng.h enum spng_format)
    internal const int SPNG_FMT_RGBA8  = 1;
    internal const int SPNG_FMT_RGBA16 = 2;
    internal const int SPNG_FMT_RGB8   = 4;
    internal const int SPNG_FMT_PNG    = 256;  // no conversion, passthrough
    internal const int SPNG_FMT_RAW    = 512;

    // spng_encode_flags (from spng.h enum spng_encode_flags)
    internal const int SPNG_ENCODE_PROGRESSIVE = 1;
    internal const int SPNG_ENCODE_FINALIZE    = 2;

    // spng_option keys (from spng.h enum spng_option)
    internal const int SPNG_KEEP_UNKNOWN_CHUNKS    = 1;
    internal const int SPNG_IMG_COMPRESSION_LEVEL  = 2;
    internal const int SPNG_IMG_WINDOW_BITS        = 3;
    internal const int SPNG_IMG_MEM_LEVEL          = 4;
    internal const int SPNG_IMG_COMPRESSION_STRATEGY = 5;
    internal const int SPNG_TEXT_COMPRESSION_LEVEL = 6;
    internal const int SPNG_TEXT_WINDOW_BITS       = 7;
    internal const int SPNG_TEXT_MEM_LEVEL         = 8;
    internal const int SPNG_TEXT_COMPRESSION_STRATEGY = 9;
    internal const int SPNG_FILTER_CHOICE          = 10;
    internal const int SPNG_CHUNK_COUNT_LIMIT      = 11;
    internal const int SPNG_ENCODE_TO_BUFFER       = 12;

    // spng_color_type
    internal const byte SPNG_COLOR_TYPE_TRUECOLOR_ALPHA = 6;

    [StructLayout(LayoutKind.Sequential)]
    internal struct spng_ihdr
    {
        public uint width;
        public uint height;
        public byte bit_depth;
        public byte color_type;
        public byte compression_method;
        public byte filter_method;
        public byte interlace_method;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct spng_iccp
    {
        // spng stores ICC profile name as a fixed 80-byte char array + compression flag + length + ptr.
        // Marshalled as raw bytes so we keep full control over null handling in the 80-byte field.
        // Layout matches libspng 0.7.x `struct spng_iccp` (see spng.h).
        public byte profile_name_0; public byte profile_name_1; public byte profile_name_2; public byte profile_name_3;
        public byte profile_name_4; public byte profile_name_5; public byte profile_name_6; public byte profile_name_7;
        public byte profile_name_8; public byte profile_name_9; public byte profile_name_10; public byte profile_name_11;
        public byte profile_name_12; public byte profile_name_13; public byte profile_name_14; public byte profile_name_15;
        public byte profile_name_16; public byte profile_name_17; public byte profile_name_18; public byte profile_name_19;
        public byte profile_name_20; public byte profile_name_21; public byte profile_name_22; public byte profile_name_23;
        public byte profile_name_24; public byte profile_name_25; public byte profile_name_26; public byte profile_name_27;
        public byte profile_name_28; public byte profile_name_29; public byte profile_name_30; public byte profile_name_31;
        public byte profile_name_32; public byte profile_name_33; public byte profile_name_34; public byte profile_name_35;
        public byte profile_name_36; public byte profile_name_37; public byte profile_name_38; public byte profile_name_39;
        public byte profile_name_40; public byte profile_name_41; public byte profile_name_42; public byte profile_name_43;
        public byte profile_name_44; public byte profile_name_45; public byte profile_name_46; public byte profile_name_47;
        public byte profile_name_48; public byte profile_name_49; public byte profile_name_50; public byte profile_name_51;
        public byte profile_name_52; public byte profile_name_53; public byte profile_name_54; public byte profile_name_55;
        public byte profile_name_56; public byte profile_name_57; public byte profile_name_58; public byte profile_name_59;
        public byte profile_name_60; public byte profile_name_61; public byte profile_name_62; public byte profile_name_63;
        public byte profile_name_64; public byte profile_name_65; public byte profile_name_66; public byte profile_name_67;
        public byte profile_name_68; public byte profile_name_69; public byte profile_name_70; public byte profile_name_71;
        public byte profile_name_72; public byte profile_name_73; public byte profile_name_74; public byte profile_name_75;
        public byte profile_name_76; public byte profile_name_77; public byte profile_name_78; public byte profile_name_79;
        public nuint profile_len;
        public IntPtr profile;
    }

    [LibraryImport(Lib, EntryPoint = "spng_ctx_new")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial IntPtr spng_ctx_new(int flags);

    [LibraryImport(Lib, EntryPoint = "spng_ctx_free")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial void spng_ctx_free(IntPtr ctx);

    [LibraryImport(Lib, EntryPoint = "spng_set_png_buffer")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial int spng_set_png_buffer(IntPtr ctx, IntPtr buf, nuint size);

    [LibraryImport(Lib, EntryPoint = "spng_get_ihdr")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial int spng_get_ihdr(IntPtr ctx, out spng_ihdr ihdr);

    [LibraryImport(Lib, EntryPoint = "spng_set_ihdr")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial int spng_set_ihdr(IntPtr ctx, ref spng_ihdr ihdr);

    [LibraryImport(Lib, EntryPoint = "spng_set_option")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial int spng_set_option(IntPtr ctx, int option, int value);

    [LibraryImport(Lib, EntryPoint = "spng_decoded_image_size")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial int spng_decoded_image_size(IntPtr ctx, int fmt, out nuint len);

    [LibraryImport(Lib, EntryPoint = "spng_decode_image")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial int spng_decode_image(IntPtr ctx, IntPtr output, nuint len, int fmt, int flags);

    [LibraryImport(Lib, EntryPoint = "spng_encode_image")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial int spng_encode_image(IntPtr ctx, IntPtr img, nuint len, int fmt, int flags);

    [LibraryImport(Lib, EntryPoint = "spng_get_png_buffer")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial IntPtr spng_get_png_buffer(IntPtr ctx, out nuint size, out int error);

    [LibraryImport(Lib, EntryPoint = "spng_set_iccp")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial int spng_set_iccp(IntPtr ctx, ref spng_iccp iccp);

    [LibraryImport(Lib, EntryPoint = "spng_get_iccp")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    internal static partial int spng_get_iccp(IntPtr ctx, out spng_iccp iccp);
}
