using System;
using System.Collections;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using Plankton;
using PlanktonGh;

namespace Leopard
{
    public class GHC_EdgeNeighbours : GH_Component
    {


        public GHC_EdgeNeighbours()
            : base("Leopard's EdgeNeighbours", "L EdgeNB",
                "Getting Edge Neighbours Data, including face-edge-vertices",
                "Leopard", "Topology")
        {
        }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Mesh", "M", "input mesh", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Index", "I", "Edge Indices", GH_ParamAccess.tree);
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("This Edge", "T", "This Edge Position", GH_ParamAccess.tree);
            pManager.AddPointParameter("Edge Vertices", "V", "edge end points as list", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Edge Faces", "F", "two faces as curves list", GH_ParamAccess.tree);
            pManager.AddPointParameter("Edge Faces Center", "C", "two faces center points as list", GH_ParamAccess.tree);
            pManager.HideParameter(3);
        }



        protected override void SolveInstance(IGH_DataAccess DA)
        {

            GH_Structure<IGH_GeometricGoo> iMeshTree = new GH_Structure<IGH_GeometricGoo>();
            GH_Structure<GH_Integer> iEdgeIndices = new GH_Structure<GH_Integer>();

            DA.GetDataTree<IGH_GeometricGoo>(0, out iMeshTree);
            DA.GetDataTree<GH_Integer>(1, out iEdgeIndices);

            GH_Structure<IGH_GeometricGoo> oEdgeTree = new GH_Structure<IGH_GeometricGoo>();
            GH_Structure<IGH_Goo> oEdgeEndsTree = new GH_Structure<IGH_Goo>();   
            GH_Structure<IGH_Goo> oEdgeFacesTree = new GH_Structure<IGH_Goo>();      
            GH_Structure<IGH_Goo> oEdgeFaceCentersTree = new GH_Structure<IGH_Goo>();       

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

                        for (int vi = 0; vi < iEdgeIndices.PathCount; vi++)
                        {
                            int id;
                            GH_Path pathEdges = iEdgeIndices.get_Path(vi);
                            if (path != pathEdges)
                                continue;
                            for (int vj = 0; vj < iEdgeIndices.Branches[vi].Count; vj++)
                            {
                                iEdgeIndices.Branches[vi][vj].CastTo<int>(out id);

                                id *= 2;
                                // getting neighbouring vertices 
                                int pairIndex = pMesh.Halfedges.GetPairHalfedge(id);
                                if (pairIndex < id) { int temp = id; id = pairIndex; pairIndex = temp; }

                                int[] vts = pMesh.Halfedges.GetVertices(id);
                                // edge line output
                                oEdgeTree.Append(GH_Convert.ToGeometricGoo(
                                    new LineCurve(pMesh.Vertices[vts[0]].ToPoint3d(), pMesh.Vertices[vts[1]].ToPoint3d())), 
                                    path);


                                List<int> newPath = new List<int>();
                                for (int p = 0; p < path.Indices.Length; p++)
                                    newPath.Add(path.Indices[p]);
                                newPath.Add(vj);
                                GH_Path vertexPath = new GH_Path(newPath.ToArray());


                                // edge end points output  
                                oEdgeEndsTree.Append(GH_Convert.ToGeometricGoo(pMesh.Vertices[vts[0]].ToPoint3d()), vertexPath);
                                oEdgeEndsTree.Append(GH_Convert.ToGeometricGoo(pMesh.Vertices[vts[1]].ToPoint3d()), vertexPath);


                                // edge faces
                                int fA = pMesh.Halfedges[id].AdjacentFace;
                                int fB = pMesh.Halfedges[pairIndex].AdjacentFace;

                                Polyline facePolyA = new Polyline();
                                Polyline facePolyB = new Polyline();
                                if (fA != -1)
                                {
                                    int[] vsA = pMesh.Faces.GetFaceVertices(fA);

                                    for (int f = 0; f <= vsA.Length; f++)
                                    {
                                        var v = pMesh.Vertices[vsA[f % vsA.Length]];
                                        facePolyA.Add(v.X, v.Y, v.Z);
                                    }
                                    oEdgeFacesTree.Append(GH_Convert.ToGeometricGoo(facePolyA), vertexPath);
                                    oEdgeFaceCentersTree.Append(
                                        GH_Convert.ToGeometricGoo(pMesh.Faces.GetFaceCenter(fA).ToPoint3d()), 
                                        vertexPath);
                                }
                                if (fB != -1)
                                {
                                    int[] vsB = pMesh.Faces.GetFaceVertices(fB);
                                    for (int f = 0; f <= vsB.Length; f++)
                                    {
                                        var v = pMesh.Vertices[vsB[f % vsB.Length]];
                                        facePolyB.Add(v.X, v.Y, v.Z);
                                    }
                                    oEdgeFacesTree.Append(GH_Convert.ToGeometricGoo(facePolyB), vertexPath);
                                    oEdgeFaceCentersTree.Append(
                                        GH_Convert.ToGeometricGoo(pMesh.Faces.GetFaceCenter(fB).ToPoint3d()),
                                        vertexPath);
                                }

                                
                            }
                        }
                    }
                }
            }

            DA.SetDataTree(0, oEdgeTree);
            DA.SetDataTree(1, oEdgeEndsTree);
            DA.SetDataTree(2, oEdgeFacesTree);
            DA.SetDataTree(3, oEdgeFaceCentersTree);


            /*
            pMesh = iMesh.ToPlanktonMesh();

            iEdgeIndex *= 2;

            int pairIndex = pMesh.Halfedges.GetPairHalfedge(iEdgeIndex);
            if (pairIndex < iEdgeIndex) { int temp = iEdgeIndex; iEdgeIndex = pairIndex; pairIndex = temp;}

            int[] vts = pMesh.Halfedges.GetVertices(iEdgeIndex);

            // edge line output
            oEdge = new LineCurve(pMesh.Vertices[vts[0]].ToPoint3d(), pMesh.Vertices[vts[1]].ToPoint3d());

            // edge end points output 
            oEdgeEnds.Add(pMesh.Vertices[vts[0]].ToPoint3d());
            oEdgeEnds.Add(pMesh.Vertices[vts[1]].ToPoint3d());

            // edge faces
            int fA = pMesh.Halfedges[iEdgeIndex].AdjacentFace;
            int fB = pMesh.Halfedges[pairIndex].AdjacentFace;

            Polyline facePolyA = new Polyline(); 
            Polyline facePolyB = new Polyline();
            if (fA != -1)
            {
                int[] vsA = pMesh.Faces.GetFaceVertices(fA);

                for (int j = 0; j <= vsA.Length; j++)
                {
                    var v = pMesh.Vertices[vsA[j % vsA.Length]];
                    facePolyA.Add(v.X, v.Y, v.Z);
                }
                oEdgeFaces.Add(facePolyA);
                oEdgeFaceCenters.Add(pMesh.Faces.GetFaceCenter(fA).ToPoint3d());
            }
            if (fB != -1)
            {
                int[] vsB = pMesh.Faces.GetFaceVertices(fB);
                for (int j = 0; j <= vsB.Length; j++)
                {
                    var v = pMesh.Vertices[vsB[j % vsB.Length]];
                    facePolyB.Add(v.X, v.Y, v.Z);
                }
                oEdgeFaces.Add(facePolyB);
                oEdgeFaceCenters.Add(pMesh.Faces.GetFaceCenter(fB).ToPoint3d());
            }


            DA.SetData("This Edge", oEdge);
            DA.SetDataList("Edge Vertices", oEdgeEnds);
            DA.SetDataList("Edge Faces", oEdgeFaces);
            DA.SetDataList("Edge Faces Center", oEdgeFaceCenters);
            */
        }


        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.secondary; }
        }


        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.EdgeNeighbours;
            }
        }


        public override Guid ComponentGuid
        {
            get { return new Guid("{90144138-f513-4ba1-b051-b478b5eb4af7}"); }
        }
    }
}