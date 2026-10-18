Unicode true

!include "MUI2.nsh"
!include "LogicLib.nsh"

!ifndef APP_VERSION
  !define APP_VERSION "0.0.0"
!endif
!ifndef FILE_VERSION
  !define FILE_VERSION "0.0.0.0"
!endif
!ifndef PUBLISH_DIRECTORY
  !define PUBLISH_DIRECTORY "artifacts\publish"
!endif
!ifndef OUTFILE
  !define OUTFILE "FusionCanvas-Setup.exe"
!endif
!ifndef ICON_PATH
  !define ICON_PATH "src\FusionCanvas.App\Assets\FusionCanvasLogo_Square_LightBg.ico"
!endif

!define PRODUCT_NAME "FusionCanvas"
!define PRODUCT_EXE "FusionCanvas.App.exe"
!define UNINSTALL_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}"
!define INSTALL_KEY "Software\${PRODUCT_NAME}"

Var LegacyInstallDir
Var HasMachineInstall
Var LegacyDefaultDir

Name "${PRODUCT_NAME}"
Caption "${PRODUCT_NAME} ${APP_VERSION} Setup"
OutFile "${OUTFILE}"
InstallDir "$PROGRAMFILES64\FusionCanvas"
InstallDirRegKey HKLM "${INSTALL_KEY}" "InstallDir"
RequestExecutionLevel admin
Icon "${ICON_PATH}"
UninstallIcon "${ICON_PATH}"
ShowInstDetails show
ShowUninstDetails show
SetCompressor /SOLID lzma
SetCompressorDictSize 64

VIProductVersion "${FILE_VERSION}"
VIAddVersionKey "ProductName" "${PRODUCT_NAME}"
VIAddVersionKey "ProductVersion" "${APP_VERSION}"
VIAddVersionKey "FileVersion" "${APP_VERSION}"
VIAddVersionKey "FileDescription" "${PRODUCT_NAME} Windows installer"
VIAddVersionKey "CompanyName" "FusionCanvas contributors"
VIAddVersionKey "LegalCopyright" "Copyright © FusionCanvas contributors"

!define MUI_ABORTWARNING
!define MUI_ICON "${ICON_PATH}"
!define MUI_UNICON "${ICON_PATH}"
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "English"

!macro AssertApplicationStoppedBody
check_process:
  nsExec::ExecToLog 'powershell.exe -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -Command "if (Get-Process -Name FusionCanvas.App -ErrorAction SilentlyContinue) { exit 1 } else { exit 0 }"'
  Pop $0
  ${If} $0 == 0
    Return
  ${EndIf}

  IfSilent silent_process_failure
  MessageBox MB_RETRYCANCEL|MB_ICONEXCLAMATION "FusionCanvas is running. Close it before continuing, then choose Retry." IDRETRY check_process IDCANCEL cancel_process

silent_process_failure:
  Abort

cancel_process:
  Abort
!macroend

Function AssertApplicationStopped
  !insertmacro AssertApplicationStoppedBody
FunctionEnd

Function un.AssertApplicationStopped
  !insertmacro AssertApplicationStoppedBody
FunctionEnd

Function .onInit
  SetRegView 64
  SetShellVarContext current
  StrCpy $LegacyDefaultDir "$LOCALAPPDATA\Programs\FusionCanvas"
  SetShellVarContext all
  StrCpy $HasMachineInstall 0
  StrCpy $LegacyInstallDir ""

  ReadRegStr $0 HKLM "${INSTALL_KEY}" "InstallDir"
  ${If} $0 != ""
    StrCpy $HasMachineInstall 1
    StrCpy $INSTDIR $0
  ${Else}
    ; The original per-user installer used NSIS's default 32-bit registry view.
    SetRegView 32
    ReadRegStr $LegacyInstallDir HKCU "${INSTALL_KEY}" "InstallDir"
    SetRegView 64
    ${If} $LegacyInstallDir != ""
      StrCmp $LegacyInstallDir "$LegacyDefaultDir" legacy_default legacy_custom
legacy_custom:
      MessageBox MB_OK|MB_ICONSTOP "An existing FusionCanvas installation uses a custom per-user location. Uninstall that version first, then run this setup again to install FusionCanvas machine-wide. Your workspace and settings will remain in place."
      Abort
legacy_default:
      StrCpy $INSTDIR "$PROGRAMFILES64\FusionCanvas"
    ${EndIf}
  ${EndIf}

  Call AssertApplicationStopped
