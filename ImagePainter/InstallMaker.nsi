; ================================================================================================
; World Map Wallpaper - NSIS Installer Script
; ================================================================================================

!include "MUI2.nsh"
!include "FileFunc.nsh"
!include "LogicLib.nsh"
!include "nsDialogs.nsh"
!include "WinMessages.nsh"
!include "x64.nsh"

!define MUI_ABORTWARNING

!ifndef BUILD_CONFIG
  !define BUILD_CONFIG "Debug"
!endif

!ifndef APP_VERSION
  !define APP_VERSION "2.0.0"
!endif

!ifndef INSTALLER_OUTPUT
  !define INSTALLER_OUTPUT "Install.exe"
!endif

!define APP_NAME "WorldMapWallpaper"
!define FRIEND_NAME "World Map Wallpaper"
!define MAIN_APP_EXE "${APP_NAME}.exe"
!define SETTINGS_APP_EXE "${APP_NAME}.Settings.exe"
!define PUBLISH_BUILD_PATH ".\bin\publish-64"
!define TASK_NAME "World Map Wallpaper"
!define UNINSTALL_REGKEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}"
!define CONTEXT_FILE "install-context.ini"
!define ALLUSERS_SWITCH "/allusers"
!define RESUME_SCOPE_SWITCH "/resume-scope"

Name "${FRIEND_NAME}"
OutFile "${INSTALLER_OUTPUT}"
RequestExecutionLevel user
InstallDir "$LOCALAPPDATA\Programs\${APP_NAME}"
SetCompressor /SOLID lzma

Var ScopeCurrentRadio
Var ScopeAllUsersRadio
Var ScopeInfoLabel
Var SelectedScope
Var ExistingCurrentUserInstall
Var ExistingCurrentUserPath
Var ExistingAllUsersInstall
Var ExistingAllUsersPath
Var OutputFolderTextBox
Var OutputFolderValue
Var OutputFolderBrowseButton
Var TaskCreationExitCode
Var InstallContextScope
Var RemoveUserDataCheckbox
Var RemoveUserData
Var SettingsRunning
Var MainRunning
Var TaskPresent
Var ResumeFromScopePage
Var OutputFolderLoadedFromSettings
Var SettingsHelperReadScript
Var SettingsHelperWriteScript

!define MUI_PAGE_CUSTOMFUNCTION_PRE PreWelcomePage
!insertmacro MUI_PAGE_WELCOME
!ifdef MUI_PAGE_CUSTOMFUNCTION_PRE
  !undef MUI_PAGE_CUSTOMFUNCTION_PRE
!endif
!define MUI_PAGE_CUSTOMFUNCTION_PRE PreLicensePage
!insertmacro MUI_PAGE_LICENSE "License.txt"
!ifdef MUI_PAGE_CUSTOMFUNCTION_PRE
  !undef MUI_PAGE_CUSTOMFUNCTION_PRE
!endif
Page custom CreateInstallScopePage LeaveInstallScopePage
!insertmacro MUI_PAGE_DIRECTORY
Page custom CreateOutputFolderPage LeaveOutputFolderPage
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_WELCOME
!insertmacro MUI_UNPAGE_CONFIRM
UninstPage custom un.CreateRemoveDataPage un.LeaveRemoveDataPage
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

!insertmacro MUI_LANGUAGE "English"

ReserveFile "License.txt"

Function .onInit
    SetRegView 64
    InitPluginsDir
    Call CreateSettingsHelperScripts

    StrCpy $SelectedScope "CurrentUser"
    StrCpy $OutputFolderValue "$PICTURES"
    StrCpy $OutputFolderLoadedFromSettings 0
    StrCpy $RemoveUserData 0
    StrCpy $ResumeFromScopePage 0

    ${GetParameters} $0
    ClearErrors
    ${GetOptions} $0 "${ALLUSERS_SWITCH}" $1
    ${IfNot} ${Errors}
        StrCpy $SelectedScope "AllUsers"
    ${EndIf}
    ClearErrors
    ${GetOptions} $0 "${RESUME_SCOPE_SWITCH}" $1
    ${IfNot} ${Errors}
        StrCpy $ResumeFromScopePage 1
    ${EndIf}

    Call DetectExistingInstalls

    ${If} $ExistingAllUsersInstall == 1
    ${AndIf} $ExistingCurrentUserInstall != 1
        StrCpy $SelectedScope "AllUsers"
        StrCpy $INSTDIR $ExistingAllUsersPath
    ${ElseIf} $ExistingCurrentUserInstall == 1
        StrCpy $SelectedScope "CurrentUser"
        StrCpy $INSTDIR $ExistingCurrentUserPath
    ${Else}
        Call UpdateInstallDirForScope
    ${EndIf}

    Call UpdateOutputFolderForScope
