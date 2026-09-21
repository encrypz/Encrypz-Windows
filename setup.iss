[Setup]
AppName=Encrypz Desktop
AppVersion=1.0.0
DefaultDirName={autopf}\Encrypz
DefaultGroupName=Encrypz
OutputDir=e:\Encrypz-Windows
OutputBaseFilename=EncrypzSetup
Compression=lzma
SolidCompression=yes
UninstallDisplayIcon={app}\Encrypz.Desktop.exe
PrivilegesRequired=lowest

[Files]
Source: "e:\Encrypz-Windows\publish\Encrypz.Desktop.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Encrypz"; Filename: "{app}\Encrypz.Desktop.exe"
Name: "{autodesktop}\Encrypz"; Filename: "{app}\Encrypz.Desktop.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:";

[Run]
Filename: "{app}\Encrypz.Desktop.exe"; Description: "Launch Encrypz Desktop"; Flags: nowait postinstall skipifsilent
