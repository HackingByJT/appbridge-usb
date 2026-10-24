# Security policy

AppBridge USB 0.1.0-beta is the current beta series. Security handling is best effort; there is no promised response time or established support period.

## Reporting a vulnerability

Private vulnerability reporting is enabled. Use [Report a vulnerability](https://github.com/HackingByJT/appbridge-usb/security/advisories/new), or open the repository's **Security → Advisories → Report a vulnerability** page. See [GitHub's reporting feature documentation](https://docs.github.com/en/code-security/how-tos/report-and-fix-vulnerabilities/configure-vulnerability-reporting/configure-for-a-repository).

If you cannot access that option, open an issue asking for help reaching the private reporting channel, with no exploit or sensitive details. Do not put credentials, device serials, personal files or a working exploit in a public issue.

A useful private report includes the affected version, impact, prerequisites, minimal reproduction steps and sanitized evidence. Use devices and applications you are authorized to examine. Do not attach third party APKs, user sessions or protected media.

## Project boundaries

Relevant reports include unsafe download or archive handling, argument injection, unexpected process execution, unintended exposure of device data, excessive installer removal, and failures to keep input controls scoped to the session.

Ordinary app incompatibility, protected video, subscription requirements and Android lock screens are not themselves AppBridge USB vulnerabilities. Report an actual bypass or data exposure privately if one is discovered.

The Android rendering and audio engine is [scrcpy](https://github.com/Genymobile/scrcpy), downloaded separately from upstream. If a concern belongs to scrcpy, ADB, Windows or an Android app, follow that provider's security process. The AppBridge USB maintainer can assess whether the launcher also needs a change; no automatic upstream report is implied.

## User controls

USB debugging authorizes substantial access from the computer to the phone. Authorize trusted computers only and revoke authorizations from the phone when appropriate. Turning the panel off does not establish a security lock. Device audio can include other apps and notifications. The launcher does not provide recording or automatic clipboard synchronization.

Do not replace the pinned runtime with files from an untrusted download site. The beta checks the official archive's SHA-256 before preparing its runtime; a mismatch must stop preparation.