FunctionEnd

Function un.onInit
    SetRegView 64
    StrCpy $RemoveUserData 0

    Call un.EnsureUninstallPrivileges
    Call un.ResolveInstallScope
FunctionEnd

Function PreWelcomePage
    ${If} $ResumeFromScopePage == 1
        Abort
    ${EndIf}
FunctionEnd

Function PreLicensePage
    ${If} $ResumeFromScopePage == 1
        Abort
    ${EndIf}
FunctionEnd

Function DetectExistingInstalls
    StrCpy $ExistingCurrentUserInstall 0
    StrCpy $ExistingAllUsersInstall 0
    StrCpy $ExistingCurrentUserPath "$LOCALAPPDATA\Programs\${APP_NAME}"
    ${If} ${RunningX64}
        StrCpy $ExistingAllUsersPath "$PROGRAMFILES64\${APP_NAME}"
    ${Else}
        StrCpy $ExistingAllUsersPath "$PROGRAMFILES\${APP_NAME}"
    ${EndIf}

    ReadRegStr $0 HKCU "${UNINSTALL_REGKEY}" "InstallLocation"
    ${If} $0 != ""
        StrCpy $ExistingCurrentUserPath $0
    ${EndIf}
    ${If} ${FileExists} "$ExistingCurrentUserPath\${MAIN_APP_EXE}"
        StrCpy $ExistingCurrentUserInstall 1
    ${EndIf}

    ReadRegStr $1 HKLM "${UNINSTALL_REGKEY}" "InstallLocation"
    ${If} $1 != ""
        StrCpy $ExistingAllUsersPath $1
    ${EndIf}
    ${If} ${FileExists} "$ExistingAllUsersPath\${MAIN_APP_EXE}"
        StrCpy $ExistingAllUsersInstall 1
    ${EndIf}
FunctionEnd

Function CreateSettingsHelperScripts
    StrCpy $SettingsHelperReadScript "$PLUGINSDIR\ReadSettingsValue.ps1"
    StrCpy $SettingsHelperWriteScript "$PLUGINSDIR\WriteSettings.ps1"

    FileOpen $0 $SettingsHelperReadScript w
    FileWrite $0 'param([string]$$Path, [string]$$Name)$\r$\n'
    FileWrite $0 'if (-not (Test-Path -LiteralPath $$Path)) { exit 0 }$\r$\n'
    FileWrite $0 'try {$\r$\n'
    FileWrite $0 '  $$jsonText = Get-Content -LiteralPath $$Path -Raw$\r$\n'
    FileWrite $0 '  if ([string]::IsNullOrWhiteSpace($$jsonText)) { exit 0 }$\r$\n'
    FileWrite $0 '  $$json = $$jsonText | ConvertFrom-Json$\r$\n'
    FileWrite $0 '  $$value = $$json.$$Name$\r$\n'
    FileWrite $0 '  if ($$null -ne $$value) { [Console]::Out.Write($$value.ToString()) }$\r$\n'
    FileWrite $0 '} catch {$\r$\n'
    FileWrite $0 '  exit 0$\r$\n'
    FileWrite $0 '}$\r$\n'
    FileClose $0

    FileOpen $1 $SettingsHelperWriteScript w
    FileWrite $1 'param([string]$$Path, [string]$$OutputFolder, [string]$$TrayFallback)$\r$\n'
    FileWrite $1 '$$dir = Split-Path -Parent $$Path$\r$\n'
    FileWrite $1 'if (-not [string]::IsNullOrWhiteSpace($$dir)) { New-Item -ItemType Directory -Path $$dir -Force | Out-Null }$\r$\n'
    FileWrite $1 '$$defaults = [ordered]@{$\r$\n'
    FileWrite $1 '  ShowISS = $$true$\r$\n'
    FileWrite $1 '  ShowTimeZones = $$true$\r$\n'
    FileWrite $1 '  ShowPoliticalMap = $$true$\r$\n'
    FileWrite $1 '  UpdateInterval = "Hourly"$\r$\n'
    FileWrite $1 '  IsActive = $$true$\r$\n'
    FileWrite $1 '  ResolutionMode = "None"$\r$\n'
    FileWrite $1 '  CustomResolutionWidth = 0$\r$\n'
    FileWrite $1 '  CustomResolutionHeight = 0$\r$\n'
    FileWrite $1 '  SatelliteTrackingEnabled = $$true$\r$\n'
    FileWrite $1 '  WallpaperOutputDirectory = $$OutputFolder$\r$\n'
    FileWrite $1 '  TrayAutoUpdateFallbackEnabled = $$false$\r$\n'
    FileWrite $1 '}$\r$\n'
    FileWrite $1 '$$data = $$null$\r$\n'
    FileWrite $1 'if (Test-Path -LiteralPath $$Path) {$\r$\n'
    FileWrite $1 '  try {$\r$\n'
    FileWrite $1 '    $$jsonText = Get-Content -LiteralPath $$Path -Raw$\r$\n'
    FileWrite $1 '    if (-not [string]::IsNullOrWhiteSpace($$jsonText)) { $$data = $$jsonText | ConvertFrom-Json }$\r$\n'
    FileWrite $1 '  } catch {$\r$\n'
    FileWrite $1 '    $$data = $$null$\r$\n'
    FileWrite $1 '  }$\r$\n'
    FileWrite $1 '}$\r$\n'
    FileWrite $1 'if ($$null -eq $$data) { $$data = [pscustomobject]@{} }$\r$\n'
    FileWrite $1 'foreach ($$entry in $$defaults.GetEnumerator()) {$\r$\n'
    FileWrite $1 '  if ($$null -eq $$data.PSObject.Properties[$$entry.Key]) {$\r$\n'
    FileWrite $1 '    Add-Member -InputObject $$data -MemberType NoteProperty -Name $$entry.Key -Value $$entry.Value -Force$\r$\n'
    FileWrite $1 '  }$\r$\n'
    FileWrite $1 '}$\r$\n'
    FileWrite $1 '$$data.WallpaperOutputDirectory = $$OutputFolder$\r$\n'
    FileWrite $1 '$$data.TrayAutoUpdateFallbackEnabled = [System.Convert]::ToBoolean($$TrayFallback)$\r$\n'
    FileWrite $1 '$$utf8NoBom = New-Object System.Text.UTF8Encoding($$false)$\r$\n'
    FileWrite $1 '[System.IO.File]::WriteAllText($$Path, ($$data | ConvertTo-Json -Depth 10), $$utf8NoBom)$\r$\n'
    FileClose $1
