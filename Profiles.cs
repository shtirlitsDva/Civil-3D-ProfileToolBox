// Decompiled with JetBrains decompiler
// Type: ProfileToolBox.Profiles
// Assembly: ProfileToolBox, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 028DD63F-139F-4C4B-939A-7D47EA0DC56B

using Autodesk.Aec.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using System;
using System.Collections.Generic;
using System.Linq;


namespace ProfileToolBox
{
    public class Profiles : IExtensionApplication
    {
        #region IExtensionApplication Members
        public void Initialize()
        { // throw new System.Exception("The method or operation is not implemented.");
        }

        public void Terminate()
        { // throw new System.Exception("The method or operation is not implemented.");
        }
        #endregion      

        #region Global objects used throughout the application
        private DocumentCollection docCol = null;
        private CivilDocument doc = null;
        private Database database = null;
        private Editor editor = null;
        #endregion

        [CommandMethod("mypfp")]
        public void profilefrompolyline()
        {
            docCol = Application.DocumentManager;
            database = docCol.MdiActiveDocument.Database;
            editor = docCol.MdiActiveDocument.Editor;
            doc = Autodesk.Civil.ApplicationServices.CivilApplication.ActiveDocument;

            using (Transaction tx = database.TransactionManager.StartTransaction())
            {
                try
                {
                    PromptEntityOptions promptEntityOptions1 = new PromptEntityOptions("\n Select a polyline : ");
                    promptEntityOptions1.SetRejectMessage("\n Not a polyline");
                    promptEntityOptions1.AddAllowedClass(typeof(Polyline), true);
                    PromptEntityResult entity1 = editor.GetEntity(promptEntityOptions1);
                    if (((PromptResult)entity1).Status != PromptStatus.OK) return;
                    Autodesk.AutoCAD.DatabaseServices.ObjectId plObjId = entity1.ObjectId;
                    PromptEntityOptions promptEntityOptions2 = new PromptEntityOptions("\n Select a ProfileView: ");
                    promptEntityOptions1.SetRejectMessage("\n Not a ProfileView");
                    promptEntityOptions1.AddAllowedClass(typeof(ProfileView), true);
                    PromptEntityResult entity2 = editor.GetEntity(promptEntityOptions2);
                    if (((PromptResult)entity2).Status != PromptStatus.OK) return;

                    ProfileView profileView = tx.GetObject(entity2.ObjectId, OpenMode.ForWrite) as ProfileView;
                    double x = 0.0;
                    double y = 0.0;
                    if (profileView.ElevationRangeMode == ElevationRangeType.Automatic)
                    {
                        profileView.ElevationRangeMode = ElevationRangeType.UserSpecified;
                        profileView.FindXYAtStationAndElevation(profileView.StationStart, profileView.ElevationMin, ref x, ref y);
                    }
                    else
                        profileView.FindXYAtStationAndElevation(profileView.StationStart, profileView.ElevationMin, ref x, ref y);

                    ProfileViewStyle profileViewStyle = tx
                        .GetObject(((Autodesk.Aec.DatabaseServices.Entity)profileView)
                        .StyleId, OpenMode.ForRead) as ProfileViewStyle;

                    Autodesk.AutoCAD.DatabaseServices.ObjectId layerId =
                        ((Autodesk.Aec.DatabaseServices.Entity)
                        (tx.GetObject(profileView.AlignmentId, OpenMode.ForRead) as Alignment)).LayerId;

                    Autodesk.AutoCAD.DatabaseServices.ObjectId profileStyleId = ((StyleCollectionBase)doc.Styles.ProfileStyles).FirstOrDefault();

                    Autodesk.AutoCAD.DatabaseServices.ObjectId profileLabelSetStylesId =
                        ((StyleCollectionBase)doc.Styles.LabelSetStyles.ProfileLabelSetStyles).FirstOrDefault();

                    Autodesk.AutoCAD.DatabaseServices.ObjectId profByLayout =
                        Profile.CreateByLayout("New Profile", profileView.AlignmentId, layerId, profileStyleId, profileLabelSetStylesId);

                    Profile profile = tx.GetObject(profByLayout, OpenMode.ForWrite) as Profile;

                    BlockTableRecord blockTableRecord = tx.GetObject(database.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                    Polyline polyline = tx.GetObject(plObjId, OpenMode.ForRead, false) as Polyline;

                    if (polyline != null)
                    {
                        int numOfVert = polyline.NumberOfVertices - 1;
                        Point2d point2d1;
                        Point2d point2d2;
                        Point2d point2d3;

                        for (int i = 0; i < numOfVert; i++)
                        {
                            switch (polyline.GetSegmentType(i))
                            {
                                case SegmentType.Line:
                                    LineSegment2d lineSegment2dAt = polyline.GetLineSegment2dAt(i);
                                    point2d1 = lineSegment2dAt.StartPoint;
                                    double x1 = point2d1.X;
                                    double y1 = point2d1.Y;
                                    double num4 = x1 - x;
                                    double num5 = (y1 - y) / profileViewStyle.GraphStyle.VerticalExaggeration + profileView.ElevationMin;
                                    point2d2 = new Point2d(num4, num5);

                                    point2d1 = lineSegment2dAt.EndPoint;
                                    double x2 = point2d1.X;
                                    double y2 = point2d1.Y;
                                    double num6 = x2 - x;
                                    double num7 = (y2 - y) / profileViewStyle.GraphStyle.VerticalExaggeration + profileView.ElevationMin;
                                    point2d3 = new Point2d(num6, num7);

                                    profile.Entities.AddFixedTangent(point2d2, point2d3);
                                    break;
                                case SegmentType.Arc:
                                    CircularArc2d arcSegment2dAt = polyline.GetArcSegment2dAt(i);

                                    point2d1 = arcSegment2dAt.StartPoint;
                                    double x3 = point2d1.X;
                                    double y3 = point2d1.Y;
                                    point2d1 = arcSegment2dAt.EndPoint;
                                    double x4 = point2d1.X;
                                    double y4 = point2d1.Y;

                                    double num8 = x3 - x;
                                    double num9 = (y3 - y) / profileViewStyle.GraphStyle.VerticalExaggeration + profileView.ElevationMin;
                                    double num10 = x4 - x;
                                    double num11 = (y4 - y) / profileViewStyle.GraphStyle.VerticalExaggeration + profileView.ElevationMin;

                                    Point2d samplePoint = ((Curve2d)arcSegment2dAt).GetSamplePoints(11)[5]; //<-- was (10)[6] here, is wrong?
                                    double num12 = samplePoint.X - x;
                                    double num13 = samplePoint.Y - y / profileViewStyle.GraphStyle.VerticalExaggeration + profileView.ElevationMin;

                                    Point2d point2d4 = new Point2d(num12, num13);
                                    point2d3 = new Point2d(num10, num11);
                                    point2d2 = new Point2d(num8, num9);
                                    profile.Entities.AddFixedSymmetricParabolaByThreePoints(point2d2, point2d4, point2d3);

                                    break;
                                case SegmentType.Coincident:
                                    break;
                                case SegmentType.Point:
                                    break;
                                case SegmentType.Empty:
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    editor.WriteMessage("\n" + ex.Message);
                }
                tx.Commit();
            }
        }
    }
}


