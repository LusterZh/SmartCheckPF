using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace samrtCheckPF.json
{
    class GetIniRes
    {
        public int code { get; set; }
        public string msg { get; set; }
        public GetIniResBean data { get; set; }
    }

    public class GetIniResBean
    {
        public string line;
        public string pc_uuid;
        public string project;
        public string build;
        public string sku;
        public string phase;
        public string version;
    }


}
