#ifndef PublishDir
  #error Pass /DPublishDir pointing to the output from build-windows.ps1
#endif
[Setup]
AppId=Encrypz.Desktop
AppName=Encrypz Desktop
AppVersion=1.0.1
DefaultDirName={localappdata}\Programs\Encrypz
DefaultGroupName=Encrypz
OutputBaseFilename=EncrypzSetup
Compression=lzma
SolidCompression=yes
UninstallDisplayIcon={app}\Encrypz.Desktop.exe
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
SetupIconFile=logo.ico
CloseApplications=yes

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Excludes: "*.pdb,*.log"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Encrypz"; Filename: "{app}\Encrypz.Desktop.exe"
Name: "{autodesktop}\Encrypz"; Filename: "{app}\Encrypz.Desktop.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:";

[Run]
Filename: "{app}\Encrypz.Desktop.exe"; Description: "Launch Encrypz Desktop"; Flags: nowait postinstall skipifsilent
