#pragma once
#include "ExplorerCommandBase.h"

namespace rtclick {

// Parent "Right Click to PNG" verb, the only verb registered per file type.  Serves the five
// commands in ConvertToPngCommand.h as its submenu.  ABI: must match Package.appxmanifest; never renumber.
constexpr GUID CLSID_RightClickPngParent =
    { 0x582E2707, 0x2A81, 0x491E, { 0x98, 0x6F, 0x14, 0x52, 0x08, 0xBF, 0x87, 0xD2 } };

class RightClickPngParentCommand : public ExplorerCommandBase<RightClickPngParentCommand>
{
public:
    IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* title) noexcept override;
    IFACEMETHODIMP GetIcon(IShellItemArray*, LPWSTR* icon) noexcept override;
    IFACEMETHODIMP GetState(IShellItemArray*, BOOL okToBeSlow, EXPCMDSTATE* state) noexcept override;
    IFACEMETHODIMP GetFlags(EXPCMDFLAGS* flags) noexcept override;
    IFACEMETHODIMP EnumSubCommands(IEnumExplorerCommand** enumCmd) noexcept override;
    // A parent with subcommands is never invoked by the shell.
    IFACEMETHODIMP Invoke(IShellItemArray*, IBindCtx*) noexcept override { return E_NOTIMPL; }
};

} // namespace rtclick
