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

using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using Rhino.DocObjects;

using Plankton;
using PlanktonGh;

namespace Leopard
{
    public abstract class SelectComponent : LeopardComponent
    {


        public bool inputLock { get; set; }
        protected bool freezePreviewObjects { get; set; }
        protected bool resetStoredPath { get; set; }
        protected bool generatePreview { get; set; }
        protected List<string> storedPath = new List<string>();
        protected bool maintainPath = false;
        protected bool sortByIndex = false;
        public bool respondToSelection { get; set; }


        public SelectComponent(string Name, string Nickname, string Description, string Category, string Subcategory)
            : base(Name, Nickname, Description, Category, Subcategory)
        {
            inputLock = false;
            resetStoredPath = false;
            freezePreviewObjects = false;
            generatePreview = true;
            respondToSelection = true;
            this.Params.ParameterSourcesChanged += new GH_ComponentParamServer.ParameterSourcesChangedEventHandler(Params_ParameterSourcesChanged);
            this.ObjectChanged += new IGH_DocumentObject.ObjectChangedEventHandler(SelectablePreviewComponent_ObjectChanged);
            //this.AttributesChanged += new IGH_DocumentObject.AttributesChangedEventHandler(SelectablePreviewComponent_AttributesChanged);
            //this.PreviewExpired += new IGH_DocumentObject.PreviewExpiredEventHandler(SelectablePreviewComponent_PreviewExpired);
        }

        //remove when disabling the component
        void SelectablePreviewComponent_ObjectChanged(IGH_DocumentObject sender, GH_ObjectChangedEventArgs e)
        {
            UpdatePreview();
        }

        //remove preview when disconnect source
        public void Params_ParameterSourcesChanged(object sender, GH_ParamServerEventArgs e)
        {
            if (!inputLock)
            {
                if (e.Parameter.SourceCount == 0)
                    RemovePreviewObjects();
            }
        }

        public override void CreateAttributes()
        {
            m_attributes = new SelectAttributesButton(this);
        }

        public void ForceExpireSolution(bool freezePreview, bool resetPath, bool generatePreview = false)
        {
            this.generatePreview = generatePreview;
            freezePreviewObjects = freezePreview;
            resetStoredPath = resetPath;
            this.ExpireSolution(true);
            //RhinoApp.WriteLine("Forced SolutionExpired");
            freezePreviewObjects = false; //refresh all the time unless locked
            resetStoredPath = false; //only reset when need to: when it was locked and get unlock
            this.generatePreview = false;
        }

        public void SelectStoredPathObj()
        {
            string key = "Leopard(" + this.InstanceGuid + ")";
            Rhino.RhinoDoc.ActiveDoc.Objects.UnselectAll();
            List<Guid> id = new List<Guid>();
            foreach (string v in this.storedPath)
            {
                Rhino.DocObjects.RhinoObject[] obj = RhinoDoc.ActiveDoc.Objects.FindByUserString(key, v, true);
                //Rhino.RhinoDoc.ActiveDoc.Objects.Select(obj[0].Id, true, false);
                id.Add(obj[0].Id);
            }
            respondToSelection = false;
            Rhino.RhinoDoc.ActiveDoc.Objects.Select(id);  //this will trigger selection event multiple times!!! 
            respondToSelection = true;
            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
            //ForceExpireSolution(true, false);
        }

        //this only hide or show the geometry, faster than remove
        public void UpdatePreview()
        {
            this.respondToSelection = false;
            if (!inputLock)
            {
                GH_Document doc = this.OnPingDocument();
                if (this.Locked)
                    RemovePreviewObjects();
                else
                {
                    if (this.Hidden)
                        HidePreviewObjects();
                    else
                    {
                        if (!this.Hidden && doc.PreviewFilter == GH_PreviewFilter.None)
                            ShowPreviewObjects();
                        else if (doc.PreviewFilter == GH_PreviewFilter.Selected && this.Attributes.Selected)  //or overide the event in attribute?
                            ShowPreviewObjects();
                        else
                            HidePreviewObjects();
                    }
                }
            }
            this.respondToSelection = true;
        }

