using Autodesk.Revit.DB;

namespace RebarTools.ViewRangeVisualizer.Services
{
    internal class ViewRangeInfo
    {
        public bool IsValid { get; set; }

        public double TopZ { get; set; }
        public double CutZ { get; set; }
        public double BottomZ { get; set; }
        public double DepthZ { get; set; }

        public string TopLabel { get; set; }
        public string CutLabel { get; set; }
        public string BottomLabel { get; set; }
        public string DepthLabel { get; set; }
    }

    internal static class ViewRangeService
    {
        public static ViewRangeInfo GetViewRangeInfo(Document doc, ViewPlan planView)
        {
            ViewRangeInfo info = new ViewRangeInfo();

            try
            {
                PlanViewRange vr = planView.GetViewRange();

                info.TopZ = GetPlaneElevation(doc, vr, PlanViewPlane.TopClipPlane, out string topLabel);
                info.CutZ = GetPlaneElevation(doc, vr, PlanViewPlane.CutPlane, out string cutLabel);
                info.BottomZ = GetPlaneElevation(doc, vr, PlanViewPlane.BottomClipPlane, out string bottomLabel);
                info.DepthZ = GetPlaneElevation(doc, vr, PlanViewPlane.ViewDepthPlane, out string depthLabel);

                info.TopLabel = topLabel;
                info.CutLabel = cutLabel;
                info.BottomLabel = bottomLabel;
                info.DepthLabel = depthLabel;

                // Basic validity: depth should be <= bottom <= cut <= top (usually)
                info.IsValid = true;
                return info;
            }
            catch
            {
                info.IsValid = false;
                return info;
            }
        }

        private static double GetPlaneElevation(Document doc, PlanViewRange vr, PlanViewPlane plane, out string label)
        {
            label = plane.ToString();

            ElementId levelId = vr.GetLevelId(plane);
            double offset = vr.GetOffset(plane);

            double levelElev = 0.0;
            string levelName = "(None)";

            if (levelId != null && levelId != ElementId.InvalidElementId)
            {
                Level lvl = doc.GetElement(levelId) as Level;
                if (lvl != null)
                {
                    levelElev = lvl.Elevation;
                    levelName = lvl.Name;
                }
            }

            // Revit internal units (feet)
            double z = levelElev + offset;

            label = levelName + " + " + offset.ToString("0.###") + " ft";
            return z;
        }
    }
}
