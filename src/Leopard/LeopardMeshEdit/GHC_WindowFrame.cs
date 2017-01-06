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
    public class GHC_WindowFrame : GH_Component
    {


        public GHC_WindowFrame()
            : base("Leopard's WindowFrame", "L WindowFrame",
                "WindowFrame",
                "Leopard", "Editing")
        {
        }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Mesh", "M", "Mesh to edit", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Face", "F", "Face Indices", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Thickness Ratio", "R", "Thickness ration of frames", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Extrudes Length", "L", "Extrude Length", GH_ParamAccess.tree);
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Edited Mesh", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<IGH_GeometricGoo> iMeshTree = new GH_Structure<IGH_GeometricGoo>();
            GH_Structure<GH_Integer> iFaceIndices = new GH_Structure<GH_Integer>();
            GH_Structure<GH_Number> iThicknesses = new GH_Structure<GH_Number>();
            GH_Structure<GH_Number> iExtrudes = new GH_Structure<GH_Number>();

            DA.GetDataTree<IGH_GeometricGoo>(0, out iMeshTree);
            DA.GetDataTree<GH_Integer>(1, out iFaceIndices);
            DA.GetDataTree<GH_Number>(2, out iThicknesses);
            DA.GetDataTree<GH_Number>(3, out iExtrudes);

            GH_Structure<IGH_GeometricGoo> oNewMeshTree = new GH_Structure<IGH_GeometricGoo>();


            for (int i = 0; i < iMeshTree.PathCount; i++)
            {
                GH_Path pathMesh = iMeshTree.get_Path(i);

                for (int j = 0; j < iMeshTree.Branches[i].Count; j++)
                {
                    Mesh mesh;
                    if (!iMeshTree.Branches[i][j].CastTo<Mesh>(out mesh))
                        continue;
                    else
                    {
                        PlanktonMesh pMesh = mesh.ToPlanktonMesh();
                        PlanktonMesh pSMesh = new PlanktonMesh();
                        List<Point3d[]> faceOffset = new List<Point3d[]>();

                        List<int> facesIDs = new List<int>();
                        List<double> thicks = new List<double>();
                        List<double> offsets = new List<double>();

                        #region value initial
                        for (int vi = 0; vi < iFaceIndices.PathCount; vi++)
                        {
                            if (pathMesh != iFaceIndices.get_Path(vi))
                                continue;
                            else
                                foreach (var sv in iFaceIndices[vi].ToList())
                                {
                                    int number;
                                    sv.CastTo<int>(out number);
                                    facesIDs.Add(number);

                                }
                        }

                        for (int vi = 0; vi < iThicknesses.PathCount; vi++)
                        {
                            if (pathMesh != iThicknesses.get_Path(vi))
                                continue;
                            else
                                foreach (var sv in iThicknesses[vi].ToList())
                                {
                                    double number;
                                    sv.CastTo<double>(out number);
                                    thicks.Add(number);
                                }
                        }

                        for (int vi = 0; vi < iExtrudes.PathCount; vi++)
                        {
                            if (pathMesh != iExtrudes.get_Path(vi))
                                continue;
                            else
                                foreach (var sv in iExtrudes[vi].ToList())
                                {
                                    double number;
                                    sv.CastTo<double>(out number);
                                    offsets.Add(number);
                                }
                        }
                        #endregion

                        #region Create windows vertices
                        for (int k = 0; k < pMesh.Faces.Count; k++)
                        {
                            if (!facesIDs.Contains(k))
                                continue;

                            int id = facesIDs.IndexOf(k);

                            Point3d center = pMesh.Faces.GetFaceCenter(k).ToPoint3d();
                            int[] fVertices = pMesh.Faces.GetFaceVertices(k);
                            Point3d[] centerVertices = new Point3d[fVertices.Length];

                            //Vector3d normal = pMesh.Faces.GetFaceNormal(k).ToVector3d();
                            //!!!!!!!!!!!!!!!!! plankton vertices normal have bugs!!!!!!!

                            for (int fv = 0; fv < fVertices.Length; fv++)
                            {
                                Point3d v = pMesh.Vertices[fVertices[fv]].ToPoint3d();
                                Vector3d vec = center - v;

                                Point3d v2 = new Point3d();
                                if (fv == fVertices.Length - 1)
                                    v2 = pMesh.Vertices[fVertices[0]].ToPoint3d();
                                else
                                    v2 = pMesh.Vertices[fVertices[fv + 1]].ToPoint3d();

                                int thickID = (id >= thicks.Count) ? thicks.Count - 1 : id;
                                int offsetID = (id >= offsets.Count) ? offsets.Count - 1 : id;

                                Vector3d normal = Vector3d.CrossProduct(vec, v - v2);
                                normal.Unitize();

                                centerVertices[fv] = v + vec * thicks[thickID] + normal * offsets[offsetID];
                            }
                            faceOffset.Add(centerVertices);
                        }
                        #endregion

                        // add vertices 
                        #region add Vertices
                        foreach (PlanktonVertex v in pMesh.Vertices)
                            pSMesh.Vertices.Add(v.ToPoint3d());
                        #endregion

                        #region add frame vertices
                        // add offset vertices
                        foreach (Point3d[] pts in faceOffset)
                            foreach (Point3d p in pts)
                                pSMesh.Vertices.Add(p);
                        #endregion

                        // add mesh
                        int vc = 0; // vertice counter
                        for (int k = 0; k < pMesh.Faces.Count; k++)
                        {
                            if (!facesIDs.Contains(k))
                            {
                                int[] fVertices = pMesh.Faces.GetFaceVertices(k);

                                if (fVertices.Length == 3)
                                    pSMesh.Faces.AddFace(fVertices[0], fVertices[1], fVertices[2]);
                                else
                                    pSMesh.Faces.AddFace(fVertices[0], fVertices[1], fVertices[2], fVertices[3]);
                            }
                            else
                            {
                                int[] fVertices = pMesh.Faces.GetFaceVertices(k);

                                if (fVertices.Length == 3)
                                {
                                    pSMesh.Faces.AddFace(fVertices[0], fVertices[1], pMesh.Vertices.Count + vc + 1, pMesh.Vertices.Count + vc + 0);
                                    pSMesh.Faces.AddFace(fVertices[1], fVertices[2], pMesh.Vertices.Count + vc + 2, pMesh.Vertices.Count + vc + 1);
                                    pSMesh.Faces.AddFace(fVertices[2], fVertices[0], pMesh.Vertices.Count + vc + 0, pMesh.Vertices.Count + vc + 2);

                                    if (m_cap)
                                        pSMesh.Faces.AddFace(pMesh.Vertices.Count + vc + 0, pMesh.Vertices.Count + vc + 1, pMesh.Vertices.Count + vc + 2);

                                    vc += 3;
                                }
                                else
                                {
                                    pSMesh.Faces.AddFace(fVertices[0], fVertices[1], pMesh.Vertices.Count + vc + 1, pMesh.Vertices.Count + vc + 0);
                                    pSMesh.Faces.AddFace(fVertices[1], fVertices[2], pMesh.Vertices.Count + vc + 2, pMesh.Vertices.Count + vc + 1);
                                    pSMesh.Faces.AddFace(fVertices[2], fVertices[3], pMesh.Vertices.Count + vc + 3, pMesh.Vertices.Count + vc + 2);
                                    pSMesh.Faces.AddFace(fVertices[3], fVertices[0], pMesh.Vertices.Count + vc + 0, pMesh.Vertices.Count + vc + 3);

                                    if (m_cap)
                                        pSMesh.Faces.AddFace(pMesh.Vertices.Count + vc + 0, pMesh.Vertices.Count + vc + 1, pMesh.Vertices.Count + vc + 2, pMesh.Vertices.Count + vc + 3);

                                    vc += 4;
                                }
                            }
                        }


                        oNewMeshTree.Append(
                                GH_Convert.ToGeometricGoo(pSMesh.ToRhinoMesh()),
                                pathMesh);

                    }
                }
            }



            DA.SetDataTree(0, oNewMeshTree);

        }

        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            ToolStripMenuItem item = Menu_AppendItem(menu, "Cap", Menu_CapClicked, true, Cap);
            // Specifically assign a tooltip text to the menu item.
            item.ToolTipText = "When checked, cap the opening of the frame";
            //base.AppendAdditionalMenuItems(menu);

        }

        private bool m_cap = false;
        public bool Cap
        {
            get { return m_cap; }
            set
            {
                m_cap = value;
                if ((m_cap))
                {
                    Message = "Cap";
                }
                else
                {
                    Message = "Open";
                }
            }
        }


        private void Menu_CapClicked(object sender, EventArgs e)
        {
            RecordUndoEvent("Cap");
            Cap = !Cap;
            ExpireSolution(true);
        }

        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            // First add our own field.
            writer.SetBoolean("Cap", Cap);
            // Then call the base class implementation.
            return base.Write(writer);
        }
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            // First read our own field.
            Cap = reader.GetBoolean("Cap");
            // Then call the base class implementation.
            return base.Read(reader);
        }


        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.WindowFrame;
            }
        }


        public override Guid ComponentGuid
        {
            get { return new Guid("{97022386-3193-4f46-8a92-65f939d394b9}"); }
        }
    }
}