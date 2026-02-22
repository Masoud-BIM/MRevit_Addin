# MRevit (Revit Add-in)

A collection of Revit tools developed in C#.
Current features include Rebar tools, Auto Annotation, IFC conversion helpers, and a View Range visualizer.

## Requirements
- Autodesk Revit 2025
- Visual Studio
- Platform: x64
- NuGet (IFC parsing):
  - Xbim.Ifc
  - Xbim.Common

## Installation (Manual)

### 1) Build
1. Open the project in Visual Studio (open the `.csproj`)
2. Build:
   - Configuration: Debug (or Release)
   - Platform: x64
3. Close Revit before rebuilding (Revit locks the DLL).

### 2) Create the `.addin` file
Create a file (example: `MRevit.addin`) in:

`C:\Users\<USERNAME>\AppData\Roaming\Autodesk\Revit\Addins\2025\`

Example `.addin` (edit the Assembly path):

```xml
<?xml version="1.0" encoding="utf-8" standalone="no"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Name>MRevit</Name>
    <Assembly>PATH_TO_YOUR_DLL\RebarTools.dll</Assembly>
    <AddInId>9F5B6E2A-1D4E-4C38-9E6F-123456789ABC</AddInId>
    <FullClassName>RebarTools.App</FullClassName>
    <VendorId>MR01</VendorId>
    <VendorDescription>MRevit Tools</VendorDescription>
  </AddIn>
</RevitAddIns>
