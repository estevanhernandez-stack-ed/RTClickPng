# Right Click PNG

> Convert images to PNG from the right-click menu. No network calls, no detours.

You right-click a `.webp` or `.avif` or `.heic` on a web page, save it, and Windows
hands you a file half the apps you use don't understand. **Right Click PNG** adds
*Convert to PNG* and *Copy as PNG* to the File Explorer context menu so the round-trip
through an online converter stops happening. Paste works too — into Teams, Slack,
Discord, Figma, Photoshop, Paint, Word.

All decoding happens locally. The app never makes a network call.

## Install

- **Microsoft Store**: [Right Click PNG](https://apps.microsoft.com/detail/<TBD>) (pending submission)
- **From source**: see below
- **Source**: <https://github.com/estevanhernandez-stack-ed/RTClickPng>

## What it does

| You right-click… | You see |
| --- | --- |
| `.webp` / `.avif` / `.heic` / `.heif` / `.jpg` / `.bmp` / `.tif` / `.gif` | **Convert to PNG**, **Copy as PNG** |
| `.png` | **Copy as PNG** (workflow win — one-click paste) |
| A supported image with *Show JPEG variants* turned on | Adds **Convert to JPEG** and **Copy as JPEG** |
| Multiple files selected | *Convert to PNG* for the batch; *Copy as PNG* hides (single-select only) |
| A file whose extension isn't in the list | No verbs from us. Silent. |

Convert writes the output **next to the source**, same base name, new extension.
Copy loads the clipboard in three formats (`CF_DIBV5`, `CF_DIB`, registered `PNG`)
so transparency and ICC profiles survive the hop into modern editors.

## How it works

Three pieces, one MSIX package:

| Piece | Lives in | Role |
| --- | --- | --- |
| **Engine** (.NET 9 AOT, ~1 MB) | `src/Engine/` | Standalone CLI that does the decode + encode. Called by the shell extension per-invocation. |
| **Shell Extension** (C++/WinRT DLL) | `src/ShellExtension/` | Implements `IExplorerCommand`. Decides what verbs to show, what to name them, what to do on click. Hosted by `dllhost.exe` via the modern Win11 context menu. |
| **MSIX Package** (wapproj) | `src/Package/` | Signs + packages the above, registers COM classes + file-type associations via the app manifest. |

Decoding is done by bundled native libraries: **libwebp, libavif (dav1d), libheif (libde265),
libspng, libjpeg-turbo**. Licenses documented in [`docs/licenses.md`](docs/licenses.md).

## Privacy

Right Click PNG makes **zero network calls**. Decoders run locally against bytes you
already have on disk. Nothing leaves your machine. You can verify with:

```powershell
# Install the app, then while actively using it:
procmon.exe   # filter on Operation = TCP Connect, Process = RTClickPng.Engine.exe
# Expect: no events.
```

Or read the source. Look in `src/Engine/Interop/` — the P/Invoke bindings. There are
no HTTP bindings because there's no HTTP.

## Build from source

### Prerequisites

- **Windows 11 22H2 or newer**
- **Visual Studio 2022 Build Tools 17.14+** with:
  - *Desktop development with C++* workload (v143 toolset)
  - *Universal Windows Platform build tools* workload (for the wapproj DesktopBridge targets)
- **.NET 9 SDK** (9.0.x)
- **PowerShell 7+** (`pwsh`)
- **Git**

### Steps

```powershell
# 1. Fetch the native decoder DLLs (30-60 min first time; vcpkg builds them from source)
./build/fetch-native.ps1

# 2. Verify CRT consistency across the 14 native DLLs
./build/verify-crt.ps1

# 3. Run tests (61 total: 40 xUnit + 45 C++ — as of v0.1)
dotnet test tests/Engine.Tests/ -c Release
msbuild tests/ShellExtension.Tests/ShellExtension.Tests.vcxproj /p:Configuration=Release /p:Platform=x64
./tests/ShellExtension.Tests/bin/Release/ShellExtensionTests.exe

# 4. Create a dev signing cert (one-time, elevated PowerShell)
./build/create-dev-cert.ps1

# 5. Build the MSIX
msbuild src/Package/Package.wapproj /p:Configuration=Release /p:Platform=x64 /p:AppxBundle=Never

# 6. Install
Add-AppxPackage -Path src/Package/AppPackages/Package_0.1.0.0_x64_Test/Package_0.1.0.0_x64.msix

# 7. Restart Explorer to pick up the new shell-extension registration
taskkill /f /im explorer.exe; Start-Process explorer.exe
```

All of this mirrors what the CI pipeline runs — see [`.github/workflows/build.yml`](.github/workflows/build.yml).

## Settings

Open the branded WPF Settings window from the Start menu tile, or from the
right-click menu on any supported image via **Right Click PNG → settings…**.
Toggles persist to a JSON file the shell extension reads on every right-click:

```text
%LOCALAPPDATA%\Packages\626labs.RTClickPng_3fjztnatnmz7a\LocalState\settings.json
```

Hand-editing the file works too — schema and default values documented in
[`docs/SETTINGS.md`](docs/SETTINGS.md).

## Architecture brief

Engine and shell extension are split across process boundaries on purpose:

- **Explorer loads the shell extension as a DLL**. If the shell extension crashed
  Explorer, Explorer would be sad. So the shell extension stays minimal: read
  selection, read settings, maybe launch the engine, handle the toast.
- **The engine is a separate exe**. It decodes + encodes + writes the output file
  or streams PNG bytes on stdout. When the engine crashes (or hits a malformed file,
  or runs out of memory), the shell extension sees a non-zero exit code and reports
  an error toast. Explorer never notices.

Communication is stdio: command-line args in, exit codes + stderr + stdout bytes out.
For the Copy verb, the engine writes a 4-byte big-endian length prefix then raw PNG bytes
on stdout; the shell extension decodes that into the three clipboard formats via WIC.

See [`docs/spec.md`](docs/spec.md) for the full technical breakdown.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## Licenses

- **This app**: MIT (see [LICENSE](LICENSE)).
- **Bundled decoders**: mixed BSD + LGPL (libheif). Full breakdown + compliance
  reasoning in [`docs/licenses.md`](docs/licenses.md). LGPL components (libheif,
  libde265) are shipped as standalone DLLs; users can replace them by unpacking
  the MSIX, substituting the DLL, and re-signing — per LGPL-3.0 §4.
