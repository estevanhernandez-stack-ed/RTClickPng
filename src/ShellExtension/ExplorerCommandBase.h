#pragma once
#include "pch.h"

// Forward declarations to avoid including the full command headers in pch.
namespace rtclick {

/// <summary>
/// Minimal shared helper for IExplorerCommand implementations — wraps GetToolTip, GetIcon
/// (both unused), GetCanonicalName (unused), and GetFlags (returns ECF_DEFAULT).
/// Derived classes override GetTitle, GetState, and Invoke.
/// </summary>
template <typename TDerived>
class ExplorerCommandBase :
    public RuntimeClass<
        RuntimeClassFlags<ClassicCom>,
        IExplorerCommand,
        IObjectWithSite>
{
public:
    // --- IExplorerCommand required: every method that returns HRESULT must implement. ---

    IFACEMETHODIMP GetToolTip(IShellItemArray*, LPWSTR* tooltip) noexcept override
    {
        *tooltip = nullptr;
        return E_NOTIMPL;
    }
    IFACEMETHODIMP GetIcon(IShellItemArray*, LPWSTR* icon) noexcept override
    {
        *icon = nullptr;
        return E_NOTIMPL;
    }
    IFACEMETHODIMP GetCanonicalName(GUID* guid) noexcept override
    {
        *guid = GUID_NULL;
        return S_OK;
    }
    IFACEMETHODIMP GetFlags(EXPCMDFLAGS* flags) noexcept override
    {
        *flags = ECF_DEFAULT;
        return S_OK;
    }
    IFACEMETHODIMP EnumSubCommands(IEnumExplorerCommand** enumCmd) noexcept override
    {
        *enumCmd = nullptr;
        return E_NOTIMPL;
    }

    // --- IObjectWithSite: Explorer uses this to pass a site pointer.  We cache it for later use. ---

    IFACEMETHODIMP SetSite(IUnknown* site) noexcept override
    {
        m_site = site;
        return S_OK;
    }
    IFACEMETHODIMP GetSite(REFIID riid, void** site) noexcept override
    {
        if (!m_site) { *site = nullptr; return E_FAIL; }
        return m_site.CopyTo(riid, site);
    }

    /// <summary>
    /// Extract file system paths from an IShellItemArray selection.
    /// Public so helper free functions in ConvertToPngCommand.cpp can call it without friending each.
    /// </summary>
    static std::vector<std::wstring> GetSelectionPaths(IShellItemArray* items) noexcept
    {
        std::vector<std::wstring> out;
        if (!items) return out;
        DWORD count = 0;
        if (FAILED(items->GetCount(&count))) return out;
        out.reserve(count);
        for (DWORD i = 0; i < count; ++i)
        {
            ComPtr<IShellItem> item;
            if (FAILED(items->GetItemAt(i, &item))) continue;
            PWSTR path = nullptr;
            if (SUCCEEDED(item->GetDisplayName(SIGDN_FILESYSPATH, &path)) && path)
            {
                out.emplace_back(path);
                CoTaskMemFree(path);
            }
        }
        return out;
    }

protected:
    ComPtr<IUnknown> m_site;
};

} // namespace rtclick
