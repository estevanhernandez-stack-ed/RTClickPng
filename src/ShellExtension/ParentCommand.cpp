#include "pch.h"
#include "ParentCommand.h"
#include "ConvertToPngCommand.h"
#include "FileFilter.h"
#include "SubCommandOrder.h"
#include "resource.h"

EXTERN_C IMAGE_DOS_HEADER __ImageBase;

namespace rtclick {

namespace {
    /// Fixed list of child commands.  Reset/Clone matter: the shell may enumerate more than once.
    class CommandEnum : public RuntimeClass<RuntimeClassFlags<ClassicCom>, IEnumExplorerCommand>
    {
    public:
        std::vector<ComPtr<IExplorerCommand>> cmds;
        size_t pos = 0;

        IFACEMETHODIMP Next(ULONG celt, IExplorerCommand** out, ULONG* fetched) noexcept override
        {
            if (!out) return E_POINTER;
            ULONG n = 0;
            while (n < celt && pos < cmds.size())
            {
                cmds[pos].CopyTo(&out[n]);
                ++n;
                ++pos;
            }
            if (fetched) *fetched = n;
            return n == celt ? S_OK : S_FALSE;
        }
        IFACEMETHODIMP Skip(ULONG celt) noexcept override
        {
            pos = (std::min)(cmds.size(), pos + celt);
            return pos < cmds.size() ? S_OK : S_FALSE;
        }
        IFACEMETHODIMP Reset() noexcept override
        {
            pos = 0;
            return S_OK;
        }
        IFACEMETHODIMP Clone(IEnumExplorerCommand** out) noexcept override
        {
            if (!out) return E_POINTER;
            *out = nullptr;
            auto e = Microsoft::WRL::Make<CommandEnum>();
            if (!e) return E_OUTOFMEMORY;
            e->cmds = cmds;
            e->pos = pos;
            return e.CopyTo(out);
        }
    };

    ComPtr<IExplorerCommand> MakeSubCommand(SubCommand kind) noexcept
    {
        switch (kind)
        {
        case SubCommand::ConvertToPng:  return Microsoft::WRL::Make<ConvertToPngCommand>();
        case SubCommand::CopyAsPng:     return Microsoft::WRL::Make<CopyAsPngCommand>();
        case SubCommand::ConvertToJpeg: return Microsoft::WRL::Make<ConvertToJpegCommand>();
        case SubCommand::CopyAsJpeg:    return Microsoft::WRL::Make<CopyAsJpegCommand>();
        case SubCommand::Settings:      return Microsoft::WRL::Make<OpenSettingsCommand>();
        }
        return nullptr;
    }
}

IFACEMETHODIMP RightClickPngParentCommand::GetTitle(IShellItemArray*, LPWSTR* title) noexcept
{
    return SHStrDupW(L"Right Click to PNG", title);
}

IFACEMETHODIMP RightClickPngParentCommand::GetIcon(IShellItemArray*, LPWSTR* icon) noexcept
{
    *icon = nullptr;
    wchar_t buf[MAX_PATH];
    auto n = GetModuleFileNameW(reinterpret_cast<HMODULE>(&__ImageBase), buf, MAX_PATH);
    if (n == 0 || n == MAX_PATH) return E_NOTIMPL;   // no icon beats a broken one
    std::wstring location(buf, n);
    location += L",-" + std::to_wstring(IDI_RIGHTCLICKPNG);
    return SHStrDupW(location.c_str(), icon);
}

IFACEMETHODIMP RightClickPngParentCommand::GetState(IShellItemArray* items, BOOL, EXPCMDSTATE* state) noexcept
{
    auto paths = GetSelectionPaths(items);
    *state = FileFilter::ShouldShowParent(paths) ? ECS_ENABLED : ECS_HIDDEN;
    return S_OK;
}

IFACEMETHODIMP RightClickPngParentCommand::GetFlags(EXPCMDFLAGS* flags) noexcept
{
    *flags = ECF_HASSUBCOMMANDS;
    return S_OK;
}

IFACEMETHODIMP RightClickPngParentCommand::EnumSubCommands(IEnumExplorerCommand** enumCmd) noexcept
{
    if (!enumCmd) return E_POINTER;
    *enumCmd = nullptr;
    auto e = Microsoft::WRL::Make<CommandEnum>();
    if (!e) return E_OUTOFMEMORY;
    e->cmds.reserve(kSubCommandOrder.size());
    for (auto kind : kSubCommandOrder)
    {
        auto cmd = MakeSubCommand(kind);
        if (!cmd) return E_OUTOFMEMORY;   // no submenu beats a partial one
        e->cmds.push_back(std::move(cmd));
    }
    return e.CopyTo(enumCmd);
}

} // namespace rtclick
