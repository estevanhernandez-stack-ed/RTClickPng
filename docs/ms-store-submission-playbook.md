# Microsoft Store submission — Right Click PNG

Companion to [`Sanduhr/docs/ms-store-submission-playbook.md`](../../Sanduhr/docs/ms-store-submission-playbook.md).
Same voice, different project shape: this one ships a **shell extension** plus a
**native-AOT engine** plus a small **WPF settings window** — not a single-window
app. The submission risks are different, so the checklist is different.

Reviewer assumptions that do *not* apply here (read the Sanduhr doc for those).
What follows is only the RTClickPng-specific surface.

---

## What will get us flagged that Sanduhr didn't

### 1. `rescap:Capability Name="runFullTrust"` (restricted capability)

[`Package.appxmanifest:158`](../src/Package/Package.appxmanifest#L158) declares
the restricted full-trust capability. The Store will not ingest a `runFullTrust`
app from an unverified publisher — Partner Center must have a **verified identity**
(Individual or Company) before the first submission, and the submission questionnaire
asks *why* full trust is required.

**Justification text to paste into "Why do you need this restricted capability?":**

> Right Click PNG is a Win32 shell extension (IExplorerCommand / IContextMenu)
> hosted in `dllhost.exe` under our package identity, plus a native-AOT engine
> that decodes third-party image formats and writes files next to the source.
> Both require Win32-style filesystem access and in-process COM registration
> that the sandboxed packaged-app APIs don't expose. No network, no elevated
> privileges, no persistence beyond `ApplicationData.LocalState`.

Don't be cute about this. Reviewers see hundreds of `runFullTrust` requests a
week; precise technical justification gets approved, hand-wavy marketing copy
gets bounced.

### 2. Context menu entries count as app surface (policy 10.8)

Five verbs register across twelve file extensions — see
[`Package.appxmanifest:72-151`](../src/Package/Package.appxmanifest#L72-L151).
The Store reviews context-menu apps under a quality bar specifically because
shell extensions can spam Explorer. Ways we already pass:

- Verbs show *only* when the selection actually contains supported formats
  ([`ConvertToPngCommand.cpp` — every `GetState` calls `FileFilter::ShouldShow*`](../src/ShellExtension/ConvertToPngCommand.cpp))
- "Convert to PNG" hides when every file is already `.png` (it would be a no-op)
- JPEG verbs default off; opt-in via Settings. Reviewer-visible surface is 2 items,
  not 4, until the user asks for more
- Single "Right Click PNG →" parent label keeps our verbs grouped, not sprinkled

Reviewer-facing sentence: *"Our verbs never appear on non-image files and obey a
user-controlled visibility toggle; see settings.json → showJpegVariants."*

### 3. Native-AOT engine + vcpkg'd decoders = third-party attribution

The engine statically compiles in decoder libraries fetched via vcpkg
([`vcpkg.json`](../vcpkg.json), built into [`build/native/`](../build/native/)).
The Store's content policy wants clear attribution for every third-party library
that ships inside the MSIX.

- [`docs/licenses.md`](licenses.md) and [`docs/third-party-notices.txt`](third-party-notices.txt)
  are authoritative — verify both are current before each submission
- Partner Center → Properties → **Copyright and trademark info** field: paste
  the one-liner *"Includes third-party open-source software; see Credits in the
  app for full list."*
- In-app Credits screen is worth adding before a Store submission. For now,
  link to `docs/third-party-notices.txt` from the README. Reviewers click through.

### 4. Start-tile activation launches the Settings window, not the engine

After the 2026-04-21 fix,
[`Package.appxmanifest:36`](../src/Package/Package.appxmanifest#L36) points
`Executable` at `Settings\RTClickPng.Settings.exe`. This means clicking the
tile opens a real window — **reviewers test this**. A null-launching tile
(the shape we had during the silent-exit era) fails review.

Verify before submitting: install the MSIX, click the Start tile, confirm a
visible WPF window with a dark title bar and working toggles.

### 5. Publisher name vs MSIX `Publisher` vs cert subject

Store ingestion re-signs with Microsoft's publisher cert, BUT the `Publisher`
attribute in [`Package.appxmanifest:14`](../src/Package/Package.appxmanifest#L14)
must match Partner Center's registered identity *exactly* — case-sensitive,
punctuation-sensitive. We swapped to the Partner-Center-issued values on
2026-04-22:

- `Name`: `626LabsLLC.RightClicktoPNG`
- `Publisher`: `CN=177BCE59-0966-4975-9962-10E36652141F` (Partner-Center-issued GUID)
- `PublisherDisplayName`: `626Labs LLC`
- Package Family Name: `626LabsLLC.RightClicktoPNG_wz1chhb2h2v4a`
- Store ID: `9PKKLK6R5WFL`

The self-signed dev cert (`CN=626 Labs Dev (Self-Signed)`) will no longer sign
the production manifest because the subject mismatches the Publisher GUID.
For Store builds this is fine — we publish unsigned and let Microsoft re-sign.
For local sideload testing, regenerate the dev cert with the Publisher GUID as
its subject via [`build/create-dev-cert.ps1`](../build/create-dev-cert.ps1)
(its default was updated to match the new Publisher).

---

## Screenshots to capture

At least 5, targeted at what differentiates this app:

1. **Context menu open on a `.webp` file in Explorer** — shows the Right Click
   PNG group with Convert + Copy verbs visible. Reviewers need to see the
   primary affordance actually exists.
2. **Toast notification** immediately after "Convert to PNG" — shows the
   feedback path and that the app does what it claims.
3. **Paste target** — a screenshot of pasted PNG landing in Teams / Slack /
   Figma, proving "Copy as PNG" flow end-to-end.
4. **Settings window** (after fix) with both toggles visible, title bar in the
   branded dark caption. Proves the Start tile isn't a dead end.
5. **Batch behavior** — multi-selection context menu + the summary toast
   after the batch completes.

All captures on a fresh Win11 install at 100% scale, not your dev machine with
accumulated themes.

---

## Category and keyword draft

- **Primary category**: Productivity → Utilities & Tools
- **Secondary** (if the picker allows): Photo & Video
- **Keywords**: webp png, avif png, heic png, convert image, copy image clipboard,
  right click convert, image context menu, paste image

Short description (200 char cap):

> Convert WebP, AVIF, HEIC, JPEG, and more to PNG from the right-click menu —
> or copy straight to the clipboard, ready to paste into Teams, Slack, Figma.
> No uploads, no accounts.

---

## Notes-to-Publisher boilerplate (first submission)

```text
Right Click PNG is a Win32-identity shell extension packaged as MSIX.
Architecture brief:
- src/ShellExtension (C++/COM) — IExplorerCommand verbs hosted in dllhost
  under our package identity.
- src/Engine (C# / NativeAOT) — out-of-process decoder/encoder, launched
  per-verb via CreateProcessW.
- src/Settings (C# / WPF) — Start-tile and context-menu-accessible settings UI.
  (Background note: an earlier submission attempt silent-exited from a
  globalization config issue; fixed in the committed build — the Start tile
  now opens the window reliably. Repro + fix in git.)

No network calls, no account system, no telemetry. Full third-party
attribution in docs/licenses.md and surfaced in-app via About.

runFullTrust is required because the shell extension registers COM in-proc
and the engine uses Win32 file APIs on source-file directories. No elevated
privileges or broadFileSystemAccess requested.
```

Tone: precise, technical, volunteers the one prior-known-issue instead of
hiding it. Reviewers trust honest submissions faster.

---

## Common gotchas specific to this project

1. **Shell extension DLL must be inside the package, path-matching the manifest.**
   [`Package.appxmanifest:57`](../src/Package/Package.appxmanifest#L57) expects
   `ShellExtension\ShellExtension.dll`. The wapproj drops it there via the
   `ProjectReference` to `ShellExtension.vcxproj`; if you ever rename the
   output folder, CLSIDs won't resolve and every verb silently hides.
2. **`PackageFamilyName` is computed from the cert publisher subject.** The
   current value `626LabsLLC.RightClicktoPNG_wz1chhb2h2v4a` is baked into
   [`src/Shared/Paths.cs:14`](../src/Shared/Paths.cs#L14) and the
   [`SettingsReader.cpp:46`](../src/ShellExtension/SettingsReader.cpp#L46)
   path. If the Partner Center publisher identity ever changes, both files
   need to update in lockstep or the settings file and the shell extension
   will disagree about where `settings.json` lives.
3. **`InvariantGlobalization` is per-project, not repo-wide.** The AOT Engine
   sets it in [`src/Engine/Engine.csproj:19`](../src/Engine/Engine.csproj#L19),
   the Settings WPF app explicitly disables it in
   [`src/Settings/Settings.csproj:15`](../src/Settings/Settings.csproj#L15).
   Don't "clean up" by moving either into `Directory.Build.props` — WPF + invariant
   globalization crashes the process before any user code runs (fixed 2026-04-21).
4. **CI builds with `windows-latest` images that include MSBuild + .NET 9 +
   VS Build Tools.** Local builds need the same — see next section.
5. **`.msix` from CI is signed with our self-signed cert.** For a Store
   submission you upload an **unsigned** `.msix` (or `.msixupload`). Strip the
   signature step from the submission-ready pipeline, or use a separate
   `Release-Store` configuration that sets `AppxPackageSigningEnabled=false`.

---

## Reference artifacts from this project

- **MSIX build output** — `src/Package/AppPackages/Package_0.1.0.0_x64_Test/Package_0.1.0.0_x64.msix`
- **Manifest** — [`src/Package/Package.appxmanifest`](../src/Package/Package.appxmanifest)
- **Third-party attribution** — [`docs/licenses.md`](licenses.md),
  [`docs/third-party-notices.txt`](third-party-notices.txt)
- **Settings schema (published in listing)** — [`docs/SETTINGS.md`](SETTINGS.md)
- **CI build definition** — [`.github/workflows/build.yml`](../.github/workflows/build.yml)
- **Sanduhr's playbook (general patterns)** —
  [`../../Sanduhr/docs/ms-store-submission-playbook.md`](../../Sanduhr/docs/ms-store-submission-playbook.md)

---

## Building the MSIX locally (alternative to CI)

As of 2026-04-21 this machine can build the full package end-to-end without
pushing to GitHub. Prerequisites already installed:

- **Visual Studio 2022 Build Tools** at
  `C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools`
  — provides MSBuild, the C++ toolset for ShellExtension.vcxproj, and the
  Desktop Bridge / MSIX packaging targets.
- **.NET 9 SDK** on PATH (`dotnet --version` → `9.0.x`).
- **vswhere.exe** at `C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe`.
  NativeAOT invokes this to find the MSVC toolset; not on PATH by default, so
  the script below prepends it.
- **vcpkg + native decoders** already built at `build/native/` (one-time
  ~30–60 minutes; re-runs are cached). See [`build/fetch-native.ps1`](../build/fetch-native.ps1).

**One-shot local build (PowerShell):**

```powershell
$vswhere = 'C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe'
$env:PATH = "$(Split-Path $vswhere);$env:PATH"

$msbuild = 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe'

# Restore first (NuGet + vcpkg)
& $msbuild src\Package\Package.wapproj -t:Restore -p:Configuration=Release -p:Platform=x64

# Full build (C++ shell extension → AOT engine → WPF settings → MSIX)
& $msbuild src\Package\Package.wapproj -t:Build -p:Configuration=Release -p:Platform=x64 -p:AppxBundlePlatforms=x64
```

Output lands in `src/Package/AppPackages/Package_<version>_x64_Test/`.

**CI equivalence.** [`.github/workflows/build.yml`](../.github/workflows/build.yml)
runs the same `msbuild … Package.wapproj` invocation inside `windows-latest` with
the same toolset. If a build passes here it should pass there — the one
non-obvious divergence is `vswhere` being pre-on-PATH in GitHub runners.

**When to use which.** Local for iteration, rejection-fix turnaround, and
one-off store submissions. CI for verified-source-of-truth builds and the
actual Release artifact you upload to Partner Center.

---

## Lessons from the 2026-04-22 submission cycle

Submitted 2026-04-22 ~15:00 CST, certified and live by 2026-04-22 ~15:15 CST.
Idea-to-live: ~24 hours. Seven landmines we hit along the way that I didn't
know before this cycle; preserving them here so the next 626Labs app skips
them.

### 1. `InvariantGlobalization=true` silently kills WPF on first text layout

**Where it surfaced.** WPF Settings window silent-exited before any user code
ran. No WER, no crash dump, no log — because the crash was in `MS.Internal.FontCache.MajorLanguages..cctor()`
creating `new CultureInfo("en")` during the first `TextBlock` measure pass,
and the process was spawned by `dllhost.exe` with no attached console.

**Root cause.** `Directory.Build.props` set `<InvariantGlobalization>true</InvariantGlobalization>`
at the repo level — correct for the AOT Engine (size + no ICU dep), fatal for
WPF (needs real cultures for text-layout typography checks).

**Rule.** `InvariantGlobalization` is per-project, not repo-wide. If a repo
mixes AOT native-code projects with WPF / WinUI / any UI that touches text
layout, the UI project MUST explicitly override:

```xml
<InvariantGlobalization>false</InvariantGlobalization>
```

**Faster diagnosis next time.** When a packaged .NET WinExe silent-exits
with no WER, run the unpackaged `.exe` from `cmd.exe` — stderr with the
full stack trace surfaces instantly.

### 2. `EntryPointProjectUniqueName` in `.wapproj` silently overrides the manifest `Executable`

**Where it surfaced.** Tile clicks launched the wrong executable (the AOT
console engine, which exited immediately with usage text). Looked like a
crash because the window flashed and disappeared; no crash dump because
it was a clean exit with a non-zero code.

**Root cause.** MSBuild's MSIX packaging step rewrites
`Application/@Executable` in the built `AppxManifest.xml` using whatever
project is named in `<EntryPointProjectUniqueName>`. Whatever the source
manifest says is ignored.

**Rule.** `<EntryPointProjectUniqueName>` must point at the project that
provides your tile-launched entry point, not just "a project that's
involved in the package." Cross-check by `grep Executable=` in the built
manifest after a build.

### 3. `<Content>` packaging doesn't auto-stage into the loose layout

**Where it surfaced.** `Add-AppxPackage -Register` against the loose
build layout failed to render tile icons and splash because the `Assets\`
folder wasn't next to `AppxManifest.xml`. The MSIX had them; the loose
layout didn't.

**Rule.** `<Content Include="Assets\**\*.png" />` packs into the MSIX
but doesn't stage for `Register`. Add:

```xml
<Content Include="Assets\**\*.png">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</Content>
```

### 4. `DisplayName` in manifest must *exactly* match the Partner-Center-reserved name

Case-sensitive, whitespace-sensitive. Our reservation was
**"Right Click to PNG"** (with the "to"); the manifest said
**"Right Click PNG"**. Rejection: `Package/Properties/DisplayName`
mismatch.

**Trick for brand consistency.** Use the formal reservation as `DisplayName`
(required), and add `ShortName="Your Friendly Name"` to `uap:DefaultTile`
so the actual Start tile reads the friendly version. The only place users
ever see the formal name is the Store listing header.

### 5. MSIX `Identity.Version` uniqueness is *global across all uploads* — even rejected ones

You can't re-upload a corrected package at the same version as a rejected
one. Every attempt consumes the (Name, Version, Architecture) slot. Bump
the version on every resubmission, even if the bits change <1 KB.

### 6. MSIX `Identity.Version` revision digit must be `0`

Store policy: `X.Y.Z.0` required. The revision (4th) digit is reserved
for OEM / internal use; Store rejects anything else. So the bump ladder
for rebuilds is the **build digit**:

```text
0.1.0.0  →  0.1.1.0  →  0.1.2.0  →  0.2.0.0
```

The revision digit stays `.0` forever.

### 7. `Add-AppxPackage -AllowUnsigned` fails without the "unsigned namespace" in Publisher

When the manifest Publisher is a production-format CN (especially a GUID
from Partner Center), Windows refuses the `-AllowUnsigned` install with
HRESULT `0x80073D2C` because the Publisher isn't in the reserved unsigned
namespace that `AppxPackageSigningEnabled=false` is supposed to add.

**Workaround for developer-mode sideload against a production-identity
manifest.** Use `Add-AppxPackage -Register <path-to-AppxManifest.xml>`
against the loose build layout. `-Register` bypasses signature verification
entirely — same iteration loop, no cert dance.

### 8. "Classic context menus" tweak silently eats IExplorerCommand groupings

**Where it surfaced.** After install from the Store on a second PC, the
user saw only `Right Click PNG settings…` as a standalone legacy-menu
entry; the full `Right Click PNG →` submenu with Convert/Copy verbs never
appeared. Installation was healthy (`Get-AppxPackage` Status `Ok`, signed
by Store), no events in the logs — the shell extension was registered and
functional, Windows just wasn't surfacing it.

**Root cause.** The reg key
`HKCU\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}` (with
empty default value) forces Windows 11 back to the classic/legacy
context menu as the default on every right-click. Our
`desktop4:FileExplorerContextMenus` verbs register as `IExplorerCommand`,
which Win11 surfaces cleanly in the **modern** short menu but bridges
inconsistently to the legacy menu — grouped submenus get flattened,
sometimes only one verb of the group appears.

Commonly planted by PowerToys' "Classic Context Menus" toggle, Win10→Win11
migration tools, and "make Windows 11 look like Windows 10" utilities.

**Diagnosis.**

```powershell
Test-Path 'HKCU:\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}'
```

`True` means classic-menu-forced. Remove the key and restart Explorer:

```powershell
Remove-Item 'HKCU:\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}' -Recurse -Force
Stop-Process -Name explorer -Force
```

**Why this matters for the playbook.** Any future 626Labs app that relies
on `IExplorerCommand` verb grouping will show the same symptom on machines
with this tweak. Surface the fix in the app's README under Troubleshooting
before the first support ticket arrives.

### Honorable mentions

- **Partner Center's "short name" field** is cosmetic, not the DisplayName
  constraint. They're different fields with different rules.
- **Different `Identity.Name` → different package.** Windows considers
  `OldLabs.OldName` and `NewLabs.NewName` two separate MSIX packages even
  if they declare the same shell extension CLSIDs. Uninstall the old one
  before registering the new one or you get duplicated context-menu verbs.
- **Notes-to-reviewers is underrated.** A tight architecture brief + a
  60-second review path + honest disclosure of any known quirks appears
  to compress certification wall-clock dramatically. Worth the 30 minutes
  of writing time every submission.
