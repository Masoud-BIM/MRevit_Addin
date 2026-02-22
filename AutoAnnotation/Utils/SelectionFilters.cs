using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace RebarTools.AutoAnnotation.Utils
{
    internal class SectionElevationViewOrViewportFilter : ISelectionFilter
    {
        private readonly Document _doc;
        public SectionElevationViewOrViewportFilter(Document doc) { _doc = doc; }

        public bool AllowElement(Element elem)
        {
            if (elem == null) return false;

            // Viewport on sheet -> check its view type
            Viewport vp = elem as Viewport;
            if (vp != null)
            {
                View v = _doc.GetElement(vp.ViewId) as View;
                return v != null && (v.ViewType == ViewType.Section || v.ViewType == ViewType.Elevation);
            }

            // View directly (rare)
            View view = elem as View;
            if (view != null)
                return view.ViewType == ViewType.Section || view.ViewType == ViewType.Elevation;

            return false;
        }

        public bool AllowReference(Reference reference, XYZ position) => false;
    }
}
