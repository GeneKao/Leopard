using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Leopard
{
    public class GHC_SelectGeometry : SelectComponent
    {

        public GHC_SelectGeometry()
          : base("Leopard's SelectGeometry", "LSelGeo",
              "Select Geometry, Selectable Preview",
              "Leopard", "Select")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geometry", "G", "Items to bake", GH_ParamAccess.tree);
            //pManager.AddBooleanParameter("Lock", "L", "Lock Selected Objects", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geometry", "G", "output", GH_ParamAccess.tree);
            pManager.AddTextParameter("Path", "P", "output", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Index", "I", "output", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Order", "O", "output", GH_ParamAccess.tree);
        }




        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //bool inputLock = false;
            string key = "Leopard(" + this.InstanceGuid + ")";
            GH_Structure<IGH_GeometricGoo> inGeoTree;
            if (!DA.GetDataTree<IGH_GeometricGoo>(0, out inGeoTree)) { return; }

            //clear all stored selection
            if (resetStoredPath) storedPath.Clear();

            //delete the preview baked objects
            if (!freezePreviewObjects) RemovePreviewObjects();

            //generate the preview baked objects
            if (!inputLock && (generatePreview || !freezePreviewObjects)) GeneratePreViewObjectsI(inGeoTree);// && !freezePreviewObjects)

            //happens when unlock
            //if (addSelection)
            //    SelectStoredPathObj(storedPath);

            GH_Structure<GH_String> pathTree = new GH_Structure<GH_String>(); //a tree that the data stored is its path
            for (int i = 0; i < inGeoTree.PathCount; i++)
            {
                string path = inGeoTree.Paths[i].ToString();
                for (int j = 0; j < inGeoTree.Branches[i].Count; j++)
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
            List<IGH_Goo> outGeoList = new List<IGH_Goo>();
            GH_Structure<IGH_Goo> outPathTree = new GH_Structure<IGH_Goo>();
            List<IGH_Goo> outPathList = new List<IGH_Goo>();
            GH_Structure<IGH_Goo> outIndTree = new GH_Structure<IGH_Goo>();
            List<IGH_Goo> outIndList = new List<IGH_Goo>();
            GH_Structure<GH_Integer> outOrderTree = new GH_Structure<GH_Integer>();
            List<GH_Integer> outOrderList = new List<GH_Integer>();

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
                    if (maintainPath)
                    {
                        outGeoTree.Append((IGH_GeometricGoo)inGeoTree.get_Branch(path)[index], path);
                        outPathTree.Append(new GH_String(path.ToString()), path);
                        outIndTree.Append(new GH_Integer(index), path);
                        outOrderTree.Append((GH_Integer)(orderTree.get_Branch(path)[index]), path);
                    }
                    else
                    {
                        outGeoList.Add((IGH_GeometricGoo)inGeoTree.get_Branch(path)[index]);
                        outPathList.Add(new GH_String(path.ToString()));
                        outIndList.Add(new GH_Integer(index));
                        outOrderList.Add((GH_Integer)orderTree.get_Branch(path)[index]);
                    }
                }
            }
            if (maintainPath)
            {
                if (this.sortByIndex)
                {
                    outGeoTree = SortTreeByIndex(outGeoTree, outOrderTree);
                    outIndTree = SortTreeByIndex(outIndTree, outOrderTree);
                }
                DA.SetDataTree(0, outGeoTree);
                DA.SetDataTree(1, outPathTree);
                DA.SetDataTree(2, outIndTree);
                DA.SetDataTree(3, outOrderTree);
            }
            else
            {
                if (this.sortByIndex)
                {
                    List<IGH_Goo> gooCopy = outGeoList;
                    outGeoList.Sort(new SelectCompareGoo(gooCopy, outOrderList));
                    gooCopy = outPathList;
                    outPathList.Sort(new SelectCompareGoo(gooCopy, outOrderList));
                    gooCopy = outIndList;
                    outIndList.Sort(new SelectCompareGoo(gooCopy, outOrderList));

                }
                DA.SetDataList(0, outGeoList);
                DA.SetDataList(1, outPathList);
                DA.SetDataList(2, outIndList);
                DA.SetDataTree(3, outOrderTree);
            }

        }

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.secondary; }
        }


        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.SelectGeometry;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{d083fcc1-6a12-4312-912b-95f444f91810}"); }
        }
    }
}