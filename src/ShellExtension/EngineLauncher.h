#pragma once
#include "pch.h"

namespace rtclick {

struct EngineResult
{
    int exitCode;        // ExitCode enum from Engine (0 success, 1 generic, 2 src-not-found, 3 unsupported, 4 overwrite-denied, 5 output-failed, 10 uncaught)
    std::wstring stderrMessage;
    bool timedOut;
};

/// <summary>
/// Spawns RTClickPng.Engine.exe with the given args and blocks for its completion (60s cap).
/// Always uses CREATE_NO_WINDOW so no flash-of-console while Explorer is live.
/// </summary>
class EngineLauncher
{
public:
    /// <summary>
    /// Resolve the engine exe path by taking the DLL's own location and swapping to ..\Engine\RTClickPng.Engine.exe.
    /// Called once per Invoke — cheap enough.
    /// </summary>
    static std::wstring ResolveEngineExePath() noexcept;

    /// <summary>
    /// Run the engine synchronously with the given arguments (already quoted/escaped as needed).
    /// </summary>
    static EngineResult Run(const std::wstring& exePath, const std::wstring& commandLine) noexcept;
};

} // namespace rtclick
