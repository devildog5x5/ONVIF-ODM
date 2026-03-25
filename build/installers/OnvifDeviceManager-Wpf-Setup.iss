; ONVIF Device Manager (WPF) - Inno Setup Installer Script
; Compile with Inno Setup 6.x: https://jrsoftware.org/isinfo.php
; Output filename includes compile date/time (local) via ISPP: OnvifDeviceManager-Wpf-Setup-{version}-{yyyyMMddHHmmss}.exe
;
; Before compiling, run:
;   dotnet publish src/OnvifDeviceManager.Wpf -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish/OnvifDeviceManager-Wpf-win-x64

#define MyAppName "ONVIF Device Manager"
#define MyAppVersion "1.5.0"
; ISPP FormatDateTime: yyyy MM dd; hh=24h hour (padded); nn=minute; ss=second
#define BuildStamp GetDateTimeString('yyyyMMdd-hhnnss', '', '')
#define MyAppPublisher "Robert Foster"
#define MyAppURL "https://github.com/devildog5x5/ONVIF-ODM"
#define MyAppExeName "OnvifDeviceManager.Wpf.exe"
#define MyAppSourceDir "..\..\publish\OnvifDeviceManager-Wpf-win-x64"

[Setup]
AppId={{A7F8E3D2-1B4C-5D6E-9F0A-B2C3D4E5F678}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=..\..\LICENSE
OutputDir=..\..\publish\installers
OutputBaseFilename=OnvifDeviceManager-Wpf-Setup-{#MyAppVersion}-{#BuildStamp}
SetupIconFile=..\..\warrior_icon.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#MyAppSourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\warrior_icon.ico"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; IconFilename: "{app}\warrior_icon.ico"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
