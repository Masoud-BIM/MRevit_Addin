using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;

namespace RebarTools.ViewRangeVisualizer.Utils
{
    internal static class GeometryHelpers
    {
        public static Solid CreateThinRectangleSolid(
            double minX, double minY, double maxX, double maxY,
            double baseZ, double thickness,
            Transform transform)
        {
            // rectangle loop at Z = baseZ (in section box local coords)
            XYZ p1 = new XYZ(minX, minY, baseZ);
            XYZ p2 = new XYZ(maxX, minY, baseZ);
            XYZ p3 = new XYZ(maxX, maxY, baseZ);
            XYZ p4 = new XYZ(minX, maxY, baseZ);

            CurveLoop loop = new CurveLoop();
            loop.Append(Line.CreateBound(p1, p2));
            loop.Append(Line.CreateBound(p2, p3));
            loop.Append(Line.CreateBound(p3, p4));
            loop.Append(Line.CreateBound(p4, p1));

            List<CurveLoop> loops = new List<CurveLoop> { loop };

            // Extrude in +Z in local coordinates
            XYZ direction = XYZ.BasisZ;
            Solid solid = GeometryCreationUtilities.CreateExtrusionGeometry(loops, direction, thickness);

            if (solid == null) return null;

            // Apply crop transform so it matches rotated crop region
            if (transform != null && !transform.IsIdentity)
                solid = SolidUtils.CreateTransformed(solid, transform);

            return solid;
        }

        public static FillPatternElement GetSolidFillPattern(Document doc)
        {
            // Find Solid fill pattern
            return new FilteredElementCollector(doc)
                .OfClass(typeof(FillPatternElement))
                .Cast<FillPatternElement>()
                .FirstOrDefault(fpe =>
                {
                    FillPattern fp = fpe.GetFillPattern();
                    return fp != null && fp.IsSolidFill;
                });
        }
    }
}
