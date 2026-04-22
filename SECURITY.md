# Security Policy

## No data comes back to us

Right Click PNG is a fully offline image converter. The shipped binary:

- **Does not make any network calls.** Not for telemetry, not for update checks,
  not for crash reports. Everything the app does happens locally, end-to-end.
- **Does not collect, store, or transmit user data.** No analytics, no
  identifiers, no phone-home.
- **Does not require a user account.** Nothing to sign up for, nothing to sign in to.
- **Runs fully offline-capable.** Pull the network cable and every feature still works.

The engine is a native-AOT console executable with no HTTP / TLS code linked in.
The shell extension is a Win32 COM in-proc server with no network dependencies.
The settings UI reads and writes a single JSON file in the package's LocalState.

## Supported versions

Only the most recent release is supported. If a critical security issue
surfaces we'll cut a patch release and advise users to update via the Microsoft
Store (which auto-updates by default).

| Version | Supported |
|---------|-----------|
| 0.1.x   | ✅         |

## Reporting a vulnerability

**Please do not open a public issue for security-sensitive reports.**

Email: `estevan.hernandez@gmail.com` (subject line starting `[RTClickPng security]`)

Expect an acknowledgement within 5 business days. We aim to ship a fix or an
explicit won't-fix rationale within 30 days of the initial report for
critical issues.

## Third-party dependencies

The engine statically links decoder libraries built via vcpkg. Every shipped
library is listed in [`docs/licenses.md`](docs/licenses.md) with its license and
upstream URL. We track upstream security advisories for each and ship patch
releases when a relevant fix lands.

Current decoders:

- libpng, libjpeg-turbo, libwebp, libavif, libheif, libspng, libtiff

## Code signing

The Microsoft Store redistributes this app signed with Microsoft's publisher
certificate. The MSIX binary attached to the GitHub Releases page is **unsigned**
and intended for developer-mode sideload only. If you need a binary signed by
the author for deployment in environments that require it, open an issue.

## Responsible disclosure acknowledgements

Will list any researchers who report and confirm issues here, with permission.
