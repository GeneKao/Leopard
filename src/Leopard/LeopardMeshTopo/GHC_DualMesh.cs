using System;
using System.Collections.Generic;
using System.Windows.Forms;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Rhino;
using Rhino.Geometry.Intersect;
using Rhino.Collections;

using Plankton;
using PlanktonGh;
using Utilities;
using System.Linq;

namespace Leopard
{
    public class GHC_DualMesh : GH_Component
    {

        public GHC_DualMesh()
            : base("Leopard's DualMesh", "L DualMesh",
                "Dual Mesh",
                "Leopard", "Topology")
        {
        }

        
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Mesh", "M", "input mesh", GH_ParamAccess.tree);
        }

        
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Dual Mesh", "D", "dual mesh as mesh", GH_ParamAccess.tree);
        }

        
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<IGH_GeometricGoo> iMeshTree = new GH_Structure<IGH_GeometricGoo>();

            DA.GetDataTree<IGH_GeometricGoo>(0, out iMeshTree);

            GH_Structure<IGH_GeometricGoo> oDualMeshTree = new GH_Structure<IGH_GeometricGoo>();
            GH_Structure<IGH_GeometricGoo> oDualMeshCurveTree = new GH_Structure<IGH_GeometricGoo>();


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

                        oDualMeshTree.Append(
                            GH_Convert.ToGeometricGoo(mesh.ToPlanktonMesh().Dual(0).ToRhinoMesh()),
                            path);
                        
                    }
                }
            }

            DA.SetDataTree(0, oDualMeshTree);
        }
        

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.tertiary; }
        }
        

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.DualMesh;
            }
        }

        
        public override Guid ComponentGuid
        {
            get { return new Guid("{feb186da-a8a3-49e1-ba7b-705410cfb3c0}"); }
        }
    }
}