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
    public class GHC_SelectEdges : SelectComponent
    {

        public GHC_SelectEdges()
          : base("Leopard's SelectMeshEdges", "L SelEdges",
              "Select Mesh Edges",
              "Leopard", "Select")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Mesh", "M", "Meshes", GH_ParamAccess.tree);
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geometry", "G", "output", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Index", "I", "output", GH_ParamAccess.tree);
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {

            //bool inputLock = false;
            string key = "Leopard(" + this.InstanceGuid + ")";

            GH_Structure<IGH_GeometricGoo> inGeoTree;
            if (!DA.GetDataTree<IGH_GeometricGoo>(0, out inGeoTree)) { return; }

            GH_Structure<IGH_GeometricGoo> edgesGoo = new GH_Structure<IGH_GeometricGoo>();

            for (int i = 0; i < inGeoTree.PathCount; i++)
                for (int j = 0; j < inGeoTree.Branches[i].Count; j++)
                {
                    Mesh mesh;
                    if (!inGeoTree.Branches[i][j].CastTo<Mesh>(out mesh))
                        continue;
                    else
                    {
                        GH_Path path = inGeoTree.get_Path(i);
                        List<int> newPath = new List<int>();
                        for (int p = 0; p < path.Indices.Length; p++)
                            newPath.Add(path.Indices[p]);

                        PlanktonMesh pMesh = mesh.ToPlanktonMesh();

                        List<Curve> edges = new List<Curve>();
                        for (int e = 0; e < pMesh.Halfedges.Count; e++)
                        {
                            if (pMesh.Halfedges.GetPairHalfedge(e) > e) continue;

                            int[] vts = pMesh.Halfedges.GetVertices(e);
                            edges.Add(
                                new LineCurve(
                                    pMesh.Vertices[vts[0]].ToPoint3d(),
                                    pMesh.Vertices[vts[1]].ToPoint3d()));
                        }

                        foreach (Curve e in edges)
                            if (inGeoTree.PathCount == 1)
                                edgesGoo.Append(GH_Convert.ToGeometricGoo(e), inGeoTree.get_Path(i));
                            else
                                edgesGoo.Append(GH_Convert.ToGeometricGoo(e), new GH_Path(newPath.ToArray()));
                    }
                }

            //ptsGoo.Simplify(GH_SimplificationMode.CollapseLeadingOverlaps);

            //clear all stored selection
            if (resetStoredPath) storedPath.Clear();

            //delete the preview baked objects
            if (!freezePreviewObjects) RemovePreviewObjects();

            //generate the preview baked objects
            if (!inputLock && (generatePreview || !freezePreviewObjects)) GeneratePreViewObjectsI(edgesGoo);// && !freezePreviewObjects)

            //happens when unlock
            //if (addSelection)
            //    SelectStoredPathObj(storedPath);


            GH_Structure<GH_String> pathTree = new GH_Structure<GH_String>(); //a tree that the data stored is its path
            for (int i = 0; i < edgesGoo.PathCount; i++)
            {
                string path = edgesGoo.Paths[i].ToString();
                for (int j = 0; j < edgesGoo.Branches[i].Count; j++)
                {
                    string str = path + "(" + j.ToString() + ")";
                    pathTree.Append(new GH_String(str));
                }
            }
            List<string> pathOrder = new List<string>();
            foreach (GH_String s in pathTree.AllData(false))
                pathOrder.Add(s.ToString());

            GH_Structure<GH_Integer> orderTree = new GH_Structure<GH_Integer>(); //a tree that the data is the order of each data, this tree is reference for sorting
            for (int i = 0; i < pathOrder.Count; i++)
            {
                string[] pathSeg;
                string indSeg;
                GH_Path.SplitPathLikeString(pathOrder[i], out pathSeg, out indSeg);
                int[] pInd = System.Array.ConvertAll(pathSeg, str => System.Convert.ToInt32(str));
                int index = System.Convert.ToInt32(indSeg);
                orderTree.Insert(new GH_Integer(i), new GH_Path(pInd), index);
            }

            GH_Structure<IGH_Goo> outGeoTree = new GH_Structure<IGH_Goo>();
            GH_Structure<IGH_Goo> outIndTree = new GH_Structure<IGH_Goo>();
            GH_Structure<GH_Integer> outOrderTree = new GH_Structure<GH_Integer>();

            for (int i = 0; i < storedPath.Count; i++)
            {

                string p = pathOrder[System.Convert.ToInt32(storedPath[i])];
                string[] pathSeg;
                string indSeg;

                if (GH_Path.SplitPathLikeString(p, out pathSeg, out indSeg))
                {
                    int[] pInd = System.Array.ConvertAll(pathSeg, str => System.Convert.ToInt32(str));
                    GH_Path path = new GH_Path(pInd);
                    int index = System.Convert.ToInt32(indSeg);

                    outGeoTree.Append((IGH_GeometricGoo)edgesGoo.get_Branch(path)[index], path);
                    outIndTree.Append(new GH_Integer(index), path);
                    outOrderTree.Append((GH_Integer)(orderTree.get_Branch(path)[index]), path);
                }
            }

            if (this.sortByIndex)
            {
                outGeoTree = SortTreeByIndex(outGeoTree, outOrderTree);
                outIndTree = SortTreeByIndex(outIndTree, outOrderTree);
            }
            DA.SetDataTree(0, outGeoTree);
            DA.SetDataTree(1, outIndTree);


        }


        public override void AppendAdditionalMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            ToolStripMenuItem sortItem = GH_DocumentObject.Menu_AppendItem(menu, "Toggle Sort By Select Order", new EventHandler(this.Menu_SortIndex), true);
        }

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }


        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.SelectMeshEdges;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{4166a2b4-837b-4720-a0c3-facc6baae616}"); }
        }
    }
}