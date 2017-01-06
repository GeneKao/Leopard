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
    public class GHC_FaceNeighbours : GH_Component
    {

        public GHC_FaceNeighbours()
            : base("Leopard's FaceNeighbours", "L FaceNB",
                "getting face neighbours information",
                "Leopard", "Topology")
        {
        }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Mesh", "M", "input mesh", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Face", "F", "Face Indices", GH_ParamAccess.tree);
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("This Face", "T", "This Face Curve", GH_ParamAccess.tree);
            pManager.AddGenericParameter("Face Vertices", "V", "neighbour vertices", GH_ParamAccess.tree);
            pManager.AddGenericParameter("Face Neighbour Edges", "E", "neighbour edges", GH_ParamAccess.tree);
            pManager.AddGenericParameter("Face Neighbour Faces", "F", "Neighbuor Faces", GH_ParamAccess.tree);
        }



        protected override void SolveInstance(IGH_DataAccess DA)
        {

            GH_Structure<IGH_GeometricGoo> iMeshTree = new GH_Structure<IGH_GeometricGoo>();
            GH_Structure<GH_Integer> iFaceIndices = new GH_Structure<GH_Integer>();

            DA.GetDataTree<IGH_GeometricGoo>(0, out iMeshTree);
            DA.GetDataTree<GH_Integer>(1, out iFaceIndices);

            GH_Structure<IGH_GeometricGoo> oFaceTree = new GH_Structure<IGH_GeometricGoo>();
            GH_Structure<IGH_Goo> oFaceVerticesTree = new GH_Structure<IGH_Goo>();   
            GH_Structure<IGH_Goo> oFacesEdgesTree = new GH_Structure<IGH_Goo>();      
            GH_Structure<IGH_Goo> oVertexNFTree = new GH_Structure<IGH_Goo>();

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
                        Polyline[] polyFaces = pMesh.ToPolylines();

                        for (int vi = 0; vi < iFaceIndices.PathCount; vi++)
                        {
                            int id;
                            GH_Path pathFaces = iFaceIndices.get_Path(vi);
                            if (path != pathFaces)
                                continue;
                            for (int vj = 0; vj < iFaceIndices.Branches[vi].Count; vj++)
                            {
                                iFaceIndices.Branches[vi][vj].CastTo<int>(out id);
                                // getting neighbouring vertices 
                                oFaceTree.Append(
                                    GH_Convert.ToGeometricGoo(polyFaces[id]), 
                                    path);
                                
                                List<int> newPath = new List<int>();
                                for (int p = 0; p < path.Indices.Length; p++)
                                    newPath.Add(path.Indices[p]);
                                newPath.Add(vj);
                                GH_Path vertexPath = new GH_Path(newPath.ToArray());

                                
                                //vertices on face
                                foreach (int fv in pMesh.Faces.GetFaceVertices(id))
                                    oFaceVerticesTree.Append(GH_Convert.ToGeometricGoo(pMesh.Vertices[fv].ToPoint3d()), vertexPath);
                                
                                //edges on face and neighbour face of face
                                foreach (int fe in pMesh.Faces.GetHalfedges(id))
                                {
                                    if (pMesh.Halfedges.IsBoundary(fe)) continue;

                                    int fae = pMesh.Halfedges.GetPairHalfedge(fe);

                                    if (fae != -1)
                                    {
                                        int[] vts = pMesh.Halfedges.GetVertices(fae);
                                        oFacesEdgesTree.Append(GH_Convert.ToGeometricGoo(
                                            new LineCurve(pMesh.Vertices[vts[0]].ToPoint3d(), pMesh.Vertices[vts[1]].ToPoint3d())),
                                            path);


                                        int f = pMesh.Halfedges[fae].AdjacentFace;
                                        Polyline facePoly = Leopard.MeshEdit.getFacePolyline(pMesh, f);
                                        oVertexNFTree.Append(GH_Convert.ToGeometricGoo(facePoly), vertexPath);
                                    }
                                    
                                }

                            }
                        }
                    }
                }
            }

            DA.SetDataTree(0, oFaceTree);
            DA.SetDataTree(1, oFaceVerticesTree);
            DA.SetDataTree(2, oFacesEdgesTree);
            DA.SetDataTree(3, oVertexNFTree);



        }


        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.secondary; }
        }


        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.FaceNeighbours;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{b6e7964f-9b2f-4f83-84e1-d3f0fa7443c3}"); }
        }
    }
}