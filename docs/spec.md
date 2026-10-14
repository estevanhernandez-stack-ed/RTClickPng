# Right Click PNG — Technical Spec

## Stack

A three-executable architecture with a strict engine/UI split. Each piece is built in the language its job demands, not a shared-stack preference.

| Layer | Language / Framework | Purpose |
|---|---|---|
| **Shell extension** | **C++ / C++/WinRT** | In-process COM DLL that registers `IExplorerCommand` handlers via sparse package. Tiny code surface. |
| **Converter engine** | **C# / .NET 10 Native AOT** | Out-of-process CLI that decodes input, encodes output, preserves ICC, strips EXIF, emits to file or stdout. |
| **Settings UI** | **C# / WinUI 3 on Windows App SDK 1.8** | Packaged desktop app — single Fluent/WinUI 3 window, launched from Start menu. |
| **Native decoders** | **libwebp, libavif, libheif, libspng, libjpeg-turbo** (bundled native DLLs, invoked via P/Invoke) | Zero external codec dependencies. |
| **Packaging** | **MSIX + sparse package registration** | Single MSIX bundle deployed to Microsoft Store. Shell extension registered via `desktop4:FileExplorerContextMenus`. |

**Why three executables instead of one:** Microsoft heavily discourages managed-code shell extensions — CLR version clashes inside Explorer processes are a documented stability risk. The shell extension has to be native C++. The engine and settings UI have no such constraint; they live in the builder's .NET comfort zone. The separation isn't style — it's a practical forcing function that also gives us process-isolation stability (a decoder crash kills a transient engine process, not Explorer).

**Stack documentation links:**
- C++/WinRT: https://learn.microsoft.com/en-us/windows/uwp/cpp-and-winrt-apis/
- .NET 10 Native AOT: https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/
- Windows App SDK 1.8 (stable): https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/stable-channel
- WinUI 3: https://learn.microsoft.com/en-us/windows/apps/winui/winui3/
- MSIX sparse packages: https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/grant-identity-to-nonpackaged-apps
- `IExplorerCommand`: https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nn-shobjidl_core-iexplorercommand
- `desktop4:FileExplorerContextMenus`: https://learn.microsoft.com/en-us/uwp/schemas/appxpackage/uapmanifestschema/element-desktop4-fileexplorercontextmenus
- libwebp: https://developers.google.com/speed/webp/docs/api
- libavif: https://github.com/AOMediaCodec/libavif
- libheif: https://github.com/strukturag/libheif
- libspng: https://libspng.org/
- libjpeg-turbo: https://libjpeg-turbo.org/

## Runtime & Deployment

**Runtime target:** Windows desktop, Windows 11 version 22H2 (build `10.0.22621.0`) or later. Windows 10 is explicitly not supported — Microsoft ended consumer support October 14, 2025, and targeting an EOL OS for a tool that touches files and clipboard is the wrong stability trade.

**Deployment target:** Microsoft Store, free, under the builder's existing publisher account. MSIX bundle. OSS repo with build-from-source instructions as an alternate install path.

**Floor configuration (three places, must agree):**

1. `Package.appxmanifest`:
   ```xml
   <Dependencies>
     <TargetDeviceFamily Name="Windows.Desktop"
                         MinVersion="10.0.22621.0"
                         MaxVersionTested="10.0.26100.0" />
   </Dependencies>
   ```
2. `ShellExtension.vcxproj`:
   ```xml
   <WindowsTargetPlatformVersion>10.0.26100.0</WindowsTargetPlatformVersion>
   <WindowsTargetPlatformMinVersion>10.0.22621.0</WindowsTargetPlatformMinVersion>
   ```
3. `Engine.csproj` and `Settings.csproj`:
   ```xml
   <TargetFramework>net10.0-windows10.0.22621.0</TargetFramework>
   ```

**Architecture target:** x64 only for v1. ARM64 deferred to v1.1 (all three projects support it with a rebuild; deferring to keep initial Store submission scope tight).

