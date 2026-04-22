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

    /// <summary>
    /// Drain a handle to EOF into <paramref name="dst"/> (appends).  Non-blocking for the final
    /// read since the child has already exited by the time we call this.
    /// </summary>
    void DrainHandle(HANDLE h, std::string& dst) noexcept
    {
        char buf[4096];
        for (;;)
        {
            DWORD avail = 0;
            if (!PeekNamedPipe(h, nullptr, 0, nullptr, &avail, nullptr) || avail == 0) break;
            DWORD got = 0;
            if (!ReadFile(h, buf, static_cast<DWORD>(std::min<size_t>(avail, sizeof(buf))), &got, nullptr) || got == 0) break;
            dst.append(buf, got);
        }
    }
}

std::wstring EngineLauncher::ResolveEngineExePath() noexcept
{
    auto dllDir = SelfDllDir();
    if (dllDir.empty()) return {};
    auto enginePath = dllDir.parent_path() / L"Engine" / L"RTClickPng.Engine.exe";
    std::error_code ec;
    if (!std::filesystem::exists(enginePath, ec)) return {};
    return enginePath.wstring();
}

EngineResult EngineLauncher::Run(const std::wstring& exePath, const std::wstring& commandLine, bool captureStdout) noexcept
{
    EngineResult r{};
    r.exitCode = -1;

    if (exePath.empty()) { r.stderrMessage = L"engine exe not found"; return r; }

    std::wstring fullCmd;
    fullCmd.reserve(exePath.size() + commandLine.size() + 4);
    fullCmd.push_back(L'"');
    fullCmd.append(exePath);
    fullCmd.push_back(L'"');
    if (!commandLine.empty()) { fullCmd.push_back(L' '); fullCmd.append(commandLine); }

    // Pipes: always capture stderr; stdout only when asked.
    HANDLE hReadErr = nullptr, hWriteErr = nullptr;
    HANDLE hReadOut = nullptr, hWriteOut = nullptr;
    SECURITY_ATTRIBUTES sa{sizeof(sa), nullptr, TRUE};
    if (!CreatePipe(&hReadErr, &hWriteErr, &sa, 0))
    {
        r.stderrMessage = L"CreatePipe(stderr) failed";
        return r;
    }
    SetHandleInformation(hReadErr, HANDLE_FLAG_INHERIT, 0);
    if (captureStdout)
    {
        // 1 MB buffer — a 20-megapixel PNG can exceed 64KB default.
        if (!CreatePipe(&hReadOut, &hWriteOut, &sa, 1024 * 1024))
        {
            CloseHandle(hReadErr); CloseHandle(hWriteErr);
            r.stderrMessage = L"CreatePipe(stdout) failed";
            return r;
        }
        SetHandleInformation(hReadOut, HANDLE_FLAG_INHERIT, 0);
    }

    STARTUPINFOW si{};
    si.cb = sizeof(si);
    si.dwFlags = STARTF_USESTDHANDLES;
    si.hStdError = hWriteErr;
    si.hStdOutput = hWriteOut;  // nullptr when not capturing
    si.hStdInput = nullptr;

    PROCESS_INFORMATION pi{};
    auto ok = CreateProcessW(
        nullptr, fullCmd.data(),
        nullptr, nullptr,
        TRUE,
        CREATE_NO_WINDOW,
        nullptr, nullptr,
        &si, &pi);

    CloseHandle(hWriteErr);
    if (hWriteOut) CloseHandle(hWriteOut);

    if (!ok)
    {
        CloseHandle(hReadErr);
        if (hReadOut) CloseHandle(hReadOut);
        auto err = GetLastError();
        r.stderrMessage = L"CreateProcessW failed, err=" + std::to_wstring(err);
        return r;
    }

    // For stdout capture we must drain the pipe concurrently with the process running —
    // otherwise the engine blocks once the pipe buffer fills.  Simple thread: read until EOF.
    std::string outUtf8;
    std::thread outDrainer;
    if (captureStdout && hReadOut)
    {
        outDrainer = std::thread([&outUtf8, hReadOut]() {
            char buf[4096];
            for (;;)
            {
                DWORD got = 0;
                if (!ReadFile(hReadOut, buf, sizeof(buf), &got, nullptr) || got == 0) break;
                outUtf8.append(buf, got);
            }
        });
    }

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

    // Close the read end to unblock our drainer (ReadFile returns on EOF once the write end is gone).
    if (hReadOut)
    {
        // First let drainer see any remaining bytes + hit EOF naturally.
        // We already closed hWriteOut; ReadFile will return 0 when the pipe is empty.
    }

    if (outDrainer.joinable()) outDrainer.join();
    if (hReadOut) CloseHandle(hReadOut);

    if (captureStdout)
    {
        r.stdoutBytes.resize(outUtf8.size());
        std::memcpy(r.stdoutBytes.data(), outUtf8.data(), outUtf8.size());
    }

    // Drain stderr post-exit.
    {
        std::string accum;
        DrainHandle(hReadErr, accum);
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
