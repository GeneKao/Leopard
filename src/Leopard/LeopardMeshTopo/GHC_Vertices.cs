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
    public class GHC_Vertices : GH_Component
    {
        
        public GHC_Vertices()
            : base("Leopard's Vertices", "L Vertices",
                "get vertices in plankton sequence",
                "Leopard", "Topology")
        {
        }

        
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Mesh", "M", "input mesh", GH_ParamAccess.tree);
        }

        
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Vertex Points", "V", "all vertices as list", GH_ParamAccess.tree);
            pManager.AddVectorParameter("Vector Normal", "D", "all vertices normal", GH_ParamAccess.tree);
            pManager.AddPointParameter("Vertex NakedPoints", "N", "all naked vertices as list", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Vertex NakedPoints Index", "NI", "all naked vertices as list", GH_ParamAccess.tree);
            pManager.AddPointParameter("Vertex ClosedPoints", "C", "all closed vertices as list", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Vertex ClosedPoints Index", "CI", "all closed vertices as list", GH_ParamAccess.tree);
        }
        

        protected override void SolveInstance(IGH_DataAccess DA)
        {

            GH_Structure<IGH_GeometricGoo> iMeshTree = new GH_Structure<IGH_GeometricGoo>();

            DA.GetDataTree<IGH_GeometricGoo>(0, out iMeshTree);

            GH_Structure<IGH_Goo> oVerticesTree = new GH_Structure<IGH_Goo>();
            GH_Structure<IGH_Goo> oNormalsTree = new GH_Structure<IGH_Goo>();
            GH_Structure<IGH_Goo> oNakedVerticesTree = new GH_Structure<IGH_Goo>();
            GH_Structure<GH_Integer> oNakedIndicesTree = new GH_Structure<GH_Integer>();
            GH_Structure<IGH_Goo> oClosedVerticesTree = new GH_Structure<IGH_Goo>();
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

                        PlanktonXYZ[] xyz = pMesh.Vertices.GetPositions();
                        PlanktonXYZ[] normals = pMesh.Vertices.GetNormals();

                        for (int v = 0; v < xyz.Length; v++)
                        {
                            oVerticesTree.Append(GH_Convert.ToGeometricGoo(xyz[v].ToPoint3d()), path);
                            oNormalsTree.Append(GH_Convert.ToGeometricGoo(normals[v].ToVector3f()), path);

                            if (pMesh.Vertices.IsBoundary(v))
                            {
                                oNakedVerticesTree.Append(GH_Convert.ToGeometricGoo(xyz[v].ToPoint3d()), path);
                                oNakedIndicesTree.Append(new GH_Integer(v), path);
                            }
                            else
                            {
                                oClosedVerticesTree.Append(GH_Convert.ToGeometricGoo(xyz[v].ToPoint3d()), path);
                                oClosedIndicesTree.Append(new GH_Integer(v), path);
                            }
                        }

                    }
                }
            }

            DA.SetDataTree(0, oVerticesTree);
            DA.SetDataTree(1, oNormalsTree);
            DA.SetDataTree(2, oNakedVerticesTree);
            DA.SetDataTree(3, oNakedIndicesTree);
            DA.SetDataTree(4, oClosedVerticesTree);
            DA.SetDataTree(5, oClosedIndicesTree);

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
                return Properties.Resources.Vertex; 
            }
        }

        
        public override Guid ComponentGuid
        {
            get { return new Guid("{f56977f2-a56d-4ca8-b5c2-fc682397cb99}"); }
        }
    }
}