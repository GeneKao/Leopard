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
    public class GHC_Edges : GH_Component
    {

        public GHC_Edges()
            : base("Leopard's Edges", "L Edges",
                "Edges",
                "Leopard", "Topology")
        {
        }

        
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Mesh", "M", "input mesh", GH_ParamAccess.tree);
        }

        
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Edges", "E", "output Edges", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Naked Edges", "N", "Naked Edges", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Naked Edges Indices", "NI", "Naked Edges Indices", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Closed Edges", "C", "Closed Edges", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Closed Edges Indices", "CI", "Closed Edges Indices", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {

            GH_Structure<IGH_GeometricGoo> iMeshTree = new GH_Structure<IGH_GeometricGoo>();

            DA.GetDataTree<IGH_GeometricGoo>(0, out iMeshTree);

            GH_Structure<IGH_Goo> oEdgesTree = new GH_Structure<IGH_Goo>();
            GH_Structure<IGH_Goo> oNakedEdgesTree = new GH_Structure<IGH_Goo>();
            GH_Structure<GH_Integer> oNakedIndicesTree = new GH_Structure<GH_Integer>();
            GH_Structure<IGH_Goo> oClosedEdgesTree = new GH_Structure<IGH_Goo>();
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


                        for (int e = 0; e < pMesh.Halfedges.Count; e++)
                        {
                            if (pMesh.Halfedges.GetPairHalfedge(e) > e) continue;

                            int[] vts = pMesh.Halfedges.GetVertices(e);
                            oEdgesTree.Append(
                                GH_Convert.ToGeometricGoo(new LineCurve(
                                    pMesh.Vertices[vts[0]].ToPoint3d(),
                                    pMesh.Vertices[vts[1]].ToPoint3d())), path);

                            if (pMesh.Halfedges.IsBoundary(e))
                            {
                                oNakedEdgesTree.Append(
                                    GH_Convert.ToGeometricGoo(new LineCurve(
                                    pMesh.Vertices[vts[0]].ToPoint3d(),
                                    pMesh.Vertices[vts[1]].ToPoint3d())), path);

                                oNakedIndicesTree.Append(new GH_Integer(e/2), path);
                            }
                            else
                            {
                                oClosedEdgesTree.Append(
                                    GH_Convert.ToGeometricGoo(new LineCurve(
                                    pMesh.Vertices[vts[0]].ToPoint3d(),
                                    pMesh.Vertices[vts[1]].ToPoint3d())), path);

                                oClosedIndicesTree.Append(new GH_Integer(e/2), path);
                            }
                        }

                    }
                }
            }

            DA.SetDataTree(0, oEdgesTree);
            DA.SetDataTree(1, oNakedEdgesTree);
            DA.SetDataTree(2, oNakedIndicesTree);
            DA.SetDataTree(3, oClosedEdgesTree);
            DA.SetDataTree(4, oClosedIndicesTree);

            
        }


        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }


        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Properties.Resources.Edge;
            }
        }

        
        public override Guid ComponentGuid
        {
            get { return new Guid("{bbba5ee1-0f71-4db6-ab74-19415f6cb6d7}"); }
        }
    }
}