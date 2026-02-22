using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

using RebarTools.AutoAnnotation.Models;
using RebarTools.AutoAnnotation.UI;
using RebarTools.AutoAnnotation.Utils;

using WF = System.Windows.Forms;

namespace RebarTools.AutoAnnotation.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class AutoAnnotateViewCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // Pick a Section/Elevation viewport (or ESC to use active view)
                View selectedView = null;

                try
                {
                    Reference picked = uidoc.Selection.PickObject(
                        ObjectType.Element,
                        new SectionElevationViewOrViewportFilter(doc),
                        "Pick a Section/Elevation viewport (or press ESC to use the active Section/Elevation view)"
                    );

                    Element pickedElem = doc.GetElement(picked);
                    selectedView = ResolveView(doc, pickedElem);
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    View av = doc.ActiveView;
                    if (IsSectionOrElevation(av)) selectedView = av;
                    else return Result.Cancelled;
                }

                if (selectedView == null)
                {
                    TaskDialog.Show("MRevit", "No valid Section/Elevation view selected.");
                    return Result.Cancelled;
                }

                // Supported categories (no rebar)
                var supported = new List<CategoryMap>
                {
                    new CategoryMap("Walls",               BuiltInCategory.OST_Walls,               BuiltInCategory.OST_WallTags),
                    new CategoryMap("Doors",               BuiltInCategory.OST_Doors,               BuiltInCategory.OST_DoorTags),
                    new CategoryMap("Windows",             BuiltInCategory.OST_Windows,             BuiltInCategory.OST_WindowTags),
                    new CategoryMap("Columns",             BuiltInCategory.OST_Columns,             BuiltInCategory.OST_StructuralColumnTags),
                    new CategoryMap("Structural Framing",  BuiltInCategory.OST_StructuralFraming,   BuiltInCategory.OST_StructuralFramingTags),
                    new CategoryMap("Floors",              BuiltInCategory.OST_Floors,              BuiltInCategory.OST_FloorTags),
                    new CategoryMap("Generic Model",       BuiltInCategory.OST_GenericModel,        BuiltInCategory.OST_GenericModelTags),
                };

                // Build options with available tag types per category
                List<CategoryTagOption> options = supported.Select(m => BuildOption(doc, m)).ToList();

                using (var dlg = new AutoAnnotateForm(options))
                {
                    if (dlg.ShowDialog() != WF.DialogResult.OK)
                        return Result.Cancelled;

                    var chosen = dlg.Options
                        .Where(o => o.IsSelected && o.SelectedTagTypeId != ElementId.InvalidElementId)
                        .ToList();

                    if (chosen.Count == 0)
                    {
                        TaskDialog.Show("MRevit", "No categories selected (or no tag types available).");
                        return Result.Cancelled;
                    }

                    int created = 0;
                    int skipped = 0;

                    using (Transaction t = new Transaction(doc, "Auto Annotate View"))
                    {
                        t.Start();

                        // Activate selected tag symbols
                        foreach (var opt in chosen)
                        {
                            FamilySymbol sym = doc.GetElement(opt.SelectedTagTypeId) as FamilySymbol;
                            if (sym != null && !sym.IsActive) sym.Activate();
                        }

                        foreach (var opt in chosen)
                        {
                            var elems = new FilteredElementCollector(doc, selectedView.Id)
                                .WhereElementIsNotElementType()
                                .OfCategory(opt.ElementCategory);

                            foreach (Element e in elems)
                            {
                                try
                                {
                                    BoundingBoxXYZ bbox = e.get_BoundingBox(selectedView);
                                    if (bbox == null) { skipped++; continue; }

                                    XYZ pt = (bbox.Min + bbox.Max) * 0.5;

                                    IndependentTag tag = IndependentTag.Create(
                                        doc,
                                        opt.SelectedTagTypeId,
                                        selectedView.Id,
                                        new Reference(e),
                                        false,
                                        TagOrientation.Horizontal,
                                        pt
                                    );

                                    if (tag != null) created++;
                                    else skipped++;
                                }
                                catch
                                {
                                    skipped++;
                                }
                            }
                        }

                        t.Commit();
                    }

                    TaskDialog.Show("MRevit", $"View: {selectedView.Name}\nCreated: {created}\nSkipped: {skipped}");
                    return Result.Succeeded;
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private static bool IsSectionOrElevation(View v)
        {
            if (v == null) return false;
            return v.ViewType == ViewType.Section || v.ViewType == ViewType.Elevation;
        }

        private static View ResolveView(Document doc, Element pickedElem)
        {
            Viewport vp = pickedElem as Viewport;
            if (vp != null)
            {
                View v = doc.GetElement(vp.ViewId) as View;
                return IsSectionOrElevation(v) ? v : null;
            }

            View view = pickedElem as View;
            return IsSectionOrElevation(view) ? view : null;
        }

        private static CategoryTagOption BuildOption(Document doc, CategoryMap map)
        {
            List<FamilySymbol> tagTypes = new FilteredElementCollector(doc)
                .WhereElementIsElementType()
                .OfClass(typeof(FamilySymbol))
                .OfCategory(map.TagCategory)
                .Cast<FamilySymbol>()
                .OrderBy(x => x.FamilyName)
                .ThenBy(x => x.Name)
                .ToList();

            return new CategoryTagOption
            {
                DisplayName = map.DisplayName,
                ElementCategory = map.ElementCategory,
                TagCategory = map.TagCategory,
                IsSelected = tagTypes.Count > 0,
                AvailableTagTypes = tagTypes,
                SelectedTagTypeId = tagTypes.Count > 0 ? tagTypes[0].Id : ElementId.InvalidElementId
            };
        }

        private class CategoryMap
        {
            public string DisplayName;
            public BuiltInCategory ElementCategory;
            public BuiltInCategory TagCategory;

            public CategoryMap(string displayName, BuiltInCategory elemCat, BuiltInCategory tagCat)
            {
                DisplayName = displayName;
                ElementCategory = elemCat;
                TagCategory = tagCat;
            }
        }
    }
}
