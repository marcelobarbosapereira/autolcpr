#define MyAppName "AutoLCPR"
#define MyAppVersion "1.1"
#define MyAppPublisher "Marcelo"
#define MyAppExeName "AutoLCPR 1.1.exe"
#define MySourceDir "C:\Users\marce\Desktop\DEV\AutoLCPR\publish\AutoLCPR-1.1"
#define MyOutputDir "C:\Users\marce\Desktop"
#define MySetupIcon "C:\Users\marce\Desktop\DEV\AutoLCPR\src\AutoLCPR.UI.WPF\Assets\chart.ico"

[Setup]
AppId={{3F6E2D6B-B9D5-4DA2-8C52-76888CC5D6A8}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir={#MyOutputDir}
OutputBaseFilename={#MyAppName} {#MyAppVersion}
SetupIconFile={#MySetupIcon}
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na Área de Trabalho"; GroupDescription: "Atalhos adicionais:"; Flags: unchecked

[Files]
Source: "{#MySourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Executar {#MyAppName}"; Flags: nowait postinstall skipifsilent