FunctionEnd

Function un.onInit
  SetRegView 64
  SetShellVarContext all
  Call un.AssertApplicationStopped
FunctionEnd

Section "FusionCanvas" SEC_APPLICATION
  SectionIn RO
  SetRegView 64
  SetShellVarContext all
  SetOutPath "$INSTDIR"

  ; The installer owns only this application directory. User data remains below
  ; %LOCALAPPDATA%\FusionCanvas and is intentionally never touched here.
  File /r "${PUBLISH_DIRECTORY}\*.*"
  IfErrors install_failed

  WriteUninstaller "$INSTDIR\Uninstall.exe"
  IfErrors install_failed
  WriteRegStr HKLM "${INSTALL_KEY}" "InstallDir" "$INSTDIR"
  WriteRegStr HKLM "${UNINSTALL_KEY}" "DisplayName" "${PRODUCT_NAME}"
  WriteRegStr HKLM "${UNINSTALL_KEY}" "DisplayVersion" "${APP_VERSION}"
  WriteRegStr HKLM "${UNINSTALL_KEY}" "Publisher" "FusionCanvas contributors"
  WriteRegStr HKLM "${UNINSTALL_KEY}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKLM "${UNINSTALL_KEY}" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegDWORD HKLM "${UNINSTALL_KEY}" "NoModify" 1
  WriteRegDWORD HKLM "${UNINSTALL_KEY}" "NoRepair" 1
  IfErrors install_failed

  ; Remove old user-scoped shortcuts from the initial per-user installer and
  ; stale shared shortcuts before the selected all-users shortcuts are created.
  SetShellVarContext current
  Delete "$SMPROGRAMS\FusionCanvas\FusionCanvas.lnk"
  RMDir "$SMPROGRAMS\FusionCanvas"
  Delete "$DESKTOP\FusionCanvas.lnk"
  SetShellVarContext all
  Delete "$SMPROGRAMS\FusionCanvas\FusionCanvas.lnk"
  RMDir "$SMPROGRAMS\FusionCanvas"
  Delete "$DESKTOP\FusionCanvas.lnk"

  ${If} $LegacyInstallDir != ""
    SetRegView 32
    DeleteRegKey HKCU "${UNINSTALL_KEY}"
    DeleteRegKey HKCU "${INSTALL_KEY}"
    SetRegView 64
    RMDir /r "$LegacyInstallDir"
  ${EndIf}
  Goto install_complete

install_failed:
  ${If} $HasMachineInstall == 0
    DeleteRegKey HKLM "${UNINSTALL_KEY}"
    DeleteRegKey HKLM "${INSTALL_KEY}"
  ${EndIf}
  MessageBox MB_ICONSTOP "FusionCanvas could not be installed completely. No successful installation is reported."
  Abort

install_complete:
SectionEnd

Section "Start Menu shortcut" SEC_START_MENU
  SetShellVarContext all
  CreateDirectory "$SMPROGRAMS\FusionCanvas"
  CreateShortCut "$SMPROGRAMS\FusionCanvas\FusionCanvas.lnk" "$INSTDIR\${PRODUCT_EXE}"
SectionEnd

Section "Desktop shortcut" SEC_DESKTOP
  SetShellVarContext all
  CreateShortCut "$DESKTOP\FusionCanvas.lnk" "$INSTDIR\${PRODUCT_EXE}"
SectionEnd

Section "Uninstall"
  SetRegView 64
  SetShellVarContext all
  Delete "$SMPROGRAMS\FusionCanvas\FusionCanvas.lnk"
  RMDir "$SMPROGRAMS\FusionCanvas"
  Delete "$DESKTOP\FusionCanvas.lnk"
  SetShellVarContext current
  Delete "$SMPROGRAMS\FusionCanvas\FusionCanvas.lnk"
  RMDir "$SMPROGRAMS\FusionCanvas"
  Delete "$DESKTOP\FusionCanvas.lnk"
  DeleteRegKey HKLM "${UNINSTALL_KEY}"
  DeleteRegKey HKLM "${INSTALL_KEY}"
  SetRegView 32
  DeleteRegKey HKCU "${UNINSTALL_KEY}"
  DeleteRegKey HKCU "${INSTALL_KEY}"
  SetRegView 64
  RMDir /r "$INSTDIR"
SectionEnd
