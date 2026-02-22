using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace RebarTools.IfcConversion.Utils
{
    internal class RevitLinkInstanceFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem) => elem is RevitLinkInstance;
        public bool AllowReference(Reference reference, XYZ position) => false;
    }
}
