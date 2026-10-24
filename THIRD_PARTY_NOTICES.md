# Third party notices

AppBridge USB is an independent launcher and session controller. Its original code and documentation are licensed under the [Apache License 2.0](LICENSE), copyright 2026 HackingByJT and contributors. That license does not replace the licenses of the components below.

## scrcpy

The Android display, input and audio functionality comes from **scrcpy**. AppBridge USB does not claim authorship of that engine.

- Project and official releases: [Genymobile/scrcpy](https://github.com/Genymobile/scrcpy).
- Pinned version: [v5.0.1](https://github.com/Genymobile/scrcpy/releases/tag/v5.0.1), Windows x64.
- License: [Apache License 2.0, as provided by upstream](https://github.com/Genymobile/scrcpy/blob/v5.0.1/LICENSE).
- Upstream copyright credits: Genymobile (2018); Romain Vimont (2018–2026); additional contributors are identified in the upstream project.
- [Upstream Windows archive and published checksums](https://github.com/Genymobile/scrcpy/blob/v5.0.1/doc/windows.md).

The AppBridge USB installer and portable ZIP do not bundle the scrcpy distribution. When the user selects **Prepare components**, AppBridge USB downloads `scrcpy-win64-v5.0.1.zip` directly from the official release, checks SHA-256 `b12a2c4ee8be317422451fc7dcf8ee20a71b5ea7ef9ad73ddd825a25316227a5`, and prepares the required runtime files locally. No modified scrcpy binaries are published as part of AppBridge USB.

## Components used by the upstream runtime

The official scrcpy Windows distribution supplies Android Debug Bridge and supporting native libraries. These remain the work of their respective projects and contributors. Refer to the exact upstream release and its build configuration for the applicable versions and terms.

| Project | Role | Original source and licensing information |
| --- | --- | --- |
| Android Debug Bridge (ADB) | Communication with the authorized Android device | [Android Open Source Project, ADB](https://android.googlesource.com/platform/packages/modules/adb/); [official platform tools](https://developer.android.com/tools/releases/platform-tools) |
| FFmpeg | Media codec and processing libraries | [FFmpeg](https://ffmpeg.org/); [upstream licensing information](https://ffmpeg.org/legal.html). Terms depend on the actual build and enabled components. |
| Simple DirectMedia Layer (SDL) | Windowing, input and playback support | [SDL](https://www.libsdl.org/); [license information](https://www.libsdl.org/license.php) |
| libusb | USB support in the upstream runtime | [libusb](https://libusb.info/); [upstream source and license](https://github.com/libusb/libusb/blob/master/COPYING) |

These links identify upstream providers. They are not a claim that every component uses AppBridge USB's license. If a future AppBridge USB release redistributes any upstream runtime files, review the exact build, include all required license and notice files, and update this document before distribution.

## Installer tooling

The setup executable is built with **[NSIS (Nullsoft Scriptable Install System) 3.13](https://nsis.sourceforge.io/)**. The generated installer and uninstaller use the NSIS runtime and zlib compression module, provided under the zlib/libpng license, copyright 1999–2026 NSIS contributors. See the [official NSIS license page](https://nsis.sourceforge.io/License) and the selected toolchain's notices. The compiler is not included in the AppBridge USB payload. The installer script and AppBridge USB application code are original project files.

## Windows and Android applications

.NET Framework and Windows are Microsoft components supplied separately by the operating system or their official installers. Android and installed Android apps are supplied by their own providers and retain their respective terms. No third party Android APK, account, app data, channels or streams are distributed here.

Names of third party products are used to identify compatibility or credit their authors. No affiliation, sponsorship or endorsement is implied.
