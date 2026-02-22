using System;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

using RebarTools.IfcConversion.Services;
using RebarTools.IfcConversion.Utils;

namespace RebarTools.IfcConversion.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class ConvertIfcColumnsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                if (!RequiredTypeService.EnsureColumnType(doc, out FamilySymbol colType))
                    return Result.Cancelled;

                Reference r = uidoc.Selection.PickObject(ObjectType.Element, new RevitLinkInstanceFilter(), "Pick the IFC Link instance");
                RevitLinkInstance linkInst = doc.GetElement(r) as RevitLinkInstance;
                if (linkInst == null) return Result.Cancelled;

                string ifcPath = IfcPathResolverService.ResolveIfcPathOrPrompt(doc, linkInst);
                if (string.IsNullOrWhiteSpace(ifcPath))
                    return Result.Cancelled;


                using (Transaction t = new Transaction(doc, "Convert IFC Columns"))
                {
                    t.Start();

                    var res = IfcBeamColumnConverter.ConvertColumns(doc, ifcPath, linkInst.GetTransform(), colType);

                    t.Commit();

                    TaskDialog.Show("MRevit", $"Columns created: {res.Created}\nSkipped: {res.Skipped}");
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
