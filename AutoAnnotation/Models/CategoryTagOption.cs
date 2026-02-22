using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RebarTools.AutoAnnotation.Models
{
    internal class CategoryTagOption
    {
        public string DisplayName { get; set; }

        public BuiltInCategory ElementCategory { get; set; }
        public BuiltInCategory TagCategory { get; set; }

        public bool IsSelected { get; set; }

        public List<FamilySymbol> AvailableTagTypes { get; set; } = new List<FamilySymbol>();
        public ElementId SelectedTagTypeId { get; set; } = ElementId.InvalidElementId;
    }
}
