#include "pch.h"
#include "ConvertToPngCommand.h"
#include "FileFilter.h"
#include "SettingsReader.h"

namespace rtclick {

namespace {
    EXPCMDSTATE StateFromBool(bool visible) noexcept
    {
        return visible ? ECS_ENABLED : ECS_HIDDEN;
    }
}

// ============ Convert to PNG ============

IFACEMETHODIMP ConvertToPngCommand::GetTitle(IShellItemArray*, LPWSTR* title) noexcept
{
    return SHStrDupW(L"Convert to PNG", title);
}

IFACEMETHODIMP ConvertToPngCommand::GetState(IShellItemArray* items, BOOL, EXPCMDSTATE* state) noexcept
{
    auto paths = GetSelectionPaths(items);
    *state = StateFromBool(FileFilter::ShouldShowConvertToPng(paths));
    return S_OK;
}

IFACEMETHODIMP ConvertToPngCommand::Invoke(IShellItemArray*, IBindCtx*) noexcept
{
    // Item 7 wires EngineLauncher + Notifier here.
    return S_OK;
}

// ============ Copy as PNG ============

IFACEMETHODIMP CopyAsPngCommand::GetTitle(IShellItemArray*, LPWSTR* title) noexcept
{
    return SHStrDupW(L"Copy as PNG", title);
}

IFACEMETHODIMP CopyAsPngCommand::GetState(IShellItemArray* items, BOOL, EXPCMDSTATE* state) noexcept
{
    auto paths = GetSelectionPaths(items);
    *state = StateFromBool(FileFilter::ShouldShowCopyAsPng(paths));
    return S_OK;
}

IFACEMETHODIMP CopyAsPngCommand::Invoke(IShellItemArray*, IBindCtx*) noexcept
{
    // Item 8 wires ClipboardWriter here.
    return S_OK;
}

// ============ Convert to JPEG ============

IFACEMETHODIMP ConvertToJpegCommand::GetTitle(IShellItemArray*, LPWSTR* title) noexcept
{
    return SHStrDupW(L"Convert to JPEG", title);
}

IFACEMETHODIMP ConvertToJpegCommand::GetState(IShellItemArray* items, BOOL, EXPCMDSTATE* state) noexcept
{
    auto paths = GetSelectionPaths(items);
    auto settings = SettingsReader::Read();
    *state = StateFromBool(FileFilter::ShouldShowConvertToJpeg(paths, settings.showJpegVariants));
    return S_OK;
}

IFACEMETHODIMP ConvertToJpegCommand::Invoke(IShellItemArray*, IBindCtx*) noexcept
{
    return S_OK;
}

// ============ Copy as JPEG ============

IFACEMETHODIMP CopyAsJpegCommand::GetTitle(IShellItemArray*, LPWSTR* title) noexcept
{
    return SHStrDupW(L"Copy as JPEG", title);
}

IFACEMETHODIMP CopyAsJpegCommand::GetState(IShellItemArray* items, BOOL, EXPCMDSTATE* state) noexcept
{
    auto paths = GetSelectionPaths(items);
    auto settings = SettingsReader::Read();
    *state = StateFromBool(FileFilter::ShouldShowCopyAsJpeg(paths, settings.showJpegVariants));
    return S_OK;
}

IFACEMETHODIMP CopyAsJpegCommand::Invoke(IShellItemArray*, IBindCtx*) noexcept
{
    return S_OK;
}

} // namespace rtclick
