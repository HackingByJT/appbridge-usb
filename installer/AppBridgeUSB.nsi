; Build only through scripts/package.ps1 so both distributions use the same payload.
Unicode true
!include "MUI2.nsh"
!include "x64.nsh"
!include "LogicLib.nsh"

!ifndef APP_VERSION
  !error "APP_VERSION must be supplied by package.ps1"
!endif
!ifndef APP_VERSION_NUMERIC
  !error "APP_VERSION_NUMERIC must be supplied by package.ps1"
!endif
!ifndef PAYLOAD_DIR
  !error "PAYLOAD_DIR must be supplied by package.ps1"
!endif
!ifndef OUTPUT_FILE
  !error "OUTPUT_FILE must be supplied by package.ps1"
!endif

!define PRODUCT_NAME "AppBridge USB"
!define UNINSTALL_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\AppBridgeUSB"

Name "${PRODUCT_NAME} ${APP_VERSION}"
OutFile "${OUTPUT_FILE}"
InstallDir "$LOCALAPPDATA\Programs\AppBridgeUSB"
RequestExecutionLevel user
SetCompressor zlib
SetDatablockOptimize on
CRCCheck force
ShowInstDetails show
ShowUninstDetails show
VIProductVersion "${APP_VERSION_NUMERIC}"
VIAddVersionKey /LANG=1033 "ProductName" "${PRODUCT_NAME}"
VIAddVersionKey /LANG=1033 "ProductVersion" "${APP_VERSION}"
VIAddVersionKey /LANG=1033 "FileVersion" "${APP_VERSION_NUMERIC}"
VIAddVersionKey /LANG=1033 "FileDescription" "${PRODUCT_NAME} per-user installer"
VIAddVersionKey /LANG=1033 "LegalCopyright" "AppBridge USB contributors"

!define MUI_ABORTWARNING
!define MUI_WELCOMEPAGE_TEXT "Install AppBridge USB beta for your Windows user account.$\r$\n$\r$\nRequires Windows 11 x64, .NET Framework 4.8 and an Android 12+ phone connected by USB.$\r$\n$\r$\nThe app downloads official scrcpy components only when you choose Prepare components. No Android apps are included."
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "${PAYLOAD_DIR}\LICENSE"
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES
!define MUI_FINISHPAGE_TEXT "AppBridge USB beta has been installed for your account.$\r$\n$\r$\nOpen it from the Start menu. Choose Prepare components in the app, then connect your phone and authorize USB debugging.$\r$\n$\r$\nCompatibility depends on the Android app and phone."
!insertmacro MUI_PAGE_FINISH
!define MUI_UNCONFIRMPAGE_TEXT_TOP "Remove AppBridge USB and its shortcuts?$\r$\n$\r$\nDownloaded runtime components in your local app data will be kept."
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH
!insertmacro MUI_LANGUAGE "English"

Function .onInit
  SetShellVarContext current
  ${IfNot} ${RunningX64}
    MessageBox MB_ICONSTOP "AppBridge USB requires 64-bit Windows 11."
    Abort
  ${EndIf}
  SetRegView 64
  ReadRegStr $0 HKLM "SOFTWARE\Microsoft\Windows NT\CurrentVersion" "CurrentBuildNumber"
  IntCmp $0 22000 windows_supported windows_unsupported windows_supported
  windows_unsupported:
    MessageBox MB_ICONSTOP "AppBridge USB beta supports Windows 11 x64."
    Abort
  windows_supported:
  ReadRegDWORD $0 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" "Release"
  IntCmp $0 528040 framework_supported framework_unsupported framework_supported
  framework_unsupported:
    MessageBox MB_ICONSTOP "Install .NET Framework 4.8 or later before installing AppBridge USB."
    Abort
  framework_supported:
  ; Keep the destination limited to this product's per-user directory, even with /D.
  StrCpy $INSTDIR "$LOCALAPPDATA\Programs\AppBridgeUSB"
FunctionEnd

