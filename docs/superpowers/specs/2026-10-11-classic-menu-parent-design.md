# Classic menu: one icon'd parent verb

**Date:** 2026-10-11 · **Issue:** [#2](https://github.com/estevanhernandez-stack-ed/RTClickPng/issues/2) · **Ships in:** 0.1.2.0, with the crash-log fix (PR #4)

## Goal

In the classic "Show more options" menu, Right Click PNG's verbs are flat, have no icon, and other apps' entries land between them. After this change both menus show one **Right Click to PNG** entry, with the app icon, holding a submenu of our verbs. The modern menu must look at least as good as it does today.

## What the spike settled

A throwaway build on `spike/classic-menu-parent` (local, unpushed) was checked by eye on Win11 on 2026-10-11:

- One parent `IExplorerCommand` returning `ECF_HASSUBCOMMANDS` renders as a single flyout in **both** menus. In the classic menu it's grouped, and it sits higher than the old loose verbs.
- The child commands' `GetState` still decides visibility per item. Copy as PNG hides on a multi-selection.
- `Invoke` reaches the children through the submenu: Convert to PNG on a `.heic` wrote the `.png`.
- **No icon in either menu.** The spike pointed `GetIcon` at `Settings.exe,0`, which has no icon resource. The modern menu also lost the package-logo icon it showed while there were five separate verbs. **A real icon is required, or the change makes the modern menu worse.**

## Design

### Registration (`Package.appxmanifest`)

- A new permanent CLSID for the parent: `582E2707-2A81-491E-986F-145208BF87D2` (minted for the spike; kept).
- Each of the 11 `desktop4:ItemType` blocks registers exactly one verb, `Id="RightClickPng"`, pointing at the parent CLSID.
- All five existing `com:Class` entries **stay registered**. They're permanent ABI, and keeping them costs nothing. The parent instantiates the child commands directly with `Make<>` and never goes through COM, so these entries become dormant but stay reserved.

### Parent command (`ShellExtension`)

| Method | Behavior |
|---|---|
| `GetTitle` | `Right Click to PNG` (the Partner Center `DisplayName`; the old modern-menu group label) |
| `GetIcon` | `<dir of ShellExtension.dll>\ShellExtension.dll,-101` (resolved via `g_hModule`) |
| `GetFlags` | `ECF_HASSUBCOMMANDS` |
| `GetState` | Visible when `FileFilter::AllSupported(selection)`, otherwise hidden. Same gate the settings verb uses today. |
| `EnumSubCommands` | A fresh `IEnumExplorerCommand` over the five child commands, in the order below |
| `Invoke` | `E_NOTIMPL`. A parent with subcommands is never invoked. |

The parent lives in its own `ParentCommand.{h,cpp}` with its enumerator rather than growing `ConvertToPngCommand.cpp` further. `dllmain.cpp` gets one more factory branch.

### Submenu

Flat list, no separator:

1. Convert to PNG
2. Copy as PNG
3. Convert to JPEG (only when *Show JPEG variants* is on)
4. Copy as JPEG (only when *Show JPEG variants* is on)
5. **Settings…** (renamed from "Right Click PNG settings…"; the parent now supplies the app name)

There's no separator because the default list is three items, the "…" already marks settings as different from the actions, and modern-menu rendering of `ECF_ISSEPARATOR` is unverified. Revisit if the five-item list feels crowded.

The order lives in one place: a `constexpr` array of child kinds in `SubCommandOrder.h`, with no COM dependencies, so the C++ test runner can pin it without building any COM objects.

### Icon

- `render-brand-icons.ps1` gains an `.ico` output: `src/ShellExtension/RightClickPng.ico`, at 16, 20, 24, 32, 40 and 48 px. It uses the same `DrawGlyph` as the Store tiles. The PNG frames are embedded in the ICO container, which the shell supports on Win11.
- `ShellExtension.rc` embeds it as resource `101`, and the vcxproj gains a `ResourceCompile` item.
- The generated `.ico` is committed, the same way the tile PNGs are, so CI doesn't run the renderer.

## Error handling

All new methods are `noexcept` and return HRESULTs, like the existing commands. If `GetIcon` can't resolve the module path, it returns `E_NOTIMPL` and the menu shows no icon, which is today's behavior. Any failed `Make<>` makes `EnumSubCommands` return `E_OUTOFMEMORY`, and the shell then shows no submenu rather than a broken one.

## Testing

- **C++ runner:** new cases in `tests/ShellExtension.Tests/` pin `SubCommandOrder` (five entries, in order, settings last). The `FileFilter` matrix is untouched and must stay green.
- **xUnit:** unchanged, 43/43.
- **Manual, on the dev install,** in both menus:
  - single `.webp`, `.avif`, `.heic`, with the JPEG variants off and on
  - a multi-selection: Convert only, with no Copy
  - `.png`: no Convert to PNG; Copy as PNG and Settings present
  - the icon renders in both menus, in light and dark mode
  - Settings… opens the settings window
- **Upgrade path, with the real package (required).** Loose `-Register` installs run from the repo's `bin\`, not `WindowsApps`, and from the build output rather than the `.msix` contents. So the upgrade check uses signed `.msix` files:
  1. Sign with the dev cert from `build/create-dev-cert.ps1` (subject = the production publisher CN; one-time, elevated, trusts the cert under LocalMachine\Root).
  2. Remove the dev install. Build `main` (0.1.1.0, five verbs) as a signed `.msix` and install it with `Add-AppxPackage`. Confirm it lands under `C:\Program Files\WindowsApps\` and shows the old five-verb menu.
  3. Install the signed 0.1.2.0 `.msix` over it as an in-place update, while Explorer is running and holding the DLL. Record whether the update needs `-ForceApplicationShutdown` or an Explorer restart; that becomes the README / what's-new note owed by the Store-submission task.
  4. After restarting Explorer, run the manual matrix above against the `WindowsApps` install. Confirm there are no leftover loose verbs and the icon renders.

  A Store-to-Store upgrade could only be fully proven by a Partner Center package flight. That was declined on 2026-10-11: sideload and Store updates use the same deployment engine for the verb change, and the remaining update-while-running failure gets a README note either way.

## Docs to update

`docs/spec.md` and `src/ShellExtension/README.md` (CLSID table, plus the parent), `README.md` ("What it does" and the classic-menu troubleshooting entry), `CHANGELOG.md`, and `CLAUDE.md` (the CLSID gotcha now includes "and `ParentCommand`'s enumerator"). Check the Store screenshot `01-right-click-menu.png` against the new menu, and recapture it only if it visibly differs.

## Out of scope

- A separator, or any new verbs.
- Retiring the five dormant `com:Class` registrations.
- The 0.1.2.0 version bump and Store submission. Those follow once this and PR #4 merge.

## Cleanup

Delete the local `spike/classic-menu-parent` branch once this merges.
