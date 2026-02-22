using System;
using System.Linq;

using Autodesk.Revit.DB;

namespace RebarTools.ViewRangeVisualizer.Services
{
    internal static class View3DService
    {
        private const string ViewPrefix = "MRevit - ViewRange - ";

        public static View3D GetOrCreateDebug3DView(Document doc, View planView)
        {
            string viewName = ViewPrefix + planView.Name;

            // Try find existing
            View3D existing = new FilteredElementCollector(doc)
                .OfClass(typeof(View3D))
                .Cast<View3D>()
                .FirstOrDefault(v => !v.IsTemplate && string.Equals(v.Name, viewName, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
                return existing;

            // Create new 3D view
            ViewFamilyType vft = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(x => x.ViewFamily == ViewFamily.ThreeDimensional);

            if (vft == null)
                throw new InvalidOperationException("No 3D ViewFamilyType found.");

            View3D v3d = View3D.CreateIsometric(doc, vft.Id);
            v3d.Name = viewName;

            // Make sure section box is available
            v3d.IsSectionBoxActive = true;

            return v3d;
        }

        public static BoundingBoxXYZ BuildSectionBoxFromCropAndRange(ViewPlan planView, ViewRangeInfo range)
        {
            BoundingBoxXYZ crop = planView.CropBox;

            // Section box uses crop local coordinates + crop.Transform
            BoundingBoxXYZ sec = new BoundingBoxXYZ();
            sec.Transform = crop.Transform;

            // Z extents: View Depth (min) to Top (max)
            double minZ = Math.Min(range.DepthZ, range.TopZ);
            double maxZ = Math.Max(range.DepthZ, range.TopZ);

            sec.Min = new XYZ(crop.Min.X, crop.Min.Y, minZ);
            sec.Max = new XYZ(crop.Max.X, crop.Max.Y, maxZ);

            return sec;
        }
    }
}
