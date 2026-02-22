using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

using WF = System.Windows.Forms;

namespace RebarTools.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class SortHostRebarsAndMarkCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // 1) Pick host
                Reference pickedRef = uidoc.Selection.PickObject(
                    ObjectType.Element,
                    new AllowAnyElementSelectionFilter(),
                    "Pick a host element"
                );

                Element host = doc.GetElement(pickedRef);
                if (host == null) return Result.Cancelled;

                // 2) Collect hosted rebars
                ElementId hostId = host.Id;

                List<Rebar> rebars = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .OfCategory(BuiltInCategory.OST_Rebar)
                    .OfClass(typeof(Rebar))
                    .Cast<Rebar>()
                    .Where(r => r.GetHostId() == hostId)
                    .ToList();

                if (rebars.Count == 0)
                {
                    TaskDialog.Show("Rebar Tools", "No hosted rebars found on the selected element.");
                    return Result.Succeeded;
                }

                List<RebarInfo> infos = rebars.Select(r => RebarInfo.FromRebar(doc, r)).ToList();

                // 3) Ask user sort order + start number
                using (var dlg = new SortAndMarkForm())
                {
                    if (dlg.ShowDialog() != WF.DialogResult.OK)
                        return Result.Cancelled;

                    int startNumber = dlg.StartNumber;

                    var keys = new List<SortKey>();
                    if (dlg.PrimaryKey.HasValue) keys.Add(dlg.PrimaryKey.Value);
                    if (dlg.SecondaryKey.HasValue) keys.Add(dlg.SecondaryKey.Value);
                    if (dlg.TertiaryKey.HasValue) keys.Add(dlg.TertiaryKey.Value);

                    if (keys.Count == 0)
                    {
                        TaskDialog.Show("Rebar Tools", "Choose at least one sorting parameter.");
                        return Result.Cancelled;
                    }

                    // 4) Sort
                    IOrderedEnumerable<RebarInfo> ordered = OrderByKey(infos, keys[0]);
                    for (int i = 1; i < keys.Count; i++)
                        ordered = ThenByKey(ordered, keys[i]);

                    List<RebarInfo> sorted = ordered.ToList();

                    // 5) Write sequential numbers to Rebar Mark
                    using (Transaction t = new Transaction(doc, "Sort Host Rebars and Mark"))
                    {
                        t.Start();

                        int current = startNumber;
                        foreach (var info in sorted)
                        {
                            SetRebarMarkOnly(info.Rebar, current.ToString());
                            current++;
                        }

                        t.Commit();
                    }

                    // Select processed rebars
                    uidoc.Selection.SetElementIds(sorted.Select(x => x.Rebar.Id).ToList());

                    TaskDialog.Show("Rebar Tools", $"Sorted and marked {sorted.Count} rebars starting from {startNumber}.");
                    return Result.Succeeded;
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private static void SetRebarMarkOnly(Rebar rebar, string value)
        {
            // This is the "Mark" parameter in Properties.
            Parameter p = rebar.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);

            if (p == null)
                throw new InvalidOperationException("Mark parameter (ALL_MODEL_MARK) not found on a rebar.");

            if (p.IsReadOnly)
                throw new InvalidOperationException("Mark parameter is read-only on one or more rebars.");

            p.Set(value);
        }

        private static IOrderedEnumerable<RebarInfo> OrderByKey(IEnumerable<RebarInfo> src, SortKey key)
        {
            switch (key)
            {
                case SortKey.BarLength: return src.OrderBy(x => x.BarLengthInternal);
                case SortKey.Diameter: return src.OrderBy(x => x.DiameterInternal);
                case SortKey.Shape: return src.OrderBy(x => x.ShapeName ?? "");
                default: return src.OrderBy(x => 0);
            }
        }

        private static IOrderedEnumerable<RebarInfo> ThenByKey(IOrderedEnumerable<RebarInfo> src, SortKey key)
        {
            switch (key)
            {
                case SortKey.BarLength: return src.ThenBy(x => x.BarLengthInternal);
                case SortKey.Diameter: return src.ThenBy(x => x.DiameterInternal);
                case SortKey.Shape: return src.ThenBy(x => x.ShapeName ?? "");
                default: return src.ThenBy(x => 0);
            }
        }

        private class AllowAnyElementSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem) => elem != null;
            public bool AllowReference(Reference reference, XYZ position) => true;
        }
    }

    public enum SortKey
    {
        BarLength,
        Diameter,
        Shape
    }

    internal class RebarInfo
    {
        public Rebar Rebar { get; private set; }
        public double BarLengthInternal { get; private set; } // feet
        public double DiameterInternal { get; private set; }  // feet
        public string ShapeName { get; private set; }

        public static RebarInfo FromRebar(Document doc, Rebar rebar)
        {
            return new RebarInfo
            {
                Rebar = rebar,
                BarLengthInternal = GetBarLengthInternal(rebar),
                DiameterInternal = GetDiameterInternal(doc, rebar),
                ShapeName = GetShapeName(doc, rebar)
            };
        }

        private static double GetBarLengthInternal(Rebar rebar)
        {
            // "Bar Length" in UI (stable access via BuiltInParameter)
            Parameter p = rebar.get_Parameter(BuiltInParameter.REBAR_ELEM_LENGTH);
            if (p != null && p.StorageType == StorageType.Double) return p.AsDouble();

            Parameter p2 = rebar.get_Parameter(BuiltInParameter.REBAR_ELEM_TOTAL_LENGTH);
            if (p2 != null && p2.StorageType == StorageType.Double) return p2.AsDouble();

            return 0.0;
        }

        private static double GetDiameterInternal(Document doc, Rebar rebar)
        {
            Element typeElem = doc.GetElement(rebar.GetTypeId());
            if (typeElem is RebarBarType barType)
            {
                Parameter p = barType.get_Parameter(BuiltInParameter.REBAR_BAR_DIAMETER);
                if (p != null && p.StorageType == StorageType.Double) return p.AsDouble();
            }
            return 0.0;
        }

        private static string GetShapeName(Document doc, Rebar rebar)
        {
            ElementId shapeId = rebar.GetShapeId();
            if (shapeId != ElementId.InvalidElementId)
            {
                Element shape = doc.GetElement(shapeId);
                if (shape != null) return shape.Name;
            }
            return "";
        }
    }
}
