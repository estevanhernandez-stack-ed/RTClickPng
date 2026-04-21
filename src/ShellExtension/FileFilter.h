#pragma once
#include "pch.h"

namespace rtclick {

/// <summary>
/// Static lowercase extension allowlist for the Convert/Copy verbs.
/// Item 5: stub.  Item 6: implements the full matrix per spec.md > Shell Extension > FileFilter.
/// </summary>
class FileFilter
{
public:
    /// <summary>
    /// Returns true if all items in the selection are supported source formats.
    /// Multi-select: every item must be supported (matches "if ALL supported, show menu").
    /// </summary>
    static bool AllSupported(const std::vector<std::wstring>& paths) noexcept;

    /// <summary>
    /// Returns true if this single path's extension is in the supported set.
    /// </summary>
    static bool IsSupportedExtension(std::wstring_view path) noexcept;

    /// <summary>
    /// Returns true if the extension is '.png' — used to hide Convert-to-PNG on already-PNG sources.
    /// </summary>
    static bool IsPngExtension(std::wstring_view path) noexcept;
};

} // namespace rtclick
