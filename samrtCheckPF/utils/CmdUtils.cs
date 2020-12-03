using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace samrtCheckPF.utils
{
    class CmdUtils
    {
        private static CmdUtils cmdUtils = null;
        Process p;
        Process p1;
        string proName = null;
        private CmdUtils()
        {
            p = new Process();
            p1 = new Process();
        }

        public static CmdUtils getInstance()
        {
            if(cmdUtils == null)
            {
                cmdUtils = new CmdUtils();
            }
            return cmdUtils;
        }

        public void killProcess()
        {
            Process[] pr = Process.GetProcessesByName("python");
            foreach (Process p in pr)
            {
                try
                {
                    p.Kill();
                  //  p.WaitForExit(); // possibly with a timeout
                }
                catch (Win32Exception e)
                {
                    LogUtils.e("CmdUtils killProcess Win32Exception:" + e.Message.ToString());
                }
                catch (InvalidOperationException e)
                {
                    LogUtils.e("CmdUtils killProcess InvalidOperationException:" + e.Message.ToString());
                }
            }
        }

        public void killTCPProcess()
        {
            Process[] pr = Process.GetProcessesByName("ZbarDecode");
            foreach (Process p in pr)
            {
                try
                {
                    p.Kill();
                    //  p.WaitForExit(); // possibly with a timeout
                }
                catch (Win32Exception e)
                {
                    LogUtils.e("CmdUtils killTCPProcess Win32Exception:" + e.Message.ToString());
                }
                catch (InvalidOperationException e)
                {
                    LogUtils.e("CmdUtils killTCPProcess InvalidOperationException:" + e.Message.ToString());
                }
            }
        }

        public void RunCmd(string command)
        {
            try
            {
                
                p.StartInfo.FileName = "cmd.exe";
                // 必须禁用操作系统外壳程序
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.OutputDataReceived += new DataReceivedEventHandler(p_OutputDataReceived);
                p.ErrorDataReceived += new DataReceivedEventHandler(p_ErrorDataReceived);
                //启动进程
                p.Start();
                proName = p.ProcessName;
                p.StandardInput.WriteLine(command);
                p.StandardInput.AutoFlush = true;
                //p.BeginOutputReadLine();

                //准备读出输出流和错误流
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                //等待退出
                p.WaitForExit();

                //关闭进程
                p.Close();
                
            }
            catch (Exception e)
            {
                LogUtils.e("CmdUtils RunCmd Exception : " + e.Message);
            }
        }


        public void RunTCPCmd(string command)
        {
            try
            {
                p1.StartInfo.FileName = "cmd.exe";
                // 必须禁用操作系统外壳程序
                p1.StartInfo.UseShellExecute = false;
                p1.StartInfo.CreateNoWindow = true;
                p1.StartInfo.RedirectStandardInput = true;
                p1.StartInfo.RedirectStandardOutput = true;
                p1.StartInfo.RedirectStandardError = true;
                p1.OutputDataReceived += new DataReceivedEventHandler(p_OutputDataTCPReceived);
                p1.ErrorDataReceived += new DataReceivedEventHandler(p_ErrorDataTCPReceived);
                //启动进程
                p1.Start();
                proName = p1.ProcessName;
                p1.StandardInput.WriteLine(command + "&exit");
                p1.StandardInput.AutoFlush = true;
                //p.BeginOutputReadLine();

                //准备读出输出流和错误流
                p1.BeginOutputReadLine();
                p1.BeginErrorReadLine();

                ////等待退出
                //p1.WaitForExit();

                ////关闭进程
                //p1.Close();

            }
            catch (Exception e)
            {
                LogUtils.e("CmdUtils RunTCPCmd Exception : " + e.Message);
            }
        }

        private void p_OutputDataTCPReceived(object sender, DataReceivedEventArgs e)
        {
            LogUtils.d("CmdUtils p_OutputDataTCPReceived : " + e.Data);
        }


        private void p_ErrorDataTCPReceived(object sender, DataReceivedEventArgs e)
        {
            LogUtils.d("CmdUtils p_ErrorDataTCPReceived : " + e.Data);
        }

        private void p_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            LogUtils.d("CmdUtils p_OutputDataReceived : " + e.Data);
        }

        private void p_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            LogUtils.d("CmdUtils ErrorDataReceived : " + e.Data);
        }
    }
}
