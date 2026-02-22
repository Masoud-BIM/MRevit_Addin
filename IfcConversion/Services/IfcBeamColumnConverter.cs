using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace RebarTools.IfcConversion.Services
{
    internal static class IfcBeamColumnConverter
    {
        public class ConvertResult
        {
            public int Created { get; set; }
            public int Skipped { get; set; }
        }

        public static ConvertResult ConvertBeams(Document doc, string ifcPath, Transform linkTransform, FamilySymbol beamType)
        {
            if (beamType == null) throw new ArgumentNullException(nameof(beamType));
            if (!beamType.IsActive) beamType.Activate();

            var levels = GetLevels(doc);

            double toMetres = IfcReadService.GetIfcToMetresFactor(ifcPath);
            var res = new ConvertResult();

            foreach (var (guid, p1, p2, placement) in IfcReadService.ReadBeamAxes(ifcPath))
            {
                try
                {
                    var tf = IfcPlacementService.GetObjectPlacementTransform(placement);

                    var w1 = tf.OfPoint(p1.X, p1.Y, p1.Z);
                    var w2 = tf.OfPoint(p2.X, p2.Y, p2.Z);

                    XYZ r1 = linkTransform.OfPoint(ToRevitFeet(w1, toMetres));
                    XYZ r2 = linkTransform.OfPoint(ToRevitFeet(w2, toMetres));

                    if (r1.DistanceTo(r2) < 1e-6) { res.Skipped++; continue; }

                    Level lvl = FindNearestLevel(levels, (r1.Z + r2.Z) * 0.5);

                    Line axis = Line.CreateBound(r1, r2);
                    FamilyInstance fi = doc.Create.NewFamilyInstance(axis, beamType, lvl, StructuralType.Beam);

                    // store guid (optional)
                    TrySetComment(fi, $"IFC:{guid}");

                    res.Created++;
                }
                catch
                {
                    res.Skipped++;
                }
            }

            return res;
        }

        public static ConvertResult ConvertColumns(Document doc, string ifcPath, Transform linkTransform, FamilySymbol columnType)
        {
            if (columnType == null) throw new ArgumentNullException(nameof(columnType));
            if (!columnType.IsActive) columnType.Activate();

            var levels = GetLevels(doc);

            double toMetres = IfcReadService.GetIfcToMetresFactor(ifcPath);
            var res = new ConvertResult();

            foreach (var (guid, p1, p2, placement) in IfcReadService.ReadColumnAxes(ifcPath))
            {
                try
                {
                    var tf = IfcPlacementService.GetObjectPlacementTransform(placement);

                    var w1 = tf.OfPoint(p1.X, p1.Y, p1.Z);
                    XYZ r1 = linkTransform.OfPoint(ToRevitFeet(w1, toMetres));

                    Level lvl = FindNearestLevel(levels, r1.Z);

                    FamilyInstance fi = doc.Create.NewFamilyInstance(r1, columnType, lvl, StructuralType.Column);

                    TrySetComment(fi, $"IFC:{guid}");

                    res.Created++;
                }
                catch
                {
                    res.Skipped++;
                }
            }

            return res;
        }

        private static List<Level> GetLevels(Document doc)
        {
            var lvls = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .ToList();

            if (lvls.Count == 0)
                throw new InvalidOperationException("No Levels found in the project.");

            return lvls;
        }

        private static Level FindNearestLevel(List<Level> levels, double zFeet)
        {
            Level best = levels[0];
            double bestDist = Math.Abs(levels[0].Elevation - zFeet);

            for (int i = 1; i < levels.Count; i++)
            {
                double d = Math.Abs(levels[i].Elevation - zFeet);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = levels[i];
                }
            }
            return best;
        }

        private static XYZ ToRevitFeet((double x, double y, double z) p, double ifcToMetres)
        {
            double mx = p.x * ifcToMetres;
            double my = p.y * ifcToMetres;
            double mz = p.z * ifcToMetres;

            const double feetPerMetre = 3.280839895013123;
            return new XYZ(mx * feetPerMetre, my * feetPerMetre, mz * feetPerMetre);
        }

        private static void TrySetComment(Element e, string text)
        {
            Parameter p = e.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
            if (p != null && !p.IsReadOnly) p.Set(text);
        }
    }
}
