#pragma once
#include "pch.h"

namespace rtclick {

/// <summary>
/// Static lowercase extension allowlist for the Convert/Copy verbs.
/// Also hosts the visibility helpers used by <see cref="IExplorerCommand::GetState"/>.
/// </summary>
class FileFilter
{
public:
    /// <summary>
    /// Returns true if all items in the selection are supported source formats.
    /// Multi-select: every item must match (matches "if ALL supported, show menu").
    /// Empty selection returns false.
    /// </summary>
    static bool AllSupported(const std::vector<std::wstring>& paths) noexcept;

    /// <summary>
    /// Returns true if this path's extension is in the supported allowlist.
    /// </summary>
    static bool IsSupportedExtension(std::wstring_view path) noexcept;

    /// <summary>
    /// Returns true if the extension is '.png' (case-insensitive).
    /// </summary>
    static bool IsPngExtension(std::wstring_view path) noexcept;

    /// <summary>
    /// Returns true if the extension is '.jpg' or '.jpeg' (case-insensitive).
    /// </summary>
    static bool IsJpegExtension(std::wstring_view path) noexcept;

    // -------------------------------------------------------------------------------------------
    // Visibility matrix per spec.md > Shell Extension > Menu visibility logic
    // -------------------------------------------------------------------------------------------

    /// <summary>
    /// Convert-to-PNG verb visibility.
    ///   - Must have at least one item.
    ///   - All items must be supported.
    ///   - Hidden when every item is already .png (no-op conversion).
    /// </summary>
    static bool ShouldShowConvertToPng(const std::vector<std::wstring>& paths) noexcept;

    /// <summary>
    /// Copy-as-PNG verb visibility.
    ///   - Single selection only.
    ///   - Item must be supported.
    ///   - Visible even on .png sources (workflow win — one-click paste).
    /// </summary>
    static bool ShouldShowCopyAsPng(const std::vector<std::wstring>& paths) noexcept;

    /// <summary>
    /// Convert-to-JPEG verb visibility.  Same rules as Convert-to-PNG but:
    ///   - Only when the user has toggled "Show JPEG variants" on.
    ///   - Also hidden when all items are already .jpg/.jpeg.
    /// </summary>
    static bool ShouldShowConvertToJpeg(const std::vector<std::wstring>& paths, bool jpegVariantsEnabled) noexcept;

    /// <summary>
    /// Copy-as-JPEG verb visibility.  Same rules as Copy-as-PNG but requires the JPEG toggle.
    /// </summary>
    static bool ShouldShowCopyAsJpeg(const std::vector<std::wstring>& paths, bool jpegVariantsEnabled) noexcept;
};

} // namespace rtclick
