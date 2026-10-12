namespace RTClickPng.Shared;

/// <summary>
/// Cross-process path helpers. The Shell Extension (C++), Engine (C#), and Settings UI (C#/WinUI)
/// all need to agree on where <c>settings.json</c> lives.
/// </summary>
public static class Paths
{
    /// <summary>
    /// The MSIX package family name (PFN), format <c>&lt;Identity.Name&gt;_&lt;PublisherId&gt;</c>.
    /// The publisher id is a 13-char hash Partner Center issues from our registered publisher CN.
    /// Must stay in lockstep with Package.appxmanifest Identity and ShellExtension/SettingsReader.cpp,
    /// or the Settings UI and the shell extension will disagree about where settings.json lives.
    /// </summary>
    public const string PackageFamilyName = "626LabsLLC.RightClicktoPNG_wz1chhb2h2v4a";

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

    /// <summary>
    /// Settings-app failure log, beside <c>settings.json</c> in the package's LocalState folder so
    /// MSIX uninstall removes it.  Written only on failure (exception type + message), never on a
    /// normal launch — PRIVACY.md and the README promise exactly that.
    /// </summary>
    public static string CrashLogPath =>
        Path.Combine(Path.GetDirectoryName(SettingsJsonPath)!, "settings-crash.log");

    /// <summary>
    /// Where 0.1.1.0 and earlier wrote the log: the raw LocalAppData root, outside the package
    /// folder, so uninstall left it behind.  Settings deletes it on startup.
    /// </summary>
    public static string LegacyCrashLogPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RTClickPng.Settings.crash.log");
}
