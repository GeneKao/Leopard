using System;
using System.Collections.Generic;
using System.Windows.Forms;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Rhino;

using Plankton;
using PlanktonGh;

namespace Leopard
{
    public class GHC_Faces : GH_Component
    {

        //// input
        //Mesh iMesh = null;

        //// output        
        //List<Point3d> oFaceCenters = null;
        //List<Polyline> oFaceBoundarys = null;
        //List<Polyline> oNakedFaces = null;
        //List<Polyline> oClosedFaces = null;

        //// internal usage
        //PlanktonMesh pMesh = null;

        public GHC_Faces()
            : base("Leopard's Faces", "L Faces",
                "get faces data in plankton sequence ",
                "Leopard", "Topology")
        {
        }

        
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Mesh", "M", "input mesh", GH_ParamAccess.tree);
        }

        
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Face Centers", "O", "get all face centers", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Face Boundary Curves", "F", "get all face boundary curves", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Naked Faces", "N", "get all naked faces and its boundary curves", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Naked Faces Indices", "NI", "Naked Faces Indices", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Closed Faces", "C", "get all closed faces and its boundary curves", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Closed Faces Indices", "CI", "Closed Faces Indices", GH_ParamAccess.tree);
        }


        protected override void BeforeSolveInstance()
        {
            //// input 
            //iMesh = new Mesh();

            //// output 
            //oFaceCenters = new List<Point3d>();
            //oFaceBoundarys = new List<Polyline>();
            //oNakedFaces = new List<Polyline>();
            //oClosedFaces = new List<Polyline>();

            //// internal usage
            //pMesh = new PlanktonMesh();
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {

            GH_Structure<IGH_GeometricGoo> iMeshTree = new GH_Structure<IGH_GeometricGoo>();

            DA.GetDataTree<IGH_GeometricGoo>(0, out iMeshTree);

            GH_Structure<IGH_Goo> oFaceCentersTree = new GH_Structure<IGH_Goo>();
            GH_Structure<IGH_Goo> oFaceBoundarysTree = new GH_Structure<IGH_Goo>();
            GH_Structure<IGH_Goo> oNakedFacesTree = new GH_Structure<IGH_Goo>();
            GH_Structure<GH_Integer> oNakedIndicesTree = new GH_Structure<GH_Integer>();
            GH_Structure<IGH_Goo> oClosedFacesTree = new GH_Structure<IGH_Goo>();
            GH_Structure<GH_Integer> oClosedIndicesTree = new GH_Structure<GH_Integer>();


            for (int i = 0; i < iMeshTree.PathCount; i++)
            {
                GH_Path path = iMeshTree.get_Path(i);

                for (int j = 0; j < iMeshTree.Branches[i].Count; j++)
                {
                    Mesh mesh;
                    if (!iMeshTree.Branches[i][j].CastTo<Mesh>(out mesh))
                        continue;
                    else
                    {
                        PlanktonMesh pMesh = mesh.ToPlanktonMesh();

                        for (int f = 0; f < pMesh.Faces.Count; f++)
                        {
                            // get face centers
                            PlanktonXYZ center = pMesh.Faces.GetFaceCenter(f);
                            oFaceCentersTree.Append(GH_Convert.ToGeometricGoo(center.ToPoint3d()), path);
                        }

                        // get face boundarys
                        Polyline[] boundaryCurves = pMesh.ToPolylines();

                        for (int p = 0; p < boundaryCurves.Length; p++)
                        {
                            oFaceBoundarysTree.Append(GH_Convert.ToGeometricGoo(boundaryCurves[p]), path);
                            if (pMesh.Faces.NakedEdgeCount(p) > 0)
                            {
                                oNakedFacesTree.Append(GH_Convert.ToGeometricGoo(boundaryCurves[p]), path); 
                                oNakedIndicesTree.Append(new GH_Integer(p), path); 
                            }
                            else
                            {
                                oClosedFacesTree.Append(GH_Convert.ToGeometricGoo(boundaryCurves[p]), path);
                                oClosedIndicesTree.Append(new GH_Integer(p), path);

                            }
                        }

                        //for (int e = 0; e < pMesh.Halfedges.Count; e++)
                        //{
                        //    if (pMesh.Halfedges.GetPairHalfedge(e) > e) continue;

                        //    int[] vts = pMesh.Halfedges.GetVertices(e);
                        //    oEdgesTree.Append(
                        //        GH_Convert.ToGeometricGoo(new LineCurve(
                        //            pMesh.Vertices[vts[0]].ToPoint3d(),
                        //            pMesh.Vertices[vts[1]].ToPoint3d())), path);

                        //    if (pMesh.Halfedges.IsBoundary(e))
                        //    {
                        //        oNakedEdgesTree.Append(
                        //            GH_Convert.ToGeometricGoo(new LineCurve(
                        //            pMesh.Vertices[vts[0]].ToPoint3d(),
                        //            pMesh.Vertices[vts[1]].ToPoint3d())), path);

                        //        oNakedIndicesTree.Append(new GH_Integer(e / 2), path);
                        //    }
                        //    else
                        //    {
                        //        oClosedEdgesTree.Append(
                        //            GH_Convert.ToGeometricGoo(new LineCurve(
                        //            pMesh.Vertices[vts[0]].ToPoint3d(),
                        //            pMesh.Vertices[vts[1]].ToPoint3d())), path);

                        //        oClosedIndicesTree.Append(new GH_Integer(e / 2), path);
                        //    }
                        //}

                    }
                }
            }

            DA.SetDataTree(0, oFaceCentersTree);
            DA.SetDataTree(1, oFaceBoundarysTree);
            DA.SetDataTree(2, oNakedFacesTree);
            DA.SetDataTree(3, oNakedIndicesTree);
            DA.SetDataTree(4, oClosedFacesTree);
            DA.SetDataTree(5, oClosedIndicesTree);

            //DA.GetData<Mesh>("Mesh", ref iMesh);

            //pMesh = iMesh.ToPlanktonMesh();

            //for (int i = 0; i < pMesh.Faces.Count; i++)
            //{
            //    // get face centers
            //    PlanktonXYZ center = pMesh.Faces.GetFaceCenter(i);
            //    oFaceCenters.Add(center.ToPoint3d());          

            //}

            //// get face boundarys
            //Polyline[] boundaryCurves = pMesh.ToPolylines();

            //for (int p = 0; p < boundaryCurves.Length; p++)
            //{
            //    oFaceBoundarys.Add(boundaryCurves[p]);
            //    if (pMesh.Faces.NakedEdgeCount(p) > 0)
            //        oNakedFaces.Add(boundaryCurves[p]);
            //    else
            //        oClosedFaces.Add(boundaryCurves[p]);
            //}


            //DA.SetDataList("Face Centers", oFaceCenters);
            //DA.SetDataList("Face Boundary Curves", oFaceBoundarys);
            //DA.SetDataList("Face Naked", oNakedFaces);
            //DA.SetDataList("Face Closed", oClosedFaces);
        }


        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }
        
        
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.Face;
            }
        }


        public override Guid ComponentGuid
        {
            get { return new Guid("{51b5247a-1444-4a16-ae3d-020930c2700e}"); }
        }
    }
}