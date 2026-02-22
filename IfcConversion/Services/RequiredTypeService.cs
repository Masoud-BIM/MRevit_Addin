using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RebarTools.IfcConversion.Services
{
    internal static class RequiredTypeService
    {
        public static bool EnsureBeamType(Document doc, out FamilySymbol beamType)
        {
            beamType = GetFirstTypeOrNull(doc, BuiltInCategory.OST_StructuralFraming);

            if (beamType == null)
            {
                TaskDialog.Show("MRevit",
                    "Missing required Revit Beam type.\n\n" +
                    "Please load at least one Structural Framing (Beam) family/type:\n" +
                    "Insert tab → Load Autodesk Family (cloud) OR Load Family (local)\n\n" +
                    "Then run this command again."
                );
                return false;
            }
            return true;
        }

        public static bool EnsureColumnType(Document doc, out FamilySymbol columnType)
        {
            columnType = GetFirstTypeOrNull(doc, BuiltInCategory.OST_StructuralColumns);

            if (columnType == null)
            {
                TaskDialog.Show("MRevit",
                    "Missing required Revit Column type.\n\n" +
                    "Please load at least one Structural Column family/type:\n" +
                    "Insert tab → Load Autodesk Family (cloud) OR Load Family (local)\n\n" +
                    "Then run this command again."
                );
                return false;
            }
            return true;
        }

        private static FamilySymbol GetFirstTypeOrNull(Document doc, BuiltInCategory bic)
        {
            return new FilteredElementCollector(doc)
                .WhereElementIsElementType()
                .OfClass(typeof(FamilySymbol))
                .OfCategory(bic)
                .Cast<FamilySymbol>()
                .FirstOrDefault();
        }
    }
}
