Unicode true
RequestExecutionLevel user
SetCompressor /SOLID lzma
!include "MUI2.nsh"
!include "x64.nsh"
!include "WinVer.nsh"
!include "nsDialogs.nsh"
!include "WordFunc.nsh"
!ifndef VERSION
  !error "Define VERSION, PAYLOAD, OUTPUT, ICON, LICENSE, VCREDIST y DELETE_LIST."
!endif
!ifndef FILE_VERSION
  !error "Define FILE_VERSION con la revisión numérica de cuatro componentes. VERSION es la versión pública."
!endif
!define APPKEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\AdaptaPE.Base"
Name "Adapta PE"
OutFile "${OUTPUT}"
InstallDir "$LOCALAPPDATA\Programs\AdaptaPE"
Icon "${ICON}"
UninstallIcon "${ICON}"
VIProductVersion "${FILE_VERSION}"
VIAddVersionKey "ProductVersion" "${VERSION}"
VIAddVersionKey "ProductName" "Adapta PE"
VIAddVersionKey "FileDescription" "Instalador de Adapta PE para Windows x64"
VIAddVersionKey "FileVersion" "${FILE_VERSION}"
VIAddVersionKey "LegalCopyright" "© 2026 Rodrigo Alejandro Apcho Aliaga"
!define MUI_ICON "${ICON}"
!define MUI_UNICON "${ICON}"
!define MUI_HEADERIMAGE
!define MUI_HEADERIMAGE_RIGHT
!define MUI_HEADERIMAGE_BITMAP "../assets/installer/header.bmp"
!define MUI_WELCOMEFINISHPAGE_BITMAP "../assets/installer/welcome.bmp"
!define MUI_TEXTCOLOR "17365B"
!define MUI_ABORTWARNING
!define MUI_WELCOMEPAGE_TITLE "Bienvenido a Adapta PE"
!define MUI_WELCOMEPAGE_TEXT "Tu escritorio, a tu ritmo.$\r$\nVoz, cámara y lectura local, con tu cuy de siempre.$\r$\n$\r$\nSe instalará para tu usuario. Podrás elegir accesos en escritorio e Inicio. Incluye .NET y modelos locales.$\r$\n$\r$\nSi falta Visual C++, se abrirá el instalador oficial de Microsoft."
!define MUI_FINISHPAGE_RUN "$INSTDIR\AdaptaPE.exe"
!define MUI_FINISHPAGE_RUN_TEXT "Abrir Adapta PE"
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "${LICENSE}"
Page custom OptionsPage OptionsLeave
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "Spanish"
Var Dialog
Var DesktopCheck
Var StartCheck
Var DesktopChoice
Var StartChoice
Var ExistingVersion
Var InstallAction
Var WindowsName

Function OptionsPage
  !insertmacro MUI_HEADER_TEXT "$InstallAction Adapta PE ${VERSION}" "Elige cómo acceder a tu aplicación."
  nsDialogs::Create 1018
  Pop $Dialog
  ${If} $Dialog == error
    Abort
  ${EndIf}
  ${NSD_CreateLabel} 0 0 100% 20u "$WindowsName · x64 · edición Base completa"
  Pop $0
  ${NSD_CreateLabel} 0 24u 100% 28u "Se instalará sólo para tu usuario. Incluye .NET, voz local y todos los modelos."
  Pop $0
  ${NSD_CreateLabel} 0 54u 100% 28u "Carpeta: $INSTDIR"
  Pop $0
  ${NSD_CreateCheckbox} 0 88u 100% 18u "Crear acceso directo en el escritorio"
  Pop $DesktopCheck
  ${NSD_SetState} $DesktopCheck $DesktopChoice
  ${NSD_CreateCheckbox} 0 110u 100% 18u "Crear acceso directo en el menú Inicio"
  Pop $StartCheck
  ${NSD_SetState} $StartCheck $StartChoice
  ${NSD_CreateLabel} 0 138u 100% 36u "Tus preferencias se conservan. Voz y cámara se activan desde la app. Windows 7/8 requieren paquetes Legacy separados."
  Pop $0
  nsDialogs::Show
FunctionEnd
Function OptionsLeave
  ${NSD_GetState} $DesktopCheck $DesktopChoice
  ${NSD_GetState} $StartCheck $StartChoice
FunctionEnd

