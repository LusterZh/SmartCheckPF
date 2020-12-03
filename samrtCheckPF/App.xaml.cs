using Ezhu.AutoUpdater;
using Lierda.WPFHelper;
using samrtCheckPF.alg;
using samrtCheckPF.common;
using samrtCheckPF.utils;
using smartCheckPF;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace samrtCheckPF
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
        }
        LierdaCracker cracker = new LierdaCracker();  //内存回收

        protected override void OnStartup(StartupEventArgs e)
        {
            Application.Current.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;
            
            LoginMainWindow window = new LoginMainWindow();
            Thread t = new Thread(startRpcService);//创建了线程还未开启
            t.Start();
            bool? dialogResult = window.ShowDialog();
            if ((dialogResult.HasValue == true) &&
                (dialogResult.Value == true))
            {
               GlobalValue.CONFIG_VERSION = Application.ResourceAssembly.GetName().Version.ToString();
                cracker.Cracker(100);//垃圾回收间隔时间     
                base.OnStartup(e);
                //FileUtils.deleteFile(GlobalValue.LOG_FILE);
                LogUtils.clearLog();
                LogUtils.d("Application_Startup");

                ////启动更新程序
                Updater.CheckUpdateStatus();

                Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            }
            else
            {
                CmdUtils.getInstance().killProcess();
                Environment.Exit(0);
            }

            //cracker.Cracker(100);//垃圾回收间隔时间     
            //base.OnStartup(e);
            ////FileUtils.deleteFile(GlobalValue.LOG_FILE);
            //LogUtils.clearLog();
            //LogUtils.d("Application_Startup");
            //Thread t = new Thread(startRpcService);//创建了线程还未开启
            //t.Start();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            CmdUtils.getInstance().killProcess();
            CmdUtils.getInstance().killTCPProcess();
            LogUtils.d("Application_Exit");
            base.OnExit(e);
            Environment.Exit(0);
        }

        private void startRpcService()
        {
            CallAlg.getInstance().openRpcService();
        }



    }
}
