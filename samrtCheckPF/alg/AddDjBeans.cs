using samrtCheckPF.json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace samrtCheckPF.alg
{
    class AddDjBeans
    {
    }

    //******************************************** 算法接口实体类 *****************************************************8
    //开启python子线程
    class StartBean
    {
        ////public int i; //为兼容lambda语法，维持rpc正常通信，增加了一个无用的参数，任意值都可
        //public List<string> station { get; set; }
        //public List<string> line_list { get; set; }
        public List<string> reg_param_str_list { get; set; }
        public List<string> golden_sample_list { get; set; }
        public List<string> client_param_list { get; set; }
    }

    //停止python子线程
    class StopBean
    {
        public int i; //为兼容lambda语法，维持rpc正常通信，增加了一个无用的参数，任意值都可
    }

    //初始化函数模型
    class AddModelBean
    {
        public string function_name { get; set; }
        public int[] roi { get; set; }
        public string model_root { get; set; }
    }

    //初始化点胶模型
    class InitialSegModelBean
    {
        //public int i; //为兼容lambda语法，维持rpc正常通信，增加了一个无用的参数，任意值都可
        public string model_root { get; set; }
    }

    //开始执行算法
    class InferenceBean
    {
        public string img_path { get; set; }
        public List<int> function_list { get; set; }
        public int stat { get; set; }
    }
    
    //释放函数模型
    class ReleaseModelBean
    {
        public string function_id { get; set; }
    }

    //释放点胶模型
    class ReleaseSegModelBean
    {
        public int i; //为兼容lambda语法，维持rpc正常通信，增加了一个无用的参数，任意值都可
    }

    //****************************************** 算法解析结果实体类 ***************************************************
    class InferenceResult
    {
        public string imgPath { get; set; }
        public int state { get; set; }
        public int requireSeg { get; set; }
        public SegResult segResult { get; set; }
        public List<FunctionResult> FunctionResult { get; set; }
        public List<double[]> H { get; set; }
    }

    class UploadInferenceResult
    {
        public InferenceResult IR { get; set; }
        public int[] FunctionID { get; set; }
    }

    class SegResult
    {
        public int functionId { get; set; }
        public int IsFail { get; set; }
        public List<SegRect> rois { get; set; }
    }
    class SegRect
    {
        public int left { get; set; }
        public int right { get; set; }
        public int bottom { get; set; }
        public int top { get; set; }
    }
    class FunctionResult
    {
        public int functionId { get; set; }
        public float value { get; set; }
        public int IsFail { get; set; }
        public SegRect roi { get; set; }
    }


}