FunctionEnd

Function GetSettingsDirectoryForScope
    ${If} $SelectedScope == "AllUsers"
        ReadEnvStr $0 "ProgramData"
        ${If} $0 == ""
            StrCpy $0 "$APPDATA"
        ${EndIf}
        StrCpy $0 "$0\${APP_NAME}"
    ${Else}
        StrCpy $0 "$APPDATA\${APP_NAME}"
    ${EndIf}
FunctionEnd

Function GetSettingsFilePathForScope
    Call GetSettingsDirectoryForScope
    StrCpy $0 "$0\settings.json"
FunctionEnd

Function LoadExistingOutputFolder
    StrCpy $OutputFolderLoadedFromSettings 0
    Call GetSettingsFilePathForScope

    ${IfNot} ${FileExists} "$0"
        Return
    ${EndIf}

    nsExec::ExecToStack '"$SYSDIR\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File "$SettingsHelperReadScript" -Path "$0" -Name "WallpaperOutputDirectory"'
    Pop $1
    Pop $2

    ${If} $1 == 0
    ${AndIf} $2 != ""
        StrCpy $OutputFolderValue $2
        StrCpy $OutputFolderLoadedFromSettings 1
    ${EndIf}
FunctionEnd

Function UpdateInstallDirForScope
    ${If} $SelectedScope == "AllUsers"
        ${If} ${RunningX64}
            StrCpy $INSTDIR "$PROGRAMFILES64\${APP_NAME}"
        ${Else}
            StrCpy $INSTDIR "$PROGRAMFILES\${APP_NAME}"
        ${EndIf}
    ${Else}
        StrCpy $INSTDIR "$LOCALAPPDATA\Programs\${APP_NAME}"
    ${EndIf}
FunctionEnd

Function UpdateOutputFolderForScope
    Call LoadExistingOutputFolder

    ${If} $OutputFolderLoadedFromSettings == 1
        Return
    ${EndIf}

    ${If} $SelectedScope == "AllUsers"
        System::Call 'shell32::SHGetKnownFolderPath(g "{B6EBFB86-6907-413C-9AF7-4FC2ABF07CC5}", i 0, p 0, *p .r0)'
        ${If} $0 <> 0
            StrCpy $OutputFolderValue "$PICTURES"
        ${Else}
            System::Call '*$0(&w260 .r1)'
            System::Call 'ole32::CoTaskMemFree(p r0)'
            StrCpy $OutputFolderValue $1
        ${EndIf}
    ${Else}
        StrCpy $OutputFolderValue "$PICTURES"
    ${EndIf}