        public void HidePreviewObjects()
        {
            string key = "Leopard(" + this.InstanceGuid + ")";
            //filter by layer first may not be faster
            //Rhino.DocObjects.RhinoObject[] obj = RhinoDoc.ActiveDoc.Objects.FindByLayer("GH_Preview");
            Rhino.DocObjects.RhinoObject[] obj = RhinoDoc.ActiveDoc.Objects.FindByUserString(key, "*", true);
            for (int i = 0; i < obj.Length; i++)
                RhinoDoc.ActiveDoc.Objects.Hide(obj[i], true);
        }

        public void ShowPreviewObjects()
        {
            string key = "Leopard(" + this.InstanceGuid + ")";
            //filter by layer first may not be faster
            //Rhino.DocObjects.RhinoObject[] obj = RhinoDoc.ActiveDoc.Objects.FindByLayer("GH_Preview");
            Rhino.DocObjects.RhinoObject[] obj = RhinoDoc.ActiveDoc.Objects.FindByUserString(key, "*", true);
            for (int i = 0; i < obj.Length; i++)
                RhinoDoc.ActiveDoc.Objects.Show(obj[i], true);
            //SelectStoredPathObj();//restore selection
        }

        public void RemovePreviewObjects()
        {
            string key = "Leopard(" + this.InstanceGuid + ")";
            //filter by layer first may not be faster
            //Rhino.DocObjects.RhinoObject[] obj = RhinoDoc.ActiveDoc.Objects.FindByLayer("GH_Preview");
            Rhino.DocObjects.RhinoObject[] obj = RhinoDoc.ActiveDoc.Objects.FindByUserString(key, "*", true);
            for (int i = 0; i < obj.Length; i++)
                RhinoDoc.ActiveDoc.Objects.Delete(obj[i], true);
        }


        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            RhinoDoc.SelectObjects += new EventHandler<Rhino.DocObjects.RhinoObjectSelectionEventArgs>(SelectObjects);
            RhinoDoc.DeselectObjects += new EventHandler<Rhino.DocObjects.RhinoObjectSelectionEventArgs>(DeselectObjects);
            RhinoDoc.DeselectAllObjects += new EventHandler<Rhino.DocObjects.RhinoDeselectAllObjectsEventArgs>(DeselectAllObjects);
            document.EnabledChanged += new GH_Document.EnabledChangedEventHandler(EnabledChanged);//this event does not get triggerred after the new component is cretaed by copying 
            document.SettingsChanged += new GH_Document.SettingsChangedEventHandler(document_SettingsChanged);
        }

        public void document_SettingsChanged(object sender, GH_DocSettingsEventArgs e)
        {
            UpdatePreview();
        }

        public override void MovedBetweenDocuments(GH_Document oldDocument, GH_Document newDocument) //fix for copying and pasting component
        {
            base.MovedBetweenDocuments(oldDocument, newDocument);
            oldDocument.SettingsChanged -= new GH_Document.SettingsChangedEventHandler(document_SettingsChanged);
            oldDocument.EnabledChanged -= new GH_Document.EnabledChangedEventHandler(EnabledChanged);
            newDocument.EnabledChanged += new GH_Document.EnabledChangedEventHandler(EnabledChanged);
            newDocument.SettingsChanged += new GH_Document.SettingsChangedEventHandler(document_SettingsChanged);
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            base.RemovedFromDocument(document);
            RemovePreviewObjects();
            RhinoDoc.SelectObjects -= new EventHandler<Rhino.DocObjects.RhinoObjectSelectionEventArgs>(SelectObjects);
            RhinoDoc.DeselectObjects -= new EventHandler<Rhino.DocObjects.RhinoObjectSelectionEventArgs>(DeselectObjects);
            RhinoDoc.DeselectAllObjects -= new EventHandler<Rhino.DocObjects.RhinoDeselectAllObjectsEventArgs>(DeselectAllObjects);
            document.EnabledChanged -= new GH_Document.EnabledChangedEventHandler(EnabledChanged);
            document.SettingsChanged -= new GH_Document.SettingsChangedEventHandler(document_SettingsChanged);
        }

