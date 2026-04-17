; ==========================================
; 专注锁屏控制器 - Inno Setup 打包脚本
; 作者: kk
; 版本: 1.0.0
; ==========================================

[Setup]
AppId={{8A3B5C1D-9E2F-4A7C-B8D1-5F6E7A8C9D0E}
AppName=专注锁屏控制器
AppVersion=1.0.0
AppPublisher=kk
DefaultDirName={localappdata}\FocusTimer
DefaultGroupName=专注锁屏控制器
OutputDir=installer
OutputBaseFilename=FocusTimer_Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=lowest
CloseApplications=force
UninstallDisplayIcon={app}\icon.ico
SetupIconFile=icon.ico

[TASKS]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加任务:"; Flags: checkedonce

[Files]
Source: "bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "icon.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\专注锁屏控制器"; Filename: "{app}\FocusTimer.exe"; WorkingDir: "{app}"; IconFilename: "{app}\icon.ico"
Name: "{autodesktop}\专注锁屏控制器"; Filename: "{app}\FocusTimer.exe"; WorkingDir: "{app}"; IconFilename: "{app}\icon.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\FocusTimer.exe"; Description: "安装完成后立即运行"; Flags: nowait postinstall skipifsilent