FunctionEnd

Function CreateInstallScopePage
    nsDialogs::Create 1018
    Pop $0

    ${If} $0 == error
        Abort
    ${EndIf}

    ${NSD_CreateLabel} 0 0 100% 24u "Choose how you want to install ${FRIEND_NAME}."
    Pop $0

    ${NSD_CreateRadioButton} 0 34u 100% 12u "&Just for me"
    Pop $ScopeCurrentRadio

    ${NSD_CreateLabel} 18u 48u 90% 18u "Install to %LocalAppData%\Programs and use per-user startup/integration."
    Pop $0

    ${NSD_CreateRadioButton} 0 78u 100% 12u "F&or anyone"
    Pop $ScopeAllUsersRadio

    ${NSD_CreateLabel} 18u 92u 90% 24u "Install to Program Files and use machine-wide integration. This may require elevation."
    Pop $0

    ${NSD_CreateLabel} 0 128u 100% 36u ""
    Pop $ScopeInfoLabel

    ${If} $SelectedScope == "AllUsers"
        ${NSD_Check} $ScopeAllUsersRadio
        ${NSD_SetText} $ScopeInfoLabel "An existing machine-wide installation was detected. Upgrades should stay in the same scope."
    ${Else}
        ${NSD_Check} $ScopeCurrentRadio
        ${NSD_SetText} $ScopeInfoLabel "Per-user install is recommended when you do not need machine-wide setup."
    ${EndIf}

    nsDialogs::Show
FunctionEnd

Function LeaveInstallScopePage
    ${NSD_GetState} $ScopeAllUsersRadio $0
    ${If} $0 == ${BST_CHECKED}
        StrCpy $SelectedScope "AllUsers"
    ${Else}
        StrCpy $SelectedScope "CurrentUser"
    ${EndIf}

    ${If} $ExistingCurrentUserInstall == 1
    ${AndIf} $ExistingAllUsersInstall == 1
        MessageBox MB_ICONSTOP|MB_OK "Both a per-user and a machine-wide installation were detected. Remove one copy before continuing."
        Abort
    ${EndIf}

    ${If} $SelectedScope == "AllUsers"
        ${If} $ExistingCurrentUserInstall == 1
        ${AndIf} $ExistingAllUsersInstall != 1
            MessageBox MB_ICONSTOP|MB_OK "A 'Just for me' installation already exists at:$\r$\n$\r$\n$ExistingCurrentUserPath$\r$\n$\r$\nUpgrade in that same scope or uninstall it first."
            Abort
        ${EndIf}

        UserInfo::GetAccountType
        Pop $0
        ${If} $0 != "Admin"
            MessageBox MB_ICONINFORMATION|MB_OK "Installing for anyone requires administrator rights. The installer will now restart elevated."
            ExecShell "runas" "$EXEPATH" "${ALLUSERS_SWITCH} ${RESUME_SCOPE_SWITCH}"
            Quit
        ${EndIf}

        ${If} $ExistingAllUsersInstall == 1
            StrCpy $INSTDIR $ExistingAllUsersPath
        ${Else}
            Call UpdateInstallDirForScope
        ${EndIf}
    ${Else}
        ${If} $ExistingAllUsersInstall == 1
        ${AndIf} $ExistingCurrentUserInstall != 1
            MessageBox MB_ICONSTOP|MB_OK "A 'For anyone' installation already exists at:$\r$\n$\r$\n$ExistingAllUsersPath$\r$\n$\r$\nUpgrade in that same scope or uninstall it first."
            Abort
        ${EndIf}

        ${If} $ExistingCurrentUserInstall == 1
            StrCpy $INSTDIR $ExistingCurrentUserPath
        ${Else}
            Call UpdateInstallDirForScope
        ${EndIf}
    ${EndIf}

    Call UpdateOutputFolderForScope
FunctionEnd

