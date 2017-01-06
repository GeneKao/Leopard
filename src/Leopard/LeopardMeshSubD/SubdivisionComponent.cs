using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;

using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;

using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using Rhino.DocObjects;

using Plankton;
using PlanktonGh;

namespace Leopard
{
    public abstract class SubdivisionComponent : LeopardComponent, IGH_VariableParameterComponent
    {

        public SubdivisionComponent(string Name, string Nickname, string Description, string Category, string Subcategory)
            : base(Name, Nickname, Description, Category, Subcategory)
        {

        }

        protected List<int> iFixEdges = new List<int>();

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Mesh", "M", "input mesh to subdivide", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Level", "L", "level to subdivide", GH_ParamAccess.tree);
            _mode = FoldMode.SimpleSubdivision;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Subdivided Mesh", "SM", "output subdivided mesh", GH_ParamAccess.tree);
        }

        #region menu override
        protected enum FoldMode
        {
            SimpleSubdivision,
            Vertices,
            Edges,
            Faces,
            VerticesAndEdges,
            VerticesAndFaces,
            EdgesAndFace,
            AllSelect
        }

        protected FoldMode _mode = FoldMode.SimpleSubdivision;

        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "Simple Subdivision", SimpleSubClicked, true, _mode == FoldMode.SimpleSubdivision);
            Menu_AppendItem(menu, "Vertices", VerticeClicked, true, _mode == FoldMode.Vertices || _mode == FoldMode.VerticesAndEdges || _mode == FoldMode.VerticesAndFaces || _mode == FoldMode.AllSelect);
            Menu_AppendItem(menu, "Edges", EdgeClicked, true, _mode == FoldMode.Edges || _mode == FoldMode.VerticesAndEdges || _mode == FoldMode.EdgesAndFace || _mode == FoldMode.AllSelect);
            Menu_AppendItem(menu, "Faces", FaceClicked, true, _mode == FoldMode.Faces || _mode == FoldMode.VerticesAndFaces || _mode == FoldMode.EdgesAndFace || _mode == FoldMode.AllSelect);
        }

        private void SimpleSubClicked(object sender, EventArgs e)
        {
            if (_mode == FoldMode.SimpleSubdivision)
                return;

            RecordUndoEvent("Simple Parameters");
            _mode = FoldMode.SimpleSubdivision;
            Message = "Simple Mode";

            while (Params.Input.Count > 2)
                Params.UnregisterInputParameter(Params.Input[2], true);

            (this as IGH_VariableParameterComponent).VariableParameterMaintenance();
            Params.OnParametersChanged();
            ExpireSolution(true);
        }
        private void VerticeClicked(object sender, EventArgs e)
        {
            switch (_mode)
            {
                case FoldMode.Vertices:
                    RecordUndoEvent("Simple Parameters");
                    _mode = FoldMode.SimpleSubdivision;

                    while (Params.Input.Count > 2)
                        Params.UnregisterInputParameter(Params.Input[2], true);
                    break;

                case FoldMode.SimpleSubdivision:
                case FoldMode.VerticesAndEdges:
                case FoldMode.VerticesAndFaces:
                    RecordUndoEvent("Vertices Parameters");
                    if (_mode == FoldMode.SimpleSubdivision)
                        _mode = FoldMode.Vertices;
                    if (_mode == FoldMode.VerticesAndEdges)
                        _mode = FoldMode.Edges;
                    if (_mode == FoldMode.VerticesAndFaces)
                        _mode = FoldMode.Faces;

                    while (Params.Input.Count < 3)
                        Params.RegisterInputParam(new Param_Integer());

                    while (Params.Input.Count > 3)
                        Params.UnregisterInputParameter(Params.Input[3], true);
                    break;

                case FoldMode.Edges:
                case FoldMode.Faces:
                case FoldMode.AllSelect:
                    RecordUndoEvent("Vertices Parameters"); // should add this to each mode
                    if (_mode == FoldMode.Edges)
                        _mode = FoldMode.VerticesAndEdges;
                    if (_mode == FoldMode.Faces)
                        _mode = FoldMode.VerticesAndFaces;
                    if (_mode == FoldMode.AllSelect)
                        _mode = FoldMode.EdgesAndFace;

                    while (Params.Input.Count < 4)
                        Params.RegisterInputParam(new Param_Integer());

                    while (Params.Input.Count > 4)
                        Params.UnregisterInputParameter(Params.Input[4], true);
                    break;

                case FoldMode.EdgesAndFace:   // all selection parameters
                    RecordUndoEvent("All Parameters");
                    _mode = FoldMode.AllSelect;
                    while (Params.Input.Count < 5)
                        Params.RegisterInputParam(new Param_Integer());

                    while (Params.Input.Count > 5)
                        Params.UnregisterInputParameter(Params.Input[5], true);

                    break;

            }

            (this as IGH_VariableParameterComponent).VariableParameterMaintenance();
            Params.OnParametersChanged();
            ExpireSolution(true);
        }
        private void EdgeClicked(object sender, EventArgs e)
        {
            switch (_mode)
            {
                case FoldMode.Edges:
                    RecordUndoEvent("Simple Parameters");
                    _mode = FoldMode.SimpleSubdivision;

                    while (Params.Input.Count > 2)
                        Params.UnregisterInputParameter(Params.Input[2], true);
                    break;

                case FoldMode.SimpleSubdivision:
                case FoldMode.VerticesAndEdges:
                case FoldMode.EdgesAndFace:
                    RecordUndoEvent("Edges Parameters");
                    if (_mode == FoldMode.SimpleSubdivision)
                        _mode = FoldMode.Edges;
                    if (_mode == FoldMode.VerticesAndEdges)
                        _mode = FoldMode.Vertices;
                    if (_mode == FoldMode.EdgesAndFace)
                        _mode = FoldMode.Faces;

                    while (Params.Input.Count < 3)
                        Params.RegisterInputParam(new Param_Integer());

                    while (Params.Input.Count > 3)
                        Params.UnregisterInputParameter(Params.Input[3], true);
                    break;

                case FoldMode.Vertices:
                case FoldMode.Faces:
                case FoldMode.AllSelect:
                    RecordUndoEvent("Edges Parameters");
                    if (_mode == FoldMode.Vertices)
                        _mode = FoldMode.VerticesAndEdges;
                    if (_mode == FoldMode.Faces)
                        _mode = FoldMode.EdgesAndFace;
                    if (_mode == FoldMode.AllSelect)
                        _mode = FoldMode.VerticesAndFaces;

                    while (Params.Input.Count < 4)
                        Params.RegisterInputParam(new Param_Integer());

                    while (Params.Input.Count > 4)
                        Params.UnregisterInputParameter(Params.Input[4], true);
                    break;

                case FoldMode.VerticesAndFaces:    // all selection parameters
                    RecordUndoEvent("All Parameters");
                    _mode = FoldMode.AllSelect;
                    while (Params.Input.Count < 5)
                        Params.RegisterInputParam(new Param_Integer());

                    while (Params.Input.Count > 5)
                        Params.UnregisterInputParameter(Params.Input[5], true);
                    break;
                
            }

            (this as IGH_VariableParameterComponent).VariableParameterMaintenance();
            Params.OnParametersChanged();
            ExpireSolution(true);
        }

        private void FaceClicked(object sender, EventArgs e)
        {
            switch (_mode)
            {
                case FoldMode.Faces:
                    RecordUndoEvent("Simple Parameters");
                    _mode = FoldMode.SimpleSubdivision;

                    while (Params.Input.Count > 2)
                        Params.UnregisterInputParameter(Params.Input[2], true);
                    break;

                case FoldMode.SimpleSubdivision:
                case FoldMode.VerticesAndFaces:
                case FoldMode.EdgesAndFace:
                    RecordUndoEvent("Vertices Parameters");
                    if (_mode == FoldMode.SimpleSubdivision)
                        _mode = FoldMode.Faces;
                    if (_mode == FoldMode.VerticesAndFaces)
                        _mode = FoldMode.Vertices;
                    if (_mode == FoldMode.EdgesAndFace)
                        _mode = FoldMode.Edges;

                    while (Params.Input.Count < 3)
                        Params.RegisterInputParam(new Param_Integer());

                    while (Params.Input.Count > 3)
                        Params.UnregisterInputParameter(Params.Input[3], true);
                    break;

                case FoldMode.Vertices:
                case FoldMode.Edges:
                case FoldMode.AllSelect:
                    RecordUndoEvent("Vertices Parameters"); // should add this to each mode
                    if (_mode == FoldMode.Vertices)
                        _mode = FoldMode.VerticesAndFaces;
                    if (_mode == FoldMode.Edges)
                        _mode = FoldMode.EdgesAndFace;
                    if (_mode == FoldMode.AllSelect)
                        _mode = FoldMode.VerticesAndEdges;

                    while (Params.Input.Count < 4)
                        Params.RegisterInputParam(new Param_Integer());

                    while (Params.Input.Count > 4)
                        Params.UnregisterInputParameter(Params.Input[4], true);
                    break;

                case FoldMode.VerticesAndEdges:
                    RecordUndoEvent("All Parameters");
                    _mode = FoldMode.AllSelect;
                    while (Params.Input.Count < 5)
                        Params.RegisterInputParam(new Param_Integer());

                    while (Params.Input.Count > 5)
                        Params.UnregisterInputParameter(Params.Input[5], true);

                    break;

            }

            (this as IGH_VariableParameterComponent).VariableParameterMaintenance();
            Params.OnParametersChanged();
            ExpireSolution(true);
        }

        #endregion

        #region (de)serialization
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetInt32("Mode", (int)_mode);
            return base.Write(writer);
        }
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            _mode = (FoldMode)reader.GetInt32("Mode");
            return base.Read(reader);
        }
        #endregion


        #region IGH_VariableParameterComponent null implementation
        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index)
        {
            return false;
        }
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index)
        {
            return false;
        }
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index)
        {
            return null;
        }
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index)
        {
            return false;
        }
        void IGH_VariableParameterComponent.VariableParameterMaintenance()
        {
            switch (_mode)
            {
                case FoldMode.Vertices:

                    Params.Input[2].NickName = "V";
                    Params.Input[2].Name = "Vertices";
                    Params.Input[2].Description = "Vertices Indices";
                    Params.Input[2].Access = GH_ParamAccess.tree;
                    Params.Input[2].Optional = true;
                    Message = "Vertices";
                    break;

                case FoldMode.Edges:

                    Params.Input[2].NickName = "E";
                    Params.Input[2].Name = "Edges";
                    Params.Input[2].Description = "Edges Indices";
                    Params.Input[2].Access = GH_ParamAccess.tree;
                    Params.Input[2].Optional = true;

                    Message = "Edges";
                    break;

                case FoldMode.Faces:

                    Params.Input[2].NickName = "F";
                    Params.Input[2].Name = "Faces";
                    Params.Input[2].Description = "Faces Indices";
                    Params.Input[2].Access = GH_ParamAccess.tree;
                    Params.Input[2].Optional = true;

                    Message = "Faces";
                    break;

                case FoldMode.VerticesAndEdges:

                    Params.Input[2].NickName = "V";
                    Params.Input[2].Name = "Vertices";
                    Params.Input[2].Description = "Vertices Indices";
                    Params.Input[2].Access = GH_ParamAccess.tree;
                    Params.Input[2].Optional = true;

                    Params.Input[3].NickName = "E";
                    Params.Input[3].Name = "Edges";
                    Params.Input[3].Description = "Edges Indices";
                    Params.Input[3].Access = GH_ParamAccess.tree;
                    Params.Input[3].Optional = true;

                    Message = "Vertices & Edges";
                    break;

                case FoldMode.VerticesAndFaces:

                    Params.Input[2].NickName = "V";
                    Params.Input[2].Name = "Vertices";
                    Params.Input[2].Description = "Vertices Indices";
                    Params.Input[2].Access = GH_ParamAccess.tree;
                    Params.Input[2].Optional = true;

                    Params.Input[3].NickName = "F";
                    Params.Input[3].Name = "Faces";
                    Params.Input[3].Description = "Faces Indices";
                    Params.Input[3].Access = GH_ParamAccess.tree;
                    Params.Input[3].Optional = true;

                    Message = "Vertices & Faces";
                    break;

                case FoldMode.EdgesAndFace:

                    Params.Input[2].NickName = "E";
                    Params.Input[2].Name = "Edges";
                    Params.Input[2].Description = "Edges Indices";
                    Params.Input[2].Access = GH_ParamAccess.tree;
                    Params.Input[2].Optional = true;

                    Params.Input[3].NickName = "F";
                    Params.Input[3].Name = "Faces";
                    Params.Input[3].Description = "Faces Indices";
                    Params.Input[3].Access = GH_ParamAccess.tree;
                    Params.Input[3].Optional = true;

                    Message = "Edges & Face";
                    break;

                case FoldMode.AllSelect:

                    Params.Input[2].NickName = "V";
                    Params.Input[2].Name = "Vertices";
                    Params.Input[2].Description = "Vertices Indices";
                    Params.Input[2].Access = GH_ParamAccess.tree;
                    Params.Input[2].Optional = true;

                    Params.Input[3].NickName = "E";
                    Params.Input[3].Name = "Edges";
                    Params.Input[3].Description = "Edges Indices";
                    Params.Input[3].Access = GH_ParamAccess.tree;
                    Params.Input[3].Optional = true;

                    Params.Input[4].NickName = "F";
                    Params.Input[4].Name = "Faces";
                    Params.Input[4].Description = "Faces Indices";
                    Params.Input[4].Access = GH_ParamAccess.tree;
                    Params.Input[4].Optional = true;

                    Message = "AllSelect";
                    break;
            }



            
        }
        #endregion


    }
}
