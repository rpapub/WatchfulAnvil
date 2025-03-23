// Shared_Code.iss
[Code]
type
  TUiPathDetection = record
    Source: TStringList;
    Targets: TStringList;
    VersionRange: String;
  end;

var
  Detection: TUiPathDetection;
  RadioPage: TWizardPage;
  AutoDetectRadio, CustomFolderRadio: TRadioButton;
  FolderPage: TInputDirWizardPage;
  SelectedFolder: String;

function DirExistsUserFirst(userPathPf32, userPathPf64: String): Boolean;
begin
  Result :=
    DirExists(ExpandConstant('{localappdata}\Programs\UiPath\Studio\' + userPathPf32)) or
    DirExists(ExpandConstant('{pf}' + '\UiPath\Studio\' + userPathPf32)) or
    DirExists(ExpandConstant('{pf64}' + '\UiPath\Studio\' + userPathPf64));
end;

function IsAdminUser: Boolean;
begin
  Result := IsAdmin;
end;

function PathExistsUserFirst(userPathPf32, userPathPf64: String): String;
begin
  if IsAdminUser then
  begin
    // Admin users can access Program Files
    if DirExists(ExpandConstant('{pf64}\UiPath\Studio\' + userPathPf64)) then
      Result := ExpandConstant('{pf64}\UiPath\Studio\' + userPathPf64)
    else if DirExists(ExpandConstant('{pf}\UiPath\Studio\' + userPathPf32)) then
      Result := ExpandConstant('{pf}\UiPath\Studio\' + userPathPf32)
    else
      Result := ''; // Admin users should not default to user paths for installation
  end
  else
  begin
    // Non-admin users should be restricted to user paths
    if DirExists(ExpandConstant('{localappdata}\Programs\UiPath\Studio\' + userPathPf32)) then
      Result := ExpandConstant('{localappdata}\Programs\UiPath\Studio\' + userPathPf32)
    else if DirExists(ExpandConstant('{localappdata}\UiPath\Studio\' + userPathPf32)) then
      Result := ExpandConstant('{localappdata}\UiPath\Studio\' + userPathPf32)
    else
      Result := ''; // No match found, let ProbeUiPath handle fallback
  end;
end;

function ProbeUiPath(): TUiPathDetection;
var
  path: String;
begin
  Result.Source := TStringList.Create;
  Result.Targets := TStringList.Create;

  path := PathExistsUserFirst('Rules\net8.0', 'Rules\net8.0');
  if path <> '' then
  begin
    Result.Source.Add('net8.0');
    Result.Targets.Add(path);
    Log('Detected net8.0 target: ' + path);
  end;

  path := PathExistsUserFirst('Rules\net6.0', 'Rules\net6.0');
  if path <> '' then
  begin
    Result.Source.Add('net6.0');
    Result.Targets.Add(path);
    Log('Detected net6.0 target: ' + path);
  end;

  path := PathExistsUserFirst('net472\Rules', 'net472\Rules');
  if path <> '' then
  begin
    Result.Source.Add('net461');
    Result.Targets.Add(path);
    Log('Detected net472 target (install net461): ' + path);
  end;

  path := PathExistsUserFirst('net461\Rules', 'net461\Rules');
  if path <> '' then
  begin
    Result.Source.Add('net461');
    Result.Targets.Add(path);
    Log('Detected net461 target: ' + path);
  end;

  if Result.Targets.Count = 0 then
  begin
    Result.Source.Add('net6.0');
    Result.Targets.Add('C:\Users\Public\Documents\UiPath\Rules');
    Log('Fallback target: C:\Users\Public\Documents\UiPath\Rules');
  end;
end;

procedure InitializeWizard;
var
  defaultPath: String;
  i: Integer;
begin
  Detection := ProbeUiPath();

  if WizardSilent then
  begin
    SelectedFolder := 'autodetect';
    Exit;
  end;

  RadioPage := CreateCustomPage(wpWelcome, 'Choose Installation Mode',
    'How should the installer select the target folder?');

  AutoDetectRadio := TNewRadioButton.Create(RadioPage);
  AutoDetectRadio.Parent := RadioPage.Surface;
  AutoDetectRadio.Top := 16;
  AutoDetectRadio.Left := 0;
  AutoDetectRadio.Width := RadioPage.SurfaceWidth;
  AutoDetectRadio.Caption := 'Auto-detect based on UiPath version';
  AutoDetectRadio.Checked := True;

  CustomFolderRadio := TNewRadioButton.Create(RadioPage);
  CustomFolderRadio.Parent := RadioPage.Surface;
  CustomFolderRadio.Top := AutoDetectRadio.Top + 24;
  CustomFolderRadio.Left := 0;
  CustomFolderRadio.Width := RadioPage.SurfaceWidth;
  CustomFolderRadio.Caption := 'Let me choose a folder manually';

  if DirExists('C:\Users\Public\Documents\UiPath\Rules') then
    defaultPath := 'C:\Users\Public\Documents\UiPath\Rules'
  else
    defaultPath := 'C:\Users\Public\Documents';

  FolderPage := CreateInputDirPage(RadioPage.ID,
    'Select Custom Folder',
    'Choose a custom folder for rule installation.',
    '',
    False,
    defaultPath);
  FolderPage.Add(defaultPath);
  FolderPage.Values[0] := defaultPath;

  MsgBox('Detected UiPath Studio targets: ' + IntToStr(Detection.Targets.Count), mbInformation, MB_OK);
  for i := 0 to Detection.Targets.Count - 1 do
    Log('-> source=' + Detection.Source[i] + '  target=' + Detection.Targets[i]);
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  if PageID = FolderPage.ID then
    Result := not CustomFolderRadio.Checked
  else
    Result := False;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = FolderPage.ID then
    SelectedFolder := FolderPage.Values[0];
end;

procedure DeinitializeSetup;
var
  i: Integer;
begin
  Log('Simulated install operations:');

  if not WizardSilent and CustomFolderRadio.Checked then
  begin
    Log('User selected custom folder: ' + SelectedFolder);
    Log('Would install net6.0 DLL to: ' + SelectedFolder);
  end
  else
  begin
    for i := 0 to Detection.Targets.Count - 1 do
      Log('Would install ' + Detection.Source[i] + ' DLL to: ' + Detection.Targets[i]);
  end;

  Detection.Source.Free;
  Detection.Targets.Free;
end;
