using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using RebarTools.IfcConversion.Services;
using RebarTools.IfcConversion.Utils;
using System;

namespace RebarTools.IfcConversion.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class ConvertIfcPlatesBoltsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document hostDoc = uidoc.Document;

            try
            {
                Reference r = uidoc.Selection.PickObject(ObjectType.Element, new RevitLinkInstanceFilter(), "Pick the IFC Link instance");
                RevitLinkInstance linkInst = hostDoc.GetElement(r) as RevitLinkInstance;
                if (linkInst == null) return Result.Cancelled;

                Document linkDoc = linkInst.GetLinkDocument();
                if (linkDoc == null)
                {
                    TaskDialog.Show("MRevit", "Link document not available (link may be unloaded).");
                    return Result.Cancelled;
                }

                string ifcPath = IfcPathResolverService.ResolveIfcPathOrPrompt(hostDoc, linkInst);
                if (string.IsNullOrWhiteSpace(ifcPath))
                    return Result.Cancelled;


                // Read GUIDs of plates + bolts from IFC
                var guids = IfcReadService.ReadPlateAndBoltGuids(ifcPath);
                if (guids.Count == 0)
                {
                    TaskDialog.Show("MRevit", "No IfcPlate / IfcFastener found in the IFC file.");
                    return Result.Succeeded;
                }

                using (Transaction t = new Transaction(hostDoc, "Convert IFC Plates + Bolts"))
                {
                    t.Start();

                    var res = IfcPlateBoltDirectShapeService.ConvertPlatesAndBolts(
                        hostDoc, linkDoc, linkInst.GetTransform(), guids);

                    t.Commit();

                    TaskDialog.Show("MRevit",
                        $"DirectShapes created: {res.Created}\n" +
                        $"Skipped: {res.Skipped}\n" +
                        $"Link candidates scanned: {res.CandidatesFoundInLink}\n\n" +
                        $"Note: This step matches elements by IfcGUID parameter. If your link doesn't have IfcGUID parameters, we’ll adapt the matcher.");
                }

                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.ToString();
                return Result.Failed;
            }
        }
    }
}
