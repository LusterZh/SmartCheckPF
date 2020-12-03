using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace samrtCheckPF.alg
{
    class DjRoiRectBean
    {
        public int left;
        public int top;
        public int right;
        public int bottom;

        public DjRoiRectBean(int left, int top,int right, int bottom)
        {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
        }
    }
}
