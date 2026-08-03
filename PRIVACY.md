# Privacy Policy

**Effective date:** 2026-08-03

Right Click PNG collects no data, makes no network calls, and has no account system. What you do with the app stays on your computer: converting or copying an image reads the source file you selected, decodes it into raw pixels in memory, and either writes a new file beside the source or places the encoded result on the clipboard — the original file is never modified, moved, or deleted. The only things the app writes to disk are your two settings toggles in `%LOCALAPPDATA%\Packages\626LabsLLC.RightClicktoPNG_wz1chhb2h2v4a\LocalState\settings.json` and, if something goes wrong, a small crash log (type name and message only, never file contents) at `%LOCALAPPDATA%\RTClickPng.Settings.crash.log`. The app ships with zero third-party packages, links no networking libraries, and declares no internet capability, so there's nothing to phone home to and no SDK that could do it indirectly — there are no update checks either; updates come through the Microsoft Store. Context-menu entries are registered declaratively through the app package, so Windows removes them cleanly at uninstall along with the settings folder; the crash log above may survive and can be deleted manually.

The canonical, most current version of this policy is published at **[626labs.dev/privacy.html#privacy-rtclickpng](https://626labs.dev/privacy.html#privacy-rtclickpng)**. If this file and the site version ever disagree, the site version governs.

## Contact

`estevan.hernandez@gmail.com` — questions, concerns, or requests.