        public void EnabledChanged(object e, GH_DocEnabledEventArgs arg) //triggerred when hiding the GH document    
        {
            if (!arg.Enabled)
                RemovePreviewObjects();
            else
                ForceExpireSolution(false, false);

        }

        public void SelectObjects(Object e, Rhino.DocObjects.RhinoObjectSelectionEventArgs arg)
        {
            if (!inputLock && respondToSelection)
            {
                bool expire = false;
                string key = "Leopard(" + this.InstanceGuid + ")";

                //selectedObjects.Clear();
                Rhino.DocObjects.RhinoObject[] obj = arg.RhinoObjects;
                GH_Structure<IGH_GeometricGoo> tree = new GH_Structure<IGH_GeometricGoo>();
                foreach (Rhino.DocObjects.RhinoObject o in obj)
                {
                    string val = o.Attributes.GetUserString(key);
                    if (!string.IsNullOrEmpty(val))
                    {
                        storedPath.Add(val);
                        expire = true;
                    }
                }
                if (expire) //if storePath changes, update the output parameters
                    ForceExpireSolution(true, false);
            }

        }

        public void DeselectObjects(Object e, Rhino.DocObjects.RhinoObjectSelectionEventArgs arg)
        {
            if (!inputLock && respondToSelection)
            {
                bool expire = false;
                string key = "Leopard(" + this.InstanceGuid + ")";

                //selectedObjects.Clear();
                Rhino.DocObjects.RhinoObject[] obj = arg.RhinoObjects;
                GH_Structure<IGH_GeometricGoo> tree = new GH_Structure<IGH_GeometricGoo>();
                foreach (Rhino.DocObjects.RhinoObject o in obj)
                {
                    string val = o.Attributes.GetUserString(key);
                    if (!string.IsNullOrEmpty(val))
                    {
                        //RhinoApp.WriteLine(string.Format("{0}: {1}", key, val));
                        storedPath.Remove(val);
                        expire = true;
                    }
                }
                if (expire)
                    ForceExpireSolution(true, false);
            }
        }

