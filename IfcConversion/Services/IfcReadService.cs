using System;
using System.Collections.Generic;
using System.Linq;

using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace RebarTools.IfcConversion.Services
{
    internal static class IfcReadService
    {
        internal readonly struct IfcPoint
        {
            public readonly double X, Y, Z;
            public IfcPoint(double x, double y, double z) { X = x; Y = y; Z = z; }
        }

        public static double GetIfcToMetresFactor(string ifcPath)
        {
            using (var model = IfcStore.Open(ifcPath))
                return model.ModelFactors.LengthToMetresConversionFactor;
        }

        public static IEnumerable<(string GlobalId, IfcPoint P1, IfcPoint P2, IIfcObjectPlacement Placement)>
            ReadBeamAxes(string ifcPath)
        {
            using (var model = IfcStore.Open(ifcPath))
            {
                foreach (var beam in model.Instances.OfType<IIfcBeam>())
                {
                    if (!TryGetAxisLine(beam, out var p1, out var p2))
                        continue;

                    yield return (beam.GlobalId, p1, p2, beam.ObjectPlacement);
                }
            }
        }

        public static IEnumerable<(string GlobalId, IfcPoint P1, IfcPoint P2, IIfcObjectPlacement Placement)>
            ReadColumnAxes(string ifcPath)
        {
            using (var model = IfcStore.Open(ifcPath))
            {
                foreach (var col in model.Instances.OfType<IIfcColumn>())
                {
                    if (!TryGetAxisLine(col, out var p1, out var p2))
                        continue;

                    yield return (col.GlobalId, p1, p2, col.ObjectPlacement);
                }
            }
        }

        public static HashSet<string> ReadPlateAndBoltGuids(string ifcPath)
        {
            using (var model = IfcStore.Open(ifcPath))
            {
                var guids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Plates
                foreach (var p in model.Instances.OfType<IIfcPlate>())
                    if (!string.IsNullOrWhiteSpace(p.GlobalId)) guids.Add(p.GlobalId);

                // Bolts (different exporters use different entities; these two cover many)
                foreach (var f in model.Instances.OfType<IIfcFastener>())
                    if (!string.IsNullOrWhiteSpace(f.GlobalId)) guids.Add(f.GlobalId);

                foreach (var mf in model.Instances.OfType<IIfcMechanicalFastener>())
                    if (!string.IsNullOrWhiteSpace(mf.GlobalId)) guids.Add(mf.GlobalId);

                return guids;
            }
        }

        // MVP axis extraction: Axis representation as IfcPolyline
        private static bool TryGetAxisLine(IIfcProduct prod, out IfcPoint p1, out IfcPoint p2)
        {
            p1 = default; p2 = default;

            var reps = prod.Representation?.Representations;
            if (reps == null) return false;

            var axisRep = reps
                .OfType<IIfcShapeRepresentation>()
                .FirstOrDefault(r =>
                    string.Equals(r.RepresentationIdentifier, "Axis", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(r.RepresentationIdentifier, "CenterLine", StringComparison.OrdinalIgnoreCase));

            if (axisRep == null) return false;

            foreach (var item in axisRep.Items)
            {
                if (item is IIfcPolyline pl && pl.Points.Count >= 2)
                {
                    p1 = ToPoint(pl.Points.First());
                    p2 = ToPoint(pl.Points.Last());
                    return true;
                }
            }
            return false;
        }

        private static IfcPoint ToPoint(IIfcCartesianPoint p)
        {
            double x = p.Coordinates.Count > 0 ? p.Coordinates[0] : 0;
            double y = p.Coordinates.Count > 1 ? p.Coordinates[1] : 0;
            double z = p.Coordinates.Count > 2 ? p.Coordinates[2] : 0;
            return new IfcPoint(x, y, z);
        }
    }
}
