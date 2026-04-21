#include "pch.h"
#include "ConvertToPngCommand.h"

namespace rtclick {

IFACEMETHODIMP ConvertToPngCommand::GetTitle(IShellItemArray*, LPWSTR* title) noexcept
{
    static const wchar_t kTitle[] = L"Convert to PNG (placeholder)";
    return SHStrDupW(kTitle, title);
}

IFACEMETHODIMP ConvertToPngCommand::GetState(IShellItemArray*, BOOL, EXPCMDSTATE* state) noexcept
{
    // Item 5: always visible + enabled.  Item 6 adds FileFilter + SettingsReader for real visibility logic.
    *state = ECS_ENABLED;
    return S_OK;
}

IFACEMETHODIMP ConvertToPngCommand::Invoke(IShellItemArray*, IBindCtx*) noexcept
{
    // Item 5: no-op placeholder.  Item 7 wires this to EngineLauncher + Notifier.
    return S_OK;
}

} // namespace rtclick
