; ONVIF Device Manager (Avalonia) - Inno Setup Installer Script
; Compile with Inno Setup 6.x: https://jrsoftware.org/isinfo.php
;
; Before compiling, run:
;   dotnet publish src/OnvifDeviceManager -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish/OnvifDeviceManager-Avalonia-win-x64

#define MyAppName "ONVIF Device Manager (Cross-Platform)"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Robert Foster"
#define MyAppURL "https://github.com/devildog5x5/ONVIF-ODM"
#define MyAppExeName "OnvifDeviceManager.exe"
#define MyAppSourceDir "..\..\publish\OnvifDeviceManager-Avalonia-win-x64"

[Setup]
AppId={{C9D0E1F2-3A4B-5C6D-7E8F-091A2B3C4D5E}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\ONVIF Device Manager Avalonia
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=..\..\LICENSE
OutputDir=..\..\publish\installers
OutputBaseFilename=OnvifDeviceManager-Avalonia-Setup-{#MyAppVersion}
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
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