Function CreateOutputFolderPage
    nsDialogs::Create 1018
    Pop $0

    ${If} $0 == error
        Abort
    ${EndIf}

    ${NSD_CreateLabel} 0 0 100% 28u "Choose where generated wallpaper images should be saved. This can be changed later in Settings."
    Pop $0

    ${NSD_CreateText} 0 42u 78% 12u "$OutputFolderValue"
    Pop $OutputFolderTextBox

    ${NSD_CreateButton} 82% 40u 18% 14u "Browse..."
    Pop $OutputFolderBrowseButton
    ${NSD_OnClick} $OutputFolderBrowseButton BrowseForOutputFolder

    nsDialogs::Show
FunctionEnd

Function LeaveOutputFolderPage
    ${NSD_GetText} $OutputFolderTextBox $OutputFolderValue

    ${If} $OutputFolderValue == ""
        MessageBox MB_ICONSTOP|MB_OK "Choose a folder for generated wallpaper images before continuing."
        Abort
    ${EndIf}
FunctionEnd

Function BrowseForOutputFolder
    nsDialogs::SelectFolderDialog "Choose wallpaper output folder" "$OutputFolderValue"
    Pop $0
    ${If} $0 != error
        StrCpy $OutputFolderValue $0
        ${NSD_SetText} $OutputFolderTextBox $OutputFolderValue
    ${EndIf}
FunctionEnd

Function DetectRunningState
    StrCpy $SettingsRunning 0
    StrCpy $MainRunning 0
    StrCpy $TaskPresent 0

    nsExec::Exec 'cmd /c tasklist /FI "IMAGENAME eq ${SETTINGS_APP_EXE}" | find /I "${SETTINGS_APP_EXE}" >nul'
    Pop $0
    ${If} $0 == 0
        StrCpy $SettingsRunning 1
    ${EndIf}

    nsExec::Exec 'cmd /c tasklist /FI "IMAGENAME eq ${MAIN_APP_EXE}" | find /I "${MAIN_APP_EXE}" >nul'
    Pop $0
    ${If} $0 == 0
        StrCpy $MainRunning 1
    ${EndIf}

    nsExec::Exec 'cmd /c schtasks /query /tn "${TASK_NAME}" >nul 2>nul'
    Pop $0
    ${If} $0 == 0
        StrCpy $TaskPresent 1
    ${EndIf}
FunctionEnd

Function EnsureRunningAppsClosed
    Call DetectRunningState

    ${If} $SettingsRunning == 1
    ${OrIf} $MainRunning == 1
    ${OrIf} $TaskPresent == 1
        MessageBox MB_ICONQUESTION|MB_YESNO "A running copy of ${FRIEND_NAME} was detected. The installer needs to stop the app and its scheduled task before continuing with the upgrade.$\r$\n$\r$\nContinue?" IDYES +2
        Abort

        Call DisableScheduledTask
        Call StopRunningProcesses
        Call DetectRunningState

        ${If} $SettingsRunning == 1
        ${OrIf} $MainRunning == 1
            MessageBox MB_ICONSTOP|MB_OK "The installer could not stop the running application. Close ${FRIEND_NAME} and try again."
            Abort
        ${EndIf}
    ${EndIf}
FunctionEnd

Function StopRunningProcesses
    nsExec::Exec 'cmd /c taskkill /IM "${SETTINGS_APP_EXE}" /T /F >nul 2>nul'
    Pop $0
    nsExec::Exec 'cmd /c taskkill /IM "${MAIN_APP_EXE}" /T /F >nul 2>nul'
    Pop $0
FunctionEnd

Function DisableScheduledTask
    nsExec::Exec 'cmd /c schtasks /change /tn "${TASK_NAME}" /disable >nul 2>nul'
    Pop $0
    nsExec::Exec 'cmd /c schtasks /end /tn "${TASK_NAME}" >nul 2>nul'
    Pop $0
FunctionEnd

Function un.StopRunningProcesses
    nsExec::Exec 'cmd /c taskkill /IM "${SETTINGS_APP_EXE}" /T /F >nul 2>nul'
    Pop $0
    nsExec::Exec 'cmd /c taskkill /IM "${MAIN_APP_EXE}" /T /F >nul 2>nul'
    Pop $0
FunctionEnd

Function un.DisableScheduledTask
    nsExec::Exec 'cmd /c schtasks /change /tn "${TASK_NAME}" /disable >nul 2>nul'
    Pop $0
    nsExec::Exec 'cmd /c schtasks /end /tn "${TASK_NAME}" >nul 2>nul'
    Pop $0
FunctionEnd

Function un.DeleteScheduledTask
    nsExec::Exec 'cmd /c schtasks /delete /tn "${TASK_NAME}" /f >nul 2>nul'
    Pop $0
FunctionEnd

