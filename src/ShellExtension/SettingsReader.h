#pragma once
#include "pch.h"

namespace rtclick {

/// <summary>
/// Reads <c>settings.json</c> from the MSIX package LocalState folder on each invocation.
/// Returns safe defaults when the file is missing or corrupt — we never fail closed.
/// Item 5: stub.  Item 6: implements real JSON parsing.  Item 9 adds FileSystemWatcher bridge.
/// </summary>
struct Settings
{
    bool showJpegVariants{false};
    bool confirmBeforeOverwrite{true};
};

class SettingsReader
{
public:
    /// <summary>
    /// Synchronously read settings.json.  Always returns a value — defaults on any failure.
    /// </summary>
    static Settings Read() noexcept;
};

} // namespace rtclick
