# Right Click PNG — Product Requirements

## Problem Statement

Modern web browsers save images in formats — `.webp`, `.avif`, `.heic`, `.heif` — that many desktop apps, document editors, messaging clients, and design tools still refuse to open or paste. The user's workaround is an online converter, a third-party image app, or a manual extension rename that may or may not work. The gap between "I have the image" and "I can use the image" is measured in minutes of friction, per image, dozens of times a week. A secondary pain compounds this: even when the saved file is in a workable format, users hunt through `Downloads` or `Screenshots` folders to locate it before they can paste it into the app where they actually need it. Right Click PNG eliminates both detours with two native Windows 11 context-menu actions: **Convert to PNG** (save a `.png` next to the original) and **Copy as PNG** (put PNG bytes on the clipboard, paste-anywhere ready).

## User Stories

### Epic 1 — Convert to PNG (file → file)

- **As a user who just saved a `.webp` from the web, I want to right-click it and save a `.png` next to it, so I can use it in apps that reject `.webp`.**
  - [ ] Right-clicking a supported image file in File Explorer shows "Convert to PNG" in the context menu
  - [ ] Clicking "Convert to PNG" produces a file with the same basename + `.png` extension in the same folder as the original
  - [ ] Typical web images (<5 MB) convert in under one second; the user perceives the action as instant
  - [ ] A non-blocking Windows toast confirms the action: *"Converted image.webp → image.png"* (path included, clickable to reveal in Explorer)
  - [ ] The original source file is not modified, moved, or deleted
  - [ ] If a file with the target name already exists and "Confirm before overwrite" is ON (default), a confirmation prompt appears; if OFF, the existing file is overwritten silently
  - [ ] Output PNG preserves ICC color profile from the source when present
  - [ ] Output PNG strips EXIF metadata (orientation is applied to pixel data before strip, so rotation isn't lost)
  - [ ] If the source is `.gif`, only the first frame is converted; no warning is shown
  - [ ] If the source is already `.png`, "Convert to PNG" does not appear (redundant)

- **As a user converting multiple images, I want to select many files at once and convert them all in one action, so I don't have to repeat the same click per file.**
  - [ ] Selecting N files (N ≥ 2) and right-clicking shows "Convert to PNG" if at least one file in the selection is a supported input format
  - [ ] Clicking "Convert to PNG" processes every supported file in the selection; unsupported files in the selection are silently skipped (not errored)
  - [ ] The operation runs asynchronously — File Explorer remains responsive during conversion of up to 100 images
  - [ ] A summary toast fires when the batch completes: *"Converted 47 files. 3 failed."* (clicking reveals the list of failures)
  - [ ] Failures do not halt the batch — each file is independent
  - [ ] Per-file overwrite behavior follows the same setting as single-file (prompt once per file, or silent, depending on setting)

### Epic 2 — Copy as PNG (file → clipboard)

- **As a user about to paste an image into Teams/Figma/Slack, I want to right-click the source file and copy it as PNG to my clipboard, so I can paste without hunting for the file or opening an image editor.**
  - [ ] Right-clicking a single supported image file in File Explorer shows "Copy as PNG"
  - [ ] The action also appears for `.png` source files (the workflow — paste without opening the file — still applies)
  - [ ] Clicking "Copy as PNG" places PNG-encoded bytes on the Windows clipboard in multiple formats (`CF_DIB` for legacy apps, raw `PNG` for modern apps, `CF_DIBV5` for apps that handle transparency)
  - [ ] Pasting into the following apps produces a visible image: Microsoft Teams, Slack, Discord, Figma, Adobe Photoshop, Outlook (email body), Word, PowerPoint, OneNote, Paint, Snipping Tool
  - [ ] A non-blocking toast confirms: *"Copied as PNG — ready to paste"*
  - [ ] For source files other than `.png`, ICC profile is preserved when the clipboard format supports it; EXIF is stripped
  - [ ] Typical web image (<5 MB) reaches the clipboard in under one second
  - [ ] If the source is `.gif`, first frame is used; no animation is placed on clipboard

- **(Cut for v1, kept as future story) As a user with multiple files selected, I want a way to copy them all to the clipboard as pastable PNGs.**
  - Not implemented in v1. "Copy as PNG" is hidden when the selection contains more than one file. Rationale: clipboard semantics for multi-image paste vary wildly by target app; single-image is the clean contract.

### Epic 3 — Supported input formats

- **As a user, I want the Right Click PNG menu items to appear only on files the tool can actually process, so my context menu isn't cluttered with options that would fail.**
  - [ ] "Convert to PNG" appears on files with these extensions: `.webp`, `.avif`, `.heic`, `.heif`, `.bmp`, `.tiff`, `.tif`, `.gif`
  - [ ] "Copy as PNG" appears on all of the above PLUS `.png` (the only format where Convert is redundant but Copy still valuable)
  - [ ] Neither action appears on `.svg`, `.jxl`, `.cr2`, `.nef`, `.dng`, or other unsupported image formats (these are explicitly cut from v1)
  - [ ] Neither action appears on non-image files (`.txt`, `.mp4`, `.exe`, `.zip`, etc.)
  - [ ] Determination is based on file extension, not by opening the file — menu rendering must not incur I/O latency
  - [ ] If the JPEG-variants setting is ON, the set expands: "Convert to JPEG" and "Copy as JPEG" appear on the same supported inputs PLUS `.png` (where JPEG conversion is meaningful); on `.jpg`/`.jpeg` sources, Convert-to-JPEG is hidden (redundant) but Copy-as-JPEG remains

### Epic 4 — Settings companion window

- **As a user who prefers JPEG, I want to enable "show JPEG variants" so my context menu shows both PNG and JPEG actions.**
  - [ ] Settings window is accessible from Start menu (app icon → "Right Click PNG")
  - [ ] Settings window is a Fluent/WinUI 3 window; lightweight, single page, no tabs
  - [ ] Toggle labeled "Show JPEG variants in context menu" (default OFF)
  - [ ] Toggle change takes effect within 2 seconds, no Explorer restart needed
  - [ ] When ON, context menu shows up to four actions depending on source format (Convert/Copy × PNG/JPEG)

- **As a user who worries about overwriting files I care about, I want to toggle overwrite confirmation on/off.**
  - [ ] Toggle labeled "Confirm before overwrite" (default ON)
  - [ ] When ON, every conversion that would overwrite an existing file prompts (Yes / Yes to All / No / Cancel)
  - [ ] When OFF, overwrites happen silently with no prompt

- **As a user who changes my mind, I want the settings window to respect my Windows theme.**
  - [ ] Settings window respects Windows light/dark theme at launch
  - [ ] Theme changes applied while the settings window is open take effect immediately
  - [ ] Typography and spacing follow Windows 11 Fluent design guidelines

- **As a user, I want my settings to persist across app restarts, updates, and machine restarts.**
  - [ ] Settings persist across app close, restart, Windows reboot
  - [ ] Settings persist across MSIX app updates
  - [ ] Settings reset to defaults only on full uninstall + reinstall

### Epic 5 — Install, first run, notifications

- **As a new user, I want to install from the Microsoft Store and have the right-click actions work immediately.**
  - [ ] Installing from Microsoft Store registers the shell extension without requiring administrator elevation
  - [ ] Context menu items appear on the next right-click after install — no reboot, no Explorer restart required
  - [ ] First install does NOT open a welcome dialog — the user discovers the tool via right-click (invisible utility on first launch)
  - [ ] Context menu registrations appear in both the Windows 11 modern context menu and the Windows 10 legacy "Show more options" menu

- **As a user who just clicked Convert or Copy, I want a confirmation I can trust without being interrupted.**
  - [ ] All confirmations are Windows toast notifications (Action Center / notification flyout) — non-modal, dismissible, stack
  - [ ] Toasts never steal focus or block the active window
  - [ ] Toast for Convert includes "Show in folder" action; toast for Copy includes no action (the user is about to paste)

- **As a user uninstalling, I want the menu entries to disappear cleanly.**
  - [ ] Uninstall via Settings → Apps → Right Click PNG → Uninstall removes the shell extension registration
  - [ ] Context menu entries are gone on the next right-click after uninstall (no reboot required)
  - [ ] Uninstall does not remove user files or modify any images

- **As a user whose app updates in the background, I want updates to happen without losing my preferences.**
  - [ ] MSIX auto-update from the Microsoft Store preserves all user settings
  - [ ] Updates do not re-prompt for any first-run state (there isn't any)
  - [ ] Updates do not restart Explorer

## What We're Building

Everything in the User Stories section above is in-scope for v1. The build is complete when:

1. **Functional:** All acceptance checkboxes above pass manual verification on a clean Windows 11 install and a Windows 10 install (minimum supported build: Windows 10 version 1809, per MSIX sparse-package requirements — TBD in /spec if we raise the floor).
2. **Distributed:** Available on the Microsoft Store, free, under the builder's publisher account. Listing copy, icons, and screenshots in place.
3. **Open-sourced:** Public GitHub repository with README, license file, build instructions, contribution guide, and issue templates. Release pipeline (MSIX → Store) documented.
4. **Auditable:** Zero network calls at runtime, verifiable by both static inspection of source and runtime monitoring (Wireshark or Process Monitor confirms no outbound connections).

## What We'd Add With More Time

Not v1 — candidates for v1.1, v2, or a future `/iterate` pass:

- **Global hotkey for clipboard PNG** — e.g., `Win+Shift+V` grabs the most recent screenshot or selected image and places a PNG on the clipboard, sidestepping the right-click path entirely. (Currently cut because the right-click flow already dissolves the primary pain.)
- **Custom output naming patterns** — timestamp prefixes, counters, `{original}_converted.png` templates.
- **Batch progress UI** — a small progress window for selections larger than ~20 files, with per-file status.
- **Additional input formats** — SVG (requires render-size decision), JPEG XL (gaining adoption), RAW camera formats (`.cr2`, `.nef`, `.dng` — large surface, camera-specific).
- **Additional output formats** — WebP (sometimes users want this), PDF single-image.
- **Output destination options** — always save to a specific folder, prompt for folder per-invocation, send to Downloads.
- **Drag-and-drop conversion** — drag a file onto the tray icon / settings window to convert.
- **Command-line interface** — same engine, invokable from PowerShell for scripted workflows.
- **Browser extension companion (separate ship)** — a WebExtension that uses a Native Messaging Host to invoke the same conversion engine, bringing Right Click PNG into browser right-click menus. Enabled by the engine/UI split in v1's architecture.

## Non-Goals

Explicitly OUT of this project, with rationale:

- **Image editor features (resize, crop, rotate, color adjust).** PowerToys Image Resizer already owns resize with a clean pattern; we are a converter, not a mini-editor. Feature scope stays surgical.
- **Non-image file conversion (docs, video, audio, PDFs).** File Converter (the OSS kitchen-sink competitor) demonstrates how this path leads to mediocrity at everything. Single-purpose is the identity.
- **In-browser right-click integration.** A browser extension is a fundamentally different product — different distribution, different platform rules, different security model. Reserved as a potential future companion product that reuses this engine, not bundled here.
- **Any network calls at runtime.** No telemetry, no update pings, no cloud conversion, no crash reporting that leaves the machine. Trust is load-bearing for a tool that reads files and touches the clipboard.
- **File association hijacking.** We never register as default opener for `.webp`, `.avif`, `.heic`, or any other format. The user's default viewer remains unchanged by install/uninstall.
- **First-run welcome or onboarding flow.** Invisible utility on install; discoverability via right-click. No walkthrough, no popup, no notification on first launch.

## Open Questions

Unresolved items — flagged with whether they block `/spec` or can wait.

- **Stack decision** — C# + Windows App SDK + WinUI 3 (builder's .NET comfort zone, Microsoft's current recommended path) vs. Rust/C++ for the shell extension layer if .NET shell-extension friction proves prohibitive. **Resolve in /spec.**
- **Minimum Windows version** — MSIX sparse-package requires Windows 10 version 2004 or later (build 19041) for the modern context menu registration via `IExplorerCommand`. Are we OK cutting Windows 10 pre-2004 users? **Resolve in /spec.** (Builder's leaning: yes, 2020-era Windows 10 floor is reasonable for a 2026 tool.)
- **OSS license** — MIT (maximum permissive) vs. Apache 2.0 (permissive + explicit patent grant) vs. something else. **Resolve in /spec or later — not build-blocking.**
- **Cloud-only file behavior** — What happens when the user right-clicks a OneDrive file that's a placeholder (not materialized locally)? Auto-trigger download and then convert? Skip with a clarifying toast? Error? **Can wait until build; default to auto-download via standard Windows file-read semantics unless proven problematic.**
- **Large file handling upper bound** — At what source file size do we warn or refuse? 100 MB? 500 MB? Does it depend on format (HEIC burst images can be surprisingly large)? **Can wait until build — log an observed threshold from real usage and tune.**
- **Decoder library choice** — Windows Imaging Component (WIC) provides native decoders for many but not all of our target formats. `.avif` and `.heic` support in WIC depends on user-installed HEIF/AV1 codecs. Do we ship with our own decoders bundled, or assume WIC + Microsoft's HEIF/AV1 Codec extensions are available? **Resolve in /spec — affects packaging size and MS Store dependency declarations.**
- **Icon design** — single icon representing "right-click to PNG" for both context menu actions and the Start menu tile. **Resolve before store submission — not build-blocking.**
- **Telemetry-free crash reporting** — do we want ANY way to see that a user hit a crash (e.g., via GitHub issue templates that link from a local crash log)? Or is the rule truly "no feedback channel from runtime to developer"? **Can wait — default to "no runtime feedback, users report via GitHub."**