Function un.EnsureUninstallPrivileges
    ReadRegStr $0 HKLM "${UNINSTALL_REGKEY}" "InstallLocation"
    ${If} $0 == $INSTDIR
        UserInfo::GetAccountType
        Pop $1
        ${If} $1 != "Admin"
            MessageBox MB_ICONINFORMATION|MB_OK "Uninstalling the machine-wide installation requires administrator rights. The uninstaller will now restart elevated."
            ExecShell "runas" "$INSTDIR\Uninstall.exe"
            Quit
        ${EndIf}
    ${EndIf}
FunctionEnd

Function CreateSchedulerTask
    StrCpy $TaskCreationExitCode 1
    StrCpy $0 "$PLUGINSDIR\${APP_NAME}Task.xml"

    ClearErrors
    UserInfo::GetName
    Pop $1

    FileOpen $2 $0 w
    FileWrite $2 '<?xml version="1.0" encoding="UTF-16"?>$\r$\n'
    FileWrite $2 '<Task version="1.2" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">$\r$\n'
    FileWrite $2 '  <RegistrationInfo>$\r$\n'
    FileWrite $2 '    <Date>2020-01-01T00:00:00</Date>$\r$\n'
    FileWrite $2 '    <Author>${FRIEND_NAME}</Author>$\r$\n'
    FileWrite $2 '  </RegistrationInfo>$\r$\n'
    FileWrite $2 '  <Triggers>$\r$\n'
    FileWrite $2 '    <BootTrigger>$\r$\n'
    FileWrite $2 '      <Enabled>true</Enabled>$\r$\n'
    FileWrite $2 '    </BootTrigger>$\r$\n'
    FileWrite $2 '    <LogonTrigger>$\r$\n'
    FileWrite $2 '      <Enabled>true</Enabled>$\r$\n'
    FileWrite $2 '    </LogonTrigger>$\r$\n'
    FileWrite $2 '    <EventTrigger>$\r$\n'
    FileWrite $2 '      <Enabled>true</Enabled>$\r$\n'
    FileWrite $2 '      <Subscription>&lt;QueryList&gt;&lt;Query Id="0" Path="System"&gt;&lt;Select Path="System"&gt;*[System[Provider[@Name=$\'Microsoft-Windows-Power-Troubleshooter$\'] and EventID=1]]&lt;/Select&gt;&lt;/Query&gt;&lt;/QueryList&gt;</Subscription>$\r$\n'
    FileWrite $2 '    </EventTrigger>$\r$\n'
    FileWrite $2 '    <TimeTrigger>$\r$\n'
    FileWrite $2 '      <Repetition>$\r$\n'
    FileWrite $2 '        <Interval>PT1H</Interval>$\r$\n'
    FileWrite $2 '        <StopAtDurationEnd>false</StopAtDurationEnd>$\r$\n'
    FileWrite $2 '      </Repetition>$\r$\n'
    FileWrite $2 '      <StartBoundary>2020-01-01T00:00:00</StartBoundary>$\r$\n'
    FileWrite $2 '      <Enabled>true</Enabled>$\r$\n'
    FileWrite $2 '    </TimeTrigger>$\r$\n'
    FileWrite $2 '  </Triggers>$\r$\n'
    FileWrite $2 '  <Principals>$\r$\n'
    FileWrite $2 '    <Principal id="Author">$\r$\n'
    FileWrite $2 '      <UserId>$1</UserId>$\r$\n'
    FileWrite $2 '      <LogonType>InteractiveToken</LogonType>$\r$\n'
    FileWrite $2 '      <RunLevel>HighestAvailable</RunLevel>$\r$\n'
    FileWrite $2 '    </Principal>$\r$\n'
    FileWrite $2 '  </Principals>$\r$\n'
    FileWrite $2 '  <Settings>$\r$\n'
    FileWrite $2 '    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>$\r$\n'
    FileWrite $2 '    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>$\r$\n'
    FileWrite $2 '    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>$\r$\n'
    FileWrite $2 '    <AllowHardTerminate>true</AllowHardTerminate>$\r$\n'
    FileWrite $2 '    <StartWhenAvailable>true</StartWhenAvailable>$\r$\n'
    FileWrite $2 '    <AllowStartOnDemand>true</AllowStartOnDemand>$\r$\n'
    FileWrite $2 '    <Enabled>true</Enabled>$\r$\n'
    FileWrite $2 '    <Hidden>false</Hidden>$\r$\n'
    FileWrite $2 '    <ExecutionTimeLimit>PT72H</ExecutionTimeLimit>$\r$\n'
    FileWrite $2 '    <Priority>7</Priority>$\r$\n'
    FileWrite $2 '  </Settings>$\r$\n'
    FileWrite $2 '  <Actions Context="Author">$\r$\n'
    FileWrite $2 '    <Exec>$\r$\n'
    FileWrite $2 '      <Command>$INSTDIR\${MAIN_APP_EXE}</Command>$\r$\n'
    FileWrite $2 '    </Exec>$\r$\n'
    FileWrite $2 '  </Actions>$\r$\n'
    FileWrite $2 '</Task>$\r$\n'
    FileClose $2

    nsExec::Exec 'cmd /c schtasks /create /tn "${TASK_NAME}" /xml "$PLUGINSDIR\${APP_NAME}Task.xml" /f >nul 2>nul'
    Pop $TaskCreationExitCode
