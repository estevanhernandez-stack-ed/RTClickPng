#include "pch.h"
#include "ConvertToPngCommand.h"
#include "FileFilter.h"
#include "SettingsReader.h"
#include "EngineLauncher.h"
#include "Notifier.h"

namespace rtclick {

namespace {
    EXPCMDSTATE StateFromBool(bool visible) noexcept
    {
        return visible ? ECS_ENABLED : ECS_HIDDEN;
    }

    /// <summary>
    /// Compose destination path by replacing the source's extension with the new one.
    /// <paramref name="newExt"/> must include the leading dot.
    /// </summary>
    std::wstring SwapExtension(const std::wstring& source, const wchar_t* newExt) noexcept
    {
        std::filesystem::path p{source};
        p.replace_extension(newExt);
        return p.wstring();
    }

    /// <summary>
    /// Quote an argument for a Windows command line — wraps in "..." and escapes embedded quotes.
    /// Backslash handling per CommandLineToArgvW conventions.
    /// </summary>
    std::wstring Quote(const std::wstring& arg) noexcept
    {
        std::wstring out;
        out.push_back(L'"');
        size_t backslashes = 0;
        for (auto c : arg)
        {
            if (c == L'\\') { ++backslashes; out.push_back(c); continue; }
            if (c == L'"') { out.append(backslashes + 1, L'\\'); }
            backslashes = 0;
            out.push_back(c);
        }
        out.append(backslashes, L'\\');
        out.push_back(L'"');
        return out;
    }

    /// <summary>
    /// Run convert for every selected source, emitting the appropriate toast(s).
    /// <paramref name="targetExt"/> must include the leading dot: L".png" or L".jpg".
    /// </summary>
    void RunConvertBatch(IShellItemArray* items, const wchar_t* targetExt) noexcept
    {
        auto paths = ExplorerCommandBase<ConvertToPngCommand>::GetSelectionPaths(items);
        if (paths.empty()) return;

        auto settings = SettingsReader::Read();
        const auto* policy = settings.confirmBeforeOverwrite ? L"confirm" : L"force";
        // When the shell extension eventually owns the confirm UI (future item),
        // it re-invokes with policy=force after a yes.  For now "confirm" at the engine
        // level just exits 4 on existing target; downstream toast explains.

        auto engineExe = EngineLauncher::ResolveEngineExePath();
        if (engineExe.empty())
        {
            Notifier::ConvertError(paths[0], L"Engine executable not found in package.");
            return;
        }

        size_t succeeded = 0, failed = 0;
        std::wstring lastSuccessPath;
        std::wstring firstError;
        for (const auto& src : paths)
        {
            auto dst = SwapExtension(src, targetExt);
            std::wstring args;
            args.append(L"convert ");
            args.append(Quote(src));
            args.push_back(L' ');
            args.append(Quote(dst));
            args.append(L" --overwrite-policy=");
            args.append(policy);

            auto r = EngineLauncher::Run(engineExe, args);
            if (r.exitCode == 0)
            {
                ++succeeded;
                lastSuccessPath = dst;
            }
            else
            {
                ++failed;
                if (firstError.empty())
                {
                    firstError = r.stderrMessage.empty()
                        ? (L"engine exit " + std::to_wstring(r.exitCode))
                        : r.stderrMessage;
                }
            }
        }

        if (paths.size() == 1)
        {
            if (succeeded == 1) Notifier::ConvertSuccess(lastSuccessPath);
            else                Notifier::ConvertError(paths[0], firstError);
        }
        else
        {
            Notifier::ConvertBatchSummary(succeeded, failed);
        }
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

IFACEMETHODIMP ConvertToPngCommand::Invoke(IShellItemArray* items, IBindCtx*) noexcept
{
    RunConvertBatch(items, L".png");
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

IFACEMETHODIMP ConvertToJpegCommand::Invoke(IShellItemArray* items, IBindCtx*) noexcept
{
    RunConvertBatch(items, L".jpg");
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
    // Item 8 wires ClipboardWriter here.
    return S_OK;
}

} // namespace rtclick
