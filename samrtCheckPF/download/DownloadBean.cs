using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace samrtCheckPF.download
{
    class DownloadBean
    {
        public string url { set; get;}
        public string saveFolder { set; get;}
        public string fileName { set; get; }
        public string MD5{get;set;}
    }
}
