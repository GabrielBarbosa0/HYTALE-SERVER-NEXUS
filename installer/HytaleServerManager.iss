#ifndef MyAppVersion
  #define MyAppVersion "1.0.1"
#endif

#define MyAppName "Iniciador do Servidor Hytale"
#define MyAppPublisher "Gabriel Barbosa"
#define MyAppExeName "HytaleServerManager.exe"

[Setup]
AppId={{D23D2E29-8FA2-47F9-9BC5-64368EEC1C1E}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL=https://github.com/GabrielBarbosa0/INICIADOR-SERVIDOR-HYTALE
AppSupportURL=https://github.com/GabrielBarbosa0/INICIADOR-SERVIDOR-HYTALE/issues
AppUpdatesURL=https://github.com/GabrielBarbosa0/INICIADOR-SERVIDOR-HYTALE/releases
DefaultDirName={localappdata}\HytaleServerManager\app
DefaultGroupName=Iniciador do Servidor Hytale
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=..\artifacts\installer
OutputBaseFilename=Iniciador-Hytale-Setup-{#MyAppVersion}
SetupIconFile=..\src\HytaleServerManager\Assets\HytaleServerManager.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=force
CloseApplicationsFilter={#MyAppExeName}
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

[Icons]
Name: "{autoprograms}\Iniciador do Servidor Hytale"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\Iniciador do Servidor Hytale"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir o Iniciador do Servidor Hytale"; Flags: nowait postinstall skipifsilent
