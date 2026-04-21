using System.Diagnostics;
using Xunit;

namespace RTClickPng.Engine.Tests;

/// <summary>
/// End-to-end overwrite-policy tests that actually launch the Engine exe and check exit codes.
/// </summary>
public class OverwritePolicyTests
{
    private const int ExitSuccess = 0;
    private const int ExitOverwriteDenied = 4;

    private static readonly string EnginePath = Path.Combine(
        AppContext.BaseDirectory,   // test output dir has all native DLLs + the engine alongside
        "RTClickPng.Engine.exe");

    private static readonly string FixturesDir = Path.Combine(AppContext.BaseDirectory, "fixtures");

    private static (int ExitCode, string Stderr) RunEngine(params string[] args)
    {
        if (!File.Exists(EnginePath))
            throw new SkipException($"engine exe not next to test assembly — expected at {EnginePath}. " +
                                    "Run 'dotnet publish src/Engine -c Release -r win-x64' first.");

        var psi = new ProcessStartInfo(EnginePath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi)!;
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode, stderr);
    }

    [Fact]
    public void Convert_SucceedsWhenDestinationDoesNotExist()
    {
        var tmp = Path.Combine(Path.GetTempPath(), $"rtclick-test-{Guid.NewGuid():N}.png");
        try
        {
            var (rc, _) = RunEngine("convert", Path.Combine(FixturesDir, "sample.bmp"), tmp);
            Assert.Equal(ExitSuccess, rc);
            Assert.True(File.Exists(tmp));
        }
        finally { if (File.Exists(tmp)) File.Delete(tmp); }
    }

    [Fact]
    public void Convert_ConfirmPolicy_ExitsFourWhenDestinationExists()
    {
        var tmp = Path.Combine(Path.GetTempPath(), $"rtclick-test-{Guid.NewGuid():N}.png");
        File.WriteAllText(tmp, "placeholder");
        try
        {
            var (rc, stderr) = RunEngine("convert", Path.Combine(FixturesDir, "sample.bmp"), tmp);
            Assert.Equal(ExitOverwriteDenied, rc);
            Assert.Contains("destination exists", stderr, StringComparison.OrdinalIgnoreCase);
        }
        finally { if (File.Exists(tmp)) File.Delete(tmp); }
    }

    [Fact]
    public void Convert_SkipPolicy_ExitsFourWhenDestinationExists()
    {
        var tmp = Path.Combine(Path.GetTempPath(), $"rtclick-test-{Guid.NewGuid():N}.png");
        File.WriteAllText(tmp, "placeholder");
        try
        {
            var (rc, _) = RunEngine("convert", Path.Combine(FixturesDir, "sample.bmp"), tmp, "--overwrite-policy=skip");
            Assert.Equal(ExitOverwriteDenied, rc);
        }
        finally { if (File.Exists(tmp)) File.Delete(tmp); }
    }

    [Fact]
    public void Convert_ForcePolicy_OverwritesSilently()
    {
        var tmp = Path.Combine(Path.GetTempPath(), $"rtclick-test-{Guid.NewGuid():N}.png");
        File.WriteAllText(tmp, "placeholder");
        var originalLen = new FileInfo(tmp).Length;
        try
        {
            var (rc, _) = RunEngine("convert", Path.Combine(FixturesDir, "sample.bmp"), tmp, "--overwrite-policy=force");
            Assert.Equal(ExitSuccess, rc);
            var newLen = new FileInfo(tmp).Length;
            Assert.NotEqual(originalLen, newLen);   // contents actually changed
        }
        finally { if (File.Exists(tmp)) File.Delete(tmp); }
    }
}

/// <summary>
/// xUnit v2 doesn't have first-class skip support; throwing this mid-test surfaces as a failure.
/// Our use case is strictly "engine exe isn't present" which should be a hard fail anyway in CI.
/// </summary>
public sealed class SkipException(string reason) : Exception(reason);
