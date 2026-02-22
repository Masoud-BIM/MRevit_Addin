using System;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using RebarTools.ViewRangeVisualizer.Services;

namespace RebarTools.ViewRangeVisualizer.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class ViewRangeSectionBoxCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                View activeView = doc.ActiveView;

                // Must be a plan view
                ViewPlan planView = activeView as ViewPlan;
                if (planView == null)
                {
                    TaskDialog.Show("MRevit", "Please run this command in a Plan view (Floor/Ceiling/Engineering Plan).");
                    return Result.Cancelled;
                }

                // Crop box must be active for clean XY extents
                if (!planView.CropBoxActive)
                {
                    TaskDialog.Show("MRevit",
                        "Crop Box is not active in this view.\n\n" +
                        "Please enable Crop View (Crop Box) for the plan view, then run again.");
                    return Result.Cancelled;
                }

                ViewRangeInfo info = ViewRangeService.GetViewRangeInfo(doc, planView);
                if (!info.IsValid)
                {
                    TaskDialog.Show("MRevit", "Could not read View Range for this view.");
                    return Result.Cancelled;
                }

                using (Transaction t = new Transaction(doc, "MRevit - View Range 3D Box"))
                {
                    t.Start();

                    // Create or reuse 3D view
                    View3D v3d = View3DService.GetOrCreateDebug3DView(doc, planView);

                    // Build section box from crop box XY + view range Z
                    BoundingBoxXYZ sectionBox = View3DService.BuildSectionBoxFromCropAndRange(planView, info);
                    v3d.IsSectionBoxActive = true;
                    v3d.SetSectionBox(sectionBox);

                    // Remove old markers from previous runs
                    PlaneMarkerService.DeleteOldMarkers(doc);

                    // Create markers for planes (DirectShape rectangles)
                    PlaneMarkerService.CreateViewRangeMarkers(doc, v3d, sectionBox, info);

                    t.Commit();

                    // Switch to the 3D view
                    uidoc.RequestViewChange(v3d);
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
