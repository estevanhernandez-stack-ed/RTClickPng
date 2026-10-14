# Classic Menu Parent Verb Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the five top-level context-menu verbs with one icon'd "Right Click to PNG" parent verb whose submenu serves the existing commands, in both the modern and the classic Windows 11 menu.

**Architecture:** A new `RightClickPngParentCommand` (`IExplorerCommand`, `ECF_HASSUBCOMMANDS`) is the only verb registered per file type. Its `EnumSubCommands` builds the five existing command objects in place with `Make<>`, in an order defined by a COM-free `SubCommandOrder.h` that the C++ test runner pins. The icon is a multi-size `.ico` rendered by the existing brand script and embedded in `ShellExtension.dll` as resource 101.

**Tech Stack:** C++20 / WRL (`ShellExtension.vcxproj`, v143, `/W4 /WX`), MSIX `desktop4` manifest, PowerShell 7 + inline C# System.Drawing (icon renderer), hand-rolled C++ `TEST` runner.

**Spec:** `docs/superpowers/specs/2026-10-11-classic-menu-parent-design.md`

## Global Constraints

- Parent CLSID: `582E2707-2A81-491E-986F-145208BF87D2`. It's permanent ABI; never renumber it.
- Existing CLSIDs `68730B57-…`, `D1407350-…`, `DC177957-…`, `7C7F2F20-…`, `AB12D5C0-…` stay registered as `com:Class` entries. Never remove or renumber them.
- Parent title: `Right Click to PNG`. Settings title: `Settings…` (U+2026 ellipsis, same character as today).
- Submenu order: Convert to PNG, Copy as PNG, Convert to JPEG, Copy as JPEG, Settings. No separator.
- Icon resource id: `101`. Frames 16, 20, 24, 32, 40 and 48 px, rendered with the existing `DrawGlyph`.
- All shell-extension methods are `noexcept` and return HRESULTs. Warnings are errors.
- Package `Identity.Version` stays `0.1.1.0` in commits. The 0.1.2.0 bump is out of scope; Task 5 bumps it only temporarily, uncommitted, for the upgrade test.
- Building the package needs `vswhere.exe` on PATH: prepend `${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer`.
- MSBuild: `C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\amd64\MSBuild.exe` (written `$msbuild` below).

## Review Focus

