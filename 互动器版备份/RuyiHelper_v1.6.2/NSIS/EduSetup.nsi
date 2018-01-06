; �ýű�ʹ�� HM VNISEdit �ű��༭���򵼲���

!include "LogicLib.nsh"

!define /date PRODUCT_TIME %Y%m%d%H%M%S
!define /date DATE_Y %y
!define /date DATE_MD %m%d

;  OutFile  "Test_${PRODUCT_VERSION}_${PRODUCT_DATE}.exe"

; ��װ�����ʼ���峣��
!define PRODUCT_NAME "��eС����"
!define PRODUCT_VERSION "1.0.${DATE_Y}.${DATE_MD}"
!define PRODUCT_PUBLISHER "�������Ⱥ���Ƽ���չ���޹�˾"
!define PRODUCT_WEB_SITE "http://www.groupnetwork.cn"
!define PRODUCT_DIR_REGKEY "Software\Microsoft\Windows\CurrentVersion\App Paths\Update.exe"
!define PRODUCT_UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}"
!define PRODUCT_UNINST_ROOT_KEY "HKLM"

SetCompressor lzma

; ------ MUI �ִ����涨�� (1.67 �汾���ϼ���) ------
!include "MUI.nsh"

; ------ ȡ��ǰ���� ------
!include "FileFunc.nsh"

; MUI Ԥ���峣��
!define MUI_ABORTWARNING
!define MUI_ICON "ico32.ico"
!define MUI_UNICON "${NSISDIR}\Contrib\Graphics\Icons\modern-uninstall.ico"
!define ExeName "��eС����.exe"

;!define MUI_WELCOMEFINISHPAGE_BITMAP "left.bmp"

; ��ӭҳ��
!insertmacro MUI_PAGE_WELCOME
; ��װĿ¼ѡ��ҳ��
!insertmacro MUI_PAGE_DIRECTORY
; ��װ����ҳ��
!insertmacro MUI_PAGE_INSTFILES
; ��װ���ҳ��
!define MUI_FINISHPAGE_RUN "$INSTDIR\${ExeName}"
!insertmacro MUI_PAGE_FINISH

; ��װж�ع���ҳ��
!insertmacro MUI_UNPAGE_INSTFILES

; ��װ�����������������
!insertmacro MUI_LANGUAGE "SimpChinese"

;�ļ��汾����
  VIProductVersion "1.0.2.1"
  VIAddVersionKey /LANG=2052 "ProductName" "��eС����"
  VIAddVersionKey /LANG=2052 "Comments" "���ʹ�ã����޷ַ���"
  VIAddVersionKey /LANG=2052 "CompanyName" "www.groupnetwork.cn"
  VIAddVersionKey /LANG=2052 "LegalTrademarks" "��eС����"
  VIAddVersionKey /LANG=2052 "LegalCopyright" "�������Ⱥ���Ƽ���չ���޹�˾"
  VIAddVersionKey /LANG=2052 "FileDescription" "��eС����"
  VIAddVersionKey /LANG=2052 "FileVersion" "${Ver}"
  VIAddVersionKey /LANG=2052 "ProductVersion" "${Ver}" ;��Ʒ�汾
;VIAddVersionKey InternalName "${Name}" ;�ڲ�����
;VIAddVersionKey LegalTrademarks "${PRODUCT_PUBLISHER}" ;�Ϸ��̱� ;
;VIAddVersionKey OriginalFilename "XX.exe" ;Դ�ļ���
;VIAddVersionKey PrivateBuild "XX" ;�����ڲ��汾˵��
;VIAddVersionKey SpecialBuild "XX" ;�����ڲ�����˵��

; ��װԤ�ͷ��ļ�
!insertmacro MUI_RESERVEFILE_INSTALLOPTIONS
; ------ MUI �ִ����涨����� ------

ReserveFile "${NSISDIR}\Plugins\splash.dll"
ReserveFile "splash.png"

Name "${PRODUCT_NAME} ${PRODUCT_VERSION}"
OutFile "D:\Release\��eС����\��eС����_${PRODUCT_VERSION}_Beta.exe"
InstallDir "D:\Program Files\��eС����"
InstallDirRegKey HKLM "${PRODUCT_UNINST_KEY}" "UninstallString"
ShowInstDetails show
ShowUnInstDetails show
BrandingText "�������www.groupnetwork.cn"

