# ShellExtension (C++/WinRT)

The Windows File Explorer context-menu extension.  Implements `IExplorerCommand` and ships as an
in-proc DLL hosted in dllhost.exe by the modern File Explorer context menu (Win11 22H2+).

## Build requirements

- Visual Studio 2022 17.10+ with:
  - **Desktop development with C++** workload
  - **Universal Windows Platform development** workload (for MSIX tooling)
  - C++/WinRT component (installed by default with Desktop workload)
  - Windows 11 SDK **10.0.26100.0**
  - Windows App SDK 1.8 (VSIX)
- Build target: `WindowsTargetPlatformMinVersion=10.0.22621.0`, `Platform=x64` only.

## Files

- `dllmain.cpp` — COM class factories + DLL exports (`DllGetClassObject`, `DllCanUnloadNow`).
- `ShellExtension.def` — controls which symbols are exported.
- `ExplorerCommandBase.h` — shared `IExplorerCommand` + `IObjectWithSite` helpers.
- `ParentCommand.{h,cpp}` — the single registered "Right Click to PNG" verb; serves the five commands as a submenu, carries the icon.
- `SubCommandOrder.h` — submenu order, COM-free so the test runner can pin it.
- `ConvertToPngCommand.{h,cpp}` — the five verb commands (Convert/Copy as PNG, Convert/Copy as JPEG, Settings…).
- `FileFilter.{h,cpp}` — extension allowlist and the per-verb visibility predicates.
- `SettingsReader.{h,cpp}` — reads `%LOCALAPPDATA%\Packages\<PFN>\LocalState\settings.json` per invocation.
- `ShellExtension.rc`, `resource.h`, `RightClickPng.ico` — the parent verb's icon (resource 101), rendered by `src/Package/Assets/render-brand-icons.ps1`.

## CLSIDs (per spec)

| Verb | CLSID | Role |
|---|---|---|
| RightClickPng (parent) | `{582E2707-2A81-491E-986F-145208BF87D2}` | Only verb registered per file type; submenu host |
| ConvertToPng  | `{68730B57-152E-4BC9-A158-3A03EE03465A}` | Submenu item 1 |
| CopyAsPng     | `{D1407350-4611-4621-802E-475C82006858}` | Submenu item 2 |
| ConvertToJpeg | `{DC177957-7F24-486D-B4CB-D69FA4580C8F}` | Submenu item 3 (JPEG variants on) |
| CopyAsJpeg    | `{7C7F2F20-9F1E-4F0F-9FA8-D404369C2C07}` | Submenu item 4 (JPEG variants on) |
| OpenSettings  | `{AB12D5C0-3E4F-4B1A-9E88-5C3D7F91AA01}` | Submenu item 5, "Settings…" |

The five submenu CLSIDs stay registered in the manifest even though the parent instantiates them directly. CLSIDs are permanent ABI — never reuse or renumber.

## Testing the DLL in isolation

Shell extensions are hosted by Explorer; you can't run them directly.  The supported dev flow is:

1. Build the MSIX: `msbuild src/Package/Package.wapproj /p:Configuration=Release /p:Platform=x64`
2. Install it: `Add-AppxPackage -Path <path-to-msix>`
3. Right-click an image in File Explorer to see the verbs.
4. To iterate on a code change: uninstall (`Get-AppxPackage 626LabsLLC.RightClicktoPNG | Remove-AppxPackage`),
   rebuild, re-install.  Explorer unloads the DLL automatically on MSIX uninstall.
