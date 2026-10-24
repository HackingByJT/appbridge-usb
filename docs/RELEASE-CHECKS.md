# 0.1.0-beta release checks

Recorded on 24 October 2026. These observations describe this beta and do not establish compatibility with every Windows or Android device.

## Completed locally

- Compiled `AppBridge.exe` and `AppBridgeControls.exe` for x64 with the Windows .NET Framework C# compiler, C# 5 syntax and installed Framework assemblies. The build emitted no warnings.
- Built the per-user installer using the pinned NSIS 3.13 portable compiler and zlib compression.
- Opened the launcher without prepared components or a connected phone. Its setup instructions, disabled device/package controls, session options and status area were visible. Closed the launcher normally.
- Inspected the portable ZIP: exactly the eight files listed in the payload manifest. No Android APK, downloaded Android tooling, device data or media was included.
- Reviewed runtime download integrity, allowlisted archive extraction, Windows argument quoting, USB transport selection, session ownership and foreground-scoped fullscreen controls statically. Corrected the stalled-download cancellation issue found during review.
- Reviewed the public source inventory separately from build artifacts and private development notes.

The distribution includes a payload manifest with each file's SHA-256 and `SHA256SUMS.txt` for the setup, portable ZIP and manifest. The exact hashes belong to those accompanying files.

## Still pending

- End-to-end component preparation through the new GUI, including real network interruption and cancellation.
- An Android session started through the new general-purpose GUI: USB discovery, package selection, orientation, input, audio, physical panel behavior and F12/Esc.
- Actual installation, upgrade and uninstallation of the generated setup package.
- Multi-monitor and high-DPI compatibility beyond source review.
- Compatibility reports from additional phones, Android builds and apps.

No automated test suite was added or run. The artifacts are unsigned. A successful build or UI observation does not establish the pending behavior above.

CI performs compilation and packaging only. Consult the repository's actual workflow run for its outcome; this document does not substitute a local build for a successful CI run.

## Withdrawing a broken beta

If a release causes startup failures, unsafe behavior or unintended data exposure, mark the release as withdrawn and remove its downloadable assets until a reviewed fix is available. Do not replace an existing version silently: publish a new version with updated checksums and changelog. Keep user-owned runtime data intact and explain any necessary recovery steps.