FunctionEnd

Function WriteInstallerSettings
    Call GetSettingsFilePathForScope
    ${If} $TaskCreationExitCode == 0
        StrCpy $1 "False"
    ${Else}
        StrCpy $1 "True"
    ${EndIf}

    nsExec::Exec '"$SYSDIR\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File "$SettingsHelperWriteScript" -Path "$0" -OutputFolder "$OutputFolderValue" -TrayFallback "$1"'
    Pop $2
FunctionEnd

Function RegisterIntegration
    ${If} $SelectedScope == "AllUsers"
        WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\${MAIN_APP_EXE}" "" "$INSTDIR\${MAIN_APP_EXE}"
        WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\${MAIN_APP_EXE}" "Path" "$INSTDIR"
        WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\${SETTINGS_APP_EXE}" "" "$INSTDIR\${SETTINGS_APP_EXE}"
        WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\${SETTINGS_APP_EXE}" "Path" "$INSTDIR"
        WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Run" "WorldMapWallpaperSettings" '"$INSTDIR\${SETTINGS_APP_EXE}" --minimized'
        WriteRegStr HKLM "${UNINSTALL_REGKEY}" "DisplayName" "${FRIEND_NAME}"
        WriteRegStr HKLM "${UNINSTALL_REGKEY}" "DisplayVersion" "${APP_VERSION}"
        WriteRegStr HKLM "${UNINSTALL_REGKEY}" "Publisher" "Paul St Smith"
        WriteRegStr HKLM "${UNINSTALL_REGKEY}" "InstallLocation" "$INSTDIR"
        WriteRegStr HKLM "${UNINSTALL_REGKEY}" "UninstallString" '"$INSTDIR\Uninstall.exe"'
        WriteRegStr HKLM "${UNINSTALL_REGKEY}" "QuietUninstallString" '"$INSTDIR\Uninstall.exe" /S'
        WriteRegDWORD HKLM "${UNINSTALL_REGKEY}" "NoModify" 1
        WriteRegDWORD HKLM "${UNINSTALL_REGKEY}" "NoRepair" 1
        WriteRegStr HKLM "${UNINSTALL_REGKEY}" "InstallScope" "AllUsers"
    ${Else}
        WriteRegStr HKCU "SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\${MAIN_APP_EXE}" "" "$INSTDIR\${MAIN_APP_EXE}"
        WriteRegStr HKCU "SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\${MAIN_APP_EXE}" "Path" "$INSTDIR"
        WriteRegStr HKCU "SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\${SETTINGS_APP_EXE}" "" "$INSTDIR\${SETTINGS_APP_EXE}"
        WriteRegStr HKCU "SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\${SETTINGS_APP_EXE}" "Path" "$INSTDIR"
        WriteRegStr HKCU "SOFTWARE\Microsoft\Windows\CurrentVersion\Run" "WorldMapWallpaperSettings" '"$INSTDIR\${SETTINGS_APP_EXE}" --minimized'
        WriteRegStr HKCU "${UNINSTALL_REGKEY}" "DisplayName" "${FRIEND_NAME}"
        WriteRegStr HKCU "${UNINSTALL_REGKEY}" "DisplayVersion" "${APP_VERSION}"
        WriteRegStr HKCU "${UNINSTALL_REGKEY}" "Publisher" "Paul St Smith"
        WriteRegStr HKCU "${UNINSTALL_REGKEY}" "InstallLocation" "$INSTDIR"
        WriteRegStr HKCU "${UNINSTALL_REGKEY}" "UninstallString" '"$INSTDIR\Uninstall.exe"'
        WriteRegStr HKCU "${UNINSTALL_REGKEY}" "QuietUninstallString" '"$INSTDIR\Uninstall.exe" /S'
        WriteRegDWORD HKCU "${UNINSTALL_REGKEY}" "NoModify" 1
        WriteRegDWORD HKCU "${UNINSTALL_REGKEY}" "NoRepair" 1
        WriteRegStr HKCU "${UNINSTALL_REGKEY}" "InstallScope" "CurrentUser"
    ${EndIf}
