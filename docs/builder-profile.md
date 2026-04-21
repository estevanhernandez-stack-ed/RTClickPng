# Builder Profile

## Who They Are
**Estevan** — builder and outsider, runs 626Labs out of Fort Worth (817, the 626 exchange). Ships prolifically across theatre ops tools, hackathon apps, Roblox games, Claude Code plugins, Discord bots, creative writing, and — as of recently — **two published Microsoft Store apps**. Comes to this project fresh off shipping Vibe Test v0.2 (diagnostic-and-retrofit testing plugin) and two MS Store releases. Fourth project through the Cart.

## Technical Experience
**Level:** experienced.

**Languages:** TypeScript, Python, JavaScript, Luau, C#, HTML/CSS.

**Frameworks/tools:** React 19, Next.js, Vite, TailwindCSS, Firebase, FastAPI, Flask, Express, .NET 8/9, Azure, Expo, React Native, Drizzle ORM, Playwright.

**AI agent experience:** Deep. Has shipped three Claude Code plugins to marketplace (Vibe Cartographer, Vibe Doc, Vibe Test). Runs Claude Code as an autonomous build system with structured checklists and subagent delegation. Also uses Gemini and Replit Agent.

**New territory on this project:** Windows shell integration + converter utility as a product class. His prior MS Store apps are the release-pattern precedent, but the technical stack for a shell extension is fresh ground — he's explicit that "the stack might surprise us," and we'll nail it in `/spec`.

## Mode
**Builder** — streamlined flow, minimal explanation, brisk pacing. Confirmed this session.

## Project Goals
**Triple outcome:**
1. **Ship to Microsoft Store** — app #3 on his store portfolio. Free, not monetized.
2. **Open source** — public repo, GitHub stars / community contributions welcome.
3. **Actually useful** — solves a real, personal pain (right-clicking an image on the web, getting a `.webp` / `.avif` / `.heic` you can't use, having to round-trip through an online converter).

## Project Origin
**Greenfield.** Blank folder at `C:\Users\estev\Projects\RTClickPng`. No existing code, no prototype to escape from. Starting from zero.

**Concept (verbatim from builder):** "Do you ever hate when you find an image on the web, right click to download and save and it saves as a file you can't use? Introducing right click to .png. I figure we might need to make a XXXX to .png/jpeg converter and add a command to the right click context menu."

**Downstream implication:** Full `/scope → /prd → /spec → /checklist → /build` chain applies. No prototype to reverse-engineer, no existing architecture to respect. Clean slate.

## Design Direction
**Minimal companion** — Windows 11 citizen, Fluent/WinUI 3 surface for defaults and preferences. Right-click context menu does the real work; the UI exists for settings (output folder, format preference, naming pattern, default PNG vs JPEG) and an about/status window. Not invisible (no standalone UI) and not a full product (no batch browser, no gallery, no format explorer) — the middle path.

**Creative sensibility carried from profile:** clean, functional, high-contrast. Dark themes, muted palettes, clear information hierarchy. Polish valued but never at the expense of shipping.

## Prior SDD Experience
**Extensive.** Three prior Cart projects (two completed — Vibe Test v0.2 being the most recent). Course-corrects 2–3 times per long command on load-bearing decisions, then lets the rest flow. Zero deepening rounds when the vision is formed. `/reflect` quiz should pitch at practitioner level — he already thinks in specs, checklists, and artifact-driven agent delegation.

## Architecture Docs
**None specific to this project class.** He's shipped Claude Code plugins and web/mobile apps, but never a Windows utility tool or shell extension. The one transferable asset is the **Microsoft Store release pattern** from his two published apps — we'll carry that over for packaging, signing, submission, and update workflow.

**Stack decision deferred to `/spec`.** Builder flagged that "the stack might surprise us" — respect that openness. Likely candidates include C# + Windows App SDK + WinUI 3 with a COM/sparse-package shell extension, or Rust/C++ for the shell piece with a managed UI layer. Not committing until `/spec`.
