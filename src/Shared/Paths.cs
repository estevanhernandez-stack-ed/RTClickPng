namespace RTClickPng.Shared;

/// <summary>
/// Cross-process path helpers. The Shell Extension (C++), Engine (C#), and Settings UI (C#/WinUI)
/// all need to agree on where <c>settings.json</c> lives.
/// </summary>
public static class Paths
{
    /// <summary>
    /// The MSIX package family name (PFN). Updated at packaging time via Directory.Build.props.
    /// Format: <c>&lt;Name&gt;_&lt;PublisherId&gt;</c>. The publisher id is a 13-char hash derived from
    /// the cert subject; for dev builds we use the self-signed cert's computed id.
    /// </summary>
    public const string PackageFamilyName = "RTClickPng_626labs0000";

    /// <summary>
    /// Filename of the settings JSON document written into the package's LocalState folder.
    /// </summary>
    public const string SettingsFileName = "settings.json";

    /// <summary>
    /// Full path to <c>settings.json</c> for the installed MSIX package.
    /// Equivalent to <c>ApplicationData.Current.LocalFolder</c> inside the package, but computable
    /// from an unpackaged process (e.g. Engine when launched from the shell extension).
    /// </summary>
    public static string SettingsJsonPath
    {
        get
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "Packages", PackageFamilyName, "LocalState", SettingsFileName);
        }
    }

    /// <summary>
    /// Temp filename used for atomic settings writes (write-temp + File.Move).
    /// </summary>
    public static string SettingsJsonTempPath => SettingsJsonPath + ".tmp";
}
