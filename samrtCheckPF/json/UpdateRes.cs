using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace samrtCheckPF.json
{
    class UpdateResBean
    {
        public int function_id { get; set; }
        public string function_name { get; set; }
        public string station { get; set; }
        //public string golden_sample { get; set; }
        //public string golden_sample_path { get; set; } //golden_sample的本地存放路径
        public string golden_sample_name { get; set; }
        public string golden_sample_id { get; set; } //golden_sample的本地存放路径
        //public float value { get; set; }
        public MyRect rect { get; set; }
        public string model_url { get; set; }
        public string model_local_path { get; set; } //model的本地存放路径
        public string result { get; set; }
        public string model_md5 { get;set;}
    }

    class MyRect
    {
        public int left { get; set; }
        public int top { get; set; }
        public int right { get; set; }
        public int bottom { get; set; }
    }

    class UpdateRes
    {
        public int code { get; set; }
        public string msg { get; set; }
        public List<ConfigFileRes> materials { get; set; }  //新增料号配置文件
        public List<UpdateResBean> data { get; set; }
        public string hParam { get; set; }
        public string modelParam { get; set; }
    }

    class Registration
    {
        public int scale { get; set; }
        public float delta { get; set; }
        public int[] region { get; set; }
    }

    class ConfigFileRes
    {
        public string material_num { get; set; }
        public int[] function_ids { get; set; }
    }
}
