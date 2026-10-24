# Installer toolchain

The packaging scripts use the portable Windows binary distribution of **NSIS 3.13**, without installing its development kit or changing the system. The NSIS compiler and its ZIP are build tools and must not be committed or added to the AppBridge USB payload.

- Official project download page: <https://nsis.sourceforge.io/Download>
- Official release files and SHA-256 metadata: <https://sourceforge.net/projects/nsis/files/NSIS%203/3.13/>
- Official ZIP URL: <https://sourceforge.net/projects/nsis/files/NSIS%203/3.13/nsis-3.13.zip/download>
- SourceForge distribution mirror used by CI: <https://netactuate.dl.sourceforge.net/project/nsis/NSIS%203/3.13/nsis-3.13.zip>
- SHA-256: `ba63dffc4410ee89193e1cb5a41989991bd77c61068da17e3156d136b7b0b3d8`

The hash above comes from the `nsis-3.13.zip` metadata embedded in the official SourceForge release listing, retrieved on 2026-10-24. Verify the archive before extracting and running the compiler. This is a pinned digest check, not a digital signature or code signing certificate.

## Build on Windows

Use Windows PowerShell 5.1 or PowerShell 7 on Windows x64. .NET Framework 4.8 or later and its Windows C# compiler are required. A 4.8 targeting pack is preferred; if it is absent, the build script checks the installed Framework release and uses the installed Framework assemblies. Both executables are compiled with C# 5 syntax and an x64 target.

```powershell
.\scripts\build.ps1
```

Extract the verified NSIS ZIP to a directory of your choice. Then supply its compiler path:

```powershell
.\scripts\package.ps1 -MakeNsisPath 'C:\tools\nsis-3.13\makensis.exe'
```

`package.ps1` compiles the app by default. Use `-SkipBuild` only to package an existing `build/app` output. `-Version` changes the artifact names and installer metadata; its default is `0.1.0-beta`. The environment variable `APPBRIDGE_MAKENSIS` can supply the compiler path instead of the parameter.

Output is written to `dist/`:

- `AppBridgeUSB-0.1.0-beta-win64-setup.exe`
- `AppBridgeUSB-0.1.0-beta-win64-portable.zip`
- `AppBridgeUSB-0.1.0-beta-win64-payload.json`
- `SHA256SUMS.txt`

The payload is an explicit list of two app executables, their two .NET configuration files, `README.md`, `README.es.md`, `LICENSE`, and `THIRD_PARTY_NOTICES.md`. The JSON manifest records the size and SHA-256 of every payload file; `SHA256SUMS.txt` records the installer, portable ZIP and manifest hashes. NSIS additionally generates `Uninstall.exe` during installation. No scrcpy binaries, Android APKs, logs, account data, or private workspace files are added by these scripts.

## Installation behavior

The installer requests the current user's normal permissions and installs to `%LOCALAPPDATA%\Programs\AppBridgeUSB`. It creates Start menu launch and uninstall shortcuts. A desktop shortcut is optional. It registers only the current user's Apps uninstall entry. The installer checks Windows 11 x64 and .NET Framework 4.8 availability; it does not install prerequisites.

The uninstaller validates its registered installation directory and deletes only explicitly named app files and shortcuts. Empty product directories are removed without recursive deletion. The separately downloaded runtime under `%LOCALAPPDATA%\AppBridgeUSB\runtime` is retained. Close the app and its sessions before upgrading or uninstalling.

The portable ZIP contains the same eight payload files at its root and can be extracted into a writable folder. Its runtime location is still in the current user's local app data. Launch `AppBridge.exe` after extraction.

## CI scope and limitations

The workflow builds on pushes, pull requests and manual `workflow_dispatch`. It downloads and verifies the pinned portable compiler, builds both executables, then retains the installer, ZIP, payload manifest and checksums as workflow artifacts for 14 days. It has read-only repository permissions. It does not publish a GitHub Release, run the generated installer, connect to Android, or run tests.

These are unsigned beta artifacts. Successful compilation does not establish correct installation, uninstallation, UI behavior, USB support or Android app compatibility. Record those observations separately when a person performs them. The [initial public CI run](https://github.com/HackingByJT/appbridge-usb/actions/runs/40073374903) completed compilation, packaging and artifact retention successfully.

## Primary references

- NSIS compiler command line and `/D` definitions: <https://nsis.sourceforge.io/Docs/Chapter3.html>
- NSIS `RequestExecutionLevel`, `SetShellVarContext`, `Delete` and `RMDir`: <https://nsis.sourceforge.io/Docs/Chapter4.html>
- GitHub workflow events: <https://docs.github.com/en/actions/how-tos/write-workflows/choose-when-workflows-run/trigger-a-workflow>
- Official checkout action: <https://github.com/actions/checkout>
- Official artifact action: <https://github.com/actions/upload-artifact>
