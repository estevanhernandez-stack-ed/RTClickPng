#pragma once
#include "pch.h"

namespace rtclick {

/// <summary>
/// Fires Windows toast notifications for Convert / Copy outcomes.  Uses the in-box WinRT
/// <c>Windows.UI.Notifications.ToastNotificationManager</c> API (no WinAppSDK dependency) — our
/// MSIX is packaged, which is all that API requires.
/// </summary>
class Notifier
{
public:
    /// <summary>
    /// Fires a success toast for a single converted file.  When <paramref name="folderHintPath"/> is
    /// non-empty it's shown as secondary text so the user can paste it into Run/Explorer; a
    /// full "Show in folder" button requires toast activation, deferred to a later polish pass.
    /// </summary>
    static void ConvertSuccess(const std::wstring& destinationPath) noexcept;

    /// <summary>
    /// Fires a summary toast for a batch convert.
    /// </summary>
    static void ConvertBatchSummary(size_t succeeded, size_t failed) noexcept;

    /// <summary>
    /// Fires an error toast with a short explanation.  Intended for single-file failures where the
    /// user actively clicked the verb.
    /// </summary>
    static void ConvertError(const std::wstring& sourcePath, const std::wstring& message) noexcept;

    /// <summary>
    /// Fires a toast after Copy-as-PNG/JPEG successfully populates the clipboard.
    /// Item 8 calls this.
    /// </summary>
    static void CopyClipboardReady(const std::wstring& sourcePath) noexcept;
};

} // namespace rtclick
