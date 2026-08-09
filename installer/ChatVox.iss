#define MyAppName "ChatVox"
[Setup]
AppName={#MyAppName}
AppVersion=1.0.0-rc.11
AppId={{C4A56C35-1B17-48BD-B1C9-62D9EC22D6D1}
DefaultDirName={autopf}\ChatVox
DefaultGroupName={#MyAppName}
OutputDir=..\build\temp\installer
OutputBaseFilename=ChatVox-Setup
SetupIconFile=..\assets\branding\ChatVox.ico
UninstallDisplayIcon={app}\ChatVox.exe
[Files]
Source: "..\build\publish\current\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion; Excludes: "*.pdb"
[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\ChatVox.exe"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\ChatVox.exe"; Tasks: desktopicon
[Tasks]
Name: desktopicon; Description: "Create a &desktop shortcut"; Flags: unchecked
[Run]
Filename: "{app}\ChatVox.exe"; Parameters: "--post-install"; Description: "Launch ChatVox"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
    RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'ChatVox');
end;
