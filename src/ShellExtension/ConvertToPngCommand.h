#pragma once
#include "ExplorerCommandBase.h"

namespace rtclick {

// {68730B57-152E-4BC9-A158-3A03EE03465A} — placeholder ConvertToPng verb (item 5).
// Must match Package.appxmanifest <com:ComServer>/<com:Class> entry.
constexpr GUID CLSID_ConvertToPngCommand =
    { 0x68730B57, 0x152E, 0x4BC9, { 0xA1, 0x58, 0x3A, 0x03, 0xEE, 0x03, 0x46, 0x5A } };

class ConvertToPngCommand : public ExplorerCommandBase<ConvertToPngCommand>
{
public:
    IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* title) noexcept override;
    IFACEMETHODIMP GetState(IShellItemArray*, BOOL okToBeSlow, EXPCMDSTATE* state) noexcept override;
    IFACEMETHODIMP Invoke(IShellItemArray* items, IBindCtx*) noexcept override;
};

} // namespace rtclick
