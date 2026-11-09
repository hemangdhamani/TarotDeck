Unicode true
!include "MUI2.nsh"
!include "LogicLib.nsh"
Name "${PRODUCT_NAME}"
OutFile "${OUTPUT_FILE}"
InstallDir "$PROGRAMFILES\TarotDeck"
InstallDirRegKey HKLM "Software\TarotDeck" "InstallDir"
!ifdef TEST_INSTALL
RequestExecutionLevel user
!else
RequestExecutionLevel admin
!endif
SetCompressor lzma
SetCompress off
!define MUI_ABORTWARNING
!insertmacro MUI_PAGE_WELCOME
!ifndef ADDON
!insertmacro MUI_PAGE_DIRECTORY
!endif
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!ifndef ADDON
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!endif
!insertmacro MUI_LANGUAGE "English"

Function .onInit
  ReadRegDWORD $0 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" "Release"
  ${If} $0 < 528040
    MessageBox MB_OK|MB_ICONSTOP "TarotDeck requires Microsoft .NET Framework 4.8 or later. Install it from Microsoft's website, then run this installer again."
    Abort
  ${EndIf}
!ifdef ADDON
!ifndef TEST_INSTALL
  ReadRegStr $INSTDIR HKLM "Software\TarotDeck" "InstallDir"
!endif
  ${IfNot} ${FileExists} "$INSTDIR\TarotDeck.exe"
    MessageBox MB_OK|MB_ICONSTOP "Install the TarotDeck base application before installing this artwork deck."
    Abort
  ${EndIf}
!endif
FunctionEnd

Section "Install"
  MessageBox MB_OKCANCEL "Close TarotDeck before continuing. Your saved settings and readings will be kept." /SD IDOK IDCANCEL StopInstall
!ifdef ADDON
  SetOutPath "$INSTDIR\assets"
  ClearErrors
  File "${SOURCE_DIR}\assets\${DECK_ID}.deck"
  ${If} ${Errors}
    MessageBox MB_OK|MB_ICONSTOP "Could not replace the deck package. Close TarotDeck and retry."
    Abort
  ${EndIf}
!else
  SetOutPath "$INSTDIR"
  File "${SOURCE_DIR}\TarotDeck.exe"
  File "${SOURCE_DIR}\TarotDeck.exe.config"
  File "${SOURCE_DIR}\THIRD-PARTY-NOTICES.txt"
  File "${GUIDE_FILE}"
  SetOutPath "$INSTDIR\assets"
  File "${SOURCE_DIR}\assets\the-illustrated-arcana.deck"
  File "${SOURCE_DIR}\assets\cardbacks.deck"
!ifndef TEST_INSTALL
  WriteRegStr HKLM "Software\TarotDeck" "InstallDir" "$INSTDIR"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\TarotDeck" "DisplayName" "TarotDeck"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\TarotDeck" "DisplayVersion" "1.0.0"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\TarotDeck" "UninstallString" '$\"$INSTDIR\Uninstall.exe$\"'
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\TarotDeck" "NoModify" 1
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\TarotDeck" "NoRepair" 1
!endif
  WriteUninstaller "$INSTDIR\Uninstall.exe"
!ifndef TEST_INSTALL
  CreateDirectory "$SMPROGRAMS\TarotDeck"
  CreateShortcut "$SMPROGRAMS\TarotDeck\TarotDeck.lnk" "$INSTDIR\TarotDeck.exe"
!endif
!endif
  Goto DoneInstall
StopInstall:
  Abort
DoneInstall:
SectionEnd

!ifndef ADDON
Section "Uninstall"
  MessageBox MB_OKCANCEL "Close TarotDeck before uninstalling. Saved readings and settings in your user profile will be kept." /SD IDOK IDCANCEL StopUninstall
  Delete "$INSTDIR\TarotDeck.exe"
  Delete "$INSTDIR\TarotDeck.exe.config"
  Delete "$INSTDIR\THIRD-PARTY-NOTICES.txt"
  Delete "$INSTDIR\README.txt"
  Delete "$INSTDIR\assets\*.deck"
  RMDir "$INSTDIR\assets"
!ifndef TEST_INSTALL
  Delete "$SMPROGRAMS\TarotDeck\TarotDeck.lnk"
  RMDir "$SMPROGRAMS\TarotDeck"
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\TarotDeck"
  DeleteRegKey HKLM "Software\TarotDeck"
!endif
  Delete "$INSTDIR\Uninstall.exe"
  RMDir "$INSTDIR"
  Goto DoneUninstall
StopUninstall:
  Abort
DoneUninstall:
SectionEnd
!endif
