#include "pch.h"
#include "SettingsReader.h"

namespace rtclick {

namespace {
    /// <summary>
    /// Hand-rolled JSON scan for <c>"name": true</c> / <c>"name": false</c>.  The settings schema
    /// has exactly two bool fields; dropping in a full JSON parser would multiply binary size and
    /// cost for no upside.  Returns <paramref name="defaultValue"/> on any parse failure.
    /// </summary>
    bool ReadBoolField(const std::string& json, std::string_view name, bool defaultValue) noexcept
    {
        // Search for "name"
        std::string needle = "\"";
        needle += name;
        needle += "\"";
        auto keyPos = json.find(needle);
        if (keyPos == std::string::npos) return defaultValue;

        // Skip key + whitespace + ':' + whitespace
        auto pos = keyPos + needle.size();
        while (pos < json.size() && (json[pos] == ' ' || json[pos] == '\t' || json[pos] == '\n' || json[pos] == '\r')) ++pos;
        if (pos >= json.size() || json[pos] != ':') return defaultValue;
        ++pos;
        while (pos < json.size() && (json[pos] == ' ' || json[pos] == '\t' || json[pos] == '\n' || json[pos] == '\r')) ++pos;

        if (pos + 4 <= json.size() && json.compare(pos, 4, "true") == 0) return true;
        if (pos + 5 <= json.size() && json.compare(pos, 5, "false") == 0) return false;
        return defaultValue;
    }

    /// <summary>
    /// Resolve %LOCALAPPDATA%\Packages\&lt;PFN&gt;\LocalState\settings.json.  Kept in lockstep with
    /// src/Shared/Paths.cs — if the PFN changes in Package.appxmanifest, update both.
    /// </summary>
    std::wstring SettingsJsonPath() noexcept
    {
        PWSTR localAppData = nullptr;
        if (FAILED(SHGetKnownFolderPath(FOLDERID_LocalAppData, 0, nullptr, &localAppData)) || !localAppData)
        {
            return {};
        }
        std::wstring path = localAppData;
        CoTaskMemFree(localAppData);
        path += L"\\Packages\\RTClickPng_626labs0000\\LocalState\\settings.json";
        return path;
    }

    std::optional<std::string> ReadFileUtf8(const std::wstring& path) noexcept
    {
        auto h = CreateFileW(path.c_str(), GENERIC_READ, FILE_SHARE_READ, nullptr, OPEN_EXISTING,
                             FILE_ATTRIBUTE_NORMAL, nullptr);
        if (h == INVALID_HANDLE_VALUE) return std::nullopt;
        LARGE_INTEGER size{};
        if (!GetFileSizeEx(h, &size) || size.QuadPart > 64 * 1024 || size.QuadPart < 0)
        {
            CloseHandle(h);
            return std::nullopt;
        }
        std::string buf(static_cast<size_t>(size.QuadPart), '\0');
        DWORD read = 0;
        if (!ReadFile(h, buf.data(), static_cast<DWORD>(buf.size()), &read, nullptr) || read != buf.size())
        {
            CloseHandle(h);
            return std::nullopt;
        }
        CloseHandle(h);
        return buf;
    }
}

Settings SettingsReader::Read() noexcept
{
    Settings s{};
    auto path = SettingsJsonPath();
    if (path.empty()) return s;
    auto json = ReadFileUtf8(path);
    if (!json) return s;
    s.showJpegVariants       = ReadBoolField(*json, "showJpegVariants",       false);
    s.confirmBeforeOverwrite = ReadBoolField(*json, "confirmBeforeOverwrite", true);
    return s;
}

} // namespace rtclick
