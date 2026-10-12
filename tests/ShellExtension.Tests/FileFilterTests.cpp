// Minimal-runtime unit tests for rtclick::FileFilter.  Uses plain asserts + a lightweight
// TEST macro so we don't drag gtest or Catch2 into the toolchain.  Exit code = test failures count.

#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <windows.h>
#include <objbase.h>
#include <shobjidl_core.h>
#include <cassert>
#include <cstdio>
#include <string>
#include <string_view>
#include <vector>
#include <optional>
#include <atomic>
#include <filesystem>

#include "SubCommandOrder.h"

// rtclick::FileFilter uses the shell extension's pch symbols — we only pull in what we need.
namespace rtclick {
    class FileFilter {
    public:
        static bool AllSupported(const std::vector<std::wstring>& paths) noexcept;
        static bool IsSupportedExtension(std::wstring_view path) noexcept;
        static bool IsPngExtension(std::wstring_view path) noexcept;
        static bool IsJpegExtension(std::wstring_view path) noexcept;
        static bool ShouldShowConvertToPng(const std::vector<std::wstring>& paths) noexcept;
        static bool ShouldShowCopyAsPng(const std::vector<std::wstring>& paths) noexcept;
        static bool ShouldShowConvertToJpeg(const std::vector<std::wstring>& paths, bool jpeg) noexcept;
        static bool ShouldShowCopyAsJpeg(const std::vector<std::wstring>& paths, bool jpeg) noexcept;
        static bool ShouldShowParent(const std::vector<std::wstring>& paths) noexcept;
    };
}

static int g_fail = 0;
static int g_total = 0;

#define TEST(cond, name) do { \
    ++g_total; \
    if (!(cond)) { ++g_fail; std::fprintf(stderr, "FAIL: %s (line %d)\n", name, __LINE__); } \
    else         { std::fprintf(stdout, "  ok: %s\n", name); } \
} while (0)

// The built ShellExtension.dll, located relative to this exe
// (tests/ShellExtension.Tests/bin/Release -> src/ShellExtension/bin/<Config>).  Empty if not built.
static std::wstring FindShellExtensionDll()
{
    wchar_t buf[MAX_PATH];
    auto n = GetModuleFileNameW(nullptr, buf, MAX_PATH);
    if (n == 0 || n == MAX_PATH) return {};
    auto repo = std::filesystem::path(std::wstring(buf, n)).parent_path() / L"../../../..";
    for (auto config : { L"Release", L"Debug" })
    {
        auto dll = repo / L"src/ShellExtension/bin" / config / L"ShellExtension.dll";
        std::error_code ec;
        if (std::filesystem::exists(dll, ec)) return std::filesystem::weakly_canonical(dll, ec).wstring();
    }
    return {};
}

