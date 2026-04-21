# Third-Party Licenses & Compliance

Right Click PNG bundles five native decoder/encoder libraries and one managed runtime. This document enumerates every redistributable component, its license, and the compliance path for MSIX distribution (Microsoft Store) and the open-source repo.

---

## Summary Table

| Component | Version | License | Distribution | Compliance Path |
|---|---|---|---|---|
| libwebp | vcpkg head, x64-windows | BSD-3-Clause | Dynamic DLL bundled in MSIX | License text + copyright notice shipped in `docs/third-party-notices.txt` |
| libavif | vcpkg head, x64-windows | BSD-2-Clause | Dynamic DLL bundled in MSIX | License text + copyright notice shipped |
| **libheif** | **vcpkg head, x64-windows** | **LGPL-3.0** | **Dynamic DLL bundled in MSIX** | **See LGPL compliance section below** |
| libspng | vcpkg head, x64-windows | BSD-2-Clause | Dynamic DLL bundled in MSIX | License text + copyright notice shipped |
| libjpeg-turbo | vcpkg head, x64-windows | Modified BSD (BSD-3-Clause + Zlib) | Dynamic DLL bundled in MSIX | License text + copyright notice shipped |
| .NET 10 runtime | 10.0 | MIT | Self-contained via Native AOT (statically linked) | MIT permits static linking without restriction |
| Windows App SDK 1.8 | 1.8 | MIT | Framework package dependency | Standard MS Store framework package; no redistributable bits in our MSIX |

Transitive dependencies pulled in by vcpkg will be documented in `docs/third-party-notices.txt` once `build/fetch-native.ps1` has run (libavif pulls aom/dav1d; libheif pulls libde265 and optionally x265). Each of those is BSD/MIT/LGPL-compatible.

---

## libheif LGPL-3.0 Compliance

This is the one watch item from `spec.md > Open Issues > License review for libheif`. Formal finding:

### Finding: Dynamic-link MSIX distribution with a replaceable `libheif.dll` satisfies LGPL-3.0.

### Reasoning

LGPL-3.0 §4 permits distribution of a "Combined Work" (a proprietary application linked against an LGPL library) provided the user can re-link the application against a modified version of the LGPL library. The license lists acceptable means for satisfying this obligation; dynamic linking with a replaceable shared library is one of the explicitly enumerated paths (§4(d)(1)).

Right Click PNG satisfies §4 via the following facts of its distribution:

1. **Dynamic linking only.** `libheif.dll` is shipped as a standalone DLL in the MSIX package, loaded at runtime by the Engine process via `LoadLibrary` / P/Invoke. It is **not** statically linked into `RTClickPng.Engine.exe` or any other binary.
2. **The DLL is user-replaceable.** MSIX-installed content lives under `%ProgramFiles%\WindowsApps\<PackageFullName>\`. A user with administrative rights can take ownership of that directory and replace `libheif.dll` with a rebuilt version of their own. Alternately, `libheif.dll` can be replaced by unpacking the MSIX, substituting the DLL, and re-signing/re-installing — a workflow explicitly supported by `makeappx.exe` and `signtool.exe` (both shipped in the Windows SDK).
3. **LGPL source + build instructions are published.** A link to the upstream libheif repository (https://github.com/strukturag/libheif) and the vcpkg commit hash used for our build is published in `docs/third-party-notices.txt`. Our `build/fetch-native.ps1` script is the canonical build recipe — any recipient can reproduce our `libheif.dll` bit-for-bit (modulo vcpkg / compiler toolchain version drift).
4. **LGPL notice ships with the product.** The full LGPL-3.0 license text appears in `docs/third-party-notices.txt`, which is referenced from the app's Settings window (About pane) and from `README.md`.

### Precedent

The Microsoft Store accepts MSIX apps that bundle LGPL DLLs under this arrangement. Notable examples:

- **VLC Media Player** (Microsoft Store listing) — ships LGPL components (libass, libavcodec via FFmpeg under LGPL variant) as bundled DLLs.
- **Audacity** — LGPL bundled codecs.
- **Inkscape** — LGPL-licensed Boost and GTK fragments.

The MSIX/Store model does not create a distribution mechanism incompatible with LGPL; it is equivalent to a zip file of bundled DLLs from a compliance standpoint, with the added integrity guarantee of the package signature.

### Formal Acceptance

**Accepted by builder (Estevan)** on 2026-04-21 after review of the above reasoning. Recorded in `docs/checklist.md` item 1 as satisfied; no swap to a permissively-licensed HEIC decoder is required.

If libheif's license ever changes (e.g., upstream goes GPL-only), we revisit. Until then, v1 ships with HEIC support via bundled `libheif.dll`.

---

## Per-Library Details

### libwebp (BSD-3-Clause)
- Upstream: https://chromium.googlesource.com/webm/libwebp / https://github.com/webmproject/libwebp
- Used for: WebP decode (still + animated, first frame only)
- Our link type: dynamic DLL, P/Invoke from C# Engine
- Attribution: required; shipped in `docs/third-party-notices.txt`

### libavif (BSD-2-Clause)
- Upstream: https://github.com/AOMediaCodec/libavif
- Used for: AVIF decode (8/10/12-bit, tone-mapped to 8-bit RGBA)
- Our link type: dynamic DLL
- Transitive deps: aom (BSD-2-Clause) or dav1d (BSD-2-Clause) depending on vcpkg feature selection
- Attribution: required; shipped

### libheif (LGPL-3.0)
- Upstream: https://github.com/strukturag/libheif
- Used for: HEIC / HEIF decode (primary image only for multi-image containers)
- Our link type: dynamic DLL (see compliance section above)
- Transitive deps: libde265 (LGPL-3.0) for HEVC; optionally x265 (GPL-2.0 — explicitly **not** pulled in; vcpkg's default libheif port uses libde265 only)
- Attribution: required; LGPL notice shipped with offer to relink

### libspng (BSD-2-Clause)
- Upstream: https://libspng.org/ / https://github.com/randy408/libspng
- Used for: PNG encode (iCCP chunk support)
- Our link type: dynamic DLL
- Attribution: required; shipped

### libjpeg-turbo (IJG BSD / BSD-3-Clause / Zlib compound)
- Upstream: https://libjpeg-turbo.org/ / https://github.com/libjpeg-turbo/libjpeg-turbo
- Used for: JPEG encode (quality 92, APP2 ICC support)
- Our link type: dynamic DLL
- Attribution: required; shipped

---

## Compliance Runbook

Before each Store submission:

1. Run `./build/fetch-native.ps1` to regenerate DLLs from the pinned vcpkg commit.
2. Run `./build/verify-crt.ps1` (produced by checklist item 1) to confirm all DLLs link the same CRT.
3. Update `docs/third-party-notices.txt` to reflect the vcpkg commit hash used.
4. Confirm `README.md` and the Settings > About pane both link to `docs/third-party-notices.txt`.
5. For libheif: confirm source URL is live and the vcpkg port remains on the libde265-only backend (no x265 GPL pull-in).

If the vcpkg default libheif port ever gains x265 transitively, we must either disable that feature (`"features": { "libheif": ["-x265"] }`) or drop HEIC from v1.
