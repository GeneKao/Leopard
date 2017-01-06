
using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;

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
    public abstract class TopoComponent : SelectComponent
    {
        // for selection 
        protected List<string> oDebugText;
        // for selection 
        protected List<string> storedPath;

        // setting variable
        public byte Selected { get; set; }

        public TopoComponent(string Name, string Nickname, string Description, string Category, string Subcategory)
            : base(Name, Nickname, Description, Category, Subcategory)
        {
            storedPath = new List<string>();
            oDebugText = new List<string>();
        }

        


    }
}