**Environment requirements for building:**
- Visual Studio 2026 (or later) with:
  - MSVC v143+ (C++ workload)
  - .NET 10 SDK (C# workload)
  - Windows App SDK 1.8 VSIX
  - Windows 11 SDK (26100 or later)
- MSIX publisher certificate (for local build; Store uses publisher account's cert)

## Architecture Overview

```
┌──────────────────────────────────────────────────────────────────┐
│                        Windows 11 File Explorer                  │
│                                                                  │
│         ┌──────────────────────────────────────────┐             │
│         │ ShellExtension.dll  (C++/WinRT, in-proc) │             │
│         │ ─────────────────────────────────────────│             │
│         │  IExplorerCommand handlers:              │             │
│         │    • ConvertToPngCommand                 │             │
│         │    • CopyAsPngCommand                    │             │
│         │    • ConvertToJpegCommand   (if setting) │             │
│         │    • CopyAsJpegCommand      (if setting) │             │
│         │                                          │             │
│         │  SettingsReader  FileFilter              │             │
│         │  EngineLauncher  ClipboardWriter  Notifier│            │
│         └──────────┬───────────────────────────────┘             │
└────────────────────│─────────────────────────────────────────────┘
                     │
     CreateProcess + stdin/stdout pipes
                     │
                     ▼
         ┌────────────────────────────────────┐
         │ RTClickPng.Engine.exe              │
         │ (.NET 10 Native AOT, transient)    │
         │ ─────────────────────────────────  │
         │  CLI: convert <src> <dst> | copy <src>
         │                                    │
         │  Decoders/ Encoders/               │
         │    ├─ libwebp   ├─ libspng         │
         │    ├─ libavif   └─ libjpeg-turbo   │
         │    ├─ libheif                      │
         │    └─ built-in bmp/tiff/gif        │
         │                                    │
         │  ColorProfile  ExifHandler         │
         └────────────────────────────────────┘

                     ┌──────────────────────────┐
                     │ RTClickPng.Settings.exe  │
                     │ (WinUI 3, user-launched) │
                     │ ───────────────────────  │
                     │  Single Fluent window    │
                     │  Reads/writes settings   │
                     └──────────┬───────────────┘
                                │
                                ▼
                  ┌──────────────────────────────┐
                  │ settings.json (LocalAppData) │
                  │  {                           │
                  │    "showJpegVariants": bool, │
                  │    "confirmBeforeOverwrite": │
                  │        bool,                 │
                  │    "schemaVersion": 1        │
                  │  }                           │
                  └──────────────────────────────┘
                    ▲ read by ShellExtension (menu render)
                    ▲ read by Engine (overwrite behavior)
                    ▲ written by Settings UI
```

**Data flow — Convert to PNG** (implements `prd.md > Epic 1`):

```
Explorer right-click file.webp
  → ShellExtension.QueryContextMenu()
      • FileFilter: extension in allowlist? → yes
      • SettingsReader: read settings.json (JPEG toggle)
      • return menu items
  → User clicks "Convert to PNG"
  → EngineLauncher.Spawn:
      RTClickPng.Engine.exe convert "file.webp" "file.png"
      --overwrite-policy=confirm  (from settings)
  → Engine:
      1. decode via libwebp → RGBA bitmap in memory
      2. read ICC profile (if present), keep for encode
      3. read EXIF orientation, apply to bitmap, discard rest
      4. libspng encode → file.png on disk
      5. exit 0 (success) or non-zero with error code on stderr
  → ShellExtension reads exit code
      • 0 → Notifier toast: "Converted file.webp → file.png" with "Show in folder" action
      • non-zero → Notifier toast: error summary
```

**Data flow — Copy as PNG** (implements `prd.md > Epic 2`):

```
Explorer right-click file.webp  (single-selection only)
  → ShellExtension.QueryContextMenu()
      • FileFilter: supported extension? → yes
      • Selection count > 1? → Copy as PNG hidden
      • return menu items
  → User clicks "Copy as PNG"
  → EngineLauncher.Spawn (with stdout pipe captured):
      RTClickPng.Engine.exe copy "file.webp"
  → Engine:
      1. decode → RGBA bitmap
      2. preserve ICC, strip EXIF (apply orientation)
      3. libspng encode → PNG byte buffer
      4. write [4-byte length header][PNG bytes] to stdout
      5. exit 0
  → ShellExtension:
      read stdout → length prefix → PNG byte buffer
      OpenClipboard(hwnd)
      EmptyClipboard()
      SetClipboardData(CF_DIBV5, dibv5_buffer)   — transparency-aware apps
      SetClipboardData(CF_DIB,   dib_buffer)     — legacy apps
      SetClipboardData(CF_PNG,   png_buffer)     — registered "PNG" format
      CloseClipboard()
  → Notifier toast: "Copied as PNG — ready to paste"
```

## Shell Extension (C++/WinRT)

### RightClickPngParentCommand

Registered as the only `desktop4:Verb` per file type since 0.1.2.0; before that, the five commands were registered as separate top-level verbs. CLSID `{582E2707-2A81-491E-986F-145208BF87D2}`, in `ParentCommand.{h,cpp}`.

| Method | Behavior |
|---|---|
| `GetTitle` | `Right Click to PNG` (the Partner Center `DisplayName`) |
| `GetIcon` | `<dir of ShellExtension.dll>\ShellExtension.dll,-101` — the brand `.ico` embedded via `ShellExtension.rc` |
| `GetFlags` | `ECF_HASSUBCOMMANDS` |
| `GetState` | `FileFilter::ShouldShowParent` (all items supported), else hidden |
| `EnumSubCommands` | Fresh `IEnumExplorerCommand` over the five commands below, in `SubCommandOrder.h` order: Convert to PNG, Copy as PNG, Convert to JPEG, Copy as JPEG, Settings… |
| `Invoke` | `E_NOTIMPL` — a parent with subcommands is never invoked |

Each child keeps its own `GetState`, so per-item visibility (JPEG toggle, single-select Copy) works inside the submenu. The Settings… child uses the parent's gate, so the flyout is never empty.

### ConvertToPngCommand / CopyAsPngCommand (and JPEG variants)

Each verb is a separate class implementing `IExplorerCommand` + `IInitializeCommand` + `IObjectWithSite`. CLSIDs are declared in the sparse manifest under `<com:ComServer>` and wired to `<desktop4:FileExplorerContextMenus>` extension points.

**Key methods per handler:**

- `GetTitle()` — returns "Convert to PNG" / "Copy as PNG" / JPEG equivalents
- `GetIcon()` — returns the RTClickPng icon (shared across all verbs in v1)
- `GetState()` — returns `ECS_ENABLED` if file qualifies, `ECS_HIDDEN` otherwise (per visibility rules below)
- `Invoke()` — reads selection, builds engine args, spawns engine, marshals result

**Menu visibility logic (implements `prd.md > Epic 3`):**

```
For each selected file, ask:
  ext = lowercase file extension

For "Convert to PNG":
  show if  ext in {webp, avif, heic, heif, bmp, tiff, tif, gif}  (multi-select OK; per-file filter)
  hide if  ext == png                                            (redundant)
  hide if  ext in {jpg, jpeg} && !settings.showJpegVariants      (redundant unless JPEG mode)

For "Copy as PNG":
  hide if  selection_count > 1
  show if  ext in {webp, avif, heic, heif, bmp, tiff, tif, gif, png}  (PNG included — workflow win)
  hide if  ext in {jpg, jpeg} && !settings.showJpegVariants

For "Convert to JPEG" / "Copy as JPEG":
  show only if  settings.showJpegVariants == true
  same allow-list as PNG variants, except
    • "Convert to JPEG" hides on ext == jpg|jpeg (redundant)
    • "Copy as JPEG" shows on jpg|jpeg source (workflow win)
```

No file I/O during `GetState()` — decisions are extension-based only, so the menu renders instantly.

### FileFilter

Static allowlist of supported extensions compiled into the DLL. Lowercase comparison, UTF-16 strings. No trips to disk. Tests live in `tests/ShellExtension.Tests` and exercise all boundary cases (unsupported ext, mixed multi-select, case variants, files without extensions).

### SettingsReader

Reads `settings.json` from `%LOCALAPPDATA%\Packages\<PackageFamilyName>\LocalState\settings.json` on each `GetState()` call. Parses with a minimal hand-rolled reader (no third-party JSON dependency — keeping the DLL small). Caches the result for the lifetime of the `IExplorerCommand` instance (one menu render).

If the file is missing, corrupt, or unreadable: **default to safe values** (`showJpegVariants = false`, `confirmBeforeOverwrite = true`). No crash, no prompt.

### EngineLauncher

Wraps `CreateProcessW` with:
- Redirected `stdin`, `stdout`, `stderr` (anonymous pipes)
- `CREATE_NO_WINDOW` flag (suppress console flash)
- `STARTF_USESHOWWINDOW` + `SW_HIDE`

Locates `RTClickPng.Engine.exe` relative to the DLL's own path (both ship in the same MSIX package, same install folder). Handles timeout (default 60 seconds per invocation — configurable constant; if exceeded, terminates the engine process and emits an error toast).

### ClipboardWriter

Owns the three-format clipboard write for Copy actions:

1. `CF_DIBV5` — transparency-aware apps (Figma, Photoshop, Office, Teams, Outlook). Constructs the `BITMAPV5HEADER` with premultiplied-alpha bitfield masks. ICC profile embedded via `BITMAPV5HEADER.bV5CSType = PROFILE_EMBEDDED` when present.
2. `CF_DIB` — legacy fallback. Standard `BITMAPINFOHEADER`; transparency is lost but every Windows app back to Win95 pastes it.
3. `CF_PNG` — registered format via `RegisterClipboardFormatW(L"PNG")`. Modern apps (Discord, recent browsers) prefer this; we include it because it preserves ICC and transparency losslessly.

Clipboard owner is the Explorer process (since that's where the shell extension runs). Data is delayed-rendered via `SetClipboardData(format, NULL)` — OS calls back to our handler on first paste request. Delayed rendering keeps peak memory low if the user never actually pastes.

### Notifier

Thin wrapper around `AppNotificationManager` from Windows App SDK 1.8. Toast templates (both verbs):

- **Convert success:** `"Converted {source_name} → {dest_name}"`, with "Show in folder" button invoking `SHOpenFolderAndSelectItems`
- **Copy success:** `"Copied as PNG — ready to paste"`, no action button (user is about to paste)
- **Error:** `"Couldn't convert {source_name}: {error_code}"`, with a "Copy error" button for GitHub-issue filing

## Converter Engine (C# / .NET 10 Native AOT)

### Program.cs — CLI dispatch

```
RTClickPng.Engine.exe convert <src-path> <dst-path> [--jpeg] [--overwrite-policy={confirm|force|skip}]
RTClickPng.Engine.exe copy    <src-path>          [--jpeg]
```

Return codes:
- `0` — success
- `1` — generic failure (check stderr)
- `2` — source file not found / unreadable
- `3` — source format not supported by bundled decoders
- `4` — destination exists and overwrite policy denied it
- `5` — output write failed
- `10` — internal bug (uncaught exception) — stderr has stack trace

No third-party CLI parser — a hand-rolled switch over `args[]` keeps AOT trim size small.

### Decoders

One class per input format. Each implements a common interface:

```csharp
internal interface IImageDecoder
{
    DecodedImage Decode(ReadOnlySpan<byte> sourceBytes);
}

internal record DecodedImage(
    int Width,
    int Height,
    byte[] PixelsRgba,          // always 8-bit RGBA, premultiplied alpha
    byte[]? IccProfile,          // null if no profile
    ExifOrientation Orientation  // apply before encode, then discard
);
```

**Per-format implementation notes:**

- `WebpDecoder` — P/Invoke to `libwebp.dll`. Uses `WebPDecodeRGBA` for simple decode; falls back to `WebPAnimDecoder` for animated WebP (first frame only, no warning per PRD).
- `AvifDecoder` — P/Invoke to `libavif.dll`. `avifDecoderRead` + pixel conversion to RGBA 8-bit (source may be 10- or 12-bit; tone-map to 8 for clipboard/PNG compatibility).
- `HeifDecoder` — P/Invoke to `libheif.dll`. Uses primary image only for multi-image HEIC (Live Photos, bursts). Color profile preserved when container has one.
- `TiffDecoder` — P/Invoke to a lightweight TIFF lib (libtiff via vcpkg, or a pure-C# fallback if size matters). First image in multi-page TIFFs.
- `BmpDecoder` — pure C# using `System.Drawing.Imaging`-free code path (AOT constraint — `System.Drawing` doesn't AOT well). Hand-rolled BMP reader supporting 8/16/24/32-bit variants.
- `GifDecoder` — pure C#, first frame only.

### Encoders

```csharp
internal interface IImageEncoder
{
    byte[] Encode(DecodedImage image);
}
```

- `PngEncoder` — P/Invoke to `libspng.dll`. Chosen over libpng because it's simpler (single-header style), smaller binary footprint, and fully featured for our needs. Writes iCCP chunk when `IccProfile` is non-null. No ancillary chunks (tEXt, EXIF) written.
- `JpegEncoder` — P/Invoke to `libjpeg-turbo.dll`. Quality = 92 (non-configurable in v1; hardcoded). Preserves ICC via `APP2` marker when present.

### ColorProfile

Reads ICC profile blob from source decoder, embeds into destination encoder. If source has no profile, output has no profile (no fabricating sRGB).

### ExifHandler

- Parses orientation tag only (tag `0x0112`) from source EXIF (JPEG, TIFF, HEIC)
- Applies orientation to pixel buffer via rotate/flip before encode
- Discards all other EXIF fields — no GPS, no camera info, no timestamps

## Settings UI (C# / WinUI 3 / WinAppSDK 1.8)

### MainWindow.xaml — single Fluent page

```
┌──────────────────────────────────────────┐
│  Right Click PNG — Settings           ×  │
├──────────────────────────────────────────┤
│                                          │
│  ┌─────────────────────────────────────┐ │
│  │ Show JPEG variants in right-click   │ │
│  │ context menu                        │ │
│  │                            [ OFF ]  │ │
│  └─────────────────────────────────────┘ │
│                                          │
│  ┌─────────────────────────────────────┐ │
│  │ Confirm before overwriting files    │ │
│  │                            [  ON ]  │ │
│  └─────────────────────────────────────┘ │
│                                          │
│  Version 1.0.0 · github.com/…            │
└──────────────────────────────────────────┘
```

Two `ToggleSwitch` controls bound to `SettingsViewModel`. No tabs, no menu bar, no splash. Theme follows Windows automatically via WinAppSDK's theming system. Window size fixed at ~480×240 DIP, non-resizable.

### SettingsService

- Reads and writes `settings.json` in `ApplicationData.Current.LocalFolder` (resolves to `%LOCALAPPDATA%\Packages\<PFN>\LocalState\`)
- Uses `System.Text.Json` (AOT-friendly, built-in)
- Change notifications via a simple file-system watcher so changes from the Settings UI are visible to the shell extension on the next `GetState()` call
- Atomic writes (write to `settings.json.tmp`, `File.Move` over `settings.json`) to prevent torn reads by the shell extension

## Data Model

### `settings.json` schema (v1)

```json
{
  "schemaVersion": 1,
  "showJpegVariants": false,
  "confirmBeforeOverwrite": true
}
```

**Migration plan:** future schema additions increment `schemaVersion`. On read, if `schemaVersion < current`, SettingsService fills missing fields with defaults and writes back. No destructive migrations ever.

**Read/write ownership:**
- **Shell extension:** read-only, on every `GetState()` call
- **Engine:** read-only, on every invocation, to determine overwrite behavior
- **Settings UI:** only writer

## File Structure

```
RTClickPng/
├── .github/
│   ├── ISSUE_TEMPLATE/
│   │   ├── bug_report.md
│   │   └── feature_request.md
│   └── workflows/
│       └── build.yml                    # GH Actions: build, sign, artifact
├── build/
│   ├── sign.ps1                         # local MSIX signing helper
│   ├── make-release.ps1                 # CI release packaging
│   └── native/                          # prebuilt x64 native DLLs (vcpkg or manual)
│       ├── libwebp.dll
│       ├── libavif.dll
│       ├── libheif.dll
│       ├── libspng.dll
│       └── libjpeg-turbo.dll
├── docs/
│   ├── builder-profile.md
│   ├── scope.md
│   ├── prd.md
│   ├── spec.md
│   └── checklist.md                     # written by /checklist
├── process-notes.md
├── README.md                            # OSS front door; install + build instructions
├── LICENSE                              # MIT or Apache 2.0 (TBD in /checklist)
├── CONTRIBUTING.md                      # contribution guide
├── .gitignore
├── RTClickPng.sln                       # Visual Studio solution
├── src/
│   ├── ShellExtension/                  # C++/WinRT COM DLL
│   │   ├── ShellExtension.vcxproj
│   │   ├── pch.{h,cpp}
│   │   ├── dllmain.cpp                  # class factory, DllGetClassObject, DllCanUnloadNow
│   │   ├── ConvertToPngCommand.{h,cpp}
│   │   ├── CopyAsPngCommand.{h,cpp}
│   │   ├── ConvertToJpegCommand.{h,cpp}
│   │   ├── CopyAsJpegCommand.{h,cpp}
│   │   ├── FileFilter.{h,cpp}           # extension allowlist + visibility rules
│   │   ├── SettingsReader.{h,cpp}       # reads settings.json
│   │   ├── EngineLauncher.{h,cpp}       # CreateProcess + pipes
│   │   ├── ClipboardWriter.{h,cpp}      # CF_DIBV5 / CF_DIB / CF_PNG
│   │   ├── Notifier.{h,cpp}             # AppNotificationManager wrapper
│   │   └── ShellExtension.def           # exported symbols (DllGetClassObject etc.)
│   ├── Engine/                          # .NET 10 AOT console app
│   │   ├── Engine.csproj                # PublishAot=true, AOT-compatible only
│   │   ├── Program.cs                   # CLI dispatch
│   │   ├── Convert.cs                   # convert verb handler
│   │   ├── Copy.cs                      # copy verb handler (writes to stdout)
│   │   ├── Decoders/
│   │   │   ├── IImageDecoder.cs
│   │   │   ├── WebpDecoder.cs
│   │   │   ├── AvifDecoder.cs
│   │   │   ├── HeifDecoder.cs
│   │   │   ├── TiffDecoder.cs
│   │   │   ├── BmpDecoder.cs
│   │   │   └── GifDecoder.cs
│   │   ├── Encoders/
│   │   │   ├── IImageEncoder.cs
│   │   │   ├── PngEncoder.cs
│   │   │   └── JpegEncoder.cs
│   │   ├── Color/
│   │   │   └── ColorProfile.cs
│   │   ├── Metadata/
│   │   │   └── ExifHandler.cs
│   │   ├── Interop/
│   │   │   ├── LibWebp.cs               # P/Invoke signatures
│   │   │   ├── LibAvif.cs
│   │   │   ├── LibHeif.cs
│   │   │   ├── LibSpng.cs
│   │   │   └── LibJpegTurbo.cs
│   │   └── native/                      # copied to output for AOT runtime
│   ├── Settings/                        # WinUI 3 packaged desktop app
│   │   ├── Settings.csproj
│   │   ├── App.xaml{,.cs}
│   │   ├── MainWindow.xaml{,.cs}
│   │   ├── ViewModels/
│   │   │   └── SettingsViewModel.cs
│   │   ├── Services/
│   │   │   └── SettingsService.cs
│   │   └── Assets/                      # app icons
│   ├── Shared/                          # shared C# constants
│   │   ├── Shared.csproj
│   │   ├── SettingsSchema.cs            # SettingsJson record + version const
│   │   └── Paths.cs                     # LocalState path helpers
│   └── Package/                         # MSIX Windows Application Packaging Project
│       ├── Package.wapproj
│       ├── Package.appxmanifest         # sparse + FileExplorerContextMenus
│       └── Images/                      # Store tiles, Start tile, notification icons
└── tests/
    ├── Engine.Tests/                    # xUnit, tests each decoder + encoder + metadata path
    │   ├── Engine.Tests.csproj
    │   ├── DecoderTests.cs
    │   ├── EncoderTests.cs
    │   ├── ColorProfileTests.cs
    │   ├── ExifHandlerTests.cs
    │   └── fixtures/                    # sample images in each format
    │       ├── sample.webp
    │       ├── sample.avif
    │       ├── sample.heic
    │       ├── sample-animated.gif
    │       ├── sample-multipage.tiff
    │       └── sample-with-icc.jpg
    └── ShellExtension.Tests/            # isolated unit tests for pure logic
        ├── ShellExtension.Tests.vcxproj
        ├── FileFilterTests.cpp
        └── SettingsReaderTests.cpp
```

## Key Technical Decisions

**1. Native shell extension + managed engine.**
Decided: C++/WinRT for the shell extension DLL, C# .NET 10 Native AOT for the engine. Rationale: Microsoft formally discourages managed shell extensions due to CLR version clash risks inside Explorer. The engine/UI split (locked in scope) forces the shell extension to be tiny anyway, so native C++ is cheap there. The engine stays in the builder's .NET comfort zone. Tradeoff accepted: two-language build complexity in exchange for shell stability and future reuse (v2 browser extension calls the same engine via Native Messaging Host).

**2. Bundled decoders, not WIC codec extensions.**
Decided: ship libwebp + libavif + libheif + libspng + libjpeg-turbo as native DLLs in the MSIX. Rationale: WIC codec extensions require Store-installed codec packs, including a `$0.99` HEVC Video Extension for some HEIC variants — that is exactly the "online detour / install this thing" friction the product exists to eliminate. Tradeoff accepted: MSIX grows from ~1-2 MB to ~8-12 MB in exchange for zero-dependency, consistent behavior on every machine.

**3. Modern context menu only, no legacy COM registration.**
Decided: register only via `desktop4:FileExplorerContextMenus` + `IExplorerCommand`. Skip the classic Win10-style COM shell extension registration entirely. Rationale: Windows 10 is out of consumer support as of October 2025; Windows 11 is our floor. Modern menu is the default right-click experience on Win11 — the legacy "Show more options" menu is a power-user fallback. Saving the legacy registration cuts meaningful code and test surface. Tradeoff accepted: power users who press Shift+F10 / click "Show more options" won't see our commands. Deferrable to v1.1 if GitHub issues demand it.

**4. Spawn-per-invocation engine process (no daemon).**
Decided: shell extension spawns `RTClickPng.Engine.exe` fresh for every action, piped stdout for `copy`. Rationale: .NET 10 Native AOT cold start is ~50-100 ms — acceptable for a user-initiated action that happens a handful of times a day. A long-running engine daemon would add lifecycle complexity (start/stop, memory residency, crash recovery, process identity for clipboard marshaling) without meaningful perf benefit at our usage pattern. Tradeoff accepted: slightly slower first conversion than a warm daemon in exchange for stateless simplicity.

**5. Clipboard write via three formats (CF_DIBV5 + CF_DIB + CF_PNG).**
Decided: always publish the clipboard in all three formats during Copy actions. Rationale: app support varies — Figma and Photoshop prefer CF_DIBV5 for transparency, legacy apps only grok CF_DIB, Discord and modern browsers prefer raw PNG format. Publishing all three makes paste "just work" across the explicit target set (Teams, Slack, Discord, Figma, Photoshop, Outlook, Word, PowerPoint, OneNote, Paint). Tradeoff accepted: ~3× clipboard memory usage in exchange for universal paste compatibility. Delayed rendering (`SetClipboardData(..., NULL)`) keeps actual allocations to zero until the user pastes.

## Dependencies & External Services

**Native libraries (all bundled in MSIX):**
- [libwebp](https://developers.google.com/speed/webp/docs/api) — BSD-3-Clause. Google's reference implementation. WebP decode.
- [libavif](https://github.com/AOMediaCodec/libavif) — BSD-2-Clause. Alliance for Open Media reference implementation. AVIF decode.
- [libheif](https://github.com/strukturag/libheif) — **LGPL-3.0** (watch item — see Open Issues). HEIC/HEIF decode. May require a license review before distribution.
- [libspng](https://libspng.org/) — BSD-2-Clause. Lightweight PNG encode.
- [libjpeg-turbo](https://libjpeg-turbo.org/) — Modified BSD. JPEG encode.

**Microsoft platform dependencies (require Windows 11 22H2+, all included in OS):**
- [Windows App SDK 1.8](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/stable-channel) — for WinUI 3 settings app and AppNotificationManager
- [`IExplorerCommand`](https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nn-shobjidl_core-iexplorercommand) — modern context menu
- [Sparse packages + `desktop4:FileExplorerContextMenus`](https://learn.microsoft.com/en-us/uwp/schemas/appxpackage/uapmanifestschema/element-desktop4-fileexplorercontextmenus) — registration path

**External services at runtime:** **none.** No network, no telemetry, no update pings, no cloud. All processing is local. The MS Store handles app updates; we declare zero runtime network behavior.

**Microsoft Store listing:** free app, builder's existing publisher account. No in-app purchases. Privacy policy: "This app performs no network communication. No data leaves your device."

**Build-time dependencies:**
- Visual Studio 2026 with C++ and .NET 10 workloads
- Windows App SDK 1.8 VSIX
- Windows 11 SDK (26100 or later)
- vcpkg or prebuilt x64 binaries for native decoder libs (committed under `build/native/` or fetched by a bootstrap script)

## Open Issues

**License review for libheif (LGPL-3.0).**
Static linking LGPL code into a proprietary binary requires a relink path to comply with the license. For MSIX distribution with bundled DLLs (dynamic linking), LGPL is generally compatible as long as the LGPL component remains separate DLLs users can replace. **Action: confirm dynamic-linking distribution satisfies LGPL-3.0; if not, switch to a permissive-licensed HEIC decoder or drop HEIC from v1.** Blocks `/build` start for the HEIC decoder but nothing else.

**Decoder binary acquisition.**
We need prebuilt x64 DLLs for all five libraries, linked against a consistent CRT version (ideally `/MD` dynamic UCRT, matching what .NET AOT expects). Options: (a) consume via vcpkg in triplet `x64-windows`, (b) hand-build from source with a fixed CMake config, (c) find prebuilt release binaries and verify checksums. **Action: `/checklist` step picks the pipeline and commits it to `build/native/` or a bootstrap script.**

**libspng vs libpng.**
libspng is smaller, simpler, and AOT-friendly — preferred here. But libpng is the universal reference encoder with 20+ years of hardening. **Action: prototype encode with libspng; if any edge case surfaces (color management, indexed color, interlacing), fall back to libpng. Not build-blocking.**

**BMP/TIFF/GIF decoder choice.**
For these three formats, we have options: (a) pure C# (AOT-compatible but more code to write and test), (b) use WIC's built-in decoders for these specifically (they're always present in Windows, unlike HEIC/AVIF), (c) bundle libtiff/giflib. **Lean:** pure C# for BMP and GIF (small, simple formats); WIC for TIFF (non-trivial edge cases aren't worth reimplementing). **Action: decide during `/checklist` task for the Decoder scaffolding.**

**Icon design for context menu entries.**
All four verbs (Convert/Copy × PNG/JPEG) share one icon in v1. Store tile and notification icon also needed. **Action: create a single multi-size ICO for menu + Store tile set. Blocks Store submission but not local `/build`.**

**OSS license choice (MIT vs Apache 2.0).**
Both are permissive and Store-compatible. Apache 2.0 gives explicit patent grant (safer for library-like code); MIT is simpler for contributors. **Leaning: MIT.** Not build-blocking — can be finalized at any point before the first public release.

**Cloud-only OneDrive placeholder behavior.**
When the user right-clicks a OneDrive file that hasn't been materialized locally, the first read forces a synchronous download. For small files this is fine; for large files it stalls the engine invocation. **Lean:** accept default OS behavior (auto-download on read) for v1. **Action: add a GitHub issue to revisit if users complain.**

**Large file size handling.**
No hard cap in v1. Engine loads full source bitmap into memory during decode — at some multi-hundred-MB source, this becomes visible. **Action:** no pre-cap in v1; add crash-guard (catch OOM, emit error toast). If issues surface, add a configurable soft cap in a v1.1 setting.

**x64 only for v1.**
ARM64 Windows 11 devices (Surface Pro X, Copilot+ PCs) won't get native builds in v1. x64 binaries work under emulation but with a performance hit. **Action:** flag in README as known limitation; add ARM64 to v1.1 via `<Platforms>` update in all four project files (each native library lib needs ARM64 binaries too).
