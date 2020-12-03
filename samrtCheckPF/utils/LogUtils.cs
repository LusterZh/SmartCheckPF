using samrtCheckPF.common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace samrtCheckPF.utils
{
    class LogUtils
    {
        public static void e(string logStr)
        {
            addLog("error:  " + logStr);
            if (GlobalValue.isDebugMode)
            {
                Console.WriteLine(logStr);
            }
        }

        public static void d(string logStr)
        {
            addLog("debug:  " + logStr);
            if (GlobalValue.isDebugMode)
            {
                Console.WriteLine(logStr);
            }
        }

        public static void clearLog()
        {
            try
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"log.txt", false))
                {
                    file.WriteLine("");// 直接追加文件末尾
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("LogUtils addLog exception: " + e.Message);
            }
        }

        private static void addLog(string workRecord)//把记录加到文件里
        {
            try
            {
                //在将文本写入文件前，处理文本行
                //StreamWriter第二个参数为false覆盖现有文件，为true则把文本追加到文件末尾
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"log.txt", true))
                {
                    string time = DateTime.Now.ToString();

                    //把本次检测结果追加进txt文本里
                    //workRecord += ”我是要追加进文本的内容“
                    file.WriteLine(time + workRecord);// 直接追加文件末尾
                    file.Close();
                    file.Dispose();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("LogUtils addLog exception: "+e.Message);
            }            
        }
    }
}
