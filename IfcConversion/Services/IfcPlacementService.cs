using System;
using Xbim.Ifc4.Interfaces;

namespace RebarTools.IfcConversion.Services
{
    internal static class IfcPlacementService
    {
        internal sealed class IfcTransform
        {
            public readonly double[,] R = new double[3, 3];
            public readonly double Tx, Ty, Tz;

            public static IfcTransform Identity => new IfcTransform(
                new double[,] { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } }, 0, 0, 0);

            public IfcTransform(double[,] r, double tx, double ty, double tz)
            {
                Array.Copy(r, R, r.Length);
                Tx = tx; Ty = ty; Tz = tz;
            }

            public (double x, double y, double z) OfPoint(double x, double y, double z)
            {
                double nx = R[0, 0] * x + R[0, 1] * y + R[0, 2] * z + Tx;
                double ny = R[1, 0] * x + R[1, 1] * y + R[1, 2] * z + Ty;
                double nz = R[2, 0] * x + R[2, 1] * y + R[2, 2] * z + Tz;
                return (nx, ny, nz);
            }

            public IfcTransform Multiply(IfcTransform b)
            {
                var r = new double[3, 3];
                for (int i = 0; i < 3; i++)
                    for (int j = 0; j < 3; j++)
                        r[i, j] = R[i, 0] * b.R[0, j] + R[i, 1] * b.R[1, j] + R[i, 2] * b.R[2, j];

                var (tx, ty, tz) = OfPoint(b.Tx, b.Ty, b.Tz);
                return new IfcTransform(r, tx, ty, tz);
            }
        }

        public static IfcTransform GetObjectPlacementTransform(IIfcObjectPlacement placement)
        {
            if (placement == null) return IfcTransform.Identity;

            if (placement is IIfcLocalPlacement lp)
            {
                IfcTransform parent = IfcTransform.Identity;
                if (lp.PlacementRelTo != null)
                    parent = GetObjectPlacementTransform(lp.PlacementRelTo);

                if (lp.RelativePlacement is IIfcAxis2Placement3D a3)
                {
                    var local = FromAxis2Placement3D(a3);
                    return parent.Multiply(local);
                }

                return parent;
            }

            return IfcTransform.Identity;
        }

        private static IfcTransform FromAxis2Placement3D(IIfcAxis2Placement3D a3)
        {
            var loc = a3.Location;
            double tx = loc.Coordinates.Count > 0 ? loc.Coordinates[0] : 0;
            double ty = loc.Coordinates.Count > 1 ? loc.Coordinates[1] : 0;
            double tz = loc.Coordinates.Count > 2 ? loc.Coordinates[2] : 0;

            var z = GetDir(a3.Axis, 0, 0, 1);
            var x = GetDir(a3.RefDirection, 1, 0, 0);

            Normalize(ref x);
            Normalize(ref z);

            var y = Cross(z, x);
            Normalize(ref y);

            x = Cross(y, z);
            Normalize(ref x);

            var r = new double[3, 3]
            {
                { x.x, y.x, z.x },
                { x.y, y.y, z.y },
                { x.z, y.z, z.z },
            };

            return new IfcTransform(r, tx, ty, tz);
        }

        private static (double x, double y, double z) GetDir(IIfcDirection d, double dx, double dy, double dz)
        {
            if (d == null) return (dx, dy, dz);
            double x = d.DirectionRatios.Count > 0 ? Convert.ToDouble(d.DirectionRatios[0]) : dx;
            double y = d.DirectionRatios.Count > 1 ? Convert.ToDouble(d.DirectionRatios[1]) : dy;
            double z = d.DirectionRatios.Count > 2 ? Convert.ToDouble(d.DirectionRatios[2]) : dz;
            return (x, y, z);
        }

        private static void Normalize(ref (double x, double y, double z) v)
        {
            double len = Math.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
            if (len < 1e-9) return;
            v = (v.x / len, v.y / len, v.z / len);
        }

        private static (double x, double y, double z) Cross((double x, double y, double z) a, (double x, double y, double z) b)
        {
            return (a.y * b.z - a.z * b.y,
                    a.z * b.x - a.x * b.z,
                    a.x * b.y - a.y * b.x);
        }
    }
}
