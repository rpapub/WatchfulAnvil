[Files]
Source: "dist\rules\net8.0\*.dll"; DestDir: "{code:GetTargetFolder|net8.0}"; Flags: ignoreversion; Check: ShouldInstall('net8.0')
Source: "dist\rules\net6.0\*.dll"; DestDir: "{code:GetTargetFolder|net6.0}"; Flags: ignoreversion; Check: ShouldInstall('net6.0')
Source: "dist\rules\net461\*.dll"; DestDir: "{code:GetTargetFolder|net461}"; Flags: ignoreversion; Check: ShouldInstall('net461')
