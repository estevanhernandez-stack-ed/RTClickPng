namespace RTClickPng.Engine;

/// <summary>
/// Process exit codes for the Engine CLI. The Shell Extension interprets these to decide
/// toast content (success vs error) and whether to surface overwrite prompts.
/// </summary>
/// <remarks>
/// Codes are stable ABI — callers parse them. Do not renumber.
/// </remarks>
internal enum ExitCode
{
    Success = 0,
    Generic = 1,
    SourceNotFound = 2,
    FormatUnsupported = 3,
    OverwriteDenied = 4,
    OutputFailed = 5,
    UncaughtException = 10,
}
