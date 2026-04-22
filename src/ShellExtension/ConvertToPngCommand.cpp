#include "pch.h"
#include "ConvertToPngCommand.h"
#include "FileFilter.h"
#include "SettingsReader.h"
#include "EngineLauncher.h"
#include "Notifier.h"
#include "ClipboardWriter.h"

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

        auto engineExe = EngineLauncher::ResolveEngineExePath();
        if (engineExe.empty())
        {
            Notifier::ConvertError(paths[0], L"Engine executable not found in package.");
            return;
        }

        // Per-batch remembered answer so a multi-select "Yes to All" mental model works:
        //   confirm policy:
        //     first overwrite-denied exit -> MessageBox Yes/No/YesToAll/NoToAll
        //     remember the all-variants for the rest of the batch
        bool alwaysOverwrite = !settings.confirmBeforeOverwrite;  // force=yes baseline when setting says so
        bool alwaysSkip = false;

        size_t succeeded = 0, failed = 0;
        std::wstring lastSuccessPath;
        std::wstring firstError;
        for (const auto& src : paths)
        {
            auto dst = SwapExtension(src, targetExt);

            auto runOnce = [&](const wchar_t* policy) {
                std::wstring args;
                args.append(L"convert ");
                args.append(Quote(src));
                args.push_back(L' ');
                args.append(Quote(dst));
                args.append(L" --overwrite-policy=");
                args.append(policy);
                return EngineLauncher::Run(engineExe, args);
            };

            const wchar_t* policy = alwaysOverwrite ? L"force" : alwaysSkip ? L"skip" : L"confirm";
            auto r = runOnce(policy);

            // Overwrite-denied (exit 4) means the destination existed and policy was confirm/skip.
            // Show a prompt for confirm, then either retry with force or skip.
            if (r.exitCode == 4 && !alwaysOverwrite && !alwaysSkip)
            {
                auto prompt = std::filesystem::path(dst).filename().wstring()
                            + L" already exists.\n\nOverwrite?";
                auto buttons = paths.size() > 1 ? (MB_YESNOCANCEL | MB_ICONQUESTION | MB_SETFOREGROUND | MB_TOPMOST)
                                                : (MB_YESNO       | MB_ICONQUESTION | MB_SETFOREGROUND | MB_TOPMOST);
                auto answer = MessageBoxW(nullptr, prompt.c_str(), L"Right Click PNG", buttons);
                // For multi-select we treat Cancel as NoToAll so the user can bail out of a batch.
                if (answer == IDYES)
                {
                    r = runOnce(L"force");
                }
                else if (answer == IDNO)
                {
                    r.exitCode = 0;  // user chose to skip this one — not an error
                    --failed;        // cancel out the increment below
                    // ... but we never incremented; just continue
                    continue;
                }
                else // IDCANCEL for multi-select
                {
                    alwaysSkip = true;
                    continue;
                }
            }

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

namespace {
    /// <summary>
    /// Shared impl for Copy as PNG / JPEG — they both pipe through the PNG engine output and into
    /// the 3-format clipboard, since ClipboardWriter converts PNG -> the DIB variants internally.
    /// (JPEG-on-clipboard is niche; Teams/Figma/Photoshop all accept PNG-format clipboard data.)
    /// </summary>
    void RunCopyBatch(IShellItemArray* items, bool /*jpeg*/) noexcept
    {
        auto paths = ExplorerCommandBase<CopyAsPngCommand>::GetSelectionPaths(items);
        if (paths.size() != 1) return;   // single-selection guarded in GetState, but defensive
        const auto& src = paths[0];

        auto engineExe = EngineLauncher::ResolveEngineExePath();
        if (engineExe.empty())
        {
            Notifier::ConvertError(src, L"Engine executable not found in package.");
            return;
        }

        std::wstring args;
        args.append(L"copy ");
        args.append(Quote(src));

        auto r = EngineLauncher::Run(engineExe, args, /*captureStdout=*/true);
        if (r.exitCode != 0)
        {
            Notifier::ConvertError(src, r.stderrMessage.empty()
                ? (L"engine exit " + std::to_wstring(r.exitCode))
                : r.stderrMessage);
            return;
        }

        // stdout begins with a 4-byte big-endian length prefix, then the PNG bytes.
        if (r.stdoutBytes.size() < 4)
        {
            Notifier::ConvertError(src, L"Engine produced no output.");
            return;
        }
        const auto& b = r.stdoutBytes;
        size_t declared =
            (static_cast<size_t>(std::to_integer<uint8_t>(b[0])) << 24) |
            (static_cast<size_t>(std::to_integer<uint8_t>(b[1])) << 16) |
            (static_cast<size_t>(std::to_integer<uint8_t>(b[2])) <<  8) |
             static_cast<size_t>(std::to_integer<uint8_t>(b[3]));
        if (declared + 4 > b.size())
        {
            Notifier::ConvertError(src, L"Engine output truncated.");
            return;
        }
        std::vector<std::byte> pngBytes(b.begin() + 4, b.begin() + 4 + declared);

        if (!ClipboardWriter::SetClipboardFromPng(pngBytes))
        {
            Notifier::ConvertError(src, L"Could not write to clipboard.");
            return;
        }
        Notifier::CopyClipboardReady(src);
    }
}

IFACEMETHODIMP CopyAsPngCommand::Invoke(IShellItemArray* items, IBindCtx*) noexcept
{
    RunCopyBatch(items, /*jpeg=*/false);
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

IFACEMETHODIMP CopyAsJpegCommand::Invoke(IShellItemArray* items, IBindCtx*) noexcept
{
    RunCopyBatch(items, /*jpeg=*/true);
    return S_OK;
}

// ============ Right Click PNG Settings... ============

IFACEMETHODIMP OpenSettingsCommand::GetTitle(IShellItemArray*, LPWSTR* title) noexcept
{
    return SHStrDupW(L"Right Click PNG settings…", title);
}

IFACEMETHODIMP OpenSettingsCommand::GetState(IShellItemArray* items, BOOL, EXPCMDSTATE* state) noexcept
{
    // Show whenever any of our other verbs would show — reuse the supported-extension gate.
    auto paths = GetSelectionPaths(items);
    *state = FileFilter::AllSupported(paths) ? ECS_ENABLED : ECS_HIDDEN;
    return S_OK;
}

IFACEMETHODIMP OpenSettingsCommand::Invoke(IShellItemArray*, IBindCtx*) noexcept
{
    // Open settings.json in Notepad.  Avoids the silently-exiting WinUI / WPF packaged-exe
    // activation path that broke Settings.exe; Notepad is in-box on every Windows install
    // and Shell_Execute inherits our package identity when spawned from dllhost.
    PWSTR localAppData = nullptr;
    if (FAILED(SHGetKnownFolderPath(FOLDERID_LocalAppData, 0, nullptr, &localAppData)) || !localAppData)
        return S_OK;
    std::filesystem::path settingsPath = localAppData;
    CoTaskMemFree(localAppData);
    settingsPath = settingsPath / L"Packages" / L"626labs.RTClickPng_3fjztnatnmz7a" / L"LocalState" / L"settings.json";

    // Ensure the file exists with defaults so Notepad opens a valid, parseable document.
    std::error_code ec;
    std::filesystem::create_directories(settingsPath.parent_path(), ec);
    if (!std::filesystem::exists(settingsPath, ec))
    {
        static constexpr const char* kDefaults =
            "{\r\n"
            "  \"schemaVersion\": 1,\r\n"
            "  \"showJpegVariants\": false,\r\n"
            "  \"confirmBeforeOverwrite\": true\r\n"
            "}\r\n";
        HANDLE h = CreateFileW(settingsPath.c_str(), GENERIC_WRITE, 0, nullptr,
                               CREATE_NEW, FILE_ATTRIBUTE_NORMAL, nullptr);
        if (h != INVALID_HANDLE_VALUE)
        {
            DWORD written = 0;
            WriteFile(h, kDefaults, static_cast<DWORD>(strlen(kDefaults)), &written, nullptr);
            CloseHandle(h);
        }
    }

    // Launch Notepad on the file.
    auto cmd = L"\"" + settingsPath.wstring() + L"\"";
    SHELLEXECUTEINFOW info{};
    info.cbSize = sizeof(info);
    info.lpFile = L"notepad.exe";
    info.lpParameters = cmd.c_str();
    info.nShow = SW_SHOWNORMAL;
    info.fMask = SEE_MASK_NOASYNC;
    ShellExecuteExW(&info);
    return S_OK;
}

} // namespace rtclick
