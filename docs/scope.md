# Right Click PNG (RTClickPng)

## Idea

A Windows shell extension that adds two actions to the right-click menu of image files — **Convert to PNG** (save a `.png` next to the original) and **Copy as PNG** (put PNG bytes on the clipboard, paste-anywhere ready). Zero online converters. Zero workflow detours. Free on the Microsoft Store. Open source.

## Who It's For

**Primary user:** anyone who saves images off the modern web and gets burned by `.webp`, `.avif`, or `.heic` files that half their tools refuse to open. The explicit archetype: the builder himself — right-clicks an image in a browser, hits "Save image as," and later discovers the file is unusable in the app he actually needs it for.

**Specific unmet need:** the dead space between "I have the image" and "I can use the image." Every existing solution asks for an online detour (upload to a converter), a full-blown image app, or a manual extension change that may or may not work. None of them answer the simpler question: *can I just right-click and get a PNG?*

**Secondary user:** anyone who lives in the clipboard-paste workflow — devs dropping screenshots into Slack/Teams/issues, designers pulling reference into Figma, anyone who wishes non-screenshot images behaved like Windows Snipping Tool captures (paste-ready from clipboard, no Downloads-folder archaeology).

## Inspiration & References

**Architectural mentor:**
- [PowerToys Image Resizer](https://learn.microsoft.com/en-us/windows/powertoys/image-resizer) — the gold-standard shell-extension utility pattern from Microsoft itself. Right-click images → verb → done. No standalone app required. Our UX should feel like a citizen of the same suite.

**Market competitor (cautionary tale):**
- [File Converter](https://www.thewindowsclub.com/file-converter-quickly-convert-files-context-menu) — open-source shell extension converter that tries to handle bmp/exr/ico/jpg/png/psd/svg/tiff/tga/webp/pdf/doc/docx/odt/ppt/xls and more. Kitchen-sink scope. What NOT to become.

**Spiritual cousin (reveals the gap):**
- [PowerToys Advanced Paste](https://learn.microsoft.com/en-us/windows/powertoys/advanced-paste) — "Paste as .png file" takes clipboard → file. Nobody currently ships the inverse: **file → clipboard as PNG.** That asymmetry is this project's wedge.

**Adjacent reference:**
- [PowerToys GitHub issue #20915](https://github.com/microsoft/PowerToys/issues/20915) — community request for "paste images (snips) captured in clipboard anywhere in Windows." Validates demand for clipboard-first image workflows.

**Design energy:** clean Fluent/WinUI 3 Windows 11 citizen. Minimal companion window only for settings — no dashboard, no gallery, no batch browser. Carries the builder's established creative sensibility: high-contrast, dark themes supported, muted palettes, clear information hierarchy. Polish valued; no polish-at-expense-of-shipping.

## Goals

Triple outcome:

1. **Ship to Microsoft Store** — free, app #3 on builder's store portfolio. Builder has two apps already live, so the release pattern is known; this is a straightforward third release, not a first-run gauntlet.
2. **Open source** — public repo, permissive license, welcoming to contributors. The OSS posture is load-bearing, not incidental — it's part of the trust story for a tool that reads your image files and touches your clipboard.
3. **Actually useful daily** — the builder uses this himself. If it doesn't replace his own online-converter habit within a week of install, the scope is wrong.

**Underlying craft goal (architect):** prove that a small, single-purpose Windows utility can be shipped end-to-end (build → store submission → OSS repo → maintained) without ballooning into a kitchen-sink tool. Resist feature creep by design, not discipline.

## What "Done" Looks Like

A user on Windows 11:

1. Installs **Right Click PNG** from the Microsoft Store (one click, free).
2. Right-clicks a `.webp` or `.avif` file in File Explorer.
3. Sees two options in the context menu: **Convert to PNG** and **Copy as PNG**.
4. Clicks **Copy as PNG** → a toast confirms the clipboard now holds a PNG. They paste it into Teams/Figma/Slack/Photoshop/email. It just works.
5. Clicks **Convert to PNG** on a different file → a `.png` appears next to the original in the same folder. No dialog, no decision, no interruption.
6. Opens the companion Fluent/WinUI 3 settings window once (from Start menu or via a "Settings" entry on the submenu) to toggle JPEG variants on if they want. Closes it. Probably never opens it again.

**Quality bar:** the interaction must feel as native as PowerToys Image Resizer. Any visible lag, any awkward dialog popup, any "did that work?" moment breaks the promise. The whole pitch is *no detour* — and any friction in the tool itself is a detour.

**Distribution bar:** MS Store listing live, OSS repo public with README, license, contribution guide, and a one-click install path. Builder can point anyone at the repo OR the store link, either works.

## What's Explicitly Cut

**Cut forever (sovereignty of the tool):**

- **Image editor features** — resize, crop, rotate, color adjust. PowerToys Image Resizer already owns resize; this is not Photoshop-lite.
- **Non-image file types** — docs, video, audio, PDF. Kitchen-sink converters become mediocre at everything.
- **In-browser right-click** — tempting (it's where the image actually gets saved), but a browser extension is a fundamentally different product: different distribution (Chrome/Edge/Firefox stores), different platform rules, different threat model. **Reserved as a potential v2 product that reuses this engine** — see Loose Implementation Notes. Not built inside this project.
- **Any network or cloud calls** — pure local, always. No telemetry, no "check for updates" pings, no API keys. An OSS utility that touches clipboard + files must have zero ambient network chatter to earn trust.
- **File association hijacking** — we never register as default opener for `.webp`/`.avif`/`.heic`/etc. The user's default viewer stays their default viewer.

**Cut from v1 (may return):**

- **Global hotkey / screenshot-workflow automation** — grabbing the most recent screenshot via shortcut, clipboard-monitoring, etc. Builder confirmed the "Copy as PNG" action already dissolves the Downloads-archaeology pain via the standard save-from-web path, so this is not needed for v1.
- **Custom output naming patterns** — timestamps, counters, `{original}_converted.png` templates. V1 rule: next to original, same basename, `.png` or `.jpg` extension, overwrite-with-confirmation if a file already exists.
- **Batch UI** — multi-select on the menu still works (acts per-file), but no progress dialog, no batch preview window, no queue management. If it's slow at 50 images, it's slow at 50 images — revisit if users complain.

## Loose Implementation Notes

Non-binding — gets pinned down in `/spec`. Captured here so the direction doesn't drift.

**Architectural principle — engine/UI split (load-bearing):**

V1 is two layers:

- **Core engine** — decodes supported input formats, encodes PNG and JPEG outputs, marshals bytes to the Windows clipboard. Reusable library or small binary. **Zero Windows-Explorer-specific code.** Stateless where possible.
- **Shell extension** — thin UI layer. Registers context menu entries. Calls engine for each action. Owns toast/notification rendering.

**Why it matters:** the v2 browser extension (cut from v1, aspired for later) is a WebExtension + Native Messaging Host that wraps the *same engine*. Same logic, second UI surface. Extends to a future CLI or hotkey daemon if those ever make sense. We are not writing image conversion twice.

**Stack posture:** open. Builder is explicit that the stack "might surprise us" on this project class. C# + Windows App SDK + WinUI 3 + MSIX packaging + sparse-package shell extension is the most obvious candidate (native to builder's .NET 8/9 comfort zone, Microsoft's current recommended path for modern shell extensions). Rust/C++ alternatives stay on the table if the shell-extension friction in .NET turns out to be prohibitive. **Stack decision deferred to `/spec`.**

**Settings UI:** Fluent/WinUI 3 window, accessible via Start menu entry and optionally a "Settings…" item on the submenu. Settings include:

- Toggle: show JPEG variants in context menu (default off).
- Toggle: confirm before overwrite (default on).
- (Future) output destination preference, naming template.

**Input format set (locked via scope conversation):**

- `.webp` · `.avif` · `.heic` · `.heif` — the modern web-hostile set. Must.
- `.bmp` · `.tiff` · `.gif` (first frame only) — classic format-mismatch cases. Should.
- `.svg` · `.jxl` · RAW formats (`.cr2`, `.nef`, `.dng`) — **cut from v1.** Too format-specific, too big a surface, or (SVG) requires a rendering-size decision that belongs in a different product.

**Output formats:** `.png` always. `.jpg` behind settings toggle.

**Packaging:** MSIX for Microsoft Store distribution. Shell extension registered via sparse package (the modern, no-admin path — matches Windows 11's current shell extension model).

**OSS release:** public GitHub repo, permissive license (builder's call in `/spec` — MIT or Apache 2.0 likely fit). README with install instructions (store link + build-from-source), contribution guide, issue templates. Release pattern carries over from builder's prior two shipped MS Store apps.

**Privacy & trust posture:**

- No network calls, ever.
- No telemetry.
- No cloud sync.
- No file tracking.
- OSS source means anyone can verify the above.

This is not a compliance checkbox — it's part of the product. A tool that touches your files and your clipboard must be auditable by design.
