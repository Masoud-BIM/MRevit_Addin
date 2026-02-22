// App.cs
using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

using Autodesk.Revit.UI;

namespace RebarTools
{
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            const string tabName = "MRevit";

            // Create tab (safe: may already exist)
            try { application.CreateRibbonTab(tabName); } catch { }

            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string dllDir = Path.GetDirectoryName(assemblyPath);

            // -------------------------
            // Panel 1: Rebar
            // -------------------------
            RibbonPanel rebarPanel = application.CreateRibbonPanel(tabName, "Rebar");

            // Button 1: Select Host Rebars
            PushButtonData btnData1 = new PushButtonData(
                "SelectHostRebars",
                "Select\nHost Rebars",
                assemblyPath,
                "RebarTools.Commands.SelectHostRebarsCommand"
            );

            PushButton btn1 = rebarPanel.AddItem(btnData1) as PushButton;

            string icon1Path = Path.Combine(dllDir, "icon32.png");
            if (File.Exists(icon1Path))
                btn1.LargeImage = LoadPng(icon1Path);

            btn1.ToolTip = "Pick a host element and select all hosted rebars.";

            // Button 2: Sort Host Rebars + Mark
            PushButtonData btnData2 = new PushButtonData(
                "SortHostRebarsAndMark",
                "Sort &&\nMark",
                assemblyPath,
                "RebarTools.Commands.SortHostRebarsAndMarkCommand"
            );

            PushButton btn2 = rebarPanel.AddItem(btnData2) as PushButton;

            string icon2Path = Path.Combine(dllDir, "icon_sortmark32.png");
            if (File.Exists(icon2Path))
                btn2.LargeImage = LoadPng(icon2Path);
            else if (File.Exists(icon1Path))
                btn2.LargeImage = LoadPng(icon1Path);

            btn2.ToolTip = "Pick a host, sort its hosted rebars by up to 3 parameters, then write sequential numbers to Mark.";

            // -------------------------
            // Panel 2: Annotation
            // -------------------------
            RibbonPanel annoPanel = application.CreateRibbonPanel(tabName, "Annotation");

            PushButtonData btnData3 = new PushButtonData(
                "AutoAnnotateView",
                "Auto\nAnnotate",
                assemblyPath,
                "RebarTools.AutoAnnotation.Commands.AutoAnnotateViewCommand"
            );

            PushButton btn3 = annoPanel.AddItem(btnData3) as PushButton;

            string icon3Path = Path.Combine(dllDir, "icon_autoannotate32.png");
            if (File.Exists(icon3Path))
                btn3.LargeImage = LoadPng(icon3Path);
            else if (File.Exists(icon1Path))
                btn3.LargeImage = LoadPng(icon1Path);

            btn3.ToolTip = "Pick a Section/Elevation (viewport or active view), choose categories + tag types, then tag visible elements in that view.";

            // -------------------------
            // Panel 3: IFC
            // -------------------------
            RibbonPanel ifcPanel = application.CreateRibbonPanel(tabName, "IFC");

            // IFC Beams
            PushButtonData ifcBeamsData = new PushButtonData(
                "IfcBeams",
                "IFC\nBeams",
                assemblyPath,
                "RebarTools.IfcConversion.Commands.ConvertIfcBeamsCommand"
            );
            PushButton ifcBeamsBtn = ifcPanel.AddItem(ifcBeamsData) as PushButton;

            string iconIfcBeams = Path.Combine(dllDir, "icon_ifc_beams32.png");
            if (File.Exists(iconIfcBeams))
                ifcBeamsBtn.LargeImage = LoadPng(iconIfcBeams);
            else
            {
                string iconIfc = Path.Combine(dllDir, "icon_ifc32.png");
                if (File.Exists(iconIfc)) ifcBeamsBtn.LargeImage = LoadPng(iconIfc);
                else if (File.Exists(icon1Path)) ifcBeamsBtn.LargeImage = LoadPng(icon1Path);
            }

            ifcBeamsBtn.ToolTip = "Pick an IFC link and convert IfcBeam elements into native Revit Structural Framing.";

            // IFC Columns
            PushButtonData ifcColsData = new PushButtonData(
                "IfcColumns",
                "IFC\nColumns",
                assemblyPath,
                "RebarTools.IfcConversion.Commands.ConvertIfcColumnsCommand"
            );
            PushButton ifcColsBtn = ifcPanel.AddItem(ifcColsData) as PushButton;

            string iconIfcCols = Path.Combine(dllDir, "icon_ifc_columns32.png");
            if (File.Exists(iconIfcCols))
                ifcColsBtn.LargeImage = LoadPng(iconIfcCols);
            else
            {
                string iconIfc = Path.Combine(dllDir, "icon_ifc32.png");
                if (File.Exists(iconIfc)) ifcColsBtn.LargeImage = LoadPng(iconIfc);
                else if (File.Exists(icon1Path)) ifcColsBtn.LargeImage = LoadPng(icon1Path);
            }

            ifcColsBtn.ToolTip = "Pick an IFC link and convert IfcColumn elements into native Revit Structural Columns.";

            // IFC Plates + Bolts
            PushButtonData ifcPBData = new PushButtonData(
                "IfcPlatesBolts",
                "IFC\nPlates+Bolts",
                assemblyPath,
                "RebarTools.IfcConversion.Commands.ConvertIfcPlatesBoltsCommand"
            );
            PushButton ifcPBBtn = ifcPanel.AddItem(ifcPBData) as PushButton;

            string iconIfcPB = Path.Combine(dllDir, "icon_ifc_platebolt32.png");
            if (File.Exists(iconIfcPB))
                ifcPBBtn.LargeImage = LoadPng(iconIfcPB);
            else
            {
                string iconIfc = Path.Combine(dllDir, "icon_ifc32.png");
                if (File.Exists(iconIfc)) ifcPBBtn.LargeImage = LoadPng(iconIfc);
                else if (File.Exists(icon1Path)) ifcPBBtn.LargeImage = LoadPng(icon1Path);
            }

            ifcPBBtn.ToolTip = "Pick an IFC link and recreate plates + bolts as Generic Model DirectShape elements.";

            // -------------------------
            // Panel 4: View Tools (View Range Visualizer)
            // -------------------------
            RibbonPanel viewToolsPanel = application.CreateRibbonPanel(tabName, "View Tools");

            PushButtonData vrData = new PushButtonData(
                "ViewRange3DBox",
                "View Range\n3D Box",
                assemblyPath,
                "RebarTools.ViewRangeVisualizer.Commands.ViewRangeSectionBoxCommand"
            );

            PushButton vrBtn = viewToolsPanel.AddItem(vrData) as PushButton;

            string iconVrPath = Path.Combine(dllDir, "icon_viewrange32.png");
            if (File.Exists(iconVrPath))
                vrBtn.LargeImage = LoadPng(iconVrPath);
            else if (File.Exists(icon1Path))
                vrBtn.LargeImage = LoadPng(icon1Path);

            vrBtn.ToolTip = "Creates a 3D section box matching the plan Crop Box (XY) and View Range (Z), with highlighted planes.";

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private BitmapImage LoadPng(string path)
        {
            BitmapImage img = new BitmapImage();
            img.BeginInit();
            img.UriSource = new Uri(path, UriKind.Absolute);
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.EndInit();
            return img;
        }
    }
}
