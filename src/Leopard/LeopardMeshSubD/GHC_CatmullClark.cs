using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;

using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using Rhino.DocObjects;

using Plankton;
using PlanktonGh;
using System.Text;

namespace Leopard
{
    public class GHC_CatmullClark : SubdivisionComponent
    {

        // setting variable
        public byte EdgesSelected { get; set; }

        public GHC_CatmullClark()
            : base("Leopard's CatmullClark Subdivision", "L CatmullClark",
                "Catmull Clark Subdivision",
                "Leopard", "SubD")
        {
        }
        
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<IGH_GeometricGoo> iMeshTree = new GH_Structure<IGH_GeometricGoo>();
            GH_Structure<GH_Integer> ilevelsTree = new GH_Structure<GH_Integer>();

            DA.GetDataTree<IGH_GeometricGoo>(0, out iMeshTree);
            DA.GetDataTree<GH_Integer>(1, out ilevelsTree);

            GH_Structure<IGH_GeometricGoo> SubdivideMesh = new GH_Structure<IGH_GeometricGoo>();

            GH_Structure<GH_Integer> selectVertices = new GH_Structure<GH_Integer>();
            GH_Structure<GH_Integer> selectEdges = new GH_Structure<GH_Integer>();
            GH_Structure<GH_Integer> selectFaces = new GH_Structure<GH_Integer>();

            #region input parameters
            switch (_mode)
            {
                case FoldMode.SimpleSubdivision:
                    break;
                case FoldMode.Vertices:
                    DA.GetDataTree(2, out selectVertices);
                    break;
                case FoldMode.Edges:
                    DA.GetDataTree(2, out selectEdges);
                    break;
                case FoldMode.Faces:
                    DA.GetDataTree(2, out selectFaces);
                    break;
                case FoldMode.VerticesAndEdges:
                    DA.GetDataTree(2, out selectVertices);
                    DA.GetDataTree(3, out selectEdges);
                    break;
                case FoldMode.VerticesAndFaces:
                    DA.GetDataTree(2, out selectVertices);
                    DA.GetDataTree(3, out selectFaces);
                    break;
                case FoldMode.EdgesAndFace:
                    DA.GetDataTree(2, out selectEdges);
                    DA.GetDataTree(3, out selectFaces);
                    break;
                case FoldMode.AllSelect:
                    DA.GetDataTree(2, out selectVertices);
                    DA.GetDataTree(3, out selectEdges);
                    DA.GetDataTree(4, out selectFaces);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
            #endregion

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
                        List<int> verticesIndices = new List<int>();
                        List<int> edgesIndices = new List<int>();
                        List<int> facesIndices = new List<int>();

                        #region select vertices
                        for (int vi = 0; vi < selectVertices.PathCount; vi++)
                        {
                            if (path != selectVertices.get_Path(vi))
                                continue;
                            else
                                foreach (var sv in selectVertices[vi].ToList())
                                {
                                    int number;
                                    sv.CastTo<int>(out number);
                                    verticesIndices.Add(number);
                                }
                        }
                        #endregion
                        #region select edges
                        for (int vi = 0; vi < selectEdges.PathCount; vi++)
                        {
                            if (path != selectEdges.get_Path(vi))
                                continue;
                            else
                                foreach (var sv in selectEdges[vi].ToList())
                                {
                                    int number;
                                    sv.CastTo<int>(out number);
                                    edgesIndices.Add(number);
                                }
                        }
                        #endregion
                        #region select faces
                        for (int vi = 0; vi < selectFaces.PathCount; vi++)
                        {
                            if (path != selectFaces.get_Path(vi))
                                continue;
                            else
                                foreach (var sv in selectFaces[vi].ToList())
                                {
                                    int number;
                                    sv.CastTo<int>(out number);
                                    facesIndices.Add(number);
                                }
                        }
                        #endregion

                        #region level
                        for (int l = 0; l < ilevelsTree.PathCount; l++)
                        {
                            int level;
                            GH_Path pathLevel = ilevelsTree.get_Path(l);
                            if (ilevelsTree.Count() == 1)
                            {
                                ilevelsTree.Branches[l][0].CastTo<int>(out level);

                                SubdivideMesh.Append(
                                                GH_Convert.ToGeometricGoo(
                                                    mesh.ToPlanktonMesh().CatmullClark(
                                                        level,
                                                        verticesIndices,
                                                        edgesIndices,
                                                        facesIndices).ToRhinoMesh()), path);
                                break;
                            }
                            else if (path != pathLevel)
                                continue;
                            else
                                for (int vj = 0; vj < ilevelsTree.Branches[l].Count; vj++)
                                {
                                    ilevelsTree.Branches[l][vj].CastTo<int>(out level);

                                    SubdivideMesh.Append(
                                                GH_Convert.ToGeometricGoo(
                                                    mesh.ToPlanktonMesh().CatmullClark(
                                                        level, 
                                                        verticesIndices, 
                                                        edgesIndices, 
                                                        facesIndices).ToRhinoMesh()), path);


                                }
                        }


                        #endregion



                    }
                }
            }

            DA.SetDataTree(0, SubdivideMesh);

        }


        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }


        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.CatmullClark;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{195e1f5e-2ad6-4f17-9811-838feaaf0ec9}"); }
        }
        
    }




}