FunctionEnd

Function WriteInstallContext
    WriteINIStr "$INSTDIR\${CONTEXT_FILE}" "Install" "Scope" "$SelectedScope"
    WriteINIStr "$INSTDIR\${CONTEXT_FILE}" "Install" "OutputFolder" "$OutputFolderValue"
FunctionEnd

Section "Install" SecInstall
    Call EnsureRunningAppsClosed

    SetOutPath "$INSTDIR"
    File /a /r "${PUBLISH_BUILD_PATH}\*.*"
    File "License.txt"

    WriteUninstaller "$INSTDIR\Uninstall.exe"
    Call WriteInstallContext

    Call CreateSchedulerTask
    Call WriteInstallerSettings
    Call RegisterIntegration

    ExecWait '"$INSTDIR\${MAIN_APP_EXE}"'
    Exec '"$INSTDIR\${SETTINGS_APP_EXE}" --minimized'
SectionEnd

Function un.ResolveInstallScope
    StrCpy $InstallContextScope "CurrentUser"
    ReadINIStr $0 "$INSTDIR\${CONTEXT_FILE}" "Install" "Scope"
    ${If} $0 != ""
        StrCpy $InstallContextScope $0
        Return
    ${EndIf}

    ReadRegStr $1 HKLM "${UNINSTALL_REGKEY}" "InstallLocation"
    ${If} $1 == $INSTDIR
        StrCpy $InstallContextScope "AllUsers"
        Return
    ${EndIf}

    ReadRegStr $1 HKCU "${UNINSTALL_REGKEY}" "InstallLocation"
    ${If} $1 == $INSTDIR
        StrCpy $InstallContextScope "CurrentUser"
    ${EndIf}
FunctionEnd

Function un.CreateRemoveDataPage
    nsDialogs::Create 1018
    Pop $0

    ${If} $0 == error
        Abort
    ${EndIf}

    ${NSD_CreateLabel} 0 0 100% 24u "Choose whether user settings and app data should be removed."
    Pop $0

    ${NSD_CreateCheckbox} 0 36u 100% 12u "Remove user settings and app data (settings.json, satellites, and cache files)"
    Pop $RemoveUserDataCheckbox

    nsDialogs::Show
FunctionEnd

Function un.LeaveRemoveDataPage
    ${NSD_GetState} $RemoveUserDataCheckbox $0
    ${If} $0 == ${BST_CHECKED}
        StrCpy $RemoveUserData 1
    ${Else}
        StrCpy $RemoveUserData 0
    ${EndIf}
FunctionEnd

Section "Uninstall" SecUninstall
    Call un.DisableScheduledTask
    Call un.DeleteScheduledTask
    Call un.StopRunningProcesses

    ${If} $InstallContextScope == "AllUsers"
        DeleteRegKey HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\${MAIN_APP_EXE}"
        DeleteRegKey HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\${SETTINGS_APP_EXE}"
        DeleteRegValue HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Run" "WorldMapWallpaperSettings"
        DeleteRegKey HKLM "${UNINSTALL_REGKEY}"
    ${Else}
        DeleteRegKey HKCU "SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\${MAIN_APP_EXE}"
        DeleteRegKey HKCU "SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\${SETTINGS_APP_EXE}"
        DeleteRegValue HKCU "SOFTWARE\Microsoft\Windows\CurrentVersion\Run" "WorldMapWallpaperSettings"
        DeleteRegKey HKCU "${UNINSTALL_REGKEY}"
    ${EndIf}

    ${If} $RemoveUserData == 1
        ${If} $InstallContextScope == "AllUsers"
            ReadEnvStr $0 "ProgramData"
            ${If} $0 == ""
                StrCpy $0 "$APPDATA"
            ${EndIf}
            RMDir /r "$0\${APP_NAME}"
        ${Else}
            RMDir /r "$APPDATA\${APP_NAME}"
        ${EndIf}
    ${EndIf}

    ${If} ${FileExists} "$INSTDIR\${CONTEXT_FILE}"
        RMDir /r "$INSTDIR"
    ${Else}
        Delete "$INSTDIR\Uninstall.exe"
        RMDir "$INSTDIR"
    ${EndIf}
SectionEnd