Section "������" SEC01
  SetOutPath "$INSTDIR"
  SetOverwrite on
  
  Delete "$INSTDIR\RueHelper.exe"
  Delete "$INSTDIR\RueHelper.exe.config"
  Delete "$INSTDIR\Update.exe"
  Delete "$INSTDIR\Update.exe.config"
  Delete "$INSTDIR\RueSqlite.db"

  File "..\��eС����_Release\update.ini"
  File "..\��eС����_Release\RueUpdate.exe"
  File "..\��eС����_Release\RueMng.exe"

  File "..\��eС����_Release\${ExeName}"
  File "..\��eС����_Release\Newtonsoft.Json.dll"
  File "..\��eС����_Release\Interop.WMPLib.dll"
  File "..\��eС����_Release\ICSharpCode.SharpZipLib.dll"
  File "..\��eС����_Release\AxInterop.WMPLib.dll"
  File "..\��eС����_Release\SQLite.Interop.dll"
  File "..\��eС����_Release\System.Data.SQLite.dll"
  File "..\��eС����_Release\log4net.dll"
  File "..\��eС����_Release\log4net.config"

  CreateDirectory "$SMPROGRAMS\��eС����"
  CreateShortCut "$SMPROGRAMS\��eС����\��eС����.lnk" "$INSTDIR\${ExeName}"
  CreateShortCut "$DESKTOP\��eС����.lnk" "$INSTDIR\${ExeName}"
  
  File "..\��eС����_Release\${ExeName}.config"
  SetOverwrite off
  File "..\��eС����_Release\App.ini"
SectionEnd


Section -AdditionalIcons
  WriteIniStr "$INSTDIR\${PRODUCT_NAME}.url" "InternetShortcut" "URL" "${PRODUCT_WEB_SITE}"
  CreateShortCut "$SMPROGRAMS\��eС����\Website.lnk" "$INSTDIR\${PRODUCT_NAME}.url"
  CreateShortCut "$SMPROGRAMS\��eС����\Uninstall.lnk" "$INSTDIR\uninst.exe"
SectionEnd

Section -Post
  WriteUninstaller "$INSTDIR\uninst.exe"
  WriteRegStr HKLM "${PRODUCT_DIR_REGKEY}" "" "$INSTDIR\RueUpdate.exe"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayName" "$(^Name)"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "UninstallString" "$INSTDIR\uninst.exe"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayIcon" "$INSTDIR\Update.exe"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayVersion" "${PRODUCT_VERSION}"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "URLInfoAbout" "${PRODUCT_WEB_SITE}"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "Publisher" "${PRODUCT_PUBLISHER}"
SectionEnd

#-- ���� NSIS �ű��༭�������� Function ���α�������� Section ����֮���д���Ա��ⰲװ�������δ��Ԥ֪�����⡣--#

Function .onInit0
    Push "${ExeName}"
    ProcessWork::existsprocess
    Pop $R0
  StrCmp $R0 "1" AA BB
  AA:
  MessageBox   MB_ICONSTOP   "��eС���� ��������,���ȹرճ���!"
  Quit
  BB:
  !insertmacro MUI_LANGDLL_DISPLAY
FunctionEnd


Function .onInit
;�رս���
  Push $R0
  CheckProc:
    Push "${ExeName}"
    ProcessWork::existsprocess
    Pop $R0
    IntCmp $R0 0 Done
    MessageBox MB_OKCANCEL|MB_ICONSTOP "��װ�����⵽ ${PRODUCT_NAME} �������С�$\r$\n$\r$\n��� ��ȷ���� ǿ�ƹر�${PRODUCT_NAME}��������װ��$\r$\n��� ��ȡ���� �˳���װ����" IDCANCEL Exit
    Push "${ExeName}"
    Processwork::KillProcess
    Sleep 1000
    Goto CheckProc
    Exit:
    Abort
    Done:
    Pop $R0

FunctionEnd

/******************************
 *  �����Ǽ��NetFramework4   *
 ******************************/
Section -.NET
Call GetNetFrameworkVersion
Pop $R1
 ${If} $R1 < '4.0.30319'
 SetDetailsPrint textonly
 DetailPrint "���ڰ�װ .NET Framework 4"
 SetDetailsPrint listonly
 SetOutPath "$TEMP"
 SetOverwrite on
 ;File "dotNetFx40_Full_x86_x64.exe"
 ;ExecWait '$TEMP\dotNetFx40_Full_x86_x64.exe /q /norestart /ChainingPackage FullX64Bootstrapper' $R1
 ;Delete "$TEMP\dotNetFx40_Full_x86_x64.exe"
 ${EndIf}
SectionEnd

Function GetNetFrameworkVersion
;��ȡ.Net Framework�汾֧��
Push $1
Push $0

ReadRegDWORD $0 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" "Install"
ReadRegDWORD $1 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" "Version"
StrCmp $0 1 KnowNetFrameworkVersion +1

ReadRegDWORD $0 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v3.5" "Install"
ReadRegDWORD $1 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v3.5" "Version"
StrCmp $0 1 KnowNetFrameworkVersion +1

