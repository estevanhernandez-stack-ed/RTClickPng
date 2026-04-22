using System.Text.Json;
using RTClickPng.Shared;

namespace RTClickPng.Settings.Services;

/// <summary>
/// Reads and writes <c>settings.json</c> in the packaged app's LocalState folder.
/// Writes are atomic: temp-file-then-File.Move so the Shell Extension never sees a partial write.
/// </summary>
public sealed class SettingsService
{
    public SettingsSchema Read()
    {
        var path = Paths.SettingsJsonPath;
        try
        {
            if (!File.Exists(path)) return SettingsSchema.Defaults();
            var json = File.ReadAllBytes(path);
            var parsed = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.SettingsSchema);
            return parsed ?? SettingsSchema.Defaults();
        }
        catch
        {
            return SettingsSchema.Defaults();
        }
    }

    public void Write(SettingsSchema schema)
    {
        var path = Paths.SettingsJsonPath;
        var tmp = Paths.SettingsJsonTempPath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonSerializer.SerializeToUtf8Bytes(schema, SettingsJsonContext.Default.SettingsSchema);
        File.WriteAllBytes(tmp, json);
        File.Move(tmp, path, overwrite: true);
    }
}
