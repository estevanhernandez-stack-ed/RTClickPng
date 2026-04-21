#include "pch.h"
#include "EngineLauncher.h"

namespace rtclick {

namespace {
    extern "C" IMAGE_DOS_HEADER __ImageBase;

    /// <summary>
    /// Directory containing ShellExtension.dll.  In the MSIX layout that's
    /// %ProgramFiles%\WindowsApps\&lt;PFN&gt;\ShellExtension\.
    /// </summary>
    std::filesystem::path SelfDllDir() noexcept
    {
        wchar_t buf[MAX_PATH];
        auto n = GetModuleFileNameW(reinterpret_cast<HMODULE>(&__ImageBase), buf, MAX_PATH);
        if (n == 0 || n == MAX_PATH) return {};
        return std::filesystem::path(buf).parent_path();
    }
}

std::wstring EngineLauncher::ResolveEngineExePath() noexcept
{
    // DLL lives at <pkg>\ShellExtension\ShellExtension.dll
    // Engine lives at <pkg>\Engine\RTClickPng.Engine.exe
    auto dllDir = SelfDllDir();
    if (dllDir.empty()) return {};
    auto enginePath = dllDir.parent_path() / L"Engine" / L"RTClickPng.Engine.exe";
    std::error_code ec;
    if (!std::filesystem::exists(enginePath, ec)) return {};
    return enginePath.wstring();
}

EngineResult EngineLauncher::Run(const std::wstring& exePath, const std::wstring& commandLine) noexcept
{
    EngineResult r{};
    r.exitCode = -1;

    if (exePath.empty()) { r.stderrMessage = L"engine exe not found"; return r; }

    // Build the full command line: first arg must be the exe path itself.  CreateProcessW
    // modifies the lpCommandLine buffer, so we copy into a mutable std::wstring first.
    std::wstring fullCmd;
    fullCmd.reserve(exePath.size() + commandLine.size() + 4);
    fullCmd.push_back(L'"');
    fullCmd.append(exePath);
    fullCmd.push_back(L'"');
    if (!commandLine.empty()) { fullCmd.push_back(L' '); fullCmd.append(commandLine); }

    // Pipes for stderr so we can report engine errors up to the toast.
    HANDLE hReadErr = nullptr, hWriteErr = nullptr;
    SECURITY_ATTRIBUTES sa{sizeof(sa), nullptr, TRUE};
    if (!CreatePipe(&hReadErr, &hWriteErr, &sa, 0)) { r.stderrMessage = L"CreatePipe failed"; return r; }
    SetHandleInformation(hReadErr, HANDLE_FLAG_INHERIT, 0);  // keep parent side out of child

    STARTUPINFOW si{};
    si.cb = sizeof(si);
    si.dwFlags = STARTF_USESTDHANDLES;
    si.hStdError = hWriteErr;
    si.hStdOutput = nullptr;   // convert doesn't write anything interesting to stdout
    si.hStdInput = nullptr;

    PROCESS_INFORMATION pi{};
    auto ok = CreateProcessW(
        nullptr, fullCmd.data(),
        nullptr, nullptr,
        TRUE /*inherit handles for the pipe*/,
        CREATE_NO_WINDOW,
        nullptr, nullptr,
        &si, &pi);

    CloseHandle(hWriteErr);  // parent doesn't write to it

    if (!ok)
    {
        CloseHandle(hReadErr);
        auto err = GetLastError();
        r.stderrMessage = L"CreateProcessW failed, err=" + std::to_wstring(err);
        return r;
    }

    // 60 sec timeout per spec.
    auto waitResult = WaitForSingleObject(pi.hProcess, 60'000);
    if (waitResult == WAIT_TIMEOUT)
    {
        TerminateProcess(pi.hProcess, 1);
        WaitForSingleObject(pi.hProcess, 2'000);
        r.timedOut = true;
        r.exitCode = 1;
        r.stderrMessage = L"engine timed out after 60s";
    }
    else
    {
        DWORD code = 0;
        GetExitCodeProcess(pi.hProcess, &code);
        r.exitCode = static_cast<int>(code);
    }

    // Drain stderr (non-blocking since process is dead by now).
    {
        char buf[4096];
        DWORD n = 0;
        std::string accum;
        while (PeekNamedPipe(hReadErr, nullptr, 0, nullptr, &n, nullptr) && n > 0)
        {
            DWORD got = 0;
            if (!ReadFile(hReadErr, buf, static_cast<DWORD>(std::min<size_t>(n, sizeof(buf))), &got, nullptr) || got == 0) break;
            accum.append(buf, got);
        }
        if (!accum.empty())
        {
            auto wideLen = MultiByteToWideChar(CP_UTF8, 0, accum.c_str(), (int)accum.size(), nullptr, 0);
            if (wideLen > 0)
            {
                std::wstring wide(wideLen, L'\0');
                MultiByteToWideChar(CP_UTF8, 0, accum.c_str(), (int)accum.size(), wide.data(), wideLen);
                if (!r.stderrMessage.empty()) r.stderrMessage += L" | ";
                r.stderrMessage += wide;
            }
        }
    }

    CloseHandle(hReadErr);
    CloseHandle(pi.hProcess);
    CloseHandle(pi.hThread);
    return r;
}

} // namespace rtclick