ReadRegDWORD $0 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v3.0\Setup" "InstallSuccess"
ReadRegDWORD $1 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v3.0\Setup" "Version"
StrCmp $0 1 KnowNetFrameworkVersion +1

ReadRegDWORD $0 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v2.0.50727" "Install"
ReadRegDWORD $1 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v2.0.50727" "Version"
StrCmp $1 "" +1 +2
StrCpy $1 "2.0.50727.832"
StrCmp $0 1 KnowNetFrameworkVersion +1
ReadRegDWORD $0 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v1.1.4322" "Install"
ReadRegDWORD $1 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v1.1.4322" "Version"
StrCmp $1 "" +1 +2
StrCpy $1 "1.1.4322.573"
StrCmp $0 1 KnowNetFrameworkVersion +1
ReadRegDWORD $0 HKLM "SOFTWARE\Microsoft\.NETFramework\policy\v1.0" "Install"
ReadRegDWORD $1 HKLM "SOFTWARE\Microsoft\.NETFramework\policy\v1.0" "Version"
StrCmp $1 "" +1 +2
StrCpy $1 "1.0.3705.0"
StrCmp $0 1 KnowNetFrameworkVersion +1
StrCpy $1 "not .NetFramework"
KnowNetFrameworkVersion:
Pop $0
Exch $1
FunctionEnd

/******************************
 *  �����ǰ�װ�����ж�ز���  *
 ******************************/
Section Uninstall
  Delete "$INSTDIR\${PRODUCT_NAME}.url"
  Delete "$INSTDIR\uninst.exe"
  Delete "$INSTDIR\AxInterop.WMPLib.dll"
  Delete "$INSTDIR\ICSharpCode.SharpZipLib.dll"
  Delete "$INSTDIR\Interop.WMPLib.dll"
  Delete "$INSTDIR\log4net.config"
  Delete "$INSTDIR\log4net.dll"
  Delete "$INSTDIR\Newtonsoft.Json.dll"
  Delete "$INSTDIR\SQLite.Interop.dll"
  Delete "$INSTDIR\System.Data.SQLite.dll"
  Delete "$INSTDIR\${ExeName}"
  Delete "$INSTDIR\RueUpdate.exe"
  Delete "$INSTDIR\RueMng.exe"
  Delete "$INSTDIR\update.ini"
  
  Delete "$INSTDIR\update.log"
  Delete "$INSTDIR\msg.log"
  Delete "$INSTDIR\RueSqlite.db"

  Delete "$SMPROGRAMS\��eС����\Uninstall.lnk"
  Delete "$SMPROGRAMS\��eС����\Website.lnk"
  Delete "$DESKTOP\��eС����.lnk"
  Delete "$SMPROGRAMS\��eС����\��eС����.lnk"

  RMDir "$SMPROGRAMS\��eС����\conf"
; �����װ���򴴽�������ж��ʱ����Ϊ�յ���Ŀ¼�����ڵݹ���ӵ��ļ�Ŀ¼���������ڲ����Ŀ¼��ʼ���(ע�⣬��Ҫ�� /r �����������ʧȥ DelFileByLog ������)
;  RMDir "$SMPROGRAMS\��eС����"
;  RMDir "$INSTDIR"

  DeleteRegKey ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}"
  DeleteRegKey HKLM "${PRODUCT_DIR_REGKEY}"
  SetAutoClose true
SectionEnd

#-- ���� NSIS �ű��༭�������� Function ���α�������� Section ����֮���д���Ա��ⰲװ�������δ��Ԥ֪�����⡣--#

Function un.onInit
  MessageBox MB_ICONQUESTION|MB_YESNO|MB_DEFBUTTON2 "��ȷʵҪ��ȫ�Ƴ� $(^Name) ���������е������" IDYES +2
  Abort
  ;�������Ƿ�����

  Push $R0
  CheckProc:
    Push "${ExeName}"
    ProcessWork::existsprocess
    Pop $R0
    IntCmp $R0 0 Done
    MessageBox MB_OKCANCEL|MB_ICONSTOP "ж�س����⵽ ${PRODUCT_NAME} �������С�$\r$\n$\r$\n��� ��ȷ���� ǿ�ƹر�${PRODUCT_NAME}������ж�ء�$\r$\n��� ��ȡ���� �˳�ж�س���" IDCANCEL Exit
    Push "${ExeName}"
    Processwork::KillProcess
    Sleep 1000
    Goto CheckProc
    Exit:
    Abort
    Done:
    Pop $R0
FunctionEnd

Function un.onUninstSuccess
  HideWindow
  MessageBox MB_ICONINFORMATION|MB_OK "$(^Name) �ѳɹ��ش����ļ�����Ƴ���"
FunctionEnd

