#pragma once
#include "pch.h"

namespace rtclick {

/// <summary>
/// Publishes PNG data to the Windows clipboard using three formats for maximum compatibility:
///   - Registered "PNG" (CF_PNG): raw PNG bytes — Figma, Adobe Photoshop, Chrome, modern apps
///   - CF_DIBV5: BGRA bitmap with premultiplied alpha + embedded ICC — Microsoft Teams, Slack,
///     Office apps that want transparency support
///   - CF_DIB: BGR bitmap (alpha flattened over white) — Paint, legacy apps, older Outlook/Word
/// Decodes the PNG via WIC to get the bitmap for the two DIB formats.
/// </summary>
class ClipboardWriter
{
public:
    /// <summary>
    /// Publish the given PNG bytes + optional friendly source path in 3 clipboard formats.
    /// Returns true if at least one format made it to the clipboard.
    /// </summary>
    static bool SetClipboardFromPng(const std::vector<std::byte>& pngBytes) noexcept;
};

} // namespace rtclick
