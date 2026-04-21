# Process Notes

## /onboard

**Date:** 2026-04-21
**Builder:** Estevan (returning, 4th Cart project)
**Project:** Right Click PNG (RTClickPng)

### Profile state going in

- Persona: architect · Mode: builder · Pacing: brisk · Tone: terse
- All TTLs fresh — no decay prompt needed
- Stamped persona/tone/pacing today (2026-04-21)

### Changes since last session

- **Two Microsoft Store apps shipped.** New product class for builder. Identity line updated in unified profile to reflect.
- No other changes to stack, preferences, or mode.

### Technical experience summary

Deep-end generalist. TS/Python/JS/Luau/C#. React/Next/Vite/Tailwind/Firebase on the web side, .NET 8/9 + Azure on the backend/desktop side, Expo/RN on mobile, Luau for Roblox. Has shipped three Claude Code plugins to marketplace. Runs Claude Code as autonomous build system.

**New territory on this project:** Windows shell extensions + utility tools as a product class. Stack is open.

### Project goals

Triple outcome:

1. Ship to Microsoft Store (free, app #3)
2. Open source the repo
3. Solve a real personal pain (web images saving as unusable formats)

### Design direction

**Minimal companion** — right-click context menu action + small Fluent/WinUI 3 settings window. Not invisible, not a full product. Polished Windows 11 citizen vibe. Carries existing creative sensibility (clean, functional, high-contrast, dark themes, muted palettes).

### Prior SDD experience

Extensive — three prior Cart projects, two completed. `/reflect` should pitch at practitioner level.

### Architecture docs

None project-specific. The **MS Store release pattern** from his two shipped apps is the one transferable asset (packaging, signing, submission, update workflow). Stack decision deferred to `/spec` — builder flagged "the stack might surprise us."

### Energy / engagement

Coming in hot. Low friction, decisive answers, good vision clarity already. Architect+builder+brisk match held throughout — expect zero deepening rounds downstream unless a load-bearing decision forces one.

### Notable concept nuance

The `XXXX` placeholder in the builder's original pitch covers the full set of web-hostile formats (`.webp`, `.avif`, `.heic`, `.bmp`, `.tiff`, maybe `.gif` / `.svg` depending on scope). Input format set is a real `/scope` question. Output: PNG primary, JPEG secondary (confirmed this session).

## /scope

**Date:** 2026-04-21
**Outcome:** `docs/scope.md` written on first pass. Zero deepening rounds.

### How the idea evolved

Started with the builder's raw pitch: "Right click to PNG" — a shell extension that converts web-hostile image formats. Through the brain dump a second pain surfaced: **the Downloads-folder archaeology problem.** Builder described wanting a "paste it like a screenshot" workflow. That one sentence doubled the product's conceptual surface — not just file→file conversion but file→clipboard as PNG.

After research round, it became clear the clipboard-as-output angle is a genuine market gap. PowerToys Advanced Paste does clipboard→file (paste as .png). PowerToys Image Resizer owns right-click resize. File Converter goes kitchen-sink. Nobody does **file → clipboard as PNG** cleanly. That's the wedge.

### Load-bearing decisions

- **Action set shape B** (two explicit actions: Convert to PNG + Copy as PNG). Builder picked this over single-action-settings-driven (A) and four-action-matrix (C) without hesitation.
- **Reading 1 confirmed** on the Downloads-archaeology pain — the clipboard action solves both the format pain and the location pain in one stroke. No separate screenshot-hunting feature needed for v1.
- **Input formats: Tier 1 + Tier 2** — webp/avif/heic/heif plus bmp/tiff/gif-first-frame. SVG/JXL/RAW cut.
- **Browser extension as SEPARATE v2 product**, not merged into this one. Builder raised this explicitly — he wants to build on the engine for a future browser extension. This crystallized the engine/UI split principle.

### Architectural principle surfaced

**Engine/UI split** — core conversion engine as a reusable library/binary, shell extension as a thin UI layer. Opens the door for v2 browser extension (via Native Messaging Host), future CLI, future hotkey daemon — all reusing the same engine. This wasn't in the builder's original framing but clicked immediately when surfaced.

### Pushback & active shaping

- Builder pushed back on one framing: the "separate ship" phrasing for browser extension. He wanted it clear that v2 *is* on the roadmap and should be enabled by v1's architecture, not orphaned. Engine/UI split answers that directly.
- Builder accepted the forever-cuts list without modification — image editor features, non-image formats, network calls, file association hijacking all confirmed out.

### References that resonated

- **PowerToys Image Resizer** — UX mentor, not feature mentor.
- **PowerToys Advanced Paste** — spiritual cousin going the inverse direction; reveals the asymmetric market gap.
- **File Converter** — negative reference, the shape to avoid.

### Deepening rounds

Zero. Matches builder's on-file pattern (architect persona + builder mode, zero deepening when vision is formed). Builder re-invoked `/scope` as a signal to move to doc generation rather than entering Phase 2.

### Active shaping summary

Builder drove. He named the clipboard-paste pain unprompted (reframed the product), picked action set B decisively, chose T1+T2 input tier without negotiation, and explicitly asked that v2 browser extension be enabled by v1's architecture. Agent provided research, landscape framing, and architectural naming (engine/UI split) — builder validated and anchored the decisions.

## /prd

**Date:** 2026-04-21
**Outcome:** `docs/prd.md` written. Zero deepening rounds. Five epics, load-bearing edges resolved.

### What the builder added or changed vs scope

Scope was already sharp, so PRD expansion was mostly translation rather than re-scoping. Real additions:

- **Locked the load-bearing edges** that scope left as loose implementation notes: multi-select Copy as PNG behavior, already-PNG file handling, ICC/EXIF policy, modern-vs-legacy context menu support.
- **Added Epic 3 (Supported input formats)** as an explicit user-facing epic with its own acceptance criteria — not just "we support these formats" but "the menu ONLY appears for these formats, determined by extension without I/O to keep Explorer snappy." This became a first-class requirement.
- **Added the batch-failure contract** — failures don't halt the batch, summary toast reports counts, failures list revealable on click. Scope had said "no batch UI"; PRD clarifies that no-batch-UI still implies a sane default for partial failures.

### Edge-case decisions (load-bearing)

1. **Copy as PNG + multi-select:** hidden when N > 1. Single-image contract kept clean.
2. **Copy as PNG on `.png` inputs:** shown. The workflow win (paste without Downloads-detour) still applies to already-PNG web images. Convert to PNG is hidden on `.png` (redundant).
3. **ICC + EXIF:** preserve ICC profile, strip EXIF (apply orientation to pixels before strip so rotation isn't lost). Privacy-as-feature without betraying designers who care about color fidelity.
4. **Windows 11 modern + Windows 10 legacy context menu:** both. Register via `IExplorerCommand` (modern) and the legacy COM handler pattern. Native on Win11, still findable on Win10 via Show More Options.

### Ambiguity resolved mid-conversation

Builder answered "b" to a batch of four edge-case questions; my first read assumed "b across all four" (overriding my leans on Q1 and Q2). Builder clarified "b" was specifically for Q4. Net result: went with agent's leans across all four — builder signaled trust rather than override. Clean course-correct, matches his on-file pattern (mid-conversation course-corrections on load-bearing decisions).

### What got pushed to "What We'd Add With More Time"

- Global hotkey / screenshot workflow
- Custom naming patterns
- Batch progress UI
- SVG / JXL / RAW input support
- WebP / PDF output
- Output destination options
- Drag-and-drop conversion
- CLI
- Browser extension companion (the v2 product, enabled by engine/UI split)

### Non-Goals (locked from scope + PRD conversation)

- Image editor features
- Non-image file conversion
- In-browser right-click (separate product)
- Any runtime network calls
- File association hijacking
- First-run welcome / onboarding flow

### Open questions flagged for /spec

- Stack decision (C#/WinUI 3 vs. Rust/C++) — blocks /spec
- Minimum Windows version (floor at 10 2004 for modern context menu API) — blocks /spec
- Decoder library (WIC + codec extensions vs. bundled decoders) — blocks /spec, affects MSIX dependencies

### Open questions that can wait until /build

- OSS license choice
- Cloud-only file (OneDrive placeholder) handling default
- Large file size warning threshold
- Icon design
- Telemetry-free crash reporting strategy

### Deepening rounds

Zero again. Matches builder's on-file pattern and scope-stage behavior.

### Active shaping summary

Builder accepted the epic structure on first pass ("Perfect"), course-corrected once on the "b" interpretation, and otherwise let the agent drive. Pattern holds: when vision is sharp and agent surfaces the real load-bearing decisions, builder signals quickly and moves.

## /spec

**Date:** 2026-04-21
**Outcome:** `docs/spec.md` written. Zero deepening rounds. Three load-bearing stack decisions resolved mid-conversation.

### Stack decisions and rationale

**1. Three-executable architecture — forced, not stylistic.**
Research surfaced that Microsoft heavily discourages managed-code shell extensions due to CLR version clashes inside Explorer processes. The engine/UI split locked in scope became a practical forcing function: shell extension MUST be native (C++/WinRT), engine CAN be anything (C# .NET 10 Native AOT landed), settings UI is its own process (C# WinUI 3 on WinAppSDK 1.8). Three languages, one coherent reason.

**2. Bundled decoders (Path B).**
libwebp + libavif + libheif + libspng + libjpeg-turbo — native DLLs in the MSIX package. Alternative (WIC codec extensions) was rejected because (a) it forces users to install codec packs from the Store, violating the "no detour" product identity, and (b) some HEIC variants require a paid ($0.99) HEVC Video Extension.

**3. Windows 11 22H2 floor, modern context menu only.**
Builder course-corrected my initial Windows 10 2004 floor proposal — Microsoft ended Windows 10 consumer support October 2025, so targeting it for a new 2026 tool was the wrong stability trade. Floor moved to Windows 11 22H2 (build 22621). This unlocked dropping the legacy COM shell extension registration entirely — only `IExplorerCommand` via `desktop4:FileExplorerContextMenus` sparse package. Less code, fewer bugs, consistent with "most stable app possible."

### Course corrections worth naming

- **Builder flagged Windows 10 EOL as the reason to drop the floor.** Agent had proposed Windows 10 version 2004 as the floor citing sparse-package availability; builder pointed out Microsoft doesn't even support Windows 10 anymore. Good architect-level catch — factual correction that simplified the spec materially.
- **"Most stable app possible" anchored the C++ vs Rust call.** Agent asked C++ or Rust via `windows-rs`; builder named the underlying goal ("most stable"), which resolved to C++ without a separate language debate. Architect persona working as intended — builder framed the invariant, agent resolved the detail.

### What builder was confident about

- The product identity (minimal companion, no detours, clipboard-first wedge)
- The engine/UI split (surfaced in scope, reinforced here)
- Stability over novelty on the native layer
- Modern-only context menu (immediate lock once the EOL option was explicit)

### What builder deferred

- Decoder binary acquisition path (vcpkg vs manual build) — pushed to /checklist
- libheif LGPL compliance verification — pushed to /checklist as blocker
- OSS license choice (MIT vs Apache 2.0) — non-blocking, can wait
- Icon design — blocks Store submission, not build
- ARM64 support — v1.1

### Open issues flagged for downstream

- **Blocks /build start:**
  - libheif LGPL-3.0 compliance (dynamic linking plan needs confirmation, or swap decoder)
  - Decoder binary acquisition pipeline (pick vcpkg or commit binaries)
- **Blocks Store submission but not /build:**
  - Icon design (single ICO + Store tile set)
- **Can wait:**
  - OSS license choice
  - OneDrive placeholder behavior
  - Large file size cap
  - libspng vs libpng final pick
  - BMP/TIFF/GIF decoder choice (pure C# vs WIC)

### Deepening rounds

Zero again. Builder re-invoked /spec as the "skip deepening, generate the doc" signal — same signaling pattern as /scope and /prd. Three-for-three on zero-deepening when vision is formed. Pattern fully consistent with on-file builder notes.

### Active shaping summary

Builder made two load-bearing architectural moves this session: naming "most stable" as the C++/Rust tiebreaker, and calling the Windows 10 EOL factor as the reason to raise the floor. Both simplified the spec. Agent drove the research (shell extension guidance, WIC codec story, WinAppSDK 1.8 status, Windows 10 EOL confirmation) and the component-level proposals; builder applied architectural veto/lock as needed. Classic architect + builder-mode working rhythm — builder is the compass, agent is the surveyor.

## /checklist

**Date:** 2026-04-21
**Outcome:** `docs/checklist.md` written. Twelve items. Zero deepening rounds. Standard five-field format throughout.

### Build preferences locked

- **Build mode:** Autonomous — matches builder's on-file `iterative-prototype` preference (autonomous chunks with deliberate review gates).
- **Verification:** ON, with three checkpoints — after items **3** (engine correctness), **6** (shell extension install + menu visibility), **10** (MSIX end-to-end on fresh VM). Matches the review-gates half of iterative-prototype.
- **Git cadence:** Commit after each item, `chore(build): complete step N — <title>`.
- **Comprehension checks / check-in cadence:** N/A (autonomous).

### Sequencing logic

**Risk-first for items 1-3, then component-by-component.**

- Item 1 de-risks two spec-blocking items simultaneously: decoder binary pipeline and libheif LGPL compliance. If LGPL review fails, the decoder choice ripples through items 3-4 — better to know immediately.
- Item 2 locks the CLI contract before any implementation, so the shell extension in items 5-8 knows what it's calling.
- Item 3 validates .NET 10 Native AOT + P/Invoke works end-to-end before the whole decoder surface is built on top of it. AOT has surprising edge cases (trimming, `[UnmanagedCallersOnly]`, static init) — surfacing them early prevents expensive refactors.
- Items 4-10 follow the component stack: engine metadata → shell extension scaffold → visibility → Convert flow → Copy flow → Settings UI → MSIX packaging. Each unblocks the next.
- Items 11-12 are delivery infrastructure: CI and documentation+security.

### Checkpoints placed at natural verification boundaries

- After item 3: engine is correct for all formats — builder can eyeball the output files.
- After item 6: shell extension installs cleanly, menu appears right — builder can verify the UX contract on a real VM.
- After item 10: full MSIX install on fresh Win11 — this is "v1 functionally done" moment.

### What the builder was confident about

- Autonomous mode with checkpoints (matched profile, no hesitation)
- 12-item count accepted without reorg
- No pushback on sequencing rationale

### What was deferred to /build (per-item open calls)

- **Item 1:** vcpkg vs prebuilt vs hand-built decoder acquisition pipeline — picked during item execution.
- **Item 3:** TIFF decoder pure-C# vs WIC-backed — picked during item execution.
- **Item 12:** MIT vs Apache 2.0 license — default is MIT, can override.

### Deepening rounds

Zero. Four-for-four on zero-deepening when vision is formed. Pattern is now unambiguous across all command types.

### Active shaping summary

Builder drove briefly and decisively: "Generate!" on the proposed sequence. Agent proposed the preferences, sequencing logic, and all 12 items; builder accepted the whole shape. Classic pattern for this builder — having invested architectural decisions in scope and spec, /checklist was translation work, not negotiation. The five-field format for each item is preserved so /build has exactly what it needs to execute without re-asking.