        public void DeselectAllObjects(Object e, Rhino.DocObjects.RhinoDeselectAllObjectsEventArgs arg)
        {
            if (!inputLock)
                ForceExpireSolution(true, true);
        }

        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            string all = "";
            if (storedPath.Count > 0)
            {
                foreach (string s in storedPath)
                    all += s + "|";
                all = all.Remove(all.Length - 1);
            }
            writer.SetString("selectedobject", all);
            writer.SetBoolean("lock", this.inputLock);
            writer.SetBoolean("maintain", this.maintainPath);
            writer.SetBoolean("order", this.sortByIndex);
            return base.Write(writer);
        }

        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            string all = "";
            if (reader.TryGetString("selectedobject", ref all))
            {
                //storedPath = all;
                if (all.Length > 0)
                {
                    string[] seg = all.Split(new char[] { '|' });
                    storedPath = new List<string>(seg);
                }
            }
            bool readLock = false;
            bool readMaintain = false;
            bool readOrder = false;
            if (reader.TryGetBoolean("lock", ref readLock))
                inputLock = readLock;
            if (reader.TryGetBoolean("maintain", ref readMaintain))
                maintainPath = readMaintain;
            if (reader.TryGetBoolean("order", ref readOrder))
                sortByIndex = readMaintain;
            return base.Read(reader);
        }



        public override void AppendAdditionalMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            ToolStripMenuItem maintainItem = GH_DocumentObject.Menu_AppendItem(menu, "Toggle Maintain Path", new EventHandler(this.Menu_MaintainPath), true);
            //base.AppendAdditionalMenuItems(menu);
            ToolStripMenuItem sortItem = GH_DocumentObject.Menu_AppendItem(menu, "Toggle Sort By Tree Order", new EventHandler(this.Menu_SortIndex), true);
            base.AppendAdditionalMenuItems(menu);
        }

        public void Menu_MaintainPath(object s, EventArgs e)
        {
            this.maintainPath = !this.maintainPath;
            ForceExpireSolution(true, false);
        }

        public void Menu_SortIndex(object s, EventArgs e)
        {
            this.sortByIndex = !this.sortByIndex;
            ForceExpireSolution(true, false);
        }

        public GH_Structure<IGH_Goo> SortTreeByIndex(GH_Structure<IGH_Goo> gooTree, GH_Structure<GH_Integer> indTree)
        {
            GH_Structure<IGH_Goo> sorted = new GH_Structure<IGH_Goo>();
            foreach (GH_Path p in gooTree.Paths)
            {
                //feed a copy of the goo tree, because the tree oreder will not correspond to index when sorting starts
                List<IGH_Goo> goo = (List<IGH_Goo>)(gooTree.get_Branch(p));
                List<IGH_Goo> gooCopy = new List<IGH_Goo>(goo);
                List<GH_Integer> index = (List<GH_Integer>)(indTree.get_Branch(p));
                SelectCompareGoo cg = new SelectCompareGoo(gooCopy, index);
                goo.Sort(cg);
                sorted.AppendRange(goo, p);
            }
            return sorted;
        }


        public void GeneratePreViewObjectsI(GH_Structure<IGH_GeometricGoo> inGeoTree)
        {
            string key = "Leopard(" + this.InstanceGuid + ")";

            RhinoDoc.ActiveDoc.Layers.Add("Leopard_Preview", System.Drawing.Color.Maroon);
            int layer = RhinoDoc.ActiveDoc.Layers.Find("Leopard_Preview", true);

            Rhino.DocObjects.RhinoObject[] obj = RhinoDoc.ActiveDoc.Objects.FindByUserString(key, "*", true);

            if (obj.Length == 0) //if no preview item
            {
                int count = 0;
                foreach (IGH_GeometricGoo goo in inGeoTree.AllData(false))
                {
                    Rhino.DocObjects.ObjectAttributes att = new Rhino.DocObjects.ObjectAttributes();
                    att.SetUserString(key, count.ToString());
                    att.LayerIndex = layer;
                    count++;
                    if (goo is IGH_BakeAwareData)
                    {
                        IGH_BakeAwareData data = (IGH_BakeAwareData)goo;
                        Guid guid;
                        data.BakeGeometry(RhinoDoc.ActiveDoc, att, out guid);
                    }
                }
            }
        }


        public void GeneratePreViewMeshVertices(GH_Structure<IGH_GeometricGoo> inGeoTree)
        {
            string key = "Leopard(" + this.InstanceGuid + ")";

            RhinoDoc.ActiveDoc.Layers.Add("Leopard_Preview", System.Drawing.Color.Maroon);
            int layer = RhinoDoc.ActiveDoc.Layers.Find("Leopard_Preview", true);

            Rhino.DocObjects.RhinoObject[] obj = RhinoDoc.ActiveDoc.Objects.FindByUserString(key, "*", true);

            if (obj.Length == 0) //if no preview item
            {
                int count = 0;
                foreach (IGH_GeometricGoo goo in inGeoTree.AllData(false))
                {

                    PlanktonMesh pMesh = new PlanktonMesh();
                    Mesh mesh;
                    if (!goo.CastTo<Mesh>(out mesh))
                        RhinoApp.WriteLine("input invalid");

                    pMesh = mesh.ToPlanktonMesh();

                    PlanktonXYZ[] xyz = pMesh.Vertices.GetPositions();
                    List<Point3d> oVertices = new List<Point3d>();

                    for (int i = 0; i < xyz.Length; i++)
                        oVertices.Add(xyz[i].ToPoint3d());

                    count++;

                    int vCount = 0;

                    foreach (Point3d p in oVertices)
                    {
                        string keyV = "Leopard(" + this.InstanceGuid + vCount + ")";


                        Rhino.DocObjects.ObjectAttributes att = new Rhino.DocObjects.ObjectAttributes();
                        att.SetUserString(keyV, count.ToString());
                        att.LayerIndex = layer;

                        GH_Point point = new GH_Point(p);

                        if (goo is IGH_BakeAwareData)
                        {
                            IGH_BakeAwareData data = (IGH_BakeAwareData)point;
                            Guid guid;
                            data.BakeGeometry(RhinoDoc.ActiveDoc, att, out guid);
                        }

                        vCount++; 
                    }
                }
            }
        }
    }
}
