#include "pch.h"
#include "FileFilter.h"

namespace rtclick {

namespace {
    // Lowercased extensions we accept as source.  Kept in sync with FormatRegistry.cs in the Engine.
    constexpr const wchar_t* kSupported[] = {
        L".webp", L".avif", L".heic", L".heif",
        L".jpg", L".jpeg",
        L".png",
        L".bmp",
        L".tif", L".tiff",
        L".gif",
    };

    std::wstring ExtractExtensionLower(std::wstring_view path) noexcept
    {
        auto dot = path.find_last_of(L'.');
        if (dot == std::wstring_view::npos) return {};
        std::wstring ext(path.substr(dot));
        for (auto& c : ext) c = (wchar_t)::towlower(c);
        return ext;
    }
}

bool FileFilter::IsSupportedExtension(std::wstring_view path) noexcept
{
    auto ext = ExtractExtensionLower(path);
    if (ext.empty()) return false;
    for (auto s : kSupported) { if (ext == s) return true; }
    return false;
}

bool FileFilter::IsPngExtension(std::wstring_view path) noexcept
{
    auto ext = ExtractExtensionLower(path);
    return ext == L".png";
}

bool FileFilter::IsJpegExtension(std::wstring_view path) noexcept
{
    auto ext = ExtractExtensionLower(path);
    return ext == L".jpg" || ext == L".jpeg";
}

bool FileFilter::AllSupported(const std::vector<std::wstring>& paths) noexcept
{
    if (paths.empty()) return false;
    for (const auto& p : paths) { if (!IsSupportedExtension(p)) return false; }
    return true;
}

bool FileFilter::ShouldShowConvertToPng(const std::vector<std::wstring>& paths) noexcept
{
    if (!AllSupported(paths)) return false;
    // Hide when every selected item is already .png — no useful conversion to run.
    bool allPng = true;
    for (const auto& p : paths) { if (!IsPngExtension(p)) { allPng = false; break; } }
    return !allPng;
}

bool FileFilter::ShouldShowCopyAsPng(const std::vector<std::wstring>& paths) noexcept
{
    if (paths.size() != 1) return false;
    return IsSupportedExtension(paths[0]);
}

bool FileFilter::ShouldShowConvertToJpeg(const std::vector<std::wstring>& paths, bool jpegVariantsEnabled) noexcept
{
    if (!jpegVariantsEnabled) return false;
    if (!AllSupported(paths)) return false;
    bool allJpeg = true;
    for (const auto& p : paths) { if (!IsJpegExtension(p)) { allJpeg = false; break; } }
    return !allJpeg;
}

bool FileFilter::ShouldShowCopyAsJpeg(const std::vector<std::wstring>& paths, bool jpegVariantsEnabled) noexcept
{
    if (!jpegVariantsEnabled) return false;
    if (paths.size() != 1) return false;
    return IsSupportedExtension(paths[0]);
}

} // namespace rtclick
