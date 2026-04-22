#pragma once
#include "ExplorerCommandBase.h"

namespace rtclick {

// CLSIDs — must match Package.appxmanifest <com:Class Id="..."> entries exactly.
// These are ABI; never renumber.

// ConvertToPng  -> decode source, write .png next to source (or .jpg if the dst extension is set via the verb)
constexpr GUID CLSID_ConvertToPngCommand =
    { 0x68730B57, 0x152E, 0x4BC9, { 0xA1, 0x58, 0x3A, 0x03, 0xEE, 0x03, 0x46, 0x5A } };

// CopyAsPng -> decode source, push to clipboard in 3 formats (item 8 wires the clipboard writer)
constexpr GUID CLSID_CopyAsPngCommand =
    { 0xD1407350, 0x4611, 0x4621, { 0x80, 0x2E, 0x47, 0x5C, 0x82, 0x00, 0x68, 0x58 } };

// ConvertToJpeg -> .jpg output, only visible when "Show JPEG variants" is on
constexpr GUID CLSID_ConvertToJpegCommand =
    { 0xDC177957, 0x7F24, 0x486D, { 0xB4, 0xCB, 0xD6, 0x9F, 0xA4, 0x58, 0x0C, 0x8F } };

// CopyAsJpeg -> decode source, push JPEG bytes to clipboard
constexpr GUID CLSID_CopyAsJpegCommand =
    { 0x7C7F2F20, 0x9F1E, 0x4F0F, { 0x9F, 0xA8, 0xD4, 0x04, 0x36, 0x9C, 0x2C, 0x07 } };

// OpenSettings -> launch Settings.exe via CreateProcessW (dllhost child, inherits package identity).
// Works around broken Start-menu activation of our WPF Settings app on this packaging config.
constexpr GUID CLSID_OpenSettingsCommand =
    { 0xAB12D5C0, 0x3E4F, 0x4B1A, { 0x9E, 0x88, 0x5C, 0x3D, 0x7F, 0x91, 0xAA, 0x01 } };

// Each verb is a thin subclass that differs only in title + visibility predicate + output mode.

class ConvertToPngCommand : public ExplorerCommandBase<ConvertToPngCommand>
{
public:
    IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* title) noexcept override;
    IFACEMETHODIMP GetState(IShellItemArray*, BOOL okToBeSlow, EXPCMDSTATE* state) noexcept override;
    IFACEMETHODIMP Invoke(IShellItemArray* items, IBindCtx*) noexcept override;
};

class CopyAsPngCommand : public ExplorerCommandBase<CopyAsPngCommand>
{
public:
    IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* title) noexcept override;
    IFACEMETHODIMP GetState(IShellItemArray*, BOOL okToBeSlow, EXPCMDSTATE* state) noexcept override;
    IFACEMETHODIMP Invoke(IShellItemArray* items, IBindCtx*) noexcept override;
};

class ConvertToJpegCommand : public ExplorerCommandBase<ConvertToJpegCommand>
{
public:
    IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* title) noexcept override;
    IFACEMETHODIMP GetState(IShellItemArray*, BOOL okToBeSlow, EXPCMDSTATE* state) noexcept override;
    IFACEMETHODIMP Invoke(IShellItemArray* items, IBindCtx*) noexcept override;
};

class CopyAsJpegCommand : public ExplorerCommandBase<CopyAsJpegCommand>
{
public:
    IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* title) noexcept override;
    IFACEMETHODIMP GetState(IShellItemArray*, BOOL okToBeSlow, EXPCMDSTATE* state) noexcept override;
    IFACEMETHODIMP Invoke(IShellItemArray* items, IBindCtx*) noexcept override;
};

class OpenSettingsCommand : public ExplorerCommandBase<OpenSettingsCommand>
{
public:
    IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* title) noexcept override;
    IFACEMETHODIMP GetState(IShellItemArray*, BOOL okToBeSlow, EXPCMDSTATE* state) noexcept override;
    IFACEMETHODIMP Invoke(IShellItemArray* items, IBindCtx*) noexcept override;
};

} // namespace rtclick
