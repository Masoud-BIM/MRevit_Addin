using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;

namespace RebarTools.IfcConversion.Services
{
    internal static class IfcPlateBoltDirectShapeService
    {
        public class ConvertResult
        {
            public int Created { get; set; }
            public int Skipped { get; set; }
            public int CandidatesFoundInLink { get; set; }
        }

        public static ConvertResult ConvertPlatesAndBolts(
            Document hostDoc,
            Document linkDoc,
            Transform linkTransform,
            HashSet<string> ifcGuids)
        {
            var res = new ConvertResult();

            // Collect likely elements in the linked IFC document:
            // Most linked IFC parts come in as DirectShape or Generic Model-like categories.
            var candidates = new FilteredElementCollector(linkDoc)
                .WhereElementIsNotElementType()
                .ToElements()
                .Where(e => e.Category != null) // keep only categorized
                .ToList();

            res.CandidatesFoundInLink = candidates.Count;

            using (var opt = new Options())
            {
                opt.DetailLevel = ViewDetailLevel.Fine;
                opt.IncludeNonVisibleObjects = false;

                foreach (var e in candidates)
                {
                    try
                    {
                        // Try to match by IFC GUID parameter (common in linked IFC workflows).
                        // If not found, we skip. (You can expand this later.)
                        string guid = GetIfcGuidFromElement(e);
                        if (string.IsNullOrWhiteSpace(guid) || !ifcGuids.Contains(guid))
                        {
                            continue;
                        }

                        GeometryElement ge = e.get_Geometry(opt);
                        if (ge == null) { res.Skipped++; continue; }

                        List<GeometryObject> shapes = ExtractAndTransformGeometry(ge, linkTransform);
                        if (shapes.Count == 0) { res.Skipped++; continue; }

                        DirectShape ds = DirectShape.CreateElement(hostDoc, new ElementId(BuiltInCategory.OST_GenericModel));
                        ds.ApplicationId = "MRevit";
                        ds.ApplicationDataId = guid;

                        ds.SetShape(shapes);

                        // Store readable info
                        TrySetComment(ds, $"IFC:{guid}");

                        res.Created++;
                    }
                    catch
                    {
                        res.Skipped++;
                    }
                }
            }

            return res;
        }

        // Try a few common parameter names that appear on IFC-linked elements
        private static string GetIfcGuidFromElement(Element e)
        {
            // Common names (depends on IFC link setup / shared parameters)
            string[] names = { "IfcGUID", "IFCGUID", "IFC GUID", "GlobalId", "IFC GlobalId" };

            foreach (string n in names)
            {
                Parameter p = e.LookupParameter(n);
                if (p != null && p.StorageType == StorageType.String)
                {
                    string val = p.AsString();
                    if (!string.IsNullOrWhiteSpace(val)) return val.Trim();
                }
            }

            return null;
        }

        private static List<GeometryObject> ExtractAndTransformGeometry(GeometryElement ge, Transform linkTransform)
        {
            var solids = new List<Solid>();
            var meshes = new List<Mesh>();

            foreach (GeometryObject obj in ge)
            {
                if (obj is GeometryInstance inst)
                {
                    GeometryElement instGe = inst.GetInstanceGeometry();
                    foreach (GeometryObject iobj in instGe)
                        Collect(iobj, solids, meshes);
                }
                else
                {
                    Collect(obj, solids, meshes);
                }
            }

            var outObjs = new List<GeometryObject>();

            // Solids -> transform
            foreach (var s in solids)
            {
                if (s == null) continue;
                if (s.Volume < 1e-9) continue;

                Solid ts = SolidUtils.CreateTransformed(s, linkTransform);
                outObjs.Add(ts);
            }

            // Meshes -> rebuild using TessellatedShapeBuilder so we can apply transform
            if (meshes.Count > 0)
            {
                var tessObjs = BuildTessellatedObjects(meshes, linkTransform);
                outObjs.AddRange(tessObjs);
            }

            return outObjs;
        }

        private static void Collect(GeometryObject obj, List<Solid> solids, List<Mesh> meshes)
        {
            if (obj is Solid s)
            {
                if (s != null && s.Faces.Size > 0) solids.Add(s);
            }
            else if (obj is Mesh m)
            {
                meshes.Add(m);
            }
        }

        private static IList<GeometryObject> BuildTessellatedObjects(List<Mesh> meshes, Transform t)
        {
            var builder = new TessellatedShapeBuilder();
            builder.OpenConnectedFaceSet(false);
            builder.Target = TessellatedShapeBuilderTarget.AnyGeometry;
            builder.Fallback = TessellatedShapeBuilderFallback.Mesh;
            builder.GraphicsStyleId = ElementId.InvalidElementId;

            foreach (var m in meshes)
            {
                int triCount = m.NumTriangles;
                for (int i = 0; i < triCount; i++)
                {
                    MeshTriangle tri = m.get_Triangle(i);

                    XYZ v0 = t.OfPoint(tri.get_Vertex(0));
                    XYZ v1 = t.OfPoint(tri.get_Vertex(1));
                    XYZ v2 = t.OfPoint(tri.get_Vertex(2));

                    var face = new TessellatedFace(new List<XYZ> { v0, v1, v2 }, ElementId.InvalidElementId);
                    try
                    {
                        builder.AddFace(face);
                    }
                    catch
                    {
                        // ignore invalid triangles
                    }

                }
            }

            builder.CloseConnectedFaceSet();
            builder.Build();

            TessellatedShapeBuilderResult result = builder.GetBuildResult();
            return result.GetGeometricalObjects();
        }

        private static void TrySetComment(Element e, string text)
        {
            Parameter p = e.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
            if (p != null && !p.IsReadOnly) p.Set(text);
        }
    }
}