!macro CheckClosed PREFIX
Function ${PREFIX}CheckClosed
  System::Call 'kernel32::CreateMutexW(p0, i0, w "Local\AdaptaPE.Base.Instance") p.r0 ?e'
  Pop $1
  System::Call 'kernel32::CloseHandle(p r0)'
  ${If} $1 == 183
    MessageBox MB_ICONEXCLAMATION|MB_OK "Cierra Adapta PE desde la bandeja junto al reloj y vuelve a intentar."
    Abort
  ${EndIf}
FunctionEnd
!macroend
!insertmacro CheckClosed ""
!insertmacro CheckClosed "un."

Function .onInit
  ${IfNot} ${IsNativeAMD64}
  ${OrIfNot} ${AtLeastWin10}
    MessageBox MB_ICONSTOP|MB_OK "Este instalador requiere Windows 10 u 11 x64."
    Abort
  ${EndIf}
  SetShellVarContext current
  SetRegView 64
  StrCpy $INSTDIR "$LOCALAPPDATA\Programs\AdaptaPE"
  Call CheckClosed
  ReadRegStr $0 HKLM "Software\Microsoft\Windows NT\CurrentVersion" "CurrentBuildNumber"
  StrCpy $WindowsName "Windows 10"
  ${If} $0 >= 22000
    StrCpy $WindowsName "Windows 11"
  ${EndIf}
  StrCpy $InstallAction "Instalar"
  StrCpy $DesktopChoice ${BST_CHECKED}
  StrCpy $StartChoice ${BST_CHECKED}
  ReadRegStr $ExistingVersion HKCU "${APPKEY}" "BuildVersion"
  ${If} $ExistingVersion == ""
    ReadRegStr $ExistingVersion HKCU "${APPKEY}" "DisplayVersion"
  ${EndIf}
  ${If} $ExistingVersion == ""
    IfFileExists "$INSTDIR\AdaptaPE.exe" 0 versions_done
    ClearErrors
    GetDLLVersion "$INSTDIR\AdaptaPE.exe" $1 $2
    ${IfNot} ${Errors}
      IntOp $3 $1 >> 16
      IntOp $4 $1 & 0xFFFF
      IntOp $5 $2 >> 16
      IntOp $6 $2 & 0xFFFF
      StrCpy $ExistingVersion "$3.$4.$5.$6"
    ${EndIf}
  ${EndIf}
  ${If} $ExistingVersion != ""
    ${VersionCompare} "$ExistingVersion" "${FILE_VERSION}" $0
    ${If} $0 == 1
      MessageBox MB_ICONEXCLAMATION|MB_OK "Ya tienes una compilación de Adapta PE más reciente que este instalador. Usa esa instalación."
      Abort
    ${EndIf}
    StrCpy $InstallAction "Actualizar"
    ${If} $0 == 0
      StrCpy $InstallAction "Reparar"
    ${EndIf}
    ClearErrors
    ReadRegDWORD $DesktopChoice HKCU "${APPKEY}" "DesktopShortcut"
    ${If} ${Errors}
      StrCpy $DesktopChoice ${BST_UNCHECKED}
      IfFileExists "$DESKTOP\Adapta PE.lnk" 0 +2
        StrCpy $DesktopChoice ${BST_CHECKED}
    ${EndIf}
    ClearErrors
    ReadRegDWORD $StartChoice HKCU "${APPKEY}" "StartMenuShortcut"
    ${If} ${Errors}
      StrCpy $StartChoice ${BST_UNCHECKED}
      IfFileExists "$SMPROGRAMS\Adapta PE\Adapta PE.lnk" 0 +2
        StrCpy $StartChoice ${BST_CHECKED}
    ${EndIf}
  ${EndIf}
  versions_done:
FunctionEnd

Function un.onInit
  SetShellVarContext current
  SetRegView 64
  StrCmp $INSTDIR "$LOCALAPPDATA\Programs\AdaptaPE" +3
    MessageBox MB_ICONSTOP|MB_OK "La ubicación no coincide con esta instalación. No se borrarán archivos."
    Abort
  Call un.CheckClosed
FunctionEnd

