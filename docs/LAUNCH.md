# Release and community plan

AppBridge USB 0.1.0-beta, 24 October 2026. The public project is [HackingByJT/appbridge-usb](https://github.com/HackingByJT/appbridge-usb), with packages on [GitHub Releases](https://github.com/HackingByJT/appbridge-usb/releases). This document records the release process and organic community plan. No social posts or messages have been sent as part of this plan.

## Repository identity

| Field | Value |
| --- | --- |
| Owner | `HackingByJT` |
| Repository | `appbridge-usb` |
| Display name | AppBridge USB |
| Description | Windows USB launcher for installed Android apps, using scrcpy virtual displays. Physical Android device required. |
| First tag | `v0.1.0-beta` |
| Release title | AppBridge USB 0.1.0 beta |
| License | Apache-2.0 for original project work; upstream components remain separate |

Repository topics: `android`, `adb`, `scrcpy`, `usb`, `windows`, `windows-11`, `csharp`, `dotnet-framework`, `virtual-display`, `open-source`. Keep descriptions accurate about the physical phone requirement, app compatibility and the role of scrcpy.

## Release process

1. Keep the full `LICENSE`, third party notices and English/Spanish instructions aligned with the shipped version.
2. Review a clean, explicit public file manifest. Exclude private history, internal task files, device identifiers, app accounts, APK/DEX files, decompilation, private reports, third party media and downloaded runtime binaries.
3. Produce the installer and portable ZIP from the reviewed source. Record their SHA-256 values and the build tools used. This beta is unsigned.
4. Keep compilation, launcher UI observation, Android sessions and installation/removal evidence distinct. Report only checks actually completed.
5. Document first use, runtime location, package validation, app restarts, scoped fullscreen keys, device audio and retained runtime components accurately.
6. Use a GitHub prerelease, attach the final assets and hashes, and verify the public links after publication.

For 0.1.0-beta, both executables, the installer and the portable ZIP compiled locally, and the launcher UI was observed without a phone. An Android session through the new GUI, installer installation/removal and GitHub Actions execution have not yet been observed. These are the current validation limits.

Private vulnerability reporting is enabled. The reporting channel is documented in [SECURITY.md](../SECURITY.md).

## Release assets and notes

Release assets:

- `AppBridgeUSB-0.1.0-beta-win64-setup.exe`
- `AppBridgeUSB-0.1.0-beta-win64-portable.zip`
- `AppBridgeUSB-0.1.0-beta-win64-payload.json`
- `SHA256SUMS.txt`, containing hashes calculated from the exact assets.

For subsequent releases, prepare a draft, attach the final assets and mark beta versions as prereleases. See [GitHub's release documentation](https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository). Keep public links and hashes tied to the corresponding version.

Release description:

> AppBridge USB 0.1.0 beta opens an app already installed on a physical Android phone in a Windows window over USB. It uses scrcpy's virtual display engine and adds a Windows setup interface and session controls. This beta targets Windows 11 x64 and Android 12 or newer. A connected, authorized phone is required. Compatibility depends on each app and device.

Keep panel off versus PIN lock, system-wide device audio, optional force stopping of the selected app, upstream download on request, validation status and unresolved known problems visible in release notes. Link the changelog and credits. Name actual contributors only.

## Original demonstration

Prepare a 30–45 second demonstration with an app and data owned by the project or demonstrator. A small sample page containing a local counter, colors and text is sufficient. Show the phone connection concept, package selection, launch, F12 and Esc, then close the session. Label any conceptual artwork as an illustration, not a screenshot.

An original [conceptual SVG cover](../assets/social-preview.svg) accompanies the READMEs. It uses no third party logos or media. It is an illustration, not a product screenshot or an uploaded social preview. Export an appropriate format and check the destination's current requirements before using it elsewhere.

Capture no third party channels, films, protected media, personal notifications, accounts, serial numbers or private desktop paths. Keep the XuperTV shortcut out of the general demonstration; it is an optional installed-package shortcut, not the identity of the utility. Credit scrcpy visibly in the description and link its original repository.

## First community contributions

Proposed `good first issue` topics:

1. Improve the USB authorization checklist in English and Spanish. Acceptance: short instructions with official Android/OEM sources, aligned in both READMEs.
2. Submit a sanitized compatibility report. Acceptance: hardware/Android/app versions, selected options, observed behavior and no private identifiers.
3. Improve keyboard navigation with a concrete reproduction. Acceptance: the control and expected interaction are named, and the change remains within the launcher.
4. Add an original demonstration asset. Acceptance: the contributor owns the data, credits scrcpy and states the device/version actually used.

Open only issues that describe real work; labels, sample comments and proposed issues are not evidence of existing contributors or users. Keep security reports in the private channel.

## Organic discovery

- Maintain accurate English and Spanish READMEs, a concise description, relevant topics and useful release notes. Add the original demonstration when it has been recorded.
- Invite focused compatibility feedback in the release text: phone model, Android/app versions and whether launch, input, audio and closing worked.
- If the owner chooses to share the project, follow each community's rules and disclose that this is an independent scrcpy-based launcher. Provide the limitation and upstream credit alongside the demo.
- Turn recurring real questions into documentation improvements and reproducible bugs into issues. Credit accepted work and record fixes in the changelog.
- Review feedback after the first week and again after the first month. Choose the next changes from actual startup failures, device compatibility and usability reports.

No purchased stars, fabricated testimonials, fake accounts, engagement exchanges, unsolicited messages or promised star counts. Useful adoption is measured by genuine reports, resolved issues, repeat contributors and opt-in user feedback. Any public figures should reflect their actual source and date; do not infer unique users from download counts.

Future social sharing is optional. This plan has not posted or messaged anyone.
