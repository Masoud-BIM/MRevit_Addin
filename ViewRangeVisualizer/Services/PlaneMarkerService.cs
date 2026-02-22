using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;

using RebarTools.ViewRangeVisualizer.Utils;

namespace RebarTools.ViewRangeVisualizer.Services
{
    internal static class PlaneMarkerService
    {
        private const string AppId = "MRevit.ViewRange";
        private const double ThicknessFt = 0.05; // ~15mm (visual)

        public static void DeleteOldMarkers(Document doc)
        {
            IList<ElementId> ids = new FilteredElementCollector(doc)
                .OfClass(typeof(DirectShape))
                .Cast<DirectShape>()
                .Where(ds => string.Equals(ds.ApplicationId, AppId, StringComparison.OrdinalIgnoreCase))
                .Select(ds => ds.Id)
                .ToList();

            if (ids.Count > 0)
                doc.Delete(ids);
        }

        public static void CreateViewRangeMarkers(Document doc, View3D view3d, BoundingBoxXYZ sectionBox, ViewRangeInfo range)
        {
            // Build 4 markers at Z elevations
            CreateOne(doc, view3d, sectionBox, range.TopZ, "Top", range.TopLabel, new Color(0, 180, 255));
            CreateOne(doc, view3d, sectionBox, range.CutZ, "Cut", range.CutLabel, new Color(255, 60, 60));
            CreateOne(doc, view3d, sectionBox, range.BottomZ, "Bottom", range.BottomLabel, new Color(60, 255, 120));
            CreateOne(doc, view3d, sectionBox, range.DepthZ, "ViewDepth", range.DepthLabel, new Color(255, 200, 0));
        }

        private static void CreateOne(Document doc, View3D view3d, BoundingBoxXYZ sectionBox, double z, string key, string label, Color color)
        {
            // Rectangle in sectionBox local coordinates
            double minX = sectionBox.Min.X;
            double minY = sectionBox.Min.Y;
            double maxX = sectionBox.Max.X;
            double maxY = sectionBox.Max.Y;

            Solid slab = GeometryHelpers.CreateThinRectangleSolid(
                minX, minY, maxX, maxY,
                z, ThicknessFt,
                sectionBox.Transform // apply rotation/translation
            );

            if (slab == null || slab.Volume < 1e-9)
                return;

            DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
            ds.ApplicationId = AppId;
            ds.ApplicationDataId = key;

            ds.SetShape(new List<GeometryObject> { slab });

            // Comments store plane description
            Parameter c = ds.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
            if (c != null && !c.IsReadOnly)
                c.Set(key + " | " + label);

            // View-specific override (color + transparency)
            OverrideGraphicSettings ogs = new OverrideGraphicSettings();
            ogs.SetSurfaceForegroundPatternColor(color);
            ogs.SetCutForegroundPatternColor(color);
            ogs.SetSurfaceTransparency(60);

            // Ensure we have a solid fill pattern for visibility
            FillPatternElement solidFill = GeometryHelpers.GetSolidFillPattern(doc);
            if (solidFill != null)
            {
                ogs.SetSurfaceForegroundPatternId(solidFill.Id);
                ogs.SetCutForegroundPatternId(solidFill.Id);
            }

            view3d.SetElementOverrides(ds.Id, ogs);
        }
    }
}