Section "AppBridge USB (required)" AppSection
  SectionIn RO
  SetShellVarContext current
  SetRegView 64
  SetOutPath "$INSTDIR"
  File "${PAYLOAD_DIR}\AppBridge.exe"
  File "${PAYLOAD_DIR}\AppBridge.exe.config"
  File "${PAYLOAD_DIR}\AppBridgeControls.exe"
  File "${PAYLOAD_DIR}\AppBridgeControls.exe.config"
  File "${PAYLOAD_DIR}\README.md"
  File "${PAYLOAD_DIR}\README.es.md"
  File "${PAYLOAD_DIR}\LICENSE"
  File "${PAYLOAD_DIR}\THIRD_PARTY_NOTICES.md"
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  CreateDirectory "$SMPROGRAMS\AppBridge USB"
  CreateShortcut "$SMPROGRAMS\AppBridge USB\AppBridge USB.lnk" "$INSTDIR\AppBridge.exe" "" "$INSTDIR\AppBridge.exe"
  CreateShortcut "$SMPROGRAMS\AppBridge USB\Uninstall AppBridge USB.lnk" "$INSTDIR\Uninstall.exe"
  WriteRegStr HKCU "${UNINSTALL_KEY}" "DisplayName" "${PRODUCT_NAME} (Beta)"
  WriteRegStr HKCU "${UNINSTALL_KEY}" "DisplayVersion" "${APP_VERSION}"
  WriteRegStr HKCU "${UNINSTALL_KEY}" "Publisher" "AppBridge USB contributors"
  WriteRegStr HKCU "${UNINSTALL_KEY}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKCU "${UNINSTALL_KEY}" "DisplayIcon" "$INSTDIR\AppBridge.exe"
  WriteRegStr HKCU "${UNINSTALL_KEY}" "UninstallString" '$\"$INSTDIR\Uninstall.exe$\"'
  WriteRegDWORD HKCU "${UNINSTALL_KEY}" "NoModify" 1
  WriteRegDWORD HKCU "${UNINSTALL_KEY}" "NoRepair" 1
SectionEnd

Section /o "Desktop shortcut" DesktopSection
  CreateShortcut "$DESKTOP\AppBridge USB.lnk" "$INSTDIR\AppBridge.exe" "" "$INSTDIR\AppBridge.exe"
SectionEnd

!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${AppSection} "Install the app, documentation, Start menu shortcut and uninstaller for your user account."
  !insertmacro MUI_DESCRIPTION_TEXT ${DesktopSection} "Add an AppBridge USB shortcut to your desktop."
!insertmacro MUI_FUNCTION_DESCRIPTION_END

Function un.onInit
  SetShellVarContext current
  SetRegView 64
  ; Refuse to remove same-named files if this uninstaller has been moved elsewhere.
  StrCpy $0 "$LOCALAPPDATA\Programs\AppBridgeUSB"
  StrCmp $INSTDIR $0 0 un_invalid_location
  ReadRegStr $1 HKCU "${UNINSTALL_KEY}" "InstallLocation"
  StrCmp $INSTDIR $1 un_valid_location un_invalid_location
  un_invalid_location:
    MessageBox MB_ICONSTOP "The registered AppBridge USB installation could not be identified. Run its uninstaller from the original installation folder."
    Abort
  un_valid_location:
FunctionEnd

Section "Uninstall"
  SetShellVarContext current
  SetRegView 64
  ClearErrors
  ; Explicit product files only. Never recursively remove a user directory.
  Delete "$INSTDIR\AppBridge.exe"
  Delete "$INSTDIR\AppBridge.exe.config"
  Delete "$INSTDIR\AppBridgeControls.exe"
  Delete "$INSTDIR\AppBridgeControls.exe.config"
  Delete "$INSTDIR\README.md"
  Delete "$INSTDIR\README.es.md"
  Delete "$INSTDIR\LICENSE"
  Delete "$INSTDIR\THIRD_PARTY_NOTICES.md"
  IfErrors un_files_in_use
  Delete "$DESKTOP\AppBridge USB.lnk"
  Delete "$SMPROGRAMS\AppBridge USB\AppBridge USB.lnk"
  Delete "$SMPROGRAMS\AppBridge USB\Uninstall AppBridge USB.lnk"
  RMDir "$SMPROGRAMS\AppBridge USB"
  Delete "$INSTDIR\Uninstall.exe"
  RMDir "$INSTDIR"
  DeleteRegKey HKCU "${UNINSTALL_KEY}"
  Goto un_done
  un_files_in_use:
    MessageBox MB_ICONEXCLAMATION "Some AppBridge USB files could not be removed. Close the app and its sessions, then run Uninstall AppBridge USB again."
    SetErrorLevel 2
  un_done:
SectionEnd
