using Newtonsoft.Json;
using samrtCheckPF.common;
using samrtCheckPF.http;
using samrtCheckPF.json;
using samrtCheckPF.utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace samrtCheckPF.alg
{
    
    class CallAlg
    {
        private static CallAlg callAlg= null;
        private CallAlg()
        {
            string proPath = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "Alg\\";
        }
        public static CallAlg getInstance()
        {
            if(callAlg == null)
            {
                callAlg = new CallAlg();
            }
            return callAlg;
        }
        public void openRpcService()
        {
            CmdUtils.getInstance().RunCmd(@"python client_rpc.py");
        }

        public void openTCPService()
        {    
            
            CmdUtils.getInstance().RunTCPCmd(AppDomain.CurrentDomain.BaseDirectory + "Zbardecode\\ZbarDecode\\bin\\Debug\\ZbarDecode.exe");
        }


        public string start(List<string> client_param_list, List<string> reg_param_str_list, List<string> golden_sample_list)
        {
            StartBean startBean = new StartBean();
            startBean.reg_param_str_list = reg_param_str_list;
            startBean.golden_sample_list = golden_sample_list;
            startBean.client_param_list = client_param_list;
            RpcBean bean = new RpcBean();
            bean.id = 0;
            bean.jsonrpc = "2.0";
            bean.method = "start";
            bean.Params = startBean;
            string result = "";
            //序列化参数
            var jsonParam = JsonConvert.SerializeObject(bean);
            try
            {
                //发送请求 http://10.176.122.59:4002/service.py
                StringBuilder url = new StringBuilder();
              url.Append("http://").Append(GlobalValue.GetLocalIP()).Append(":4002/service.py");
              //   url.Append("http://0.0.0.0:4002/service.py");
                var request = (HttpWebRequest)WebRequest.Create(url.ToString());
                request.Method = "POST";
                request.ContentType = "application/json;charset=UTF-8";
                var byteData = Encoding.UTF8.GetBytes(jsonParam);
                var length = byteData.Length;
                request.ContentLength = length;
                var writer = request.GetRequestStream();
                writer.Write(byteData, 0, length);
                writer.Close();

                //接收数据
                var response = (HttpWebResponse)request.GetResponse();
                 string responseString = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("utf-8")).ReadToEnd();
                LogUtils.d("CallAlg start response :" + responseString);

                ResponseBean responseBean = JsonConvert.DeserializeObject<ResponseBean>(responseString);
                result = responseBean.result.ToString();
                LogUtils.d("CallAlg start result: " + result);
            }
            catch (Exception e)
            {
                LogUtils.e("alg start exception: " + e.Message);
                result = "error:" + e.Message;
            }
            return result;
        }

        public bool stop()
        {
            StopBean stopBean = new StopBean();
            stopBean.i = 0;
            RpcBean bean = new RpcBean();
            bean.id = 0;
            bean.jsonrpc = "2.0";
            bean.method = "stop";
            bean.Params = stopBean;
            bool result = false;
            //序列化参数
            var jsonParam = JsonConvert.SerializeObject(bean);
            try
            {
                //发送请求 http://10.176.122.59:4002/service.py
                StringBuilder url = new StringBuilder();
                url.Append("http://").Append(GlobalValue.GetLocalIP()).Append(":4002/service.py");
                // url.Append("http://0.0.0.0:4002/service.py");
                var request = (HttpWebRequest)WebRequest.Create(url.ToString());
                request.Method = "POST";
                request.ContentType = "application/json;charset=UTF-8";
                var byteData = Encoding.UTF8.GetBytes(jsonParam);
                var length = byteData.Length;
                request.ContentLength = length;
                var writer = request.GetRequestStream();
                writer.Write(byteData, 0, length);
                writer.Close();

                //接收数据
                var response = (HttpWebResponse)request.GetResponse();
                string responseString = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("utf-8")).ReadToEnd();
                LogUtils.d("CallAlg initialSegModel response :" + responseString);

                ResponseBean responseBean = JsonConvert.DeserializeObject<ResponseBean>(responseString);
                result = (bool)responseBean.result;
                LogUtils.d("CallAlg initialSegModel result: " + result);
            }
            catch (Exception e)
            {
                LogUtils.e("alg initialSegModel exception: " + e.Message);
            }
            return result;
        }

        //public string addModel(string function_id, string golden_sample, MyRect roi, double thresh)
        //{

        //    AddModelBean addModelBean = new AddModelBean();
        //    addModelBean.function_id = function_id;
        //    addModelBean.golden_sample = golden_sample;
        //    int[] i = new int[4] { roi.left,roi.top,roi.bottom-roi.top,roi.right-roi.left};
        //    addModelBean.roi = i;
        //    addModelBean.thresh = thresh;
        //    RpcBean bean = new RpcBean();
        //    bean.id = 0;
        //    bean.jsonrpc = "2.0";
        //    bean.method = "add_model";
        //    bean.Params = addModelBean;
        //    string result = "";
        //    //序列化参数
        //    var jsonParam = JsonConvert.SerializeObject(bean);
        //    try
        //    {
        //        //发送请求 http://10.176.122.59:4002/service.py
        //        StringBuilder url = new StringBuilder();
        //        url.Append("http://").Append(GlobalValue.GetLocalIP()).Append(":4002/service.py");
        //        // url.Append("http://0.0.0.0:4002/service.py");
        //        var request = (HttpWebRequest)WebRequest.Create(url.ToString());
        //        request.Method = "POST";
        //        request.ContentType = "application/json;charset=UTF-8";
        //        var byteData = Encoding.UTF8.GetBytes(jsonParam);
        //        var length = byteData.Length;
        //        request.ContentLength = length;
        //        var writer = request.GetRequestStream();
        //        writer.Write(byteData, 0, length);
        //        writer.Close();

        //        //接收数据
        //        var response = (HttpWebResponse)request.GetResponse();
        //        string responseString = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("utf-8")).ReadToEnd();
        //        LogUtils.d("CallAlg addModel response :" + responseString);

        //        ResponseBean responseBean = JsonConvert.DeserializeObject<ResponseBean>(responseString);
        //        result = (string)responseBean.result.ToString();
        //        LogUtils.d("CallAlg addModel result: " + result);
        //    }
        //    catch (Exception e)
        //    {
        //        LogUtils.e("alg addModel exception: " + e.Message);
        //        result = "error:"+e.Message;
        //    }
        //    return result;
        //}

        public string addModel(string function_name, MyRect roi, string model_root)
        {
            AddModelBean addModelBean = new AddModelBean();
            addModelBean.function_name = function_name;
            //addModelBean.golden_sample_path = golden_sample_path;
            int[] i = new int[4] { roi.left, roi.top, roi.bottom - roi.top, roi.right - roi.left };
            addModelBean.roi = i;
            addModelBean.model_root = model_root;
            RpcBean bean = new RpcBean();
            bean.id = 0;
            bean.jsonrpc = "2.0";
            bean.method = "add_model";
            bean.Params = addModelBean;
            string result = "";
            //序列化参数
            var jsonParam = JsonConvert.SerializeObject(bean);
            try
            {
                //发送请求 http://10.176.122.59:4002/service.py
                StringBuilder url = new StringBuilder();
                url.Append("http://").Append(GlobalValue.GetLocalIP()).Append(":4002/service.py");
                // url.Append("http://0.0.0.0:4002/service.py");
                var request = (HttpWebRequest)WebRequest.Create(url.ToString());
                request.Method = "POST";
                request.ContentType = "application/json;charset=UTF-8";
                var byteData = Encoding.UTF8.GetBytes(jsonParam);
                var length = byteData.Length;
                request.ContentLength = length;
                var writer = request.GetRequestStream();
                writer.Write(byteData, 0, length);
                writer.Close();

                //接收数据
                var response = (HttpWebResponse)request.GetResponse();
                string responseString = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("utf-8")).ReadToEnd();
                LogUtils.d("CallAlg addModel response :" + responseString);

                ResponseBean responseBean = JsonConvert.DeserializeObject<ResponseBean>(responseString);
                result = (string)responseBean.result.ToString();
                LogUtils.d("CallAlg addModel result: " + result);
            }
            catch (Exception e)
            {
                LogUtils.e("alg addModel exception: " + e.Message);
                result = "error:" + e.Message;
            }
            return result;
        }

        public string initialSegModel(string model_root)
        {

            InitialSegModelBean initialSegModelBean = new InitialSegModelBean();
            initialSegModelBean.model_root = model_root;
            RpcBean bean = new RpcBean();
            bean.id = 0;
            bean.jsonrpc = "2.0";
            bean.method = "InitialSegModel";
            bean.Params = initialSegModelBean;
            string result = "";
            //序列化参数
            var jsonParam = JsonConvert.SerializeObject(bean);
            try
            {
                //发送请求 http://10.176.122.59:4002/service.py
                StringBuilder url = new StringBuilder();
                url.Append("http://").Append(GlobalValue.GetLocalIP()).Append(":4002/service.py");
                //url.Append("http://0.0.0.0:4002/service.py");
                var request = (HttpWebRequest)WebRequest.Create(url.ToString());
                request.Method = "POST";
                request.ContentType = "application/json;charset=UTF-8";
                var byteData = Encoding.UTF8.GetBytes(jsonParam);
                var length = byteData.Length;
                request.ContentLength = length;
                var writer = request.GetRequestStream();
                writer.Write(byteData, 0, length);
                writer.Close();

                //接收数据
                var response = (HttpWebResponse)request.GetResponse();
                string responseString = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("utf-8")).ReadToEnd();
                LogUtils.d("CallAlg initialSegModel response :" + responseString);

                ResponseBean responseBean = JsonConvert.DeserializeObject<ResponseBean>(responseString);
                result = (string)responseBean.result.ToString();
                LogUtils.d("CallAlg initialSegModel result: " + result);
            }
            catch (Exception e)
            {
                LogUtils.e("alg initialSegModel exception: " + e.Message);
                result = "error:" + e.Message;
            }
            return result;
        }

        public bool inference(string imgPath,List<int> function_list,int stat)
        {
            InferenceBean inferenceBean = new InferenceBean();
            inferenceBean.img_path = imgPath;
            inferenceBean.function_list = function_list;
            inferenceBean.stat = stat;

            RpcBean bean = new RpcBean();
            bean.id = 0;
            bean.jsonrpc = "2.0";
            bean.method = "inference";
            bean.Params = inferenceBean;
            bool result = false;
            //序列化参数
            var jsonParam = JsonConvert.SerializeObject(bean);
            try
            {
                //发送请求 http://10.176.122.59:4002/service.py
                StringBuilder url = new StringBuilder();
                url.Append("http://").Append(GlobalValue.GetLocalIP()).Append(":4002/service.py");
                // url.Append("http://0.0.0.0:4002/service.py");
                var request = (HttpWebRequest)WebRequest.Create(url.ToString());
                request.Method = "POST";
                request.ContentType = "application/json;charset=UTF-8";
                var byteData = Encoding.UTF8.GetBytes(jsonParam);
                var length = byteData.Length;
                request.ContentLength = length;
                var writer = request.GetRequestStream();
                writer.Write(byteData, 0, length);
                writer.Close();

                //接收数据
                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("utf-8")).ReadToEnd();
                LogUtils.d("CallAlg inference response: " + responseString);

                ResponseBean responseBean = JsonConvert.DeserializeObject<ResponseBean>(responseString);
                result = (bool)responseBean.result;
                LogUtils.d("CallAlg inference result: " + result);
            }
            catch (Exception e)
            {
                LogUtils.e("alg inference error: " + e.Message);
            }

            return result;
        }

        public bool release_model(string function_id)
        {
            ReleaseModelBean releaseBean = new ReleaseModelBean();
            releaseBean.function_id = function_id;
            RpcBean bean = new RpcBean();
            bean.id = 0;
            bean.jsonrpc = "2.0";
            bean.method = "release_model";
            bean.Params = releaseBean;
            bool result = false;
            //序列化参数
            var jsonParam = JsonConvert.SerializeObject(bean);
            try
            {
                //发送请求
                StringBuilder url = new StringBuilder();
                url.Append("http://").Append(GlobalValue.GetLocalIP()).Append(":4002/service.py");
                // url.Append("http://0.0.0.0:4002/service.py");
                var request = (HttpWebRequest)WebRequest.Create(url.ToString());
                request.Method = "POST";
                request.ContentType = "application/json;charset=UTF-8";
                var byteData = Encoding.UTF8.GetBytes(jsonParam);
                var length = byteData.Length;
                request.ContentLength = length;
                var writer = request.GetRequestStream();
                writer.Write(byteData, 0, length);
                writer.Close();

                //接收数据
                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("utf-8")).ReadToEnd();
                LogUtils.d("CallAlg release response: " + responseString);
                ResponseBean responseBean = JsonConvert.DeserializeObject<ResponseBean>(responseString);
                if(responseBean.result != null)
                {
                    result = (bool)responseBean.result;
                }
                LogUtils.d("CallAlg release response: " + result);
            }
            catch(Exception e)
            {
                LogUtils.e("CallAlg release exception: " + e.Message);
            }
            return result;
        }

        public bool releaseSegModel()
        {
            ReleaseSegModelBean releaseSegModelBean = new ReleaseSegModelBean();
            releaseSegModelBean.i = 0;
            RpcBean bean = new RpcBean();
            bean.id = 0;
            bean.jsonrpc = "2.0";
            bean.method = "release_seg_model";
            bean.Params = releaseSegModelBean;
            bool result = false;
            //序列化参数
            var jsonParam = JsonConvert.SerializeObject(bean);
            try
            {
                //发送请求
                StringBuilder url = new StringBuilder();
                url.Append("http://").Append(GlobalValue.GetLocalIP()).Append(":4002/service.py");
                // url.Append("http://0.0.0.0:4002/service.py");
                var request = (HttpWebRequest)WebRequest.Create(url.ToString());
                request.Method = "POST";
                request.ContentType = "application/json;charset=UTF-8";
                var byteData = Encoding.UTF8.GetBytes(jsonParam);
                var length = byteData.Length;
                request.ContentLength = length;
                var writer = request.GetRequestStream();
                writer.Write(byteData, 0, length);
                writer.Close();

                //接收数据
                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("utf-8")).ReadToEnd();
                LogUtils.d("CallAlg releaseSegModel response: " + responseString);
                ResponseBean responseBean = JsonConvert.DeserializeObject<ResponseBean>(responseString);
                result = (bool)responseBean.result;
                LogUtils.d("CallAlg releaseSegModel response: " + result);
            }
            catch (Exception e)
            {
                LogUtils.e("CallAlg releaseSegModel exception: " + e.Message);
            }
            return result;
        }

        //算法合成点胶拼图
        public string cutImg(string imgPath)
        {
            CutImageParamBean cutImageParamBean = new CutImageParamBean();
            cutImageParamBean.image_path = imgPath;

            RpcBean bean = new RpcBean();
            bean.id = 0;
            bean.jsonrpc = "2.0";
            bean.method = "cut_image";
            bean.Params = cutImageParamBean;
            string result = "";
            //序列化参数
            var jsonParam = JsonConvert.SerializeObject(bean);
            try
            {
                //发送请求 http://10.176.122.59:4002/service.py
                StringBuilder url = new StringBuilder();
                url.Append("http://").Append(GlobalValue.GetLocalIP()).Append(":4002/service.py");
                // url.Append("http://0.0.0.0:4002/service.py");
                var request = (HttpWebRequest)WebRequest.Create(url.ToString());
                request.Method = "POST";
                request.ContentType = "application/json;charset=UTF-8";
                var byteData = Encoding.UTF8.GetBytes(jsonParam);
                var length = byteData.Length;
                request.ContentLength = length;
                var writer = request.GetRequestStream();
                writer.Write(byteData, 0, length);
                writer.Close();

                //接收数据
                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("utf-8")).ReadToEnd();
                LogUtils.d("CallAlg inference response: " + responseString);

                ResponseBean responseBean = JsonConvert.DeserializeObject<ResponseBean>(responseString);
                result = (string)responseBean.result;
                LogUtils.d("CallAlg inference result: " + result);
            }
            catch (Exception e)
            {
                LogUtils.e("alg inference error: " + e.Message);
            }

            return result;
        }
    }
}
