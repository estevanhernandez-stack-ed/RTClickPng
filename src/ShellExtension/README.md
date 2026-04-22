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
- `ConvertToPngCommand.{h,cpp}` — item 5 placeholder.  Three more handlers arrive in item 6.
- `FileFilter.{h,cpp}` — extension allowlist; real visibility logic in item 6.
- `SettingsReader.{h,cpp}` — reads `%LOCALAPPDATA%\Packages\<PFN>\LocalState\settings.json` per invocation.

## CLSIDs (per spec)

| Verb | CLSID | Status |
|---|---|---|
| ConvertToPng  | `{68730B57-152E-4BC9-A158-3A03EE03465A}` | item 5 (placeholder) |
| CopyAsPng     | `{D1407350-4611-4621-802E-475C82006858}` | item 6 |
| ConvertToJpeg | `{DC177957-7F24-486D-B4CB-D69FA4580C8F}` | item 6 |
| CopyAsJpeg    | `{7C7F2F20-9F1E-4F0F-9FA8-D404369C2C07}` | item 6 |

CLSIDs are permanent ABI — never reuse or renumber.

## Testing the DLL in isolation

Shell extensions are hosted by Explorer; you can't run them directly.  The supported dev flow is:

1. Build the MSIX: `msbuild src/Package/Package.wapproj /p:Configuration=Release /p:Platform=x64`
2. Install it: `Add-AppxPackage -Path <path-to-msix>`
3. Right-click an image in File Explorer to see the verbs.
4. To iterate on a code change: uninstall (`Get-AppxPackage 626LabsLLC.RightClicktoPNG | Remove-AppxPackage`),
   rebuild, re-install.  Explorer unloads the DLL automatically on MSIX uninstall.
