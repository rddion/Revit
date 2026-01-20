[Setup]
AppName=Revit Advanced Selection Tool
AppVersion=1.0
DefaultDirName=D:\
DefaultGroupName=Revit Advanced Selection Tool
OutputDir=.
OutputBaseFilename=RevitAdvancedSelectionToolSetup_v2
Compression=lzma
SolidCompression=yes
PrivilegesRequired=lowest

[Files]
Source: "Revit Advanced Selection Tool\bin\Debug\Revit Advanced Selection Tool.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Revit Advanced Selection Tool\bin\Debug\Revit Advanced Selection Tool.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: "Revit Advanced Selection Tool\icons\Revit.png"; DestDir: "{app}\icons"; Flags: ignoreversion
Source: "Wpf\bin\Debug\Wpf.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "Wpf\bin\Debug\Wpf.pdb"; DestDir: "{app}"; Flags: ignoreversion

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var
  AddInFile: string;
  Content: string;
begin
  if CurStep = ssPostInstall then
  begin
    AddInFile := ExpandConstant('{app}\Revit Advanced Selection Tool.addin');
    Content := '<?xml version="1.0" encoding="utf-8"?>' + #13#10 +
               '<RevitAddIns>' + #13#10 +
               '  <AddIn Type="Application">' + #13#10 +
               '    <Name>Revit Advanced Selection Tool</Name>' + #13#10 +
               '    <Assembly>' + ExpandConstant('{app}') + '\Revit Advanced Selection Tool.dll</Assembly>' + #13#10 +
               '    <ClientId>4655dd65-9276-4491-9b1a-4494f32735dd</ClientId>' + #13#10 +
               '    <FullClassName>Troyan.Button</FullClassName>' + #13#10 +
               '    <VendorId>ADSK</VendorId>' + #13#10 +
               '    <VendorDescription>Autodesk, www.autodesk.com</VendorDescription>' + #13#10 +
               '  </AddIn>' + #13#10 +
               '  <AddIn Type="Command">' + #13#10 +
               '    <Assembly>' + ExpandConstant('{app}') + '\Revit Advanced Selection Tool.dll</Assembly>' + #13#10 +
               '    <ClientId>58d5691f-770a-4c5e-9875-2130a9bdb586</ClientId>' + #13#10 +
               '    <FullClassName>Troyan.CmdFindCommonParameters</FullClassName>' + #13#10 +
               '    <Text>CmdFindCommonParameters</Text>' + #13#10 +
               '    <Description>Find common parameters</Description>' + #13#10 +
               '    <VisibilityMode>AlwaysVisible</VisibilityMode>' + #13#10 +
               '    <VendorId>ADSK</VendorId>' + #13#10 +
               '    <VendorDescription>Autodesk, www.autodesk.com</VendorDescription>' + #13#10 +
               '  </AddIn>' + #13#10 +
               '  <AddIn Type="Command">' + #13#10 +
               '    <Assembly>' + ExpandConstant('{app}') + '\Revit Advanced Selection Tool.dll</Assembly>' + #13#10 +
               '    <ClientId>bf7ff4a2-dae9-4237-97aa-64aebaa0be4a</ClientId>' + #13#10 +
               '    <FullClassName>Troyan.TroyankaCommand</FullClassName>' + #13#10 +
               '    <Text>Troyanka</Text>' + #13#10 +
               '    <Description>Advanced selection tool</Description>' + #13#10 +
               '    <VisibilityMode>AlwaysVisible</VisibilityMode>' + #13#10 +
               '    <VendorId>ADSK</VendorId>' + #13#10 +
               '    <VendorDescription>Autodesk, www.autodesk.com</VendorDescription>' + #13#10 +
               '  </AddIn>' + #13#10 +
               '</RevitAddIns>';
    SaveStringToFile(AddInFile, Content, False);
  end;
end;

[Run]
; Optionally run something after install

[UninstallDelete]
Type: files; Name: "{app}\Revit Advanced Selection Tool.dll"
Type: files; Name: "{app}\Revit.png"
Type: files; Name: "{app}\Revit Advanced Selection Tool.addin"