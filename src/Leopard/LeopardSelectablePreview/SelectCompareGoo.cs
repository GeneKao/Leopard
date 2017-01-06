using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace Leopard
{
    class SelectCompareGoo : IComparer<IGH_Goo>
    {
        
        public List<IGH_Goo> gooList { get; set; }
        public List<GH_Integer> indexList { get; set; }

        public SelectCompareGoo(List<IGH_Goo> gooList, List<GH_Integer> indexList)
        {
            this.gooList = gooList;
            this.indexList = indexList;
        }

        public int Compare(IGH_Goo g0, IGH_Goo g1)
        {
            int i0 = gooList.IndexOf(g0);
            int i1 = gooList.IndexOf(g1);
            return indexList[i0].QC_CompareTo(indexList[i1]);
        }

        
    }
}
