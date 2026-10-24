# Changelog

## 0.1.0-beta — 2026-10-24

First beta. See the repository's [Releases page](https://github.com/HackingByJT/appbridge-usb/releases) for packages and checksums.

### Added

- Windows launcher for selecting an authorized USB Android device and an installed package.
- Separate Android virtual display using device-reported size and density, with an initial portrait orientation.
- Options for fullscreen, physical panel off, device audio, and restarting the selected app to apply scale.
- Session controller with F12 to enter fullscreen and Esc to leave fullscreen while the session window is active.
- Explicit preparation of the official scrcpy 5.0.1 Windows x64 runtime with a pinned SHA-256 check.
- Per-user installer and portable package preparation.
- English and Spanish documentation, upstream credits, contribution guidance and a security reporting policy.
- Optional XuperTV selection shortcut when its package is already installed.

### Validation and known limits

- Windows 11 x64 and physical Android 12 / API 31 or newer are the beta targets. Device, manufacturer and app compatibility are not universal.
- Android continues to execute the app; the phone and its authorized USB connection remain necessary.
- Device audio can include other apps and notifications. Panel off is separate from Android's PIN lock.
- No APKs, app content, accounts or streams are included. No app authentication or content protection is changed.
- Both application executables, the installer and the portable ZIP compiled successfully locally. The launcher interface was opened and observed without a phone.
- An Android session through the new launcher interface, installer installation/removal and GitHub Actions execution have not yet been observed.
- These observations do not establish compatibility with any particular Android device or app.
