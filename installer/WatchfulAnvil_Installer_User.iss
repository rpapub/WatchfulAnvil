#define MODE_DEBUG
#include "Shared_Defines.iss"

[Setup]
PrivilegesRequired=lowest
AppName={#MyAppName}
AppVersion={#MyAppVersion}
OutputDir={#OutputDir}
OutputBaseFilename={#OutputBaseFilename_User}
DefaultDirName={localappdata}\Programs\{#MyAppName}
DisableDirPage=yes
Compression=lzma
SolidCompression=yes
AppId={{WatchfulAnvilInstaller-User}}
Uninstallable=no
CreateAppDir=no
CreateUninstallRegKey=no

#include "Shared_Code.iss"
#ifdef MODE_DISTRIBUTION
  #include "Shared_Files_DeployWrite.iss"
#else
  #include "Shared_Files_DebugReadonly.iss"
#endif
