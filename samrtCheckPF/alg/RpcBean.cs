using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace samrtCheckPF.alg
{
    class RpcBean
    {
        public int id { get; set; }
        public string jsonrpc { get; set; }
        public string method { get; set; }
        [JsonProperty("params")]
        public object Params { get; set; }
    }
}
