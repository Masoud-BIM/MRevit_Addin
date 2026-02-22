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


### 3) Icons
If `App.cs` loads icons by file name, keep icons next to the DLL:

- `icon32.png`
- `icon_sortmark32.png`
- `icon_autoannotate32.png`
- `icon_ifc_platebolt32.png`
- `icon_viewrange32.png`

### 4) Run
Start Revit → you should see the **MRevit** ribbon tab.

---

## Commands (What each one does)

### Rebar Panel

#### Select Host Rebars
Pick a host element → selects all hosted rebars.

#### Sort & Mark (Hosted Rebars)
Pick a host → sort by up to 3 parameters (Length/Diameter/Shape) → writes sequential numbers to **Mark**.

---

### Annotation Panel

#### Auto Annotate (Section/Elevation)
Pick a Section/Elevation (viewport or active view) → choose categories and tag types → places tags for visible elements.

---

### IFC Panel

#### IFC Beams
Converts IfcBeam into native Revit Structural Framing (requires at least one beam type loaded).

#### IFC Columns
Converts IfcColumn into native Revit Structural Columns (requires at least one column type loaded).

#### IFC Plates + Bolts
Recreates plates/bolts as **Generic Model DirectShape** elements.  
If Revit uses a cached `.RVT` link, the tool asks you to pick the original `.ifc`.

---

### View Tools Panel

#### View Range 3D Box
In a Plan view with Crop Box enabled: creates a 3D section box using XY from Crop Box and Z from View Range and shows colored planes for Top/Cut/Bottom/ViewDepth.
![View Range Visualizer](assets/ViewRangeVisualizer.png)
