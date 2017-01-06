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
    public class GHC_VertexNeighbours : GH_Component
    {

        public GHC_VertexNeighbours()
            : base("Leopard's VerticesNeighbours", "L VerticesN",
                "Getting Vertex Neighbours Data, including face-edge-vertices",
                "Leopard", "Topology")
        {
        }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Mesh", "M", "input mesh", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Vertex", "V", "Vertex indices", GH_ParamAccess.tree);
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("This Vertex", "T", "This Vertices", GH_ParamAccess.tree);
            pManager.AddPointParameter("Vertex NV Points", "V", "all vertices neighbours", GH_ParamAccess.tree);
            pManager.AddLineParameter("Vertex NE Lines", "E", "all edges neighbours", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Vertex NF PolyLines", "F", "all face neighbours", GH_ParamAccess.tree);
            pManager.AddPointParameter("Vertex NFC Points", "C", "all face neighbour centers", GH_ParamAccess.tree);
            pManager.HideParameter(4);
            
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {

            GH_Structure<IGH_GeometricGoo> iMeshTree = new GH_Structure<IGH_GeometricGoo>();
            GH_Structure<GH_Integer> iVertexIndices = new GH_Structure<GH_Integer>();

            DA.GetDataTree<IGH_GeometricGoo>(0, out iMeshTree);
            DA.GetDataTree<GH_Integer>(1, out iVertexIndices);

            GH_Structure<IGH_GeometricGoo> oVertexTree = new GH_Structure<IGH_GeometricGoo>();
            GH_Structure<IGH_Goo> oVertexNVTree = new GH_Structure<IGH_Goo>();   // neighbouring vertices
            GH_Structure<IGH_Goo> oVertexNETree = new GH_Structure<IGH_Goo>();      // neighbouring edges
            GH_Structure<IGH_Goo> oVertexNFTree = new GH_Structure<IGH_Goo>();       // this was polyline 
            GH_Structure<IGH_Goo> oVertexNFCTree = new GH_Structure<IGH_Goo>();

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

                        for (int vi = 0; vi < iVertexIndices.PathCount; vi++)
                        {
                            int id;
                            GH_Path pathVertices = iVertexIndices.get_Path(vi);
                            if (path != pathVertices)
                                continue;
                            for (int vj = 0; vj < iVertexIndices.Branches[vi].Count; vj++)
                            {
                                iVertexIndices.Branches[vi][vj].CastTo<int>(out id);
                                // getting neighbouring vertices 
                                oVertexTree.Append(GH_Convert.ToGeometricGoo(pMesh.Vertices[id].ToPoint3d()), path);

                                List<int> newPath = new List<int>();
                                for (int p = 0; p < path.Indices.Length; p++)
                                    newPath.Add(path.Indices[p]);
                                newPath.Add(vj);
                                GH_Path vertexPath = new GH_Path(newPath.ToArray());

                                // getting neighbouring vertices 
                                int[] vertexNV = pMesh.Vertices.GetVertexNeighbours(id);
                                foreach (int v in vertexNV)
                                    if (v != -1)
                                        oVertexNVTree.Append(GH_Convert.ToGeometricGoo(pMesh.Vertices[v].ToPoint3d()), vertexPath);


                                // getting neighbouring edges
                                int[] vertexNE = pMesh.Vertices.GetHalfedges(id);
                                foreach (int e in vertexNE)
                                    if (e != -1)
                                    {
                                        int[] edgeVerticesAB = pMesh.Halfedges.GetVertices(e);
                                        oVertexNETree.Append(
                                            GH_Convert.ToGeometricGoo(
                                                new Line(
                                                    pMesh.Vertices[edgeVerticesAB[0]].ToPoint3d(),
                                                    pMesh.Vertices[edgeVerticesAB[1]].ToPoint3d()
                                                    )), 
                                                vertexPath);
                                    }

                                // getting neighbouring faces 
                                int[] vertexNFC = pMesh.Vertices.GetVertexFaces(id);
                                foreach (int fc in vertexNFC)
                                    if (fc != -1)
                                    {
                                        int[] vertexFace = pMesh.Faces.GetFaceVertices(fc);
                                        Polyline facePoly = new Polyline();
                                        for (int f = 0; f <= vertexFace.Length; f++)
                                        {
                                            var v = pMesh.Vertices[vertexFace[f % vertexFace.Length]];
                                            facePoly.Add(v.X, v.Y, v.Z);
                                        }
                                        oVertexNFTree.Append(GH_Convert.ToGeometricGoo(facePoly), vertexPath);
                                        oVertexNFCTree.Append(GH_Convert.ToGeometricGoo(pMesh.Faces.GetFaceCenter(fc).ToPoint3d()), vertexPath);
                                    }

                            }
                        }
                    }
                }
            }

            DA.SetDataTree(0, oVertexTree);
            DA.SetDataTree(1, oVertexNVTree);
            DA.SetDataTree(2, oVertexNETree);
            DA.SetDataTree(3, oVertexNFTree);
            DA.SetDataTree(4, oVertexNFCTree);

        }


        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.secondary; }
        }


        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.VertexNeighbours;
            }
        }


        public override Guid ComponentGuid
        {
            get { return new Guid("{fe0c0e49-0cda-474b-aa53-2deef87544d4}"); }
        }
    }
}