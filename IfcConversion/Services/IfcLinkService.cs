using Autodesk.Revit.DB;

namespace RebarTools.IfcConversion.Services
{
    internal static class IfcLinkService
    {
        public static string TryGetIfcPathFromLink(Document hostDoc, RevitLinkInstance linkInst)
        {
            if (hostDoc == null || linkInst == null) return null;

            RevitLinkType linkType = hostDoc.GetElement(linkInst.GetTypeId()) as RevitLinkType;
            if (linkType == null) return null;

            ExternalFileReference extRef = linkType.GetExternalFileReference();
            if (extRef == null) return null;

            ModelPath mp = extRef.GetAbsolutePath();
            if (mp == null) return null;

            string userPath = ModelPathUtils.ConvertModelPathToUserVisiblePath(mp);
            if (string.IsNullOrWhiteSpace(userPath)) return null;

            string lower = userPath.ToLowerInvariant();
            bool isIfc = lower.EndsWith(".ifc") || lower.EndsWith(".ifczip") || lower.EndsWith(".ifcxml");

            // Revit often gives .RVT here => return null so we can ask the user
            return isIfc ? userPath : null;
        }
    }
}
