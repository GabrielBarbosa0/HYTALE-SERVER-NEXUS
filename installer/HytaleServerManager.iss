#ifndef MyAppVersion
  #define MyAppVersion "1.1.0"
#endif

#define MyAppName "HTServer Nexus"
#define MyAppPublisher "Gabriel Barbosa"
#define MyAppExeName "HTServerNexus.exe"

[Setup]
AppId={{D23D2E29-8FA2-47F9-9BC5-64368EEC1C1E}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL=https://github.com/GabrielBarbosa0/HYTALE-SERVER-NEXUS
AppSupportURL=https://github.com/GabrielBarbosa0/HYTALE-SERVER-NEXUS/issues
AppUpdatesURL=https://github.com/GabrielBarbosa0/HYTALE-SERVER-NEXUS/releases
DefaultDirName={localappdata}\HytaleServerManager\app
DefaultGroupName=HTServer Nexus
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=..\artifacts\installer
OutputBaseFilename=HTServer-Nexus-Setup-{#MyAppVersion}
SetupIconFile=..\src\HytaleServerManager\Assets\HytaleServerManager.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=force
CloseApplicationsFilter={#MyAppExeName},HytaleServerManager.exe
RestartApplications=no
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName}
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na Área de Trabalho"; GroupDescription: "Atalhos:"; Flags: checkedonce

[Files]
Source: "..\artifacts\publish\win-x64\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

[InstallDelete]
Type: files; Name: "{app}\HytaleServerManager.*"
Type: files; Name: "{autoprograms}\Iniciador do Servidor Hytale.lnk"
Type: files; Name: "{autodesktop}\Iniciador do Servidor Hytale.lnk"

[Icons]
Name: "{autoprograms}\HTServer Nexus"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\HTServer Nexus"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir o HTServer Nexus"; Flags: nowait postinstall skipifsilent
