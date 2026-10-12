using RTClickPng.Shared;
using Xunit;

namespace RTClickPng.Engine.Tests;

/// <summary>
/// The Settings crash log must live in the package-scoped LocalState folder beside settings.json,
/// so MSIX uninstall removes it.  PRIVACY.md, README.md, and 626labs.dev/privacy.html promise this.
/// </summary>
public class PathsTests
{
    [Fact]
    public void CrashLog_lives_in_the_same_LocalState_folder_as_settings()
    {
        Assert.Equal(
            Path.GetDirectoryName(Paths.SettingsJsonPath),
            Path.GetDirectoryName(Paths.CrashLogPath));
        Assert.EndsWith(@"\LocalState", Path.GetDirectoryName(Paths.CrashLogPath));
    }

    [Fact]
    public void CrashLog_is_under_the_package_family_folder()
    {
        Assert.Contains(@"\Packages\" + Paths.PackageFamilyName + @"\", Paths.CrashLogPath);
    }

    [Fact]
    public void Legacy_crash_log_is_the_pre_0_1_2_root_location()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        Assert.Equal(Path.Combine(localAppData, "RTClickPng.Settings.crash.log"), Paths.LegacyCrashLogPath);
        Assert.NotEqual(Paths.LegacyCrashLogPath, Paths.CrashLogPath);
    }
}
