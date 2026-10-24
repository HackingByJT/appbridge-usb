# Contributing to AppBridge USB

Thank you for helping make a small USB launcher useful and understandable. AppBridge USB 0.1.0-beta is developed at [HackingByJT/appbridge-usb](https://github.com/HackingByJT/appbridge-usb). Downloads are available from the repository's [Releases page](https://github.com/HackingByJT/appbridge-usb/releases).

English and Spanish feedback are welcome. Use the repository's [issue tracker](https://github.com/HackingByJT/appbridge-usb/issues) for ordinary bugs, compatibility reports and suggestions. Follow [SECURITY.md](SECURITY.md) for security concerns.

## Useful bug reports

Describe what you expected, what happened, and the shortest steps needed to reproduce it. Include:

- AppBridge USB version and whether you used the installer or portable edition.
- Windows version, phone model, Android version and manufacturer build if relevant.
- Android package identifier and app version, when you can share them.
- Selected fullscreen, panel, audio and restart options.
- Whether the problem affects startup, input, scale, audio or closing the session.
- A short, sanitized error excerpt if one is available.

Remove device serials, computer usernames, private paths, accounts, credentials, tokens and personal notifications. Use original sample data for screenshots. Do not upload APKs, decompiled code, protected media, streams or account exports. A black or protected app window is a compatibility observation, not a request to bypass its protection.

Reports from additional devices help establish compatibility. One successful session does not establish compatibility with every version of that device or app.

## Development

Read [README.md](README.md) for the build and packaging commands. The beta targets Windows 11 x64, .NET Framework 4.8 and C# 5 syntax. Keep the GUI and session controller as separate executables. Keep each proposed change small enough to explain and review.

Preserve these boundaries:

- Require an authorized physical USB device and a validated installed package.
- Use a separate virtual display; do not silently fall back to mirroring the entire phone.
- Keep scrcpy's origin, pinned version and checksum visible. Changing that pin requires reviewing the upstream release and updating both READMEs and the notices.
- Download upstream components only after the user requests preparation.
- Keep F12 and Esc scoped to the session window and clean up owned session processes.
- Avoid recording, automatic clipboard synchronization, added telemetry or unrelated device changes unless a future proposal explicitly defines that behavior.
- Do not add app APKs, app credentials, private analysis artifacts, or dependencies copied from an unrelated workspace.

For a pull request, explain the user-visible problem, the resulting behavior and the evidence you actually collected. Distinguish compilation, manual UI observation, phone-session confirmation and installation/removal checks. Do not report unperformed checks as passing. Describe remaining limitations rather than adding universal compatibility or performance claims.

Keep the English and Spanish READMEs aligned when changing behavior. Upstream contributions and substantive borrowed code must retain their original notices. Submit only work you have the right to contribute; project contributions follow the license in [LICENSE](LICENSE).

## Starter contributions

These are proposals for starter issues, not claims that the issues already exist:

- Improve the English/Spanish USB authorization and OEM driver instructions with official sources.
- Add a sanitized compatibility report for a device and app you can legally use.
- Improve wording and keyboard accessibility based on a documented interaction.
- Create an original demonstration using a sample app and data you own.
- Check that the documented upstream version and notices match a proposed dependency update.

Before starting a larger change, describe its scope in an issue so maintainers and contributors can coordinate. Be considerate, give concrete feedback and credit other people's work. Stars and popularity are not prerequisites for contributing.
