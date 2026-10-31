; Inno Setup script for the offline Windows installer.
; Build the publish folder first:
;   dotnet publish src/MarkdownDocumentConverter.App/MarkdownDocumentConverter.App.csproj -c Release -r win-x64 --self-contained true -p:WindowsAppSDKSelfContained=true -o publish
; Then compile with Inno Setup 6:
;   iscc installer\windows\setup.iss

#define MyAppName "Markdown Document Converter"
#define MyAppVersion "0.1.0"
#define MyAppPublisher "Markdown Document Converter Project"
#define MyAppExeName "MarkdownDocumentConverter.App.exe"

[Setup]
AppId={{81E8D66D-7746-431F-B6C2-770D87A335F2}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\Markdown Document Converter
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\..\artifacts\installer
OutputBaseFilename=MarkdownDocumentConverter-Setup-{#MyAppVersion}
SetupIconFile=..\..\resources\icons\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=commandline
CloseApplications=yes
RestartApplications=no
VersionInfoVersion=0.1.0.0
VersionInfoDescription={#MyAppName} offline installer
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加图标："; Flags: unchecked

[Files]
Source: "..\..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "THIRD-PARTY-NOTICES.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "启动 {#MyAppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; 用户配置和日志位于 %APPDATA%，卸载时保留，避免删除用户数据。