int wmain()
{
    using rtclick::FileFilter;

    // --- IsSupportedExtension ---
    TEST(FileFilter::IsSupportedExtension(L"foo.webp"),       "webp supported");
    TEST(FileFilter::IsSupportedExtension(L"foo.WebP"),       "WebP case-insensitive");
    TEST(FileFilter::IsSupportedExtension(L"foo.AVIF"),       "AVIF case-insensitive");
    TEST(FileFilter::IsSupportedExtension(L"foo.Heic"),       "Heic case-insensitive");
    TEST(FileFilter::IsSupportedExtension(L"foo.jpg"),        "jpg supported");
    TEST(FileFilter::IsSupportedExtension(L"foo.jpeg"),       "jpeg supported");
    TEST(FileFilter::IsSupportedExtension(L"foo.png"),        "png supported");
    TEST(FileFilter::IsSupportedExtension(L"foo.bmp"),        "bmp supported");
    TEST(FileFilter::IsSupportedExtension(L"foo.tiff"),       "tiff supported");
    TEST(FileFilter::IsSupportedExtension(L"foo.gif"),        "gif supported");
    TEST(!FileFilter::IsSupportedExtension(L"foo.txt"),       "txt not supported");
    TEST(!FileFilter::IsSupportedExtension(L"foo.exe"),       "exe not supported");
    TEST(!FileFilter::IsSupportedExtension(L"foo"),           "no extension not supported");
    TEST(!FileFilter::IsSupportedExtension(L""),              "empty path not supported");

    // --- IsPngExtension / IsJpegExtension ---
    TEST(FileFilter::IsPngExtension(L"x.PNG"),                "IsPng case-insensitive");
    TEST(!FileFilter::IsPngExtension(L"x.jpg"),               "IsPng rejects jpg");
    TEST(FileFilter::IsJpegExtension(L"x.JPG"),               "IsJpeg accepts jpg");
    TEST(FileFilter::IsJpegExtension(L"x.JPEG"),              "IsJpeg accepts jpeg");
    TEST(!FileFilter::IsJpegExtension(L"x.png"),              "IsJpeg rejects png");

    // --- AllSupported ---
    TEST(FileFilter::AllSupported({L"a.webp", L"b.heic"}),                 "mixed supported = true");
    TEST(!FileFilter::AllSupported({L"a.webp", L"b.txt"}),                 "one unsupported = false");
    TEST(!FileFilter::AllSupported({}),                                    "empty selection = false");

    // --- ShouldShowConvertToPng ---
    TEST(FileFilter::ShouldShowConvertToPng({L"a.webp"}),                  "ConvertPng: webp single = show");
    TEST(!FileFilter::ShouldShowConvertToPng({L"a.png"}),                  "ConvertPng: png single = hide");
    TEST(!FileFilter::ShouldShowConvertToPng({L"a.png", L"b.png"}),        "ConvertPng: all-png multi = hide");
    TEST(FileFilter::ShouldShowConvertToPng({L"a.webp", L"b.webp"}),       "ConvertPng: webp multi = show");
    TEST(FileFilter::ShouldShowConvertToPng({L"a.png", L"b.webp"}),        "ConvertPng: mixed with webp = show");
    TEST(!FileFilter::ShouldShowConvertToPng({L"a.webp", L"b.txt"}),       "ConvertPng: mixed with unsupported = hide");
    TEST(!FileFilter::ShouldShowConvertToPng({}),                          "ConvertPng: empty = hide");

    // --- ShouldShowCopyAsPng ---
    TEST(FileFilter::ShouldShowCopyAsPng({L"a.webp"}),                     "CopyPng: webp single = show");
    TEST(FileFilter::ShouldShowCopyAsPng({L"a.png"}),                      "CopyPng: png single = show (workflow win)");
    TEST(!FileFilter::ShouldShowCopyAsPng({L"a.webp", L"b.webp"}),         "CopyPng: multi = hide");
    TEST(!FileFilter::ShouldShowCopyAsPng({L"a.txt"}),                     "CopyPng: unsupported = hide");
    TEST(!FileFilter::ShouldShowCopyAsPng({}),                             "CopyPng: empty = hide");

    // --- ShouldShowConvertToJpeg (jpeg toggle off) ---
    TEST(!FileFilter::ShouldShowConvertToJpeg({L"a.webp"}, false),         "ConvertJpeg toggle-off: hide always");
    TEST(!FileFilter::ShouldShowConvertToJpeg({L"a.png"}, false),          "ConvertJpeg toggle-off: hide always");

    // --- ShouldShowConvertToJpeg (jpeg toggle on) ---
    TEST(FileFilter::ShouldShowConvertToJpeg({L"a.webp"}, true),           "ConvertJpeg: webp single show");
    TEST(FileFilter::ShouldShowConvertToJpeg({L"a.png"}, true),            "ConvertJpeg: png single show");
    TEST(!FileFilter::ShouldShowConvertToJpeg({L"a.jpg"}, true),           "ConvertJpeg: jpg single hide");
    TEST(!FileFilter::ShouldShowConvertToJpeg({L"a.jpg", L"b.jpeg"}, true),"ConvertJpeg: all-jpeg multi hide");
    TEST(FileFilter::ShouldShowConvertToJpeg({L"a.jpg", L"b.png"}, true),  "ConvertJpeg: mixed show");

    // --- ShouldShowCopyAsJpeg ---
    TEST(!FileFilter::ShouldShowCopyAsJpeg({L"a.webp"}, false),            "CopyJpeg toggle-off: hide");
    TEST(FileFilter::ShouldShowCopyAsJpeg({L"a.webp"}, true),              "CopyJpeg toggle-on single: show");
    TEST(FileFilter::ShouldShowCopyAsJpeg({L"a.jpg"}, true),               "CopyJpeg on jpg source: show");
    TEST(!FileFilter::ShouldShowCopyAsJpeg({L"a.webp", L"b.webp"}, true),  "CopyJpeg multi: hide");

    // --- ShouldShowParent ---
    TEST(FileFilter::ShouldShowParent({L"a.webp"}),                        "Parent: webp single = show");
    TEST(FileFilter::ShouldShowParent({L"a.png"}),                         "Parent: png single = show");
    TEST(FileFilter::ShouldShowParent({L"a.webp", L"b.heic"}),             "Parent: supported multi = show");
    TEST(!FileFilter::ShouldShowParent({L"a.webp", L"notes.txt"}),         "Parent: mixed with unsupported = hide");
    TEST(!FileFilter::ShouldShowParent({L"a.txt"}),                        "Parent: unsupported = hide");
    TEST(!FileFilter::ShouldShowParent({}),                                "Parent: empty = hide");

    // --- Parent never opens an empty flyout: visible whenever any child is ---
    {
        const std::vector<std::vector<std::wstring>> selections = {
            {L"a.webp"}, {L"a.png"}, {L"a.jpg"}, {L"a.gif"},
            {L"a.webp", L"b.webp"}, {L"a.png", L"b.png"}, {L"a.jpg", L"b.jpeg"},
            {L"a.webp", L"b.txt"}, {L"a.txt"}, {},
        };
        for (const auto& sel : selections)
        {
            for (bool jpeg : {false, true})
            {
                bool anyChild = FileFilter::ShouldShowConvertToPng(sel)
                             || FileFilter::ShouldShowCopyAsPng(sel)
                             || FileFilter::ShouldShowConvertToJpeg(sel, jpeg)
                             || FileFilter::ShouldShowCopyAsJpeg(sel, jpeg);
                if (anyChild)
                    TEST(FileFilter::ShouldShowParent(sel), "Parent visible whenever a child is");
            }
        }
    }

    // --- Submenu order (spec: flat, Settings last, no separator) ---
    {
        using rtclick::SubCommand;
        using rtclick::kSubCommandOrder;
        TEST(kSubCommandOrder.size() == 5,                                 "Order: five subcommands");
        TEST(kSubCommandOrder[0] == SubCommand::ConvertToPng,              "Order: 1 Convert to PNG");
        TEST(kSubCommandOrder[1] == SubCommand::CopyAsPng,                 "Order: 2 Copy as PNG");
        TEST(kSubCommandOrder[2] == SubCommand::ConvertToJpeg,             "Order: 3 Convert to JPEG");
        TEST(kSubCommandOrder[3] == SubCommand::CopyAsJpeg,                "Order: 4 Copy as JPEG");
        TEST(kSubCommandOrder[4] == SubCommand::Settings,                  "Order: 5 Settings last");
    }

    // --- DLL lifetime: DllCanUnloadNow must refuse while any COM object it served is alive ---
    // Loads the real built DLL.  dllhost calls CoFreeUnusedLibraries; unloading under a live
    // command or enumerator crashes the surrogate and the menu entry vanishes.
    {
        auto dll = FindShellExtensionDll();
        if (dll.empty())
        {
            std::printf("  SKIP: ShellExtension.dll not built; DLL lifetime tests not run\n");
        }
        else
        {
            // Parent verb, {582E2707-2A81-491E-986F-145208BF87D2}.
            constexpr GUID kParentClsid =
                { 0x582E2707, 0x2A81, 0x491E, { 0x98, 0x6F, 0x14, 0x52, 0x08, 0xBF, 0x87, 0xD2 } };
            using GetClassObjectFn = HRESULT (WINAPI*)(REFCLSID, REFIID, void**);
            using CanUnloadNowFn = HRESULT (WINAPI*)();

            HRESULT coInit = CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED);
            HMODULE dllModule = LoadLibraryW(dll.c_str());
            TEST(dllModule != nullptr, "Lifetime: ShellExtension.dll loads");
            if (dllModule)
            {
                auto getClassObject = reinterpret_cast<GetClassObjectFn>(
                    reinterpret_cast<void*>(GetProcAddress(dllModule, "DllGetClassObject")));
                auto canUnloadNow = reinterpret_cast<CanUnloadNowFn>(
                    reinterpret_cast<void*>(GetProcAddress(dllModule, "DllCanUnloadNow")));

                IClassFactory* factory = nullptr;
                IExplorerCommand* parent = nullptr;
                if (getClassObject && canUnloadNow
                    && SUCCEEDED(getClassObject(kParentClsid, IID_IClassFactory, reinterpret_cast<void**>(&factory))))
                {
                    factory->CreateInstance(nullptr, IID_IExplorerCommand, reinterpret_cast<void**>(&parent));
                    factory->Release();
                }
                TEST(parent != nullptr, "Lifetime: parent command instantiates");
                if (parent)
                {
                    TEST(canUnloadNow() == S_FALSE, "Lifetime: refuses unload while the parent command is alive");

                    IEnumExplorerCommand* subcommands = nullptr;
                    parent->EnumSubCommands(&subcommands);
                    parent->Release();
                    TEST(subcommands != nullptr, "Lifetime: parent serves a subcommand enumerator");
                    if (subcommands)
                    {
                        TEST(canUnloadNow() == S_FALSE, "Lifetime: refuses unload while the enumerator is alive");
                        subcommands->Release();
                    }
                    TEST(canUnloadNow() == S_OK, "Lifetime: allows unload once every object is released");
                }
                FreeLibrary(dllModule);
            }
            if (SUCCEEDED(coInit)) CoUninitialize();
        }
    }

    std::printf("\n%d / %d passed\n", g_total - g_fail, g_total);
    return g_fail;
}
