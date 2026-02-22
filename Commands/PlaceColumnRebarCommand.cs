using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Structure;

namespace RebarTools.Commands
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class SelectHostRebarsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // Pick the host element (column/beam/wall/etc.)
                Reference pickedRef = uidoc.Selection.PickObject(
                    ObjectType.Element,
                    new AllowAnyElementSelectionFilter(),
                    "Pick a host element"
                );

                Element host = doc.GetElement(pickedRef);
                ElementId hostId = host.Id;

                // Collect ONLY rebars whose HostId == selected element
                IList<ElementId> rebarIds = new FilteredElementCollector(doc)
                    .OfClass(typeof(Rebar))
                    .Cast<Rebar>()
                    .Where(r => r.GetHostId() == hostId)
                    .Select(r => r.Id)
                    .ToList();

                // Select ONLY rebars (not host)
                uidoc.Selection.SetElementIds(rebarIds);

                TaskDialog.Show("Rebar Tools", $"Selected {rebarIds.Count} rebars.");

                return Result.Succeeded;
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

        private class AllowAnyElementSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem) => elem != null;
            public bool AllowReference(Reference reference, XYZ position) => true;
        }
    }
}