1. **A selection where every child hides but the parent shows** (e.g. two `.png` files with the JPEG variants off: Convert hides because all are PNG, Copy hides because it's multi-select). The user expects a flyout that is never empty. Settings uses the parent's gate, so the flyout always has at least one item. Task 1 pins "the parent is visible whenever any child is" and "Settings is visible whenever the parent is".
2. **A selection mixing supported and unsupported files** (`a.webp` + `notes.txt`). The user expects no Right Click PNG entry at all, as today. Task 1 pins `ShouldShowParent` false for it.
3. **Explorer asking for the submenu more than once, or cloning the enumerator** (the classic menu enumerates on open, and again on re-open). The user expects the same items each time. Task 3 implements `Reset` and `Clone`. They're verified manually in Task 5 by opening the submenu twice.
4. **High-DPI scaling (150%/200%) and light/dark theme.** The user expects a crisp, visible icon. Task 2 ships 24, 32, 40 and 48 px frames, and Task 5 checks both themes.
5. **An in-place update while Explorer holds the old DLL.** The user expects the Store update to apply, or a clear instruction if it doesn't. Task 5 records the actual behavior for the README note.

---

### Task 1: Parent visibility predicate and submenu order (pure, tested)

**Files:**
- Create: `src/ShellExtension/SubCommandOrder.h`
- Modify: `src/ShellExtension/FileFilter.h` (declare `ShouldShowParent`)
- Modify: `src/ShellExtension/FileFilter.cpp` (define it)
- Modify: `src/ShellExtension/ShellExtension.vcxproj` (`ClInclude` for the new header)
- Test: `tests/ShellExtension.Tests/FileFilterTests.cpp`

**Interfaces:**
- Produces: `bool rtclick::FileFilter::ShouldShowParent(const std::vector<std::wstring>& paths) noexcept`
- Produces: `enum class rtclick::SubCommand { ConvertToPng, CopyAsPng, ConvertToJpeg, CopyAsJpeg, Settings };` and `inline constexpr std::array<rtclick::SubCommand, 5> rtclick::kSubCommandOrder`

- [ ] **Step 1: Write the failing tests**

In `tests/ShellExtension.Tests/FileFilterTests.cpp`, this file **redeclares** `FileFilter` locally (lines 17-29) instead of including `FileFilter.h`. Add the new method to that local redeclaration, after `ShouldShowCopyAsJpeg`:

```cpp
        static bool ShouldShowParent(const std::vector<std::wstring>& paths) noexcept;
```

Add the include after `#include <filesystem>`:

```cpp
#include "SubCommandOrder.h"
```

Add these blocks immediately before `std::printf("\n%d / %d passed\n", ...)`:

```cpp
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
```

- [ ] **Step 2: Run the tests to verify they fail**

```powershell
& $msbuild tests/ShellExtension.Tests/ShellExtension.Tests.vcxproj /p:Configuration=Release /p:Platform=x64 /v:minimal
```

Expected: compile FAILS with `cannot open include file 'SubCommandOrder.h'` (and an unresolved `ShouldShowParent` once that header exists).

- [ ] **Step 3: Implement**

Create `src/ShellExtension/SubCommandOrder.h`:

```cpp
#pragma once
#include <array>

// The parent verb's submenu, in display order.  No COM or Windows dependencies, so the C++ test
// runner can pin the order without building any command objects.  Spec: flat list, Settings last.
namespace rtclick {

enum class SubCommand
{
    ConvertToPng,
    CopyAsPng,
    ConvertToJpeg,
    CopyAsJpeg,
    Settings,
};

inline constexpr std::array<SubCommand, 5> kSubCommandOrder = {
    SubCommand::ConvertToPng,
    SubCommand::CopyAsPng,
    SubCommand::ConvertToJpeg,
    SubCommand::CopyAsJpeg,
    SubCommand::Settings,
};

} // namespace rtclick
```

In `src/ShellExtension/FileFilter.h`, add next to the other `ShouldShow*` declarations:

```cpp
    /// Parent "Right Click to PNG" verb (and its Settings child).  Must be true whenever any
    /// child verb is visible, so the flyout is never empty.  FileFilterTests pins that invariant.
    static bool ShouldShowParent(const std::vector<std::wstring>& paths) noexcept;
```

In `src/ShellExtension/FileFilter.cpp`, add before the closing `} // namespace rtclick`:

```cpp
bool FileFilter::ShouldShowParent(const std::vector<std::wstring>& paths) noexcept
{
    return AllSupported(paths);
}
```

In `src/ShellExtension/ShellExtension.vcxproj`, add to the `ClInclude` item group after `<ClInclude Include="ClipboardWriter.h" />`:

```xml
    <ClInclude Include="SubCommandOrder.h" />
```

- [ ] **Step 4: Run the tests to verify they pass**

```powershell
& $msbuild tests/ShellExtension.Tests/ShellExtension.Tests.vcxproj /p:Configuration=Release /p:Platform=x64 /v:minimal
./tests/ShellExtension.Tests/bin/Release/ShellExtensionTests.exe; "exit $LASTEXITCODE"
```

Expected: every line `ok:`, the summary `N / N passed`, and `exit 0`.

- [ ] **Step 5: Commit**

```bash
git add src/ShellExtension/SubCommandOrder.h src/ShellExtension/FileFilter.h src/ShellExtension/FileFilter.cpp src/ShellExtension/ShellExtension.vcxproj tests/ShellExtension.Tests/FileFilterTests.cpp
git commit -m "feat(shell): parent visibility predicate and submenu order (#2)"
```

---

### Task 2: Brand icon embedded in ShellExtension.dll

**Files:**
- Modify: `src/Package/Assets/render-brand-icons.ps1` (add `RenderIco`, emit the `.ico`)
- Create (generated, committed): `src/ShellExtension/RightClickPng.ico`
- Create: `src/ShellExtension/resource.h`
- Create: `src/ShellExtension/ShellExtension.rc`
- Modify: `src/ShellExtension/ShellExtension.vcxproj`

**Interfaces:**
- Produces: `#define IDI_RIGHTCLICKPNG 101` in `src/ShellExtension/resource.h`; an `RT_GROUP_ICON` resource 101 inside `ShellExtension.dll`.

- [ ] **Step 1: Write the failing check**

Save as `$env:TEMP\check-icon.ps1`. It's a throwaway verification script, not committed:

```powershell
param([string]$Dll = 'src/ShellExtension/bin/Release/ShellExtension.dll')
Add-Type @'
using System; using System.Runtime.InteropServices;
public static class ResProbe {
  [DllImport("kernel32", SetLastError=true, CharSet=CharSet.Unicode)]
  public static extern IntPtr LoadLibraryExW(string f, IntPtr h, uint flags);
  [DllImport("kernel32", SetLastError=true)]
  public static extern IntPtr FindResourceW(IntPtr m, IntPtr name, IntPtr type);
}
'@
$m = [ResProbe]::LoadLibraryExW((Resolve-Path $Dll).Path, [IntPtr]::Zero, 0x2)  # LOAD_LIBRARY_AS_DATAFILE
$found = [ResProbe]::FindResourceW($m, [IntPtr]101, [IntPtr]14) -ne [IntPtr]::Zero  # RT_GROUP_ICON
"icon resource 101 present: $found"; if (-not $found) { exit 1 }
```

- [ ] **Step 2: Run it to verify it fails**

```powershell
& $msbuild src/ShellExtension/ShellExtension.vcxproj /p:Configuration=Release /p:Platform=x64 /v:minimal
pwsh -File $env:TEMP\check-icon.ps1
```

Expected: `icon resource 101 present: False`, exit 1.

- [ ] **Step 3: Implement**

In `render-brand-icons.ps1`, inside `public static class RtClickIcon`, add these two members after `RenderSquare`:

```csharp
    static byte[] RenderSquarePng(int size)
    {
        using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.Clear(Bg);
        DrawGlow(g, size, size);
        DrawGlyph(g, size, size);
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    /// <summary>
    /// Multi-size .ico with PNG-compressed frames (supported by the shell on Win11).
    /// Layout: ICONDIR (6 bytes) + one ICONDIRENTRY (16 bytes) per frame + PNG payloads.
    /// </summary>
    public static void RenderIco(int[] sizes, string path)
    {
        var frames = new byte[sizes.Length][];
        for (int i = 0; i < sizes.Length; i++) frames[i] = RenderSquarePng(sizes[i]);

        using var fs = File.Create(path);
        using var w = new BinaryWriter(fs);
        w.Write((ushort)0);              // reserved
        w.Write((ushort)1);              // type: icon
        w.Write((ushort)sizes.Length);   // frame count
        int offset = 6 + 16 * sizes.Length;
        for (int i = 0; i < sizes.Length; i++)
        {
            w.Write((byte)(sizes[i] >= 256 ? 0 : sizes[i]));  // width
            w.Write((byte)(sizes[i] >= 256 ? 0 : sizes[i]));  // height
            w.Write((byte)0);            // palette colors
            w.Write((byte)0);            // reserved
            w.Write((ushort)1);          // color planes
            w.Write((ushort)32);         // bits per pixel
            w.Write(frames[i].Length);   // bytes in resource
            w.Write(offset);             // image offset
            offset += frames[i].Length;
        }
        foreach (var f in frames) w.Write(f);
    }
```

At the end of the script, before `Write-Host ""`, add:

```powershell
# Context-menu icon for the shell extension's parent verb, embedded in ShellExtension.dll (rc id 101).
$ico = Join-Path $assets '..\..\ShellExtension\RightClickPng.ico'
if ((Test-Path $ico) -and -not $Force) {
    Write-Host "    skip: RightClickPng.ico"
} else {
    [RtClickIcon]::RenderIco([int[]](16, 20, 24, 32, 40, 48), $ico)
    Write-Host "    created: RightClickPng.ico (16-48 px, $([int]((Get-Item $ico).Length/1024)) KB)"
}
```

Generate it. Existing PNGs are skipped without `-Force`, so only the `.ico` is new:

```powershell
pwsh -File src/Package/Assets/render-brand-icons.ps1
git status --short   # expect only: ?? src/ShellExtension/RightClickPng.ico  and  M render-brand-icons.ps1
```

Create `src/ShellExtension/resource.h`:

```cpp
#pragma once

// Parent verb icon.  GetIcon returns "<ShellExtension.dll>,-IDI_RIGHTCLICKPNG".
#define IDI_RIGHTCLICKPNG 101
```

Create `src/ShellExtension/ShellExtension.rc`:

```c
#include "resource.h"

IDI_RIGHTCLICKPNG ICON "RightClickPng.ico"
```

In `ShellExtension.vcxproj`: add `<ClInclude Include="resource.h" />` to the `ClInclude` group, and add a new item group after the `ClCompile` group:

```xml
  <ItemGroup>
    <ResourceCompile Include="ShellExtension.rc" />
  </ItemGroup>
```

- [ ] **Step 4: Run the check to verify it passes**

```powershell
& $msbuild src/ShellExtension/ShellExtension.vcxproj /p:Configuration=Release /p:Platform=x64 /v:minimal
pwsh -File $env:TEMP\check-icon.ps1
```

Expected: build succeeds with 0 warnings, then `icon resource 101 present: True`.

- [ ] **Step 5: Commit**

```bash
git add src/Package/Assets/render-brand-icons.ps1 src/ShellExtension/RightClickPng.ico src/ShellExtension/resource.h src/ShellExtension/ShellExtension.rc src/ShellExtension/ShellExtension.vcxproj
git commit -m "feat(shell): embed brand context-menu icon as resource 101 (#2)"
```

---

### Task 3: Parent command, registration, and the Settings rename

**Files:**
- Create: `src/ShellExtension/ParentCommand.h`
- Create: `src/ShellExtension/ParentCommand.cpp`
- Modify: `src/ShellExtension/dllmain.cpp` (factory branch)
- Modify: `src/ShellExtension/ConvertToPngCommand.cpp` (`OpenSettingsCommand` title and gate)
- Modify: `src/ShellExtension/ShellExtension.vcxproj`
- Modify: `src/Package/Package.appxmanifest`

**Interfaces:**
- Consumes: `FileFilter::ShouldShowParent`, `rtclick::SubCommand`, `rtclick::kSubCommandOrder` (Task 1); `IDI_RIGHTCLICKPNG` (Task 2); the five existing command classes from `ConvertToPngCommand.h`.
- Produces: `rtclick::CLSID_RightClickPngParent`, `class rtclick::RightClickPngParentCommand`.

- [ ] **Step 1: Write the failing check**

The parent's COM wiring can only be verified against a registered package, so the gate for this task is a build plus a manifest check. Save as `$env:TEMP\check-manifest.ps1`:

```powershell
$m = Get-Content src/Package/Package.appxmanifest -Raw
$parentVerbs = ([regex]::Matches($m, 'Verb Id="RightClickPng" Clsid="582E2707-2A81-491E-986F-145208BF87D2"')).Count
$oldVerbs    = ([regex]::Matches($m, 'Verb Id="(ConvertToPng|CopyAsPng|ConvertToJpeg|CopyAsJpeg|Settings)"')).Count
$classes     = ([regex]::Matches($m, '<com:Class Id=')).Count
"parent verbs: $parentVerbs (want 11)  old verbs: $oldVerbs (want 0)  com classes: $classes (want 6)"
if ($parentVerbs -ne 11 -or $oldVerbs -ne 0 -or $classes -ne 6) { exit 1 }
```

- [ ] **Step 2: Run it to verify it fails**

```powershell
pwsh -File $env:TEMP\check-manifest.ps1
```

Expected: `parent verbs: 0 (want 11)  old verbs: 55 (want 0)  com classes: 5 (want 6)`, exit 1.

- [ ] **Step 3: Implement**

Create `src/ShellExtension/ParentCommand.h`:

```cpp
#pragma once
#include "ExplorerCommandBase.h"

namespace rtclick {

// Parent "Right Click to PNG" verb, the only verb registered per file type.  Serves the five
// commands in ConvertToPngCommand.h as its submenu.  ABI: must match Package.appxmanifest; never renumber.
constexpr GUID CLSID_RightClickPngParent =
    { 0x582E2707, 0x2A81, 0x491E, { 0x98, 0x6F, 0x14, 0x52, 0x08, 0xBF, 0x87, 0xD2 } };

class RightClickPngParentCommand : public ExplorerCommandBase<RightClickPngParentCommand>
{
public:
    IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* title) noexcept override;
    IFACEMETHODIMP GetIcon(IShellItemArray*, LPWSTR* icon) noexcept override;
    IFACEMETHODIMP GetState(IShellItemArray*, BOOL okToBeSlow, EXPCMDSTATE* state) noexcept override;
    IFACEMETHODIMP GetFlags(EXPCMDFLAGS* flags) noexcept override;
    IFACEMETHODIMP EnumSubCommands(IEnumExplorerCommand** enumCmd) noexcept override;
    // A parent with subcommands is never invoked by the shell.
    IFACEMETHODIMP Invoke(IShellItemArray*, IBindCtx*) noexcept override { return E_NOTIMPL; }
};

} // namespace rtclick
```

Create `src/ShellExtension/ParentCommand.cpp`:

```cpp
#include "pch.h"
#include "ParentCommand.h"
#include "ConvertToPngCommand.h"
#include "FileFilter.h"
#include "SubCommandOrder.h"
#include "resource.h"

EXTERN_C IMAGE_DOS_HEADER __ImageBase;

namespace rtclick {

namespace {
    /// Fixed list of child commands.  Reset/Clone matter: the shell may enumerate more than once.
    class CommandEnum : public RuntimeClass<RuntimeClassFlags<ClassicCom>, IEnumExplorerCommand>
    {
    public:
        std::vector<ComPtr<IExplorerCommand>> cmds;
        size_t pos = 0;

        IFACEMETHODIMP Next(ULONG celt, IExplorerCommand** out, ULONG* fetched) noexcept override
        {
            if (!out) return E_POINTER;
            ULONG n = 0;
            while (n < celt && pos < cmds.size())
            {
                cmds[pos].CopyTo(&out[n]);
                ++n;
                ++pos;
            }
            if (fetched) *fetched = n;
            return n == celt ? S_OK : S_FALSE;
        }
        IFACEMETHODIMP Skip(ULONG celt) noexcept override
        {
            pos = (std::min)(cmds.size(), pos + celt);
            return pos < cmds.size() ? S_OK : S_FALSE;
        }
        IFACEMETHODIMP Reset() noexcept override
        {
            pos = 0;
            return S_OK;
        }
        IFACEMETHODIMP Clone(IEnumExplorerCommand** out) noexcept override
        {
            if (!out) return E_POINTER;
            *out = nullptr;
            auto e = Microsoft::WRL::Make<CommandEnum>();
            if (!e) return E_OUTOFMEMORY;
            e->cmds = cmds;
            e->pos = pos;
            return e.CopyTo(out);
        }
    };

    ComPtr<IExplorerCommand> MakeSubCommand(SubCommand kind) noexcept
    {
        switch (kind)
        {
        case SubCommand::ConvertToPng:  return Microsoft::WRL::Make<ConvertToPngCommand>();
        case SubCommand::CopyAsPng:     return Microsoft::WRL::Make<CopyAsPngCommand>();
        case SubCommand::ConvertToJpeg: return Microsoft::WRL::Make<ConvertToJpegCommand>();
        case SubCommand::CopyAsJpeg:    return Microsoft::WRL::Make<CopyAsJpegCommand>();
        case SubCommand::Settings:      return Microsoft::WRL::Make<OpenSettingsCommand>();
        }
        return nullptr;
    }
}

IFACEMETHODIMP RightClickPngParentCommand::GetTitle(IShellItemArray*, LPWSTR* title) noexcept
{
    return SHStrDupW(L"Right Click to PNG", title);
}

IFACEMETHODIMP RightClickPngParentCommand::GetIcon(IShellItemArray*, LPWSTR* icon) noexcept
{
    *icon = nullptr;
    wchar_t buf[MAX_PATH];
    auto n = GetModuleFileNameW(reinterpret_cast<HMODULE>(&__ImageBase), buf, MAX_PATH);
    if (n == 0 || n == MAX_PATH) return E_NOTIMPL;   // no icon beats a broken one
    std::wstring location(buf, n);
    location += L",-" + std::to_wstring(IDI_RIGHTCLICKPNG);
    return SHStrDupW(location.c_str(), icon);
}

IFACEMETHODIMP RightClickPngParentCommand::GetState(IShellItemArray* items, BOOL, EXPCMDSTATE* state) noexcept
{
    auto paths = GetSelectionPaths(items);
    *state = FileFilter::ShouldShowParent(paths) ? ECS_ENABLED : ECS_HIDDEN;
    return S_OK;
}

IFACEMETHODIMP RightClickPngParentCommand::GetFlags(EXPCMDFLAGS* flags) noexcept
{
    *flags = ECF_HASSUBCOMMANDS;
    return S_OK;
}

IFACEMETHODIMP RightClickPngParentCommand::EnumSubCommands(IEnumExplorerCommand** enumCmd) noexcept
{
    if (!enumCmd) return E_POINTER;
    *enumCmd = nullptr;
    auto e = Microsoft::WRL::Make<CommandEnum>();
    if (!e) return E_OUTOFMEMORY;
    e->cmds.reserve(kSubCommandOrder.size());
    for (auto kind : kSubCommandOrder)
    {
        auto cmd = MakeSubCommand(kind);
        if (!cmd) return E_OUTOFMEMORY;   // no submenu beats a partial one
        e->cmds.push_back(std::move(cmd));
    }
    return e.CopyTo(enumCmd);
}

} // namespace rtclick
```

Note: `cmds.reserve` / `push_back` can throw `std::bad_alloc` inside a `noexcept` function, which terminates the process. That matches the existing commands, which build `std::vector`/`std::wstring` the same way, and it stays consistent with them.

In `src/ShellExtension/dllmain.cpp`, add `#include "ParentCommand.h"` after `#include "ConvertToPngCommand.h"`, and add this before `return CLASS_E_CLASSNOTAVAILABLE;`:

```cpp
    if (IsEqualCLSID(clsid, CLSID_RightClickPngParent))
    {
        auto f = Microsoft::WRL::Make<CommandClassFactory<RightClickPngParentCommand>>();
        return f ? f->QueryInterface(riid, ppv) : E_OUTOFMEMORY;
    }
```

In `src/ShellExtension/ConvertToPngCommand.cpp`, in `OpenSettingsCommand`:

```cpp
IFACEMETHODIMP OpenSettingsCommand::GetTitle(IShellItemArray*, LPWSTR* title) noexcept
{
    return SHStrDupW(L"Settings…", title);
}

IFACEMETHODIMP OpenSettingsCommand::GetState(IShellItemArray* items, BOOL, EXPCMDSTATE* state) noexcept
{
    // Same gate as the parent, so the flyout always has at least this item.
    auto paths = GetSelectionPaths(items);
    *state = FileFilter::ShouldShowParent(paths) ? ECS_ENABLED : ECS_HIDDEN;
    return S_OK;
}
```

Also update the section banner comment above it from `// ============ Right Click PNG Settings... ============` to `// ============ Settings… ============`.

In `ShellExtension.vcxproj`, add `<ClInclude Include="ParentCommand.h" />` and `<ClCompile Include="ParentCommand.cpp" />` to their groups.

In `src/Package/Package.appxmanifest`:

1. Add a sixth class after the `AB12D5C0-…` entry inside `<com:SurrogateServer>`:

```xml
              <com:Class Id="582E2707-2A81-491E-986F-145208BF87D2"
                         Path="ShellExtension\ShellExtension.dll" ThreadingModel="STA" />
```

2. Update the comment above `com:Extension` to read: `Six COM classes: the parent verb plus the five subcommands it serves.  The five stay registered as permanent ABI even though the parent instantiates them directly.`

3. Replace the five `desktop4:Verb` lines inside **each** of the 11 `desktop4:ItemType` blocks with a single line, so every block looks like:

```xml
            <desktop4:ItemType Type=".webp">
              <desktop4:Verb Id="RightClickPng" Clsid="582E2707-2A81-491E-986F-145208BF87D2" />
            </desktop4:ItemType>
```

Do the replacement mechanically, keeping the 11 `Type` values as they are:

```powershell
$p = 'src/Package/Package.appxmanifest'
$m = Get-Content $p -Raw
$m = [regex]::Replace($m,
  '(<desktop4:ItemType Type="[^"]+">)\s*(?:<desktop4:Verb [^>]+/>\s*)+(</desktop4:ItemType>)',
  "`$1`r`n              <desktop4:Verb Id=`"RightClickPng`" Clsid=`"582E2707-2A81-491E-986F-145208BF87D2`" />`r`n            `$2")
Set-Content $p $m -NoNewline
```

4. Update the comment above `desktop4:Extension` to read: `One parent verb per supported extension.  Per-item visibility inside its submenu is decided at runtime by each subcommand's GetState using FileFilter + SettingsReader.`

- [ ] **Step 4: Verify**

```powershell
pwsh -File $env:TEMP\check-manifest.ps1
& $msbuild src/ShellExtension/ShellExtension.vcxproj /p:Configuration=Release /p:Platform=x64 /v:minimal
& $msbuild tests/ShellExtension.Tests/ShellExtension.Tests.vcxproj /p:Configuration=Release /p:Platform=x64 /v:minimal
./tests/ShellExtension.Tests/bin/Release/ShellExtensionTests.exe; "exit $LASTEXITCODE"
pwsh -File $env:TEMP\check-icon.ps1
```

Expected: `parent verbs: 11 (want 11)  old verbs: 0 (want 0)  com classes: 6 (want 6)`; both builds at 0 warnings; tests `exit 0`; icon present `True`.

- [ ] **Step 5: Commit**

```bash
git add src/ShellExtension/ParentCommand.h src/ShellExtension/ParentCommand.cpp src/ShellExtension/dllmain.cpp src/ShellExtension/ConvertToPngCommand.cpp src/ShellExtension/ShellExtension.vcxproj src/Package/Package.appxmanifest
git commit -m "feat(shell): one icon'd parent verb serves the five commands (#2)"
```

---

### Task 4: Docs

**Files:**
- Modify: `src/ShellExtension/README.md`, `docs/spec.md`, `README.md`, `CHANGELOG.md`, `CLAUDE.md`

- [ ] **Step 1: `src/ShellExtension/README.md`**

In `## Files`, add:

```markdown
- `ParentCommand.{h,cpp}` — the single registered "Right Click to PNG" verb; serves the five commands as a submenu, carries the icon.
- `SubCommandOrder.h` — submenu order, COM-free so the test runner can pin it.
- `ShellExtension.rc`, `resource.h`, `RightClickPng.ico` — the parent verb's icon (resource 101), rendered by `src/Package/Assets/render-brand-icons.ps1`.
```

Replace the CLSID table with:

```markdown
| Verb | CLSID | Role |
|---|---|---|
| RightClickPng (parent) | `{582E2707-2A81-491E-986F-145208BF87D2}` | Only verb registered per file type; submenu host |
| ConvertToPng  | `{68730B57-152E-4BC9-A158-3A03EE03465A}` | Submenu item 1 |
| CopyAsPng     | `{D1407350-4611-4621-802E-475C82006858}` | Submenu item 2 |
| ConvertToJpeg | `{DC177957-7F24-486D-B4CB-D69FA4580C8F}` | Submenu item 3 (JPEG variants on) |
| CopyAsJpeg    | `{7C7F2F20-9F1E-4F0F-9FA8-D404369C2C07}` | Submenu item 4 (JPEG variants on) |
| OpenSettings  | `{AB12D5C0-3E4F-4B1A-9E88-5C3D7F91AA01}` | Submenu item 5, "Settings…" |

The five submenu CLSIDs stay registered in the manifest even though the parent instantiates them directly. CLSIDs are permanent ABI — never reuse or renumber.
```

Also replace the stale `item 5 placeholder` / `real visibility logic in item 6` wording in `## Files` with plain descriptions: `ConvertToPngCommand.{h,cpp}` holds the five verb commands, and `FileFilter.{h,cpp}` is the extension allowlist and per-verb visibility predicates.

- [ ] **Step 2: `docs/spec.md`**

Under `## Shell Extension (C++/WinRT)`, add a `### RightClickPngParentCommand` subsection before `### ConvertToPngCommand / CopyAsPngCommand (and JPEG variants)` containing the method table from the spec's *Parent command* section, plus one sentence: "Registered as the only `desktop4:Verb` per file type since 0.1.2.0; before that, the five commands were registered as separate top-level verbs." Wherever the settings verb's title appears, change it to `Settings…`.

- [ ] **Step 3: `README.md`**

In `## What it does`, replace the table's "You see" cells so they name the submenu. For example, the first row becomes **Right Click to PNG →** *Convert to PNG*, *Copy as PNG*. Add one line under the table: "Both the modern and the classic (*Show more options*) menu show a single **Right Click to PNG** entry with the app icon; the verbs live in its submenu." In the Settings section and the third troubleshooting entry, change **Right Click PNG → settings…** to **Right Click to PNG → Settings…**. In the troubleshooting entry about the classic-menu registry tweak, delete the sentence saying the legacy menu "flattens grouped submenus inconsistently and sometimes only shows one of our verbs", and replace it with: "Since 0.1.2.0 the classic menu shows the same single entry; if you still see nothing, check for the tweak below."

- [ ] **Step 4: `CHANGELOG.md`**

Under `## Unreleased`, add above `### Fixed`:

```markdown
### Changed

- **One context-menu entry with the app icon, in both menus.** The verbs now
  live in a single **Right Click to PNG** submenu. In the classic *Show more
  options* menu they used to appear as loose, iconless items with other apps'
  entries between them (#2). The settings verb is now just **Settings…**.
```

- [ ] **Step 5: `CLAUDE.md`**

Replace the CLSID gotcha bullet with:

```markdown
- **CLSIDs are permanent ABI.** One parent verb (`ParentCommand`) is the only verb registered per file type; the five commands are its submenu, built in `SubCommandOrder.h` order. A new command gets a new CLSID in the manifest `ComServer`, a `SubCommand` entry, a case in `ParentCommand.cpp`'s `MakeSubCommand`, and a `dllmain.cpp` factory branch, with `docs/spec.md` and `src/ShellExtension/README.md` updated. Never reuse or renumber one, and never drop a shipped one from the `ComServer`.
```

- [ ] **Step 6: Commit**

```bash
git add src/ShellExtension/README.md docs/spec.md README.md CHANGELOG.md CLAUDE.md
git commit -m "docs: parent verb, Settings… rename, CLSID table (#2)"
```

---

### Task 5: Verification on real packages (human in the loop)

No code changes are committed in this task. Its outputs are a pass/fail record and the update-while-running note.

- [ ] **Step 1: Full automated pass**

```powershell
$env:PATH = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer;$env:PATH"
dotnet test tests/Engine.Tests/ -c Release
& $msbuild tests/ShellExtension.Tests/ShellExtension.Tests.vcxproj /p:Configuration=Release /p:Platform=x64 /v:minimal
./tests/ShellExtension.Tests/bin/Release/ShellExtensionTests.exe; "exit $LASTEXITCODE"
```

Expected: xUnit all passing (40 on this branch; 43 once PR #4 merges), C++ `exit 0`.

- [ ] **Step 2: Dev cert (Este, once, elevated PowerShell)**

```powershell
./build/create-dev-cert.ps1
```

Expected: `src/Package/RTClickPng_TemporaryKey.pfx` exists (gitignored), and the cert is trusted under LocalMachine\Root. The script's default PFX password `RTClickPng-Dev` matches the wapproj's default `PackageCertificatePassword`, so the builds below need no password flag.

- [ ] **Step 3: Signed 0.1.1.0 baseline from `main`**

```powershell
git switch main
& $msbuild src/Package/Package.wapproj /p:Configuration=Release /p:Platform=x64 /p:AppxBundle=Never /p:AppxPackageSigningEnabled=true /p:PackageCertificateKeyFile=RTClickPng_TemporaryKey.pfx /v:minimal
$base = "$env:TEMP\rtclick-upgrade"; New-Item -ItemType Directory -Force $base | Out-Null
Copy-Item src/Package/AppPackages/Package_0.1.1.0_x64_Test/Package_0.1.1.0_x64.msix "$base\0.1.1.0.msix" -Force
git switch feat/classic-menu-parent
Get-AppxPackage 626LabsLLC.RightClicktoPNG | Remove-AppxPackage      # drop the dev -Register install
Add-AppxPackage "$base\0.1.1.0.msix"
(Get-AppxPackage 626LabsLLC.RightClicktoPNG).InstallLocation           # expect C:\Program Files\WindowsApps\...
```

Restart Explorer (`Stop-Process -Name explorer -Force`; start it if it doesn't respawn). Este confirms the **old** menu: in the classic menu, the verbs appear loose.

- [ ] **Step 4: Signed 0.1.2.0 as an in-place update (temporary version bump, never committed)**

```powershell
(Get-Content src/Package/Package.appxmanifest -Raw) -replace 'Version="0\.1\.1\.0"', 'Version="0.1.2.0"' | Set-Content src/Package/Package.appxmanifest -NoNewline
& $msbuild src/Package/Package.wapproj /p:Configuration=Release /p:Platform=x64 /p:AppxBundle=Never /p:AppxPackageSigningEnabled=true /p:PackageCertificateKeyFile=RTClickPng_TemporaryKey.pfx /v:minimal
Copy-Item src/Package/AppPackages/Package_0.1.2.0_x64_Test/Package_0.1.2.0_x64.msix "$base\0.1.2.0.msix" -Force
git checkout -- src/Package/Package.appxmanifest                        # revert the bump
try { Add-AppxPackage "$base\0.1.2.0.msix" -ErrorAction Stop; 'plain update: OK' }
catch { "plain update FAILED: $($_.Exception.Message)"; Add-AppxPackage "$base\0.1.2.0.msix" -ForceApplicationShutdown; 'forced update: OK' }
(Get-AppxPackage 626LabsLLC.RightClicktoPNG).Version                    # expect 0.1.2.0
git status --short                                                      # expect clean
```

Record which line printed. That's the update-while-running finding for the Store-submission task's README note.

- [ ] **Step 5: Manual matrix (Este, after restarting Explorer)**

In both the modern and the classic menu, in `%TEMP%\rtclick-spike` (or any folder holding `.webp`, `.avif`, `.heic` and `.png` copies):

| Case | Expected |
|---|---|
| Single `.webp`, JPEG variants off | One **Right Click to PNG** entry with the icon → Convert to PNG, Copy as PNG, Settings… |
| Same, JPEG variants on | … plus Convert to JPEG and Copy as JPEG, in that order, before Settings… |
| `.avif`, `.heic` | Same as `.webp` |
| Single `.png` | Copy as PNG and Settings… (no Convert to PNG) |
| Two `.webp` selected | Convert to PNG and Settings… (no Copy) |
| Two `.png` selected | Settings… only. The flyout is never empty. |
| `.webp` + a `.txt` selected | No Right Click to PNG entry |
| Open the same submenu twice | Same items the second time |
| Light and dark mode | Icon visible and crisp in both |
| Click Settings… | Settings window opens |
| Click Convert to PNG on `.heic` | `.png` written beside it |
| Classic menu | No leftover loose Convert/Copy verbs from 0.1.1.0 |

- [ ] **Step 6: Restore the dev install, and record the results**

```powershell
Get-AppxPackage 626LabsLLC.RightClicktoPNG | Remove-AppxPackage
& $msbuild src/Package/Package.wapproj /p:Configuration=Release /p:Platform=x64 /p:AppxBundle=Never /v:minimal
Add-AppxPackage -Register src/Package/bin/x64/Release/AppxManifest.xml
```

Log the matrix result and the update-while-running finding to the 626 dashboard (`manage_decisions`), and add the finding to the Store-submission task. Check `docs/screenshots/01-right-click-menu.png` against the new modern menu; if it visibly differs (the icon, or the "Settings…" label), note a recapture in that same task. Delete the spike branch: `git branch -D spike/classic-menu-parent`.
