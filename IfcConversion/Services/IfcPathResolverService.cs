using System.IO;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using WF = System.Windows.Forms;

namespace RebarTools.IfcConversion.Services
{
    internal static class IfcPathResolverService
    {
        // remembers last chosen IFC path during the current Revit session
        private static string _lastIfcPath;

        public static string ResolveIfcPathOrPrompt(Document hostDoc, RevitLinkInstance linkInst)
        {
            // 1) try from link (only returns .ifc/.ifczip/.ifcxml)
            string ifcPath = IfcLinkService.TryGetIfcPathFromLink(hostDoc, linkInst);
            if (IsValidIfcFile(ifcPath))
            {
                _lastIfcPath = ifcPath;
                return ifcPath;
            }

            // 2) reuse last chosen path (if still exists)
            if (IsValidIfcFile(_lastIfcPath))
                return _lastIfcPath;

            // 3) ask user to pick the original IFC
            using (var ofd = new WF.OpenFileDialog())
            {
                ofd.Title = "Revit linked a cached RVT. Please select the ORIGINAL IFC file";
                ofd.Filter = "IFC (*.ifc;*.ifczip;*.ifcxml)|*.ifc;*.ifczip;*.ifcxml";
                ofd.Multiselect = false;

                if (ofd.ShowDialog() != WF.DialogResult.OK)
                    return null;

                ifcPath = ofd.FileName;
            }

            if (!IsValidIfcFile(ifcPath))
            {
                TaskDialog.Show("MRevit", "Selected file is not a valid IFC format (.ifc, .ifczip, .ifcxml).");
                return null;
            }

            _lastIfcPath = ifcPath;
            return ifcPath;
        }

        private static bool IsValidIfcFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            if (!File.Exists(path)) return false;

            string lower = path.ToLowerInvariant();
            return lower.EndsWith(".ifc") || lower.EndsWith(".ifczip") || lower.EndsWith(".ifcxml");
        }
    }
}
