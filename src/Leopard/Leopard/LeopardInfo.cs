using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace Leopard
{
    public class LeopardInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "Leopard version 0a1";
            }
        }
        public override Bitmap Icon
        {
            get
            {

                //Return a 24x24 pixel bitmap to represent this GHA library.
                return null;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "Mesh Editing using Plankton Half Edge Mesh";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("f632dc6e-4449-4f2c-b908-c1708d41494b");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "Gene Ting-Chun Kao & Alan Song-Ching Tai";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "kao.gene@gmail.com";
            }
        }
    }
}
