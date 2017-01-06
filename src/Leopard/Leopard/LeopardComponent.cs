using System;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;


namespace Leopard
{
    public abstract class LeopardComponent : GH_Component
    {

        public LeopardComponent(string Name, string Nickname, string Description, string Category, string Subcategory)
            : base(Name, Nickname, Description, Category, Subcategory){}
        



    }
}