Section "Adapta PE" Main
  Call CheckClosed
  ; El runtime VC x64 registra su versión en la vista de registro de 32 bits.
  SetRegView 32
  ReadRegDWORD $0 HKLM "Software\Microsoft\VisualStudio\14.0\VC\Runtimes\x64" "Installed"
  ReadRegDWORD $1 HKLM "Software\Microsoft\VisualStudio\14.0\VC\Runtimes\x64" "Minor"
  ${If} $0 != 1
  ${OrIf} $1 < 29
    InitPluginsDir
    SetOutPath "$PLUGINSDIR"
    File /oname=VC_redist.x64.exe "${VCREDIST}"
    ExecShellWait "runas" "$PLUGINSDIR\VC_redist.x64.exe" "/install /norestart"
    ReadRegDWORD $0 HKLM "Software\Microsoft\VisualStudio\14.0\VC\Runtimes\x64" "Installed"
    ReadRegDWORD $1 HKLM "Software\Microsoft\VisualStudio\14.0\VC\Runtimes\x64" "Minor"
    ${If} $0 != 1
    ${OrIf} $1 < 29
      MessageBox MB_ICONSTOP|MB_OK "Visual C++ no se instaló. Adapta PE necesita este componente para la cámara. Vuelve a ejecutar el instalador."
      Abort
    ${EndIf}
  ${EndIf}
  SetRegView 64
  SetOutPath "$INSTDIR"
  File /r "${PAYLOAD}/*"
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  WriteRegStr HKCU "${APPKEY}" "DisplayName" "Adapta PE"
  WriteRegStr HKCU "${APPKEY}" "DisplayVersion" "${VERSION}"
  WriteRegStr HKCU "${APPKEY}" "BuildVersion" "${FILE_VERSION}"
  WriteRegStr HKCU "${APPKEY}" "Publisher" "Rodrigo Alejandro Apcho Aliaga — TheAlejo160"
  WriteRegStr HKCU "${APPKEY}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKCU "${APPKEY}" "UninstallString" '$\"$INSTDIR\Uninstall.exe$\"'
  WriteRegStr HKCU "${APPKEY}" "DisplayIcon" "$INSTDIR\AdaptaPE.exe,0"
  WriteRegDWORD HKCU "${APPKEY}" "NoModify" 1
  WriteRegDWORD HKCU "${APPKEY}" "NoRepair" 1
  WriteRegDWORD HKCU "${APPKEY}" "DesktopShortcut" $DesktopChoice
  WriteRegDWORD HKCU "${APPKEY}" "StartMenuShortcut" $StartChoice
  ClearErrors
  ${If} $StartChoice == ${BST_CHECKED}
    CreateDirectory "$SMPROGRAMS\Adapta PE"
    CreateShortCut "$SMPROGRAMS\Adapta PE\Adapta PE.lnk" "$INSTDIR\AdaptaPE.exe" "" "$INSTDIR\AdaptaPE.ico"
  ${Else}
    Delete "$SMPROGRAMS\Adapta PE\Adapta PE.lnk"
    RMDir "$SMPROGRAMS\Adapta PE"
  ${EndIf}
  ${If} $DesktopChoice == ${BST_CHECKED}
    CreateShortCut "$DESKTOP\Adapta PE.lnk" "$INSTDIR\AdaptaPE.exe" "" "$INSTDIR\AdaptaPE.ico"
  ${Else}
    Delete "$DESKTOP\Adapta PE.lnk"
  ${EndIf}
  IfErrors 0 +2
    MessageBox MB_ICONEXCLAMATION|MB_OK "Adapta PE está instalado; Windows no permitió crear alguno de los accesos directos. Abre la app desde $INSTDIR."
SectionEnd

Section "Uninstall"
  Call un.CheckClosed
  ReadRegStr $0 HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "AdaptaPE"
  ${If} $0 == '$\"$INSTDIR\AdaptaPE.exe$\" --background'
    DeleteRegValue HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "AdaptaPE"
  ${EndIf}
  Delete "$DESKTOP\Adapta PE.lnk"
  Delete "$SMPROGRAMS\Adapta PE\Adapta PE.lnk"
  RMDir "$SMPROGRAMS\Adapta PE"
  DeleteRegKey HKCU "${APPKEY}"
  SetOutPath "$TEMP"
  ; Lista de archivos de esta compilación: conserva archivos ajenos y preferencias.
  !include "${DELETE_LIST}"
  Delete "$INSTDIR\Uninstall.exe"
  RMDir "$INSTDIR"
SectionEnd
