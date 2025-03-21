; CPRIMA.WorkflowAnalyzerRules.iss

[Setup]
AppName=CPRIMA Workflow Analyzer Rules
AppVersion=0.1.0
AppPublisher=cprima.net
DefaultDirName={autopf}\CPRIMA.WorkflowAnalyzerRules
DisableDirPage=yes
DisableProgramGroupPage=yes
OutputBaseFilename=CPRIMA.WorkflowAnalyzerRules-Installer
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
UninstallDisplayIcon={app}\net8.0\CPRIMA.WorkflowAnalyzerRules.dll
DisableFinishedPage=no
WizardStyle=modern

[Files]
Source: "..\..\src\CPRIMA.WorkflowAnalyzerRules\bin\Release\net8.0\CPRIMA.WorkflowAnalyzerRules.dll"; DestDir: "{tmp}\net8.0"; Flags: ignoreversion
Source: "..\..\src\CPRIMA.WorkflowAnalyzerRules\bin\Release\net6.0\CPRIMA.WorkflowAnalyzerRules.dll"; DestDir: "{tmp}\net6.0"; Flags: ignoreversion
Source: "..\..\src\CPRIMA.WorkflowAnalyzerRules\bin\Release\net461\CPRIMA.WorkflowAnalyzerRules.dll"; DestDir: "{tmp}\net461"; Flags: ignoreversion

; Install and uninstall scripts
Source: "install.ps1"; DestDir: "{tmp}"; Flags: deleteafterinstall
Source: "uninstall.ps1"; DestDir: "{app}"; Flags: ignoreversion

[Run]
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -NoExit -File {tmp}\install.ps1"; Flags: nowait

[UninstallRun]
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File {app}\uninstall.ps1"; Flags: runhidden; RunOnceId: uninstall-rules

[Icons]
Name: "{group}\Uninstall CPRIMA Workflow Analyzer Rules"; Filename: "{uninstallexe}"

[Code]
procedure InitializeWizard;
begin
  Log('Initializing CPRIMA Workflow Analyzer Rules installer...');
end;