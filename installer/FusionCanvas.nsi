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

Name "${PRODUCT_NAME}"
Caption "${PRODUCT_NAME} ${APP_VERSION} Setup"
OutFile "${OUTFILE}"
InstallDir "$LOCALAPPDATA\Programs\FusionCanvas"
InstallDirRegKey HKCU "${INSTALL_KEY}" "InstallDir"
RequestExecutionLevel user
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
  SetShellVarContext current
  Call AssertApplicationStopped
FunctionEnd

Function un.onInit
  SetShellVarContext current
  Call un.AssertApplicationStopped
FunctionEnd

Section "FusionCanvas" SEC_APPLICATION
  SectionIn RO
  SetShellVarContext current
  SetOutPath "$INSTDIR"

  ; The installer owns only this application directory. User data lives below
  ; %LOCALAPPDATA%\FusionCanvas and is intentionally never touched here.
  Delete "$SMPROGRAMS\FusionCanvas\FusionCanvas.lnk"
  RMDir "$SMPROGRAMS\FusionCanvas"
  Delete "$DESKTOP\FusionCanvas.lnk"
  File /r "${PUBLISH_DIRECTORY}\*.*"
  IfErrors install_failed

  WriteUninstaller "$INSTDIR\Uninstall.exe"
  WriteRegStr HKCU "${INSTALL_KEY}" "InstallDir" "$INSTDIR"
  WriteRegStr HKCU "${UNINSTALL_KEY}" "DisplayName" "${PRODUCT_NAME}"
  WriteRegStr HKCU "${UNINSTALL_KEY}" "DisplayVersion" "${APP_VERSION}"
  WriteRegStr HKCU "${UNINSTALL_KEY}" "Publisher" "FusionCanvas contributors"
  WriteRegStr HKCU "${UNINSTALL_KEY}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKCU "${UNINSTALL_KEY}" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegDWORD HKCU "${UNINSTALL_KEY}" "NoModify" 1
  WriteRegDWORD HKCU "${UNINSTALL_KEY}" "NoRepair" 1
  IfErrors install_failed
  Goto install_complete

install_failed:
  MessageBox MB_ICONSTOP "FusionCanvas could not be installed completely. No successful installation is reported."
  Abort

install_complete:
SectionEnd

Section /o "Start Menu shortcut" SEC_START_MENU
  SetShellVarContext current
  CreateDirectory "$SMPROGRAMS\FusionCanvas"
  CreateShortCut "$SMPROGRAMS\FusionCanvas\FusionCanvas.lnk" "$INSTDIR\${PRODUCT_EXE}"
SectionEnd

Section /o "Desktop shortcut" SEC_DESKTOP
  SetShellVarContext current
  CreateShortCut "$DESKTOP\FusionCanvas.lnk" "$INSTDIR\${PRODUCT_EXE}"
SectionEnd

Section "Uninstall"
  SetShellVarContext current
  Delete "$SMPROGRAMS\FusionCanvas\FusionCanvas.lnk"
  RMDir "$SMPROGRAMS\FusionCanvas"
  Delete "$DESKTOP\FusionCanvas.lnk"
  DeleteRegKey HKCU "${UNINSTALL_KEY}"
  DeleteRegKey HKCU "${INSTALL_KEY}"
  RMDir /r "$INSTDIR"
SectionEnd

