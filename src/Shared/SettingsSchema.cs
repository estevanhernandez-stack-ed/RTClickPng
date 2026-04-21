using System.Text.Json.Serialization;

namespace RTClickPng.Shared;

/// <summary>
/// User settings persisted to <c>%LOCALAPPDATA%\Packages\&lt;PFN&gt;\LocalState\settings.json</c>.
/// Read per-invocation by the Shell Extension and Engine; written by the Settings UI.
/// </summary>
public sealed class SettingsSchema
{
    /// <summary>Schema version. Bump on breaking field changes. v1 is the only version today.</summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Show JPEG Convert/Copy verbs alongside the PNG ones. Default: false (PNG-only out of the box).</summary>
    public bool ShowJpegVariants { get; set; } = false;

    /// <summary>
    /// Prompt before overwriting an existing target file. Default: true (safer for casual users).
    /// When false, the Engine silently overwrites; when true, the Shell Extension shows a confirmation dialog.
    /// </summary>
    public bool ConfirmBeforeOverwrite { get; set; } = true;

    public static SettingsSchema Defaults() => new();
}

/// <summary>
/// AOT-safe source-generated JSON context for <see cref="SettingsSchema"/>.
/// Required because reflection-based serialization is trimmed away by Native AOT.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(SettingsSchema))]
public partial class SettingsJsonContext : JsonSerializerContext;
