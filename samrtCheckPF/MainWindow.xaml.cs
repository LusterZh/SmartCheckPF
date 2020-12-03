using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using DataMatrix.net;

using System.ComponentModel;
using System.Data;
//using System.Drawing；
using MvCamCtrl.NET;
using System.Runtime.InteropServices;
using System.Threading;
using samrtCheckPF.http;
using samrtCheckPF.json;
using Newtonsoft.Json;
using samrtCheckPF.utils;
using samrtCheckPF.common;
using samrtCheckPF.serialPort;
//using System.Drawing.Imaging;
using SelectionMode = System.Windows.Controls.SelectionMode;
using Panel = System.Windows.Controls.Panel;
using MessageBox = System.Windows.MessageBox;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Button = System.Windows.Controls.Button;

//mali
using samrtCheckPF.alg;
using samrtCheckPF.download;
using System.Timers;
using System.Net;
using HslCommunication.Profinet.Melsec;
using HslCommunication;
using ZXing;
using OpenCvSharp;
using ZXing.Common;
using smartCheckPF.camera;
using MESinterface;
using System.Net.Sockets;
using System.Windows.Threading;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;
using Ezhu.AutoUpdater;
using smartCheckPF.utils;
using Path = System.IO.Path;

//自动模式
public enum EStepSelectWork
{
    STEP_SELECTWORK_INIT,//初始化系统操作
    STEP_SELECTWORK_DONORMALWORK,
    STEP_SELECTWORK_PLC_UPDATEIO,
    STEP_SELECTWORK_IMAGEDISPLAY,
    STEP_AUTO_INFORM_PLC_READY2MOVEIN,//告知PLC可以移动运动机构进去
    STEP_AUTO_INFORM_PLC_READY2MOVEIN_RESPONSE,
    STEP_AUTO_INFORM_PLC_READY2MOVEIN_DONE,//
    STEP_AUTO_INFORM_CONTROLLER_OPENLIGHT,//告知光源控制器开灯
    STEP_AUTO_INFORM_CONTROLLER_OPENLIGHT_RESPONSE,
    STEP_AUTO_INFORM_CONTROLLER_OPENLIGHT_DONE,
    STEP_AUTO_SHOW_SECOND_IMAGE,//展示第二面的图片
    STEP_AUTO_INFORM_SCAN_BARCODE,//扫码失败再次扫码
    STEP_AUTO_INFORM_SCAN_BARCODE_RESPONSE,//
    STEP_AUTO_INFORM_SCAN_BARCODE_DONE,//
    STEP_AUTO_WAIT_PLC_SNAP_RESPONSE,//获取相机拍照信号
    STEP_AUTO_PLC_SNAP_RESPONSE_DONE,
    STEP_AUTO_VS_INFORM_CAM,//相机拍照
    STEP_AUTO_VS_CAM_RESPOND,
    STEP_AUTO_VS_CAM_RESPOND_DONE,
    STEP_AUTO_INFORM_PLC_READY2MOVEOUT,//告知PLC可以移动运动机构出去
    STEP_AUTO_INFORM_PLC_READY2MOVEOUT_RESPONE,
    STEP_AUTO_INFORM_PLC_READY2MOVEOUT_DONE,//
    STEP_AUTO_VS_DECODE,//相机解码
    STEP_AUTO_VS_DECODE_RESPOND,
    STEP_AUTO_VS_DECODE_RESPOND_DATAMATRIC,
    STEP_AUTO_VS_DECODE_RESPOND_DONE,
    STEP_AUTO_VS_CALLRCP,//调用Python算法
    STEP_AUTO_VS_CALLRCP_RESPOND,
    STEP_AUTO_VS_CALLRCP_RESPOND_DONE,
    STEP_AUTO_INFORM_CONTROLLER_CLOSELIGHT,//告知光源控制器关灯
    STEP_AUTO_INFORM_CONTROLLER_CLOSELIGHT_RESPONSE,
    STEP_AUTO_INFORM_CONTROLLER_CLOSELIGHT_DONE,//
    STEP_AUTO_INFORM_PLC_RESULT,//告知PLC最后结果
    STEP_AUTO_INFORM_PLC_RESULT_RESPONSE,
    STEP_AUTO_INFORM_PLC_RESULT_DONE,
    STEP_AUTO_SHOW_FAILWINDOW,
    STEP_AUTO_WAIT_CYCLE_END,
    STEP_AUTO_WAIT_CYCLE_END_DONE,
}

//半自动模式
public enum EStepManualSelectWork
{
    STEP_SELECTWORK_INIT,//初始化系统操作
    STEP_SELECTWORK_DONORMALWORK,
    STEP_AUTO_VS_INFORM_CAM,//相机拍照
    STEP_AUTO_VS_CAM_RESPOND,
    STEP_AUTO_VS_CAM_RESPOND_DONE,
    STEP_AUTO_VS_DECODE,//相机解码
    STEP_AUTO_VS_DECODE_RESPOND,
    STEP_AUTO_VS_DECODE_RESPOND_DATAMATRIC,
    STEP_AUTO_VS_DECODE_RESPOND_DONE,
    STEP_AUTO_VS_CALLRCP,//调用Python算法
    STEP_AUTO_VS_CALLRCP_RESPOND,
    STEP_AUTO_VS_CALLRCP_RESPOND_DONE,
    STEP_AUTO_INFORM_PLC_RESULT_DONE,
    STEP_AUTO_SHOW_FAILWINDOW,
}


namespace samrtCheckPF
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private HIKCamera m_pHikCamera;
        public System.Drawing.Bitmap m_BitmapForDecode;
        public BitmapImage m_BitmapImageForSave;
        public bool m_bIsSvaebmpSuccessed = false;

        //alg相关
        bool isAlgEnvReady = false; //alg环境是否ready（是否正在拉取配置信息）
        List<int> addModelFailedIds = new List<int>(); // 存放加载模型失败的函数id
        List<int> FunctionIdS = new List<int>(); // 存放加载模型失败的函数id
        List<int> FunctionPartNumberID = new List<int>(); // 临时存放物料编码包含的的函数ID
        //接收推送消息
        WSocketClient client = new WSocketClient(GlobalValue.SOCKET_IP);
        CSQLiteHelper MySqlLite = new CSQLiteHelper(AppDomain.CurrentDomain.BaseDirectory+ "barcode.db");

        public DmtxImageDecoder DMdecoder = new DmtxImageDecoder();
        public List<string> DMresults = new List<string>();

        private readonly TaskScheduler _syncContextTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
        //主窗口object
        public MainWindow()
        {
            InitializeComponent();


            // 设置全屏
            this.WindowState = System.Windows.WindowState.Maximized;
            this.WindowStyle = System.Windows.WindowStyle.None;
            this.ResizeMode = System.Windows.ResizeMode.NoResize;
            // this.Topmost = true;

            this.Left = 0.0;
            this.Top = 0.0;
            this.Width = System.Windows.SystemParameters.PrimaryScreenWidth;
            this.Height = System.Windows.SystemParameters.PrimaryScreenHeight;

            checkImageLargeWidth = this.Width - 400;
            checkImageLargeHeight = this.Height - 150;


            //判断程序是否重复打开
            System.Diagnostics.Process[] myProcesses = System.Diagnostics.Process.GetProcessesByName("smartCheckPF");//获取指定的进程名   
            if (myProcesses.Length > 1) //如果可以获取到知道的进程名则说明已经启动
            {
                MessageBox.Show("拍照程序已启动，请勿重复开启！");
                Environment.Exit(0);                //关闭系统
            }


            //进程间通信
            if (InitTCPsever("127.0.0.1","20000"))
            {
                startTCPService();
            }
            else
            {
                MessageBox.Show("socket端口绑定失败，请稍后重启程序！");
                Application.Current.Shutdown();//关闭
            }

            //读取算法配置文件
        //    GlobalValue.ReadJsonConfigFile();

            InitComboxList();

            //相机初始化
            DeviceListAcq();
            InitCamera();

            //获取设备唯一标识码
            txt_uuid.Text = DeviceUtil.UUID();

            SN.Focus();
            bnClose.IsEnabled = false;
            bnSaveJpg.IsEnabled = false;
            SetBtnEnable(false);

            //zhangpeng initialization code
            resultPassImage = getBitmapImage(resultPass);
            resultfailedImage = getBitmapImage(resultFailed);
            CodeErrorImage = getBitmapImage(resultCodeError);
            MesNumError = getBitmapImage(resultMesNumError);
            InfoImage = getBitmapImage(resultInfo);

            //初始化SUFR配准算法变量
            GoldenSurfImage = getBitmapImage(GoldenSurfPath);
            QRDecodeUtils.GoldenDescripit = QRDecodeUtils.SurfDetect(QRDecodeUtils.BitmapImage2Bitmap(GoldenSurfImage), out QRDecodeUtils.GoldenkeyPoints, Scale: GlobalValue.ScaleRatio);

            LRectListView.ItemsSource = LRectList;
            LRectListView.SelectionMode = SelectionMode.Single;
           // drawSmallRect();
            updateResultDisplay();
            drawLargeRect();
            //监听窗体关闭
            this.Closing += MainWindow_Closing;
            commitWorker.DoWork += CommitWorker_DoWork;
            commitWorker.RunWorkerCompleted += CommitWorker_RunWorkerCompleted;
            commitWorkerwithResult.DoWork += CommitWorkerwithResult_DoWork;
            commitWorkerwithResult.RunWorkerCompleted += CommitWorkerwithResult_RunWorkerCompleted;
            multCommitWorker.DoWork += MultCommitWorker_DoWork;
            multCommitWorker.RunWorkerCompleted += MultCommitWorker_RunWorkerCompleted;
            getIniWorker.DoWork += GetIniWorker_DoWork;
            getIniWorker.RunWorkerCompleted += GetIniWorker_RunWorkerCompleted;
            updateWorker.DoWork += UpdateWorker_DoWork;
            updateWorker.RunWorkerCompleted += UpdateWorker_RunWorkerCompleted;
            LoadWorker.DoWork += loadWorker_DoWork;
            LoadWorker.RunWorkerCompleted += loadWorker_RunWorkerCompleted;
            updateWorkerOnce.DoWork += updateWorkerOnce_DoWork;
            updateWorkerOnce.RunWorkerCompleted += updateWorkerOnce_RunWorkerCompleted;            
            notiryServerWorker.DoWork += NotiryServerWorker_DoWork;
            notiryServerWorker.RunWorkerCompleted += NotiryServerWorker_RunWorkerCompleted;
            //algWorker.DoWork += AlgWorker_DoWork;
            //algWorker.RunWorkerCompleted += AlgWorker_RunWorkerCompleted;
            commitDjWorker.DoWork += CommitDjWorker_DoWork;
            commitDjWorker.RunWorkerCompleted += CommitDjWorker_RunWorkerCompleted;

            //连接数据库
            if (MySqlLite.OpenDb())
            {
                UpdateSysDisplayLog("数据库连接成功");
            }

            startTimer(); //开启计时器，定时批量上传之前上传失败的图片
            loadingWait.Visibility = Visibility.Visible;
            //更新服务器到各站点配置信息  网页更新
            //getIniWorker.RunWorkerAsync();
            // 手动更新
            UpdateIniWorker();
            openSocketConnection();
           // startDjListenerConnection();
            //updateWorker.RunWorkerAsync();

            ////4.刷新界面上时间
            ThreadScanIORun();

            if (GlobalValue.ISPLCUSED == "0")
            {
                //半自动模式
                InitManualModePara();
                bnSaveJpg.IsEnabled = true;
            }
            else
            {
                //自动模式
                InitAutoModePara();

                //初始化PLC方向选择项
                InitPlcDirList();

                //是否扫码
                InitManualCodeList();
             
            }
            //解码线程
            InitThreadObject();
            InitBlockSizeObject();

            //打开相机
            OpenCamera();

            UpdateSysDisplayLog("程序启动");
        }

        private void startTCPService()
        {
            CallAlg.getInstance().openTCPService();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            //退出数据库连接
            if(MySqlLite != null)
            {
                MySqlLite.CloseDb();
                MySqlLite = null;
            }

            release();
            if (GlobalValue.ISPLCUSED == "0")
            {
                //半自动模式
                UnitManualModePara();
            }
            else
            {
                //自动模式
                UnitAutoModePara();
            }
            ////停止时间线程
            ThreadScanIOStop();
        }

        private void release()
        {
            //release alg model
            if(updateResBeans != null)
            {
                foreach (UpdateResBean u in updateResBeans)
                {
                    if(u.function_id == 0)
                    {
                        CallAlg.getInstance().releaseSegModel();
                    }
                    else
                    {
                        CallAlg.getInstance().release_model(u.function_id.ToString());
                    }
                }
            }
            stopPythonThread();
            //删除文件夹（alg）
            FileUtils.DelectDir(GlobalValue.ALG_FOLDER);
            FileUtils.deleteFolder(GlobalValue.ALG_FOLDER);
            //FileUtils.deleteFolder(GlobalValue.MODEL_FOLDER);
            FileUtils.deleteFolder(GlobalValue.CUT_IMG_FOLDER);


            stopSocketConnection();
        }

        private void NotiryServerWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            LogUtils.d("NotiryServerWorker_RunWorkerCompleted : "+ e.Result);
        }

        private void NotiryServerWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            string response = "-1";
            HttpRequestClient httpRequestClient = new HttpRequestClient();
            httpRequestClient.SetFieldValue("pcUuid", DeviceUtil.UUID());
            httpRequestClient.Upload(GlobalValue.NOTIFY_SERVER_IP, out response);
            LogUtils.d("NotiryServerWorker_DoWork response: " + response);
            e.Result = response;
        }

        private void CommitDjWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            loadingWait.Visibility = Visibility.Collapsed;
            if (!SN.IsEnabled)
            {
                SN.IsEnabled = true;
                //Clear textbox content and get in focus again
                //SN.Clear();
                SN.Focus();
            }
            string result = (string)e.Result;
            if (!result.StartsWith("{"))
            {
                DisPlayLog(tbx_syslog, "上传点胶结果错误 ： " + result,true);
                //  MessageBox.Show("上传点胶结果错误 ： "+result);
            }
            else
            {
                UploadRes res = JsonConvert.DeserializeObject<UploadRes>(result);
                if(res.code != 200)
                {
                  //  MessageBox.Show("上传点胶结果错误 ： " + res.msg);
                    DisPlayLog(tbx_syslog, "上传点胶结果错误 ： " + res.msg,true);
                }
                else
                {
                    GlobalValue.IMAGENAME = FileUtils.getFileNameByPath(imagePath);
                    GlobalValue.USERNAME = "lishizhen";
                    GlobalValue.PASSWORD = "LISHIZHEN";
                    FileUtils.deleteFile(GlobalValue.DJ_MASK_PATH + FileUtils.getFileNameByPath(imagePath).Split('.')[0] + ".png");     
                    FileUtils.deleteFile(imagePath);   
                    FileUtils.DelectDir(GlobalValue.ORIGIN_IMG_FOLDER);
                  //  openShowImageWindow(GlobalValue.DJ_COMPOSEMASK_IMG_PATH);

                  //  commitWorkerwithResult.RunWorkerAsync();//上传结果数据
                } 
            }
        }

        private void CommitDjWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            int _IntIsPass = -1;
            Task.Factory.StartNew(() => UpdateTb(loading_tv, "上传点胶结果中..."),
                 new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
            if (!NetworkUtils.IsNetworkConnected)
            {
                e.Result = "网络未连接";
                return;
            }
            InferenceResult ir = (InferenceResult)e.Argument;
            string response = "-1";
            string newMaskPath = GlobalValue.DJ_MASK_PATH + FileUtils.getFileNameByPath(ir.imgPath).Split('.')[0] + ".png";
            bool renameResult = FileUtils.renameFile(GlobalValue.DJ_MASK_IMG_PATH, newMaskPath);
            if (!renameResult)
            {
                e.Result = "获取mask图片失败";
                return;
            }
            byte[] oriImgBytes = readImage(ir.imgPath);
            byte[] maskImgBytes = readImage(newMaskPath);
            if (oriImgBytes != null && maskImgBytes != null)
            {
                HttpRequestClient httpRequestClient = new HttpRequestClient();
                httpRequestClient.SetFieldValue("originImage", System.IO.Path.GetFileName(ir.imgPath), "application/octet-stream", oriImgBytes);
                httpRequestClient.SetFieldValue("maskImage", System.IO.Path.GetFileName(newMaskPath), "application/octet-stream", maskImgBytes);
                if(ir.segResult.IsFail == 1)
                {
                    _IntIsPass = 0;
                }
                else
                {
                    _IntIsPass = 1;
                }
                httpRequestClient.SetFieldValue("isPass", _IntIsPass);
                List<DjRoiRectBean> ds = new List<DjRoiRectBean>();
                List<SegRect> segRects = ir.segResult.rois;

                if(segRects != null)
                {
                    foreach (SegRect sr in segRects)
                    {
                        DjRoiRectBean d = new DjRoiRectBean(sr.left, sr.top, sr.right, sr.bottom);
                        ds.Add(d);
                    }
                }

                string s = JsonConvert.SerializeObject(ds).ToString();
                httpRequestClient.SetFieldValue("rects", s);
                httpRequestClient.Upload(GlobalValue.COMMIT_DJ_IP, out response);
                e.Result = response;
                LogUtils.d("CommitDjWorker_DoWork response: "+response);
            }
            else
            {
                e.Result = "未找到图片，请检查图片是否保存";
            }
        }

        //算法结果
        InferenceResult inferenceResult = null;
        InferenceResult modifyinferenceResult = null;
        UploadInferenceResult UploadIR = new UploadInferenceResult();


        private void exeAlgComplete(string data)
        {
            loadingWait.Visibility = Visibility.Collapsed;
            LogUtils.d("exeAlgComplete : " + data);
            inferenceResult = JsonConvert.DeserializeObject<InferenceResult>(data);
            modifyinferenceResult = JsonConvert.DeserializeObject<InferenceResult>(data);//待修改上传数据
            UploadIR.IR = JsonConvert.DeserializeObject<InferenceResult>(data);//获取函数结果

                if (inferenceResult.state != 1)
                {
                    refreshLammyRect(updateResBeans, true);
                    DisPlayLog(tbx_syslog, "算法执行失败", true);
                    m_nRPCSucessed = true;
                }
                else
                {
                    if (CheckIsDj())
                    {
                        string ImageFilePath = Directory.GetCurrentDirectory() + "\\function\\" + "merge" + ".jpg";

                        // 更新图片
                        UpdateImageSource(ImageFilePath);
                    }

                    //结果刷新标志
                    refreshLammyRect(updateResBeans, false);
                    // loadingWait.Visibility = Visibility.Visible;

                    //结果刷新标志
                    m_nRPCSucessed = true;
                }

                UpdateProductInfo(m_bFinalResult, true);

                //上传结果数据
                GlobalValue.IMAGENAME = FileUtils.getFileNameByPath(imagePath);
                GlobalValue.IsNTFMODE = false;  //确认不为NTF模式

            try
            {
                ////写入测试数据
                string strtemp = JsonConvert.SerializeObject(UploadIR).ToString();
                //Write_UploadFailFile(GlobalValue.COMMIT_FAILED_TESTDATA, GlobalValue.IMAGENAME, strtemp);
                if (MySqlLite.InsertTableData("TEST", GlobalValue.IMAGENAME, strtemp) == 0)
                {
                    DisPlayLog(tbx_syslog, "数据库插入数据成功", true);
                }
            }
            catch (Exception ex)
            {
                DisPlayLog(tbx_syslog, ex.Message, true);
            }
            commitWorkerwithResult.RunWorkerAsync(UploadIR);
        }


        private bool exeAlgInMainThread(bool IsThread)
        {
            bool result = false;
            if (!isAlgEnvReady)
            {
                //  result = "检测到配置未拉取，请重启应用更新配置";
                // MessageBox.Show(result);
                if(IsThread)
                     UpdateSysDisplayLog("检测到配置未拉取，请重启应用更新配置");
                else
                {
                    DisPlayLog(tbx_syslog, "检测到配置未拉取，请重启应用更新配置",true);
                }
                return result;
            }
            if (imagePath == null || imagePath == "")
            {
                //result = "图片路径为空，请确认是否保存拍照";
                //MessageBox.Show(result);
                if (IsThread)
                    UpdateSysDisplayLog("图片路径为空，请确认是否保存拍照");
                else
                {
                    DisPlayLog(tbx_syslog, "图片路径为空，请确认是否保存拍照",true);
                }
                return result;
            }

            if(IsThread)
            {
                UpdateLoadingWindow("show");
                UpdateLoadingWindowText("计算中...");
            }
            else
            {
                loadingWait.Visibility = Visibility;
                loading_tv.Text = "计算中...";
            }

            Console.WriteLine("inference start time: " + DateTime.Now.TimeOfDay.ToString());

            bool result1 = false;
            if (GlobalValue.ISRESNAP == "1" && phase != 0)
            {
                 result1 = CallAlg.getInstance().inference(imagePath, FunctionIdS, phase - 1);
            }
            else
            {
                //int e = FunctionIdS.Count();
                result1 = CallAlg.getInstance().inference(imagePath, FunctionIdS, 0);
            }

            result = result1;
            LogUtils.d("exeAlgInMainThread : inference result: " + result.ToString());
            Console.WriteLine("inference end time: " + DateTime.Now.TimeOfDay.ToString());
            return result;
        }

        private void GetIniWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            GetIniRes getIniRes = null;
            string response = e.Result.ToString();

            if (response.StartsWith("{"))
            {
                getIniRes = JsonConvert.DeserializeObject<GetIniRes>(response);
            }
            else
            {

                //{
                //    loadingWait.Visibility = Visibility.Collapsed;
                //    TestPhase();
                //}

                if (MessageBox.Show(response + ",即将关闭程序!") == MessageBoxResult.OK)
                {
                    Application.Current.Shutdown();//关闭
                    return;
                }
            }
            if (getIniRes != null)
            {
                if (getIniRes.code == 200)
                {
                    getIniResBean = getIniRes.data;
                    if (getIniResBean == null)
                        return;
                    //Reload Configuration:
                    txt1.Text = getIniResBean.project;
                    txt2.Text = getIniResBean.build;
                    txt3.Text = getIniResBean.sku;
                    txt4.Text = getIniResBean.phase;
                    txt_line.Text = getIniResBean.line;
                    updateWorker.RunWorkerAsync();
                }
                else
                {
                    LogUtils.d("获取配置参数错误：" + getIniRes.code + "  错误信息：" + getIniRes.msg);
                }
            }          
        }
             
        private void UpdateIniWorker()
        {
            getIniResBean = new GetIniResBean();
            getIniResBean.project = GlobalValue.CONFIG_PROJECT;
            getIniResBean.build = GlobalValue.CONFIG_BULID;
            getIniResBean.sku = GlobalValue.CONFIG_SKU;
            getIniResBean.phase = GlobalValue.CONFIG_STATION;
            getIniResBean.line = GlobalValue.CONFIG_LINE;
            getIniResBean.pc_uuid = DeviceUtil.UUID();
            getIniResBean.version = GlobalValue.CONFIG_VERSION;

            txt1.Text = getIniResBean.project;
            txt2.Text = getIniResBean.build;
            txt3.Text = getIniResBean.sku;
            txt4.Text = getIniResBean.phase;
            txt_line.Text = getIniResBean.line;
            labelVersion.Content = "版本号:" + getIniResBean.version;
            labelUserInfo.Content = "用户未登录";

            if (GlobalValue.ISRESNAP == "0")
            {
                updateWorkerOnce.RunWorkerAsync();
            }
            else
            {
                updateWorker.RunWorkerAsync();
            }
        }

        //离线测试使用
        private void TestPhase()
        {
            getIniResBean = new GetIniResBean();
            getIniResBean.project = "luster";
            getIniResBean.build = "MP";
            getIniResBean.sku = "A1";
            getIniResBean.phase = "BAT";
            getIniResBean.line = "LINE32";
            getIniResBean.pc_uuid = DeviceUtil.UUID();

            txt1.Text = getIniResBean.project;
            txt2.Text = getIniResBean.build;
            txt3.Text = getIniResBean.sku;
            txt4.Text = getIniResBean.phase;
            txt_line.Text = getIniResBean.line;
        }

        //判断是否在点胶站
        private bool CheckIsDj()
        {
            //正常使用
            if (getIniResBean.phase.StartsWith("LDA"))
            {
                return true;
            }

            ////测试使用
            //if (getIniResBean.phase.StartsWith("BAT"))
            //{
            //    return true;
            //}

            return false;
        }

        private void GetIniWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            if (!NetworkUtils.IsNetworkConnected)
            {
                e.Result = "网络未连接";
                return;
            }
            
            Task.Factory.StartNew(() => UpdateTb(loading_tv, "获取配置参数中..."),
                            new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
            string response = "-1";
            HttpRequestClient httpRequestClient = new HttpRequestClient();
            httpRequestClient.SetFieldValue("pcUuid", DeviceUtil.UUID());
            httpRequestClient.Upload(GlobalValue.GETINI_IP, out response);
            LogUtils.d("GetIniWorker_DoWork response: " + response);
            e.Result = response;
        }

        //正反拍照
        private void UpdateWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
          //  loadingWait.Visibility = Visibility.Collapsed;
            notiryServerWorker.RunWorkerAsync();
            string result = (string)e.Result;
            if(result != null && result.StartsWith("["))
            {

                //下载完成之后启动rpc
                startDjListenerConnection();

                Thread.Sleep(3000);

                //加载函数
                LoadWorker.RunWorkerAsync();

            }
            else
            {
                //MessageBox.Show("拉取函数错误："+result);
                if (MessageBox.Show("服务器连接失败：" + result) == MessageBoxResult.OK)
                {
                    Application.Current.Shutdown();//关闭
                }
            }
            isAlgEnvReady = true;
        }
        private void UpdateWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            addModelFailedIds.Clear();
            if (!NetworkUtils.IsNetworkConnected)
            {
                e.Result = "网络未连接";
                return;
            }
            if (getIniResBean == null)
                return;
            //请求更新配置
            Task.Factory.StartNew(() => UpdateTb(loading_tv, "拉取最新函数列表中..."),
                   new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
            
            isAlgEnvReady = false;
            string response = "-1";
            HttpRequestClient httpRequestClient = new HttpRequestClient();
            httpRequestClient.SetFieldValue("project", getIniResBean.project);
            httpRequestClient.SetFieldValue("build", getIniResBean.build);
            httpRequestClient.SetFieldValue("sku", getIniResBean.sku);
            httpRequestClient.SetFieldValue("stationName", GlobalValue.CONFIG_STATION+ "1");
            httpRequestClient.SetFieldValue("pcUuid", getIniResBean.pc_uuid);
            httpRequestClient.SetFieldValue("version", getIniResBean.version);
            httpRequestClient.SetFieldValue("line", getIniResBean.line);
            httpRequestClient.Upload(GlobalValue.UPDATE_IP, out response);
            LogUtils.d("UpdateWorker_DoWork response: " + response);
            //e.Result = response;
            //解析配置信息，并添加下载任务  工站1
            UpdateRes updateRes = null;
            if (response.StartsWith("{"))
            {
                updateRes = JsonConvert.DeserializeObject<UpdateRes>(response);
                GlobalValue.HParam1 = updateRes.hParam;
                GlobalValue.ModelParam = updateRes.modelParam;
            }

            HttpRequestClient httpRequestClient2 = new HttpRequestClient();
            httpRequestClient2.SetFieldValue("project", getIniResBean.project);
            httpRequestClient2.SetFieldValue("build", getIniResBean.build);
            httpRequestClient2.SetFieldValue("sku", getIniResBean.sku);
            httpRequestClient2.SetFieldValue("stationName", GlobalValue.CONFIG_STATION+ "2");
            httpRequestClient2.SetFieldValue("pcUuid", getIniResBean.pc_uuid);
            httpRequestClient2.SetFieldValue("version", getIniResBean.version);
            httpRequestClient2.SetFieldValue("line", getIniResBean.line);
            httpRequestClient2.Upload(GlobalValue.UPDATE_IP, out response);
            LogUtils.d("UpdateWorker2_DoWork response: " + response);
            UpdateRes updateRes2 = null;
            if (response.StartsWith("{"))
            {
                updateRes2 = JsonConvert.DeserializeObject<UpdateRes>(response);
                GlobalValue.HParam2 = updateRes.hParam;
            }

            if (updateRes != null)
            {
                if (updateRes.code == 200)
                {
                    LogUtils.d("UpdateWorker_DoWork download start0");
                    List<DownloadBean> downloadBeans = new List<DownloadBean>();
                   
                    updateResBeans = updateRes.data;
                    ConfigFileResBeans = updateRes.materials;//新增料号查询变量
                    //ResListTemp.Add(updateRes.registration);
                    //registration = ResListTemp;

                    foreach (UpdateResBean updateResBean in updateResBeans)
                    {
                        updateResBeans1.Add(updateResBean);
                        updateResBean.result = GlobalValue.ALG_RESULT_NO;
                        if (updateResBean.golden_sample_name != null && updateResBean.golden_sample_name != "")
                        {
                            DownloadBean downloadBean = new DownloadBean();
                            //downloadBean.url = GlobalValue.GOLDEN_SAMPLE_DOWNLOAD_URL + updateResBean.function_id;
                            downloadBean.url = GlobalValue.GOLDEN_SAMPLE_NEW_DOWNLOAD_URL + updateResBean.golden_sample_id;
                            downloadBean.saveFolder = GlobalValue.GOLDEN_SAMPLE_FOLDER;
                            downloadBean.fileName = updateResBean.golden_sample_name;
                            GlobalValue.GOLD_SAMPLE_NAME1 = updateResBean.golden_sample_name;
                            downloadBeans.Add(downloadBean);
                            //updateResBean.golden_sample_path = downloadBean.saveFolder + updateResBean.golden_sample;
                        }
                        if (updateResBean.model_url != null && updateResBean.model_url != "")
                        {
                            DownloadBean downloadBean = new DownloadBean();
                            downloadBean.url = GlobalValue.MODEL_DOWNLOAD_URL + updateResBean.function_id;
                            //downloadBean.url = GlobalValue.MODEL_NEWDOWNLOAD_URL + updateResBean.model_url;
                            downloadBean.saveFolder = GlobalValue.MODEL_FOLDER + updateResBean.function_id + "//";
                            downloadBean.fileName = getModelNameByURL(updateResBean.model_url);
                            downloadBean.MD5 = updateResBean.model_md5;
                            downloadBeans.Add(downloadBean);
                            updateResBean.model_local_path = downloadBean.saveFolder + downloadBean.fileName;
                        }
                        
                    }

                    FunctionIdS.Clear();
                    foreach (UpdateResBean x in updateResBeans1)
                    {
                        FunctionIdS.Add(x.function_id);
                    }

                    LogUtils.d("UpdateWorker_DoWork download start");
                    Task.Factory.StartNew(() => UpdateTb(loading_tv, "开始执行下载任务"),
                        new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
                    foreach (DownloadBean d in downloadBeans)
                    {
                        Task.Factory.StartNew(() => UpdateTb(loading_tv, "正在下载："+d.fileName),
                        new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
                        httpRequestClient.downloadFile(d.url, d.saveFolder, d.fileName,d.MD5);
                    }
                    LogUtils.d("UpdateWorker_DoWork download complete");
                    Task.Factory.StartNew(() => UpdateTb(loading_tv, "下载完成"),
                        new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
                    //e.Result = JsonConvert.SerializeObject(updateResBeans).ToString();
                }
                else
                {
                    e.Result = "访问服务器错误："+ updateRes.code;
                }
            }

            if (updateRes2 != null)
            {
                if (updateRes2.code == 200)
                {
                    LogUtils.d("UpdateWorker_DoWork download start0");
                    List<DownloadBean> downloadBeans = new List<DownloadBean>();
                    updateResBeans2 = updateRes2.data;
                    //ConfigFileResBeans = updateRes.materials;//新增料号查询变量
                    //registration.Add(updateRes2.registration);

                    foreach (UpdateResBean updateResBean in updateResBeans2)
                    {
                        updateResBean.result = GlobalValue.ALG_RESULT_NO;
                        if (updateResBean.golden_sample_name != null && updateResBean.golden_sample_name != "")
                        {
                            DownloadBean downloadBean = new DownloadBean();
                            //downloadBean.url = GlobalValue.GOLDEN_SAMPLE_DOWNLOAD_URL + updateResBean.function_id;
                            downloadBean.url = GlobalValue.GOLDEN_SAMPLE_NEW_DOWNLOAD_URL + updateResBean.golden_sample_id;
                            downloadBean.saveFolder = GlobalValue.GOLDEN_SAMPLE_FOLDER;
                            downloadBean.fileName = updateResBean.golden_sample_name;
                            GlobalValue.GOLD_SAMPLE_NAME2 = updateResBean.golden_sample_name;
                            downloadBeans.Add(downloadBean);
                            //updateResBean.golden_sample_path = downloadBean.saveFolder + updateResBean.golden_sample_name;
                        }
                        if (updateResBean.model_url != null && updateResBean.model_url != "")
                        {
                            DownloadBean downloadBean = new DownloadBean();
                            downloadBean.url = GlobalValue.MODEL_DOWNLOAD_URL + updateResBean.function_id;
                            //downloadBean.url = GlobalValue.MODEL_NEWDOWNLOAD_URL + updateResBean.model_url;
                            downloadBean.saveFolder = GlobalValue.MODEL_FOLDER + updateResBean.function_id + "//";
                            downloadBean.fileName = getModelNameByURL(updateResBean.model_url);
                            downloadBean.MD5 = updateResBean.model_md5;
                            downloadBeans.Add(downloadBean);
                            updateResBean.model_local_path = downloadBean.saveFolder + downloadBean.fileName;
                        }

                    }

                    foreach (UpdateResBean x in updateResBeans2)
                    {
                        FunctionIdS.Add(x.function_id);
                    }

                    LogUtils.d("UpdateWorker_DoWork download start");
                    Task.Factory.StartNew(() => UpdateTb(loading_tv, "开始执行下载任务"),
                        new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
                    foreach (DownloadBean d in downloadBeans)
                    {
                        Task.Factory.StartNew(() => UpdateTb(loading_tv, "正在下载：" + d.fileName),
                        new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
                        httpRequestClient.downloadFile(d.url, d.saveFolder, d.fileName, d.MD5);
                    }
                    LogUtils.d("UpdateWorker_DoWork download complete");
                    Task.Factory.StartNew(() => UpdateTb(loading_tv, "下载完成"),
                        new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
                    e.Result = JsonConvert.SerializeObject(updateResBeans2).ToString();
                }
                else
                {
                    e.Result = "访问服务器错误：" + updateRes.code;
                }
            }
        }
        private void loadWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            loadingWait.Visibility = Visibility.Collapsed;
            notiryServerWorker.RunWorkerAsync();
            string result = (string)e.Result;
            if (result != null && result.StartsWith("["))
            {
                var arrdata = Newtonsoft.Json.Linq.JArray.Parse(result);
                List<UpdateResBean> updateResbean = arrdata.ToObject<List<UpdateResBean>>();
                refreshLammyRect(updateResbean, false);
            }
            else
            {
                if (MessageBox.Show("服务器连接失败：" + result) == MessageBoxResult.OK)
                {
                    Application.Current.Shutdown();//关闭
                }
            }
            isAlgEnvReady = true;

        }
        private void loadWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            //loadingWait.Visibility = Visibility.Visible;
            Task.Factory.StartNew(() => UpdateTb(loading_tv, "开始加载模型"),
    new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();

            foreach (UpdateResBean updateResBean in updateResBeans2)
            {
                updateResBeans.Add(updateResBean);
            }

            //算法加载model
            foreach (UpdateResBean updateResBean in updateResBeans)
            {
                if (updateResBean.golden_sample_name != null)
                {
                    //裁剪golden_sample对应函数的roi区域，并保存
                    BitmapUtils.crop(GlobalValue.GOLDEN_SAMPLE_FOLDER + updateResBean.golden_sample_name,
                        GlobalValue.GOLDEN_SAMPLE_FOLDER, updateResBean.function_id + ".jpg", updateResBean.rect);
                }
                string loadingText = "", addModelResult;
                if (updateResBean.function_name.Equals("点胶"))
                {
                    loadingText = "正在加载点胶模型";
                    string ModelRoot = GlobalValue.MODEL_FOLDER + updateResBean.function_id + "//" + "0";
                    addModelResult = CallAlg.getInstance().initialSegModel(ModelRoot);
                }
                else
                {
                    loadingText = "正在加载函数" + updateResBean.function_name + "的模型";
                    string ModelRoot = GlobalValue.MODEL_FOLDER + updateResBean.function_id + "//" + getModelNameByURL(updateResBean.model_url);
                    addModelResult = CallAlg.getInstance().addModel(updateResBean.function_id.ToString(), updateResBean.rect, ModelRoot);
                }
                Task.Factory.StartNew(() => UpdateTb(loading_tv, loadingText),
                    new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
                LogUtils.d("UpdateWorker_DoWork 拉取函数后，加载" + updateResBean.function_name + "模型结果：" + addModelResult);
                if (addModelResult.ToLower().Equals("false"))
                {
                    LogUtils.d("rpc已开启，add model失败，函数名: " + updateResBean.function_name);
                    updateResBean.result = GlobalValue.ALG_RESULT_LOAD_FAIL;
                    addModelFailedIds.Add(updateResBean.function_id);
                }
                else if (addModelResult.StartsWith("error"))
                {
                    if (MessageBox.Show("程序加载模型失败，请重启程序") == MessageBoxResult.OK)
                    {
                        Task.Factory.StartNew(() => ShutdownApplication(),
                     new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
                    }
                }

            }

            e.Result = JsonConvert.SerializeObject(updateResBeans).ToString();
        }

        //拍照一次
        private void updateWorkerOnce_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            loadingWait.Visibility = Visibility.Collapsed;
            notiryServerWorker.RunWorkerAsync();
            string result = (string)e.Result;
            if (result != null && result.StartsWith("["))
            {
                var arrdata = Newtonsoft.Json.Linq.JArray.Parse(result);
                List<UpdateResBean> updateResbean = arrdata.ToObject<List<UpdateResBean>>();
                refreshLammyRect(updateResbean, false);
            }
            else
            {
                //MessageBox.Show("拉取函数错误："+result);

                if (MessageBox.Show("服务器连接失败：" + result) == MessageBoxResult.OK)
                {
                    Application.Current.Shutdown();//关闭
                }
            }
            isAlgEnvReady = true;
        }
        private void updateWorkerOnce_DoWork(object sender, DoWorkEventArgs e)
        {
            addModelFailedIds.Clear();
            //List<Registration> ResListTemp = new List<Registration>();
            
           if (!NetworkUtils.IsNetworkConnected)
            {
                e.Result = "网络未连接";
                return;
            }
            if (getIniResBean == null)
                return;
            //请求更新配置
            Task.Factory.StartNew(() => UpdateTb(loading_tv, "拉取最新函数列表中..."),
                   new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();

            isAlgEnvReady = false;
            string response = "-1";
            HttpRequestClient httpRequestClient = new HttpRequestClient();
            httpRequestClient.SetFieldValue("project", getIniResBean.project);
            httpRequestClient.SetFieldValue("build", getIniResBean.build);
            httpRequestClient.SetFieldValue("sku", getIniResBean.sku);
            httpRequestClient.SetFieldValue("stationName", getIniResBean.phase);
            httpRequestClient.SetFieldValue("pcUuid", getIniResBean.pc_uuid);
            httpRequestClient.SetFieldValue("line", getIniResBean.line);
            httpRequestClient.SetFieldValue("version", getIniResBean.version);
            httpRequestClient.Upload(GlobalValue.UPDATE_IP, out response);
            LogUtils.d("UpdateWorker_DoWork response: " + response);
            e.Result = response;
            //解析配置信息，并添加下载任务
            UpdateRes updateRes = null;
            if (response.StartsWith("{"))
            {
                updateRes = JsonConvert.DeserializeObject<UpdateRes>(response);
                GlobalValue.HParam1 = updateRes.hParam;
                GlobalValue.ModelParam = updateRes.modelParam;
            }

            //release alg model
            if (updateResBeans != null)
            {
                foreach (UpdateResBean u in updateResBeans)
                {
                    if (u.function_id == 0)
                    {
                        CallAlg.getInstance().releaseSegModel();
                    }
                    else
                    {
                        CallAlg.getInstance().release_model(u.function_id.ToString());
                    }
                }
            }

            if (updateRes != null)
            {
                if (updateRes.code == 200)
                {
                    LogUtils.d("UpdateWorker_DoWork download start0");
                    List<DownloadBean> downloadBeans = new List<DownloadBean>();
                    updateResBeans = updateRes.data;
                    ConfigFileResBeans = updateRes.materials;//新增料号查询变量
                    int DJIndex = -1;
                    int Temp = -1;
                    //ResListTemp.Add(updateRes.registration);
                    //registration = ResListTemp;
                    foreach (UpdateResBean updateResBean in updateResBeans)
                    {
                        Temp++;
                        updateResBean.result = GlobalValue.ALG_RESULT_NO;
                        if (updateResBean.golden_sample_name != null && updateResBean.golden_sample_name != "")
                        {
                            DownloadBean downloadBean = new DownloadBean();
                            //downloadBean.url = GlobalValue.GOLDEN_SAMPLE_DOWNLOAD_URL + updateResBean.function_id;
                            downloadBean.url = GlobalValue.GOLDEN_SAMPLE_NEW_DOWNLOAD_URL + updateResBean.golden_sample_id;
                            downloadBean.saveFolder = GlobalValue.GOLDEN_SAMPLE_FOLDER;
                            downloadBean.fileName = updateResBean.golden_sample_name;
                            GlobalValue.GOLD_SAMPLE_NAME1 = updateResBean.golden_sample_name;
                            downloadBeans.Add(downloadBean);
                            //updateResBean.golden_sample_path = downloadBean.saveFolder + updateResBean.golden_sample;
                        }
                        if (updateResBean.model_url != null && updateResBean.model_url != "")
                        {
                            DownloadBean downloadBean = new DownloadBean();
                            if (updateResBean.function_id == 0)
                            {
                                DJIndex = Temp;
                                downloadBean.url = GlobalValue.MODEL_DOWNLOAD_URL + updateResBean.function_id + "&project=" + getIniResBean.project +
                                      "&build=" + getIniResBean.build + "&sku=" + getIniResBean.sku + "&station=" + getIniResBean.phase;
                            }
                            else
                            {
                                downloadBean.url = GlobalValue.MODEL_DOWNLOAD_URL + updateResBean.function_id;
                            }
                            //downloadBean.url = GlobalValue.MODEL_NEWDOWNLOAD_URL + updateResBean.model_url;
                            downloadBean.saveFolder = GlobalValue.MODEL_FOLDER + updateResBean.function_id + "//";
                            downloadBean.fileName = getModelNameByURL(updateResBean.model_url);
                            downloadBean.MD5 = updateResBean.model_md5;
                            downloadBeans.Add(downloadBean);
                            updateResBean.model_local_path = downloadBean.saveFolder + downloadBean.fileName;
                        }
                    }

                    if(DJIndex >= 0)
                    {
                        updateResBeans.RemoveAt(DJIndex);
                    }


                    if (CheckIsDj())
                    {
                        UpdateResBean b = new UpdateResBean();
                        b.function_id = 0;
                        b.function_name = "点胶";
                        b.result = GlobalValue.ALG_RESULT_NO;
                        updateResBeans.Insert(0, b);
                    }

                    LogUtils.d("UpdateWorker_DoWork download start");
                    Task.Factory.StartNew(() => UpdateTb(loading_tv, "开始执行下载任务"),
                        new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
                    foreach (DownloadBean d in downloadBeans)
                    {
                        Task.Factory.StartNew(() => UpdateTb(loading_tv, "正在下载：" + d.fileName),
                        new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
                        httpRequestClient.downloadFile(d.url, d.saveFolder, d.fileName, d.MD5);
                    }
                    LogUtils.d("UpdateWorker_DoWork download complete");
                    Task.Factory.StartNew(() => UpdateTb(loading_tv, "下载完成"),
                        new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();


                    e.Result = JsonConvert.SerializeObject(updateResBeans).ToString();

                    Task.Factory.StartNew(() => UpdateTb(loading_tv, "开始加载模型"),
                        new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();

                    //下载完成之后启动rpc
                    startDjListenerConnection();

                    Thread.Sleep(3000);

                    //算法加载model
                    foreach (UpdateResBean updateResBean in updateResBeans)
                    {
                        if (updateResBean.golden_sample_name != null)
                        {
                            if(updateResBean.function_id != 0)
                            {
                                //裁剪golden_sample对应函数的roi区域，并保存
                                BitmapUtils.crop(GlobalValue.GOLDEN_SAMPLE_FOLDER + updateResBean.golden_sample_name,
                                    GlobalValue.GOLDEN_SAMPLE_FOLDER, updateResBean.function_id + ".jpg", updateResBean.rect);
                            }
                        }
                        string loadingText = "", addModelResult;
                        if (updateResBean.function_id == 0)
                        {
                            loadingText = "正在加载点胶模型";
                            string ModelRoot = GlobalValue.MODEL_FOLDER + updateResBean.function_id/* + "//" + "0"*/;
                            addModelResult = CallAlg.getInstance().initialSegModel(ModelRoot);
                        }
                        else
                        {
                            loadingText = "正在加载函数" + updateResBean.function_name + "的模型";                         
                            string  ModelRoot =GlobalValue.MODEL_FOLDER + updateResBean.function_id /*+ "//" + getModelNameByURL(updateResBean.model_url)*/;
                            addModelResult = CallAlg.getInstance().addModel(updateResBean.function_id.ToString(), updateResBean.rect, ModelRoot);
                        }
                        Task.Factory.StartNew(() => UpdateTb(loading_tv, loadingText),
                            new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
                        LogUtils.d("UpdateWorker_DoWork 拉取函数后，加载" + updateResBean.function_name + "模型结果：" + addModelResult);
                        //if (addModelResult.ToLower().Equals("false"))
                        //{
                        //    LogUtils.d("rpc已开启，add model失败，函数名: " + updateResBean.function_name);
                        //    updateResBean.result = GlobalValue.ALG_RESULT_LOAD_FAIL;
                        //    addModelFailedIds.Add(updateResBean.function_id);
                        //}
                         if (addModelResult.StartsWith("error") || addModelResult.ToLower().Equals("false"))
                        {
                            string filePath = Path.Combine(GlobalValue.MODEL_FOLDER + updateResBean.function_id + "//", getModelNameByURL(updateResBean.model_url));//存放地址就是本地的upload下的同名的文件
                            File.Delete(filePath);

                            if (MessageBox.Show("程序加载模型失败，请重启程序") == MessageBoxResult.OK)
                            {
                                Task.Factory.StartNew(() => ShutdownApplication(),
                             new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
                            }
                        }
                    }

                    FunctionIdS.Clear();
                    foreach (UpdateResBean x in updateResBeans)
                    {
                        FunctionIdS.Add(x.function_id);
                    }
                }
                else
                {
                    e.Result = "访问服务器错误：" + updateRes.code;
                }
            }
        }


        //根据服务器返回的"model_url": "/python/function/model/75/model.m"，获取model的名称
        private string getModelNameByURL(string model_url)
        {
            string name = "";
            if (model_url != null && model_url != "")
            {
                string[] parts = model_url.Split('/');
                name = parts[parts.Length-1];
            }
            
            return name;
        }

        private void UpdateTb(TextBlock tb, string text)
        {
            tb.Text = text;
        }

        private void UpdateLabelText(Label lb1, string text)
        {
            lb1.Content = text;
        }

        private void UpdateConfig()
        {
            if (getIniResBean == null)
                return;
            txt1.Text = getIniResBean.project;
            txt2.Text = getIniResBean.build;
            txt3.Text = getIniResBean.sku;
            txt4.Text = getIniResBean.phase;
            txt_line.Text = getIniResBean.line;
            loadingWait.Visibility = Visibility.Visible;
            updateWorker.RunWorkerAsync();
        }

        private void MultCommitWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            string result = e.Result.ToString();
            //loadingWait.Visibility = Visibility.Collapsed;
            //MessageBox.Show(result);
            LogUtils.d("MultCommitWorker_RunWorkerCompleted : "+ result);

        }

        private void MultCommitWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            LogUtils.d("MultCommitWorker  start ");
            if (!NetworkUtils.IsNetworkConnected)
            {
                e.Result = "网络未连接";
                return;
            }
            string result = "";
            string[] imgList = FileUtils.getFileList(GlobalValue.COMMIT_FAILED_FOLDER);
            if (imgList.Length > 0)
            {
                int successCount = 0;
                foreach (string img in imgList)
                {
                    string response = "-1";

                    byte[] fileBytes = readImage(img);
                    if (fileBytes == null)
                        continue;
                    //string strTemp = Read_UploadFailFile(GlobalValue.COMMIT_FAILED_TESTDATA, System.IO.Path.GetFileName(img));
                    string strTemp = MySqlLite.QueryTableData("TEST", System.IO.Path.GetFileName(img));
                    HttpRequestClient httpRequestClient = new HttpRequestClient();
                    httpRequestClient.SetFieldValue("originImage", System.IO.Path.GetFileName(img), "application/octet-stream", fileBytes);
                    httpRequestClient.SetFieldValue("result", strTemp);
                    httpRequestClient.Upload(GlobalValue.COMMIT_IP, out response);
                    UploadRes uploadRes = null;
                    if (response.StartsWith("{"))
                    {
                        uploadRes = JsonConvert.DeserializeObject<UploadRes>(response);
                    }

                    if (uploadRes != null)
                    {
                        if (uploadRes.code == 200 || uploadRes.code == 403)
                        {
                            FileUtils.deleteFile(img);
                            successCount++;
                        }
                    }
                }
                result = "共检测到" + imgList.Length + "张上传失败图片，已重新成功上传" + successCount + "张";
            }
            else
            {
                result = "未检测到上传失败的图片";
            }

            e.Result = result;

        }

        private void CommitWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            string result = e.Result.ToString();
            string imageName = FileUtils.getFileNameByPath(imagePath);
            loadingWait.Visibility = Visibility.Collapsed;
            if (!SN.IsEnabled)
            {
                SN.IsEnabled = true;
                //Clear textbox content and get in focus again
                //SN.Clear();
                SN.Focus();
            }
            LogUtils.d("CommitWorker_RunWorkerCompleted response: " + e.Result);
            UploadRes uploadRes = null;
            if (result.StartsWith("{"))
            {
                uploadRes = JsonConvert.DeserializeObject<UploadRes>(result);
            }

            if (uploadRes != null)
            {
                if (uploadRes.code != 200)
                {                    
                    if (File.Exists(imagePath))
                    {
                        if (imageName != null && ImageUtils.isImageNameCorrect(imageName) && uploadRes.code != 403)
                        {
                            FileUtils.copyFiles(imagePath, GlobalValue.COMMIT_FAILED_FOLDER);
                        }
                        MessageBox.Show("上传失败，错误码：" + uploadRes.code + " msg:"+ uploadRes.msg);
                    }
                    else
                    {
                        MessageBox.Show("图片不存在，请确认是否保存");
                    }
                }
                else
                {
                    if (File.Exists(imagePath))
                        FileUtils.deleteFile(imagePath);

                    //  commitWorkerwithResult.RunWorkerAsync();//上传结果数据
                }

                FileUtils.DelectDir(GlobalValue.ORIGIN_IMG_FOLDER);
            }
            else
            {
                if (imageName != null && ImageUtils.isImageNameCorrect(imageName))
                {
                    FileUtils.copyFiles(imagePath, GlobalValue.COMMIT_FAILED_FOLDER);
                    FileUtils.DelectDir(GlobalValue.ORIGIN_IMG_FOLDER);
                }
                MessageBox.Show("提交错误："+result);
            }
        }
        private void CommitWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Task.Factory.StartNew(() => UpdateTb(loading_tv, "上传图片中..."),
                 new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
            if (!NetworkUtils.IsNetworkConnected)
            {
                e.Result = "网络未连接";
                return;
            }
            string response = "-1";
            byte[] fileBytes = readImage(imagePath);
            if (fileBytes != null)
            {
                HttpRequestClient httpRequestClient = new HttpRequestClient();
                httpRequestClient.SetFieldValue("img", System.IO.Path.GetFileName(imagePath), "application/octet-stream", fileBytes);
                httpRequestClient.Upload(GlobalValue.COMMIT_IP, out response);
                e.Result = response;
            }
            else
            {
                e.Result = "未找到图片，请检查图片是否保存";
            }
        }

        private void CommitWorkerwithResult_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                //string imageName = FileUtils.getFileNameByPath(imagePath);

                string result = e.Result.ToString();
               
                //  loadingWait.Visibility = Visibility.Collapsed;
                LogUtils.d("CommitWorkerwithResult_RunWorkerCompleted response: " + e.Result);
                UploadRes uploadRes = null;
                if (result.StartsWith("{"))
                {
                    uploadRes = JsonConvert.DeserializeObject<UploadRes>(result);
                }

                if (uploadRes != null)
                {
                    if (GlobalValue.IsNTFMODE)    //手动修改模式
                    {
                        if (uploadRes.code == 200)
                        {
                            if (WriteMESwithResult(m_str_DUTSN, GlobalValue.MES_STATIONTYPE, m_bFinalResult))
                            {
                                if(m_bFinalResult)
                                    DisPlayLog(tbx_mes, "检测通过",false);
                                else
                                    DisPlayLog(tbx_mes, "检测失败",false);
                            }
                            else
                            {
                                DisPlayLog(tbx_mes, "MES写入失败",false);
                            }
                            MessageBox.Show("NTF结果上传成功！");
                        }
                        else
                        {
                            MessageBox.Show("修改人员无权限，请重试！");
                            return;
                        }
                    }
                    else
                    {
                        if (m_str_DUTSN != "CodeError" && m_str_DUTSN != "NoCode" && m_str_DUTSN != "")
                        {
                            //上传给mes系统参数
                            if (WriteMESwithResult(m_str_DUTSN, GlobalValue.MES_STATIONTYPE, m_bFinalResult))
                            {
                                if (m_bFinalResult)
                                    DisPlayLog(tbx_mes, "检测通过",false);
                                else
                                    DisPlayLog(tbx_mes, "检测失败",false);
                            }
                            else
                            {
                                DisPlayLog(tbx_mes, "MES写入失败",false);                              
                            }
                        }

                        if (uploadRes.code != 200)
                        {
                            if (File.Exists(imagePath))
                            {
                                if (GlobalValue.IMAGENAME != null && ImageUtils.isImageNameCorrect(GlobalValue.IMAGENAME) && uploadRes.code != 403)
                                {
                                    FileUtils.copyFiles(imagePath, GlobalValue.COMMIT_FAILED_FOLDER); 
                                }
                            }
                            DisPlayLog(tbx_syslog, "上传失败，错误码：" + uploadRes.code + " msg:" + uploadRes.msg,true);
                        }
                        else
                        {
                            //if (!m_bFinalResult)
                            //{
                            //    DisPlayLog(tbx_syslog, "准备显示图片");
                            //    if (CheckIsDj())
                            //    {
                            //        FileUtils.deleteFile(GlobalValue.DJ_MASK_PATH + GlobalValue.IMAGENAME.Split('.')[0] + ".png");
                            //        openShowImageWindow(GlobalValue.DJ_COMPOSEMASK_IMG_PATH, djRectList, true);
                            //    }
                            //    else
                            //    {
                            //        openShowImageWindow1(GlobalValue.SHOWIMAGE, funRectList, true);
                            //    }
                            //    m_str_DUTSN = "";
                            //}
                            DisPlayLog(tbx_syslog, "准备删除图片",true);
                            if (CheckIsDj())
                            {
                                FileUtils.deleteFile(GlobalValue.DJ_MASK_PATH + GlobalValue.IMAGENAME.Split('.')[0] + ".png");
                            }
                            //m_str_DUTSN = "";
                            string failPath = GlobalValue.COMMIT_FAILED_FOLDER + FileUtils.getFileNameByPath(imagePath);
                            FileUtils.deleteFile(failPath);
                            FileUtils.deleteFile(imagePath);
                            
                            DisPlayLog(tbx_syslog, "流程结束",true);

                            if (DateTime.Now.Day == 1)
                            {
                                int month = DateTime.Now.Month;
                                int year = DateTime.Now.Year;
                                if (File.Exists(GlobalValue.DATA_BASE_FOLDER + year.ToString() + month.ToString() + "01" + "barcode.db"))
                                    return;

                                string DatabaseFilePath = AppDomain.CurrentDomain.BaseDirectory + "barcode.db";
                                ////删除文件夹
                                
                                FileUtils.copyFiles(DatabaseFilePath, GlobalValue.DATA_BASE_FOLDER);
                                bool renameResult = FileUtils.renameFile(GlobalValue.DATA_BASE_FOLDER + "barcode.db", GlobalValue.DATA_BASE_FOLDER + year.ToString() + month.ToString() + "01" + "barcode.db");
                                if (renameResult)
                                {
                                    MySqlLite.CloseDb();
                                    FileUtils.deleteFile(DatabaseFilePath);
                                    MySqlLite.OpenDb();
                                    DisPlayLog(tbx_syslog, "数据库替换完成", true);
                                }
                                
                            }
                        }
                    }
                }
                else
                {
                    if (GlobalValue.IMAGENAME != null && ImageUtils.isImageNameCorrect(GlobalValue.IMAGENAME))
                    {
                        //提交失败的时候删除原来的文件夹
                        FileUtils.copyFiles(imagePath, GlobalValue.COMMIT_FAILED_FOLDER);
                        FileUtils.deleteFile(GlobalValue.DJ_MASK_PATH + GlobalValue.IMAGENAME.Split('.')[0] + ".png");
                    }
                }

            }
            catch(Exception ex)
            {
                DisPlayLog(tbx_syslog, ex.Message,true);
            }
          
        }
        private void CommitWorkerwithResult_DoWork(object sender, DoWorkEventArgs e)
        {
            //Task.Factory.StartNew(() => UpdateTb(loading_tv, "上传结果中..."),
            //     new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
            //setStep(EStepSelectWork.STEP_AUTO_SHOW_FAILWINDOW);

            if (!NetworkUtils.IsNetworkConnected)
            {
                e.Result = "网络未连接";
                return;
            }

            UploadInferenceResult ir = (UploadInferenceResult)e.Argument;
            HttpRequestClient httpRequestClient = new HttpRequestClient();
            string response = "-1";
            string newMaskPath = "";

            //////写入测试数据
            //string strtemp = JsonConvert.SerializeObject(ir).ToString();
            ////Write_UploadFailFile(GlobalValue.COMMIT_FAILED_TESTDATA, GlobalValue.IMAGENAME, strtemp);
            //if(MySqlLite.InsertTableData("TEST", GlobalValue.IMAGENAME, strtemp) == 0)
            //{
            //    UpdateSysDisplayLog("数据库插入数据成功");
            //}
            //else
            //{
            //    UpdateSysDisplayLog("数据库插入数据失败");
            //}

            if (GlobalValue.IsNTFMODE)    //手动修改模式
            {
                httpRequestClient.SetFieldValue("userName", GlobalValue.USERNAME);
                httpRequestClient.SetFieldValue("passWord", GlobalValue.PASSWORD);
                httpRequestClient.SetFieldValue("imageName", GlobalValue.IMAGENAME);
                string s = JsonConvert.SerializeObject(ir).ToString();
                httpRequestClient.SetFieldValue("result", s);
                httpRequestClient.Upload(GlobalValue.COMMIT_IP_PASSFAIL, out response);
                e.Result = response;
                LogUtils.d("CommitWorkerwithResult_DoWork response: " + response);
            }
            else  //自动运行模式
            {
                //if (/*GlobalValue.CONFIG_STATION == "MainBoard"*/ m_str_DUTSN.Length == 10)
                //{
                //    httpRequestClient.SetFieldValue("rightBoardSn", GlobalValue.MES_RightBoardNUMBER);
                //    httpRequestClient.SetFieldValue("leftBoardSn", GlobalValue.MES_LeftBoardNUMBER);
                //    httpRequestClient.SetFieldValue("hingeHousingSn", GlobalValue.MES_HingeHousingNUMBER);
                //    httpRequestClient.SetFieldValue("l5Sn", GlobalValue.MES_L5NUMBER);
                //    httpRequestClient.SetFieldValue("cLILensSn", GlobalValue.MES_CLILensNUMBER);
                //}

                //增加线体  uuid
                httpRequestClient.SetFieldValue("line", GlobalValue.CONFIG_LINE);
                httpRequestClient.SetFieldValue("pcUuid", DeviceUtil.UUID());

                byte[] oriImgBytes  = readImage(ir.IR.imgPath);
                //点胶模式
                if (CheckIsDj())
                {
                    //string imageName = FileUtils.getFileNameByPath(imagePath);
                    newMaskPath = GlobalValue.DJ_MASK_PATH + GlobalValue.IMAGENAME.Split('.')[0] + ".png";
                    bool renameResult = FileUtils.renameFile(GlobalValue.DJ_MASK_IMG_PATH, newMaskPath);
                    if (!renameResult)
                    {
                        e.Result = "获取mask图片失败";
                        return;
                    }
                    byte[] maskImgBytes  = readImage(newMaskPath);
                    if (oriImgBytes != null && maskImgBytes != null)
                    {

                        httpRequestClient.SetFieldValue("originImage", System.IO.Path.GetFileName(ir.IR.imgPath), "application/octet-stream", oriImgBytes);
                        httpRequestClient.SetFieldValue("maskImage", System.IO.Path.GetFileName(newMaskPath), "application/octet-stream", maskImgBytes);
                        httpRequestClient.SetFieldValue("userName", "lishizhen");
                        httpRequestClient.SetFieldValue("passWord", "LISHIZHEN");
                        httpRequestClient.SetFieldValue("imageName", GlobalValue.IMAGENAME);
                        string s = JsonConvert.SerializeObject(ir).ToString();
                        httpRequestClient.SetFieldValue("result", s);
                        httpRequestClient.Upload(GlobalValue.COMMIT_IP_PASSFAIL, out response);
                        e.Result = response;
                        LogUtils.d("CommitWorkerwithResult_DoWork response: " + response);
                    }
                    else
                    {
                        e.Result = "未找到图片，请检查图片是否保存";
                    }
                }
                else //非点胶模式
                {
                    httpRequestClient.SetFieldValue("originImage", System.IO.Path.GetFileName(ir.IR.imgPath), "application/octet-stream", oriImgBytes);
                    httpRequestClient.SetFieldValue("userName", "lishizhen");
                    httpRequestClient.SetFieldValue("passWord", "LISHIZHEN");
                    httpRequestClient.SetFieldValue("imageName", GlobalValue.IMAGENAME);
                    string s = JsonConvert.SerializeObject(ir).ToString();
                    httpRequestClient.SetFieldValue("result", s);
                    httpRequestClient.Upload(GlobalValue.COMMIT_IP_PASSFAIL, out response);
                    e.Result = response;
                    LogUtils.d("CommitWorkerwithResult_DoWork response: " + response);
                }                           
            }


                   
        }

        private void bnEnum_Click(object sender, EventArgs e)
        {
            DeviceListAcq();
        }

        private void DeviceListAcq()
        {
            int nRet;
            // ch:创建设备列表 en:Create Device List
            System.GC.Collect();
            cbDeviceList.Items.Clear();
            nRet = MyCamera.MV_CC_EnumDevices_NET(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref  HIKCamera.m_pDeviceList);
            if (0 != nRet)
            {
                ShowErrorMsg("Enumerate devices fail!", 0);
                return;
            }

            // ch:在窗体列表中显示设备名 | en:Display device name in the form list
            for (int i = 0; i < HIKCamera.m_pDeviceList.nDeviceNum; i++)
            {
                MyCamera.MV_CC_DEVICE_INFO device = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(HIKCamera.m_pDeviceList.pDeviceInfo[i], typeof(MyCamera.MV_CC_DEVICE_INFO));
                if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(device.SpecialInfo.stGigEInfo, 0);
                    MyCamera.MV_GIGE_DEVICE_INFO gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO)Marshal.PtrToStructure(buffer, typeof(MyCamera.MV_GIGE_DEVICE_INFO));
                    if (gigeInfo.chUserDefinedName != "")
                    {
                        cbDeviceList.Items.Add("GigE: " + gigeInfo.chUserDefinedName + " (" + gigeInfo.chSerialNumber + ")");
                    }
                    else
                    {
                        cbDeviceList.Items.Add("GigE: " + gigeInfo.chManufacturerName + " " + gigeInfo.chModelName + " (" + gigeInfo.chSerialNumber + ")");
                    }
                }
                else if (device.nTLayerType == MyCamera.MV_USB_DEVICE)
                {
                    IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(device.SpecialInfo.stUsb3VInfo, 0);
                    MyCamera.MV_USB3_DEVICE_INFO usbInfo = (MyCamera.MV_USB3_DEVICE_INFO)Marshal.PtrToStructure(buffer, typeof(MyCamera.MV_USB3_DEVICE_INFO));
                    if (usbInfo.chUserDefinedName != "")
                    {
                        cbDeviceList.Items.Add("USB: " + usbInfo.chUserDefinedName + " (" + usbInfo.chSerialNumber + ")");
                    }
                    else
                    {
                        cbDeviceList.Items.Add("USB: " + usbInfo.chManufacturerName + " " + usbInfo.chModelName + " (" + usbInfo.chSerialNumber + ")");
                    }
                }
            }

            // ch:选择第一项 | en:Select the first item
            if (HIKCamera.m_pDeviceList.nDeviceNum != 0)
            {
                cbDeviceList.SelectedIndex = 0;
            }
        }


        private void InitComboxList()
        {
            cbNegativeSample.Items.Clear();
            cbNegativeSample.Items.Add("正常模式");
            cbNegativeSample.Items.Add("正样本");
            cbNegativeSample.Items.Add("负样本");
            cbNegativeSample.SelectedIndex = 0;

            cbPLCDRegister.Items.Clear();
            cbPLCDRegister.Items.Add("D10");
            cbPLCDRegister.Items.Add("D20");
            cbPLCDRegister.Items.Add("D30");
            cbPLCDRegister.Items.Add("D40");
            cbPLCDRegister.Items.Add("D700");
            cbPLCDRegister.Items.Add("D702");
            cbPLCDRegister.Items.Add("D704");
            cbPLCDRegister.SelectedIndex = 0;
        }


        private void InitPlcDirList()
        {
            cbPlcDirList.Items.Clear();
            cbPlcDirList.Items.Add("左进右出");
            cbPlcDirList.Items.Add("右进左出");
            cbPlcDirList.Items.Add("左进左出");
            cbPlcDirList.Items.Add("右进右出");        

            cbPlcDirList.SelectedIndex = int.Parse(GlobalValue.PLC_COMBOBOX_VALUE.Trim());
        }

        private void InitManualCodeList()
        {
            cbManulCode.Items.Clear();
            cbManulCode.Items.Add("手动扫码");
            cbManulCode.Items.Add("相机扫码");

            cbManulCode.SelectedIndex = int.Parse(GlobalValue.QRCodeInOrder.Trim());
        }


        // ch:显示错误信息 | en:Show error message
        private void ShowErrorMsg(string csMessage, int nErrorNum)
        {
            string errorMsg;
            if (nErrorNum == 0)
            {
                errorMsg = csMessage;
            }
            else
            {
                errorMsg = csMessage + ": Error =" + String.Format("{0:X}", nErrorNum);
            }

            switch (nErrorNum)
            {
                case MyCamera.MV_E_HANDLE: errorMsg += " Error or invalid handle "; break;
                case MyCamera.MV_E_SUPPORT: errorMsg += " Not supported function "; break;
                case MyCamera.MV_E_BUFOVER: errorMsg += " Cache is full "; break;
                case MyCamera.MV_E_CALLORDER: errorMsg += " Function calling order error "; break;
                case MyCamera.MV_E_PARAMETER: errorMsg += " Incorrect parameter "; break;
                case MyCamera.MV_E_RESOURCE: errorMsg += " Applying resource failed "; break;
                case MyCamera.MV_E_NODATA: errorMsg += " No data "; break;
                case MyCamera.MV_E_PRECONDITION: errorMsg += " Precondition error, or running environment changed "; break;
                case MyCamera.MV_E_VERSION: errorMsg += " Version mismatches "; break;
                case MyCamera.MV_E_NOENOUGH_BUF: errorMsg += " Insufficient memory "; break;
                case MyCamera.MV_E_UNKNOW: errorMsg += " Unknown error "; break;
                case MyCamera.MV_E_GC_GENERIC: errorMsg += " General error "; break;
                case MyCamera.MV_E_GC_ACCESS: errorMsg += " Node accessing condition error "; break;
                case MyCamera.MV_E_ACCESS_DENIED: errorMsg += " No permission "; break;
                case MyCamera.MV_E_BUSY: errorMsg += " Device is busy, or network disconnected "; break;
                case MyCamera.MV_E_NETER: errorMsg += " Network error "; break;
            }

            MessageBox.Show(errorMsg, "PROMPT");
        }

        private Boolean IsMonoData(MyCamera.MvGvspPixelType enGvspPixelType)
        {
            switch (enGvspPixelType)
            {
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono10:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono10_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono12:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono12_Packed:
                    return true;

                default:
                    return false;
            }
        }

        /************************************************************************
 *  @fn     IsColorData()
 *  @brief  判断是否是彩色数据
 *  @param  enGvspPixelType         [IN]           像素格式
 *  @return 成功，返回0；错误，返回-1 
 ************************************************************************/
        private Boolean IsColorData(MyCamera.MvGvspPixelType enGvspPixelType)
        {
            switch (enGvspPixelType)
            {
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGR8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerRG8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGB8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerBG8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGR10:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerRG10:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGB10:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerBG10:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGR12:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerRG12:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGB12:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerBG12:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGR10_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerRG10_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGB10_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerBG10_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGR12_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerRG12_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGB12_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerBG12_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_RGB8_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_YUV422_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_YUV422_YUYV_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_YCBCR411_8_CBYYCRYY:
                    return true;

                default:
                    return false;
            }
        }

        private void SetCtrlWhenOpen()
        {
            bnOpen.IsEnabled = false;

            bnClose.IsEnabled = true;
           // bnSaveJpg.IsEnabled = true;

            //bnStartGrab.IsEnabled = true;
            //bnStopGrab.IsEnabled = false;
            //bnContinuesMode.IsEnabled = true;
            //bnContinuesMode.Checked = true;
            //bnTriggerMode.IsEnabled = true;
            //cbSoftTrigger.IsEnabled = false;
            //bnTriggerExec.IsEnabled = false;

        }


        private void bnOpen_Click(object sender, EventArgs e)
        {
            OpenCamera();
        }

        public void InitCamera()
        {
            m_pHikCamera = new HIKCamera();
        }

        public  bool SetGain(double value)
        {
            try
            {
                m_pHikCamera.m_pCameraPara.f_Gain = float.Parse(value.ToString());
                m_pHikCamera.WriteHIKPara(m_pHikCamera.m_pCameraPara);
                return true;
            }
            catch (Exception)
            {

                return false;
            }

        }
        public  void GetGain(out double value)
        {
            try
            {
                float f1 = m_pHikCamera.GetHIKGainPara();
                value = double.Parse(f1.ToString());
            }
            catch (Exception)
            {
                value = -1;
            }
        }
        public  bool SetExposure(double value)
        {
            try
            {
                m_pHikCamera.m_pCameraPara.f_Exposure = float.Parse(value.ToString());
                m_pHikCamera.WriteHIKPara(m_pHikCamera.m_pCameraPara);
                return true;
            }
            catch (Exception)
            {
                //camera.Parameters[PLCamera.ExposureTime].TrySetValue(100);
                return false;
            }
        }
        public  void GetExposure(out double value)
        {
            try
            {
                float f1 = m_pHikCamera.GetHIKExposurePara();
                value = double.Parse(f1.ToString());
            }
            catch (Exception)
            {
                value = -1;
            }

        }

        //触发取图
        public void StreamGrabber_ImageGrabbed(byte[] pData, int Datalength)
        {
            if (!Directory.Exists(GlobalValue.ORIGIN_IMG_FOLDER))
            {
                Directory.CreateDirectory(GlobalValue.ORIGIN_IMG_FOLDER);
            }
            string ImageFilePath = Directory.GetCurrentDirectory() + "\\checkImages\\" + "Snap" + ".jpg";

            if(GlobalValue.IsJPEGZIPED == "1")
            {
                using (System.Drawing.Image SaveImage = ImageUtils.byteArrayToImage(pData))
                {
                    ImageUtils.GetPicThumbnail(SaveImage, ImageFilePath, 5472, 3648, 95);
                }
            }
            else
            {
                FileStream file = new FileStream(ImageFilePath, FileMode.Create, FileAccess.Write);
                file.Write(pData, 0, Datalength);
                file.Close();
            }

            //更新图片
            UpdateImageSource(ImageFilePath);

            //获取待保存图像
            m_BitmapImageForSave = getBitmapImage(ImageFilePath);

            ////裁剪图片,获取待解码图像
            System.Drawing.Rectangle Rect1 = new System.Drawing.Rectangle(GlobalValue.ROI_X, GlobalValue.ROI_Y, GlobalValue.ROI_WIDTH, GlobalValue.ROI_HEIGHT);
            m_BitmapForDecode = QRDecodeUtils.CropBitmap(m_BitmapImageForSave, Rect1);

            m_bIsSvaebmpSuccessed = false;
        }

        private void OpenCamera()
        {
            int nRet = -1;
            //Check if all the image information is filled
            //if (txt1.Text == "" || txt2.Text == "" || txt3.Text == "" || txt4.Text == "")
            //{
            //    MessageBox.Show("请填写完整项目名称、编号和工站等！");
            //    return;
            //}

            //初始化相机
            if (0 != m_pHikCamera.InitHIKCamera(GlobalValue.CAMERASN))
            {
                return;
            }

            //打开取流模式
            if (0 != m_pHikCamera.OpenGrap(true))
            {
                return ;
            }

            //设置白平衡 gamma
            m_pHikCamera.SetStaticCameraPara();


            //鼠标光标foucs到SN textbox区域
            SN.Focus();


            // ch:显示 | en:Display
            nRet = m_pHikCamera.m_pMyCamera.Cam_Info.MV_CC_Display_NET(wfPictureBox.Handle);
            wfPictureBox.Visible = true;

            //设置曝光增益
            SetExposure(double.Parse(GlobalValue.CAMERAEXPOSURE));
            SetGain(double.Parse(GlobalValue.CAMERAGAIN));

            SetCtrlWhenOpen();

            m_pHikCamera.ShowImage += new DelegateShowImage(StreamGrabber_ImageGrabbed);

        }


        private void SetCtrlWhenClose()
        {
            bnOpen.IsEnabled = true;

            bnClose.IsEnabled = false;
            bnSaveJpg.IsEnabled = false;
            //bnStartGrab.IsEnabled = false;
            //bnStopGrab.IsEnabled = false;
            //bnContinuesMode.Checked = false;
            //bnTriggerMode.IsEnabled = false;
            //cbSoftTrigger.IsEnabled = false;
            //bnTriggerExec.IsEnabled = false;

            //bnSaveBmp.IsEnabled = false;
            //bnSaveJpg.IsEnabled = false;

        }


        private void bnClose_Click(object sender, EventArgs e)
        {
            CloseCamera();

            wfPictureBox.Visible = false;
          
            // ch:控件操作 | en:Control Operation
            SetCtrlWhenClose();
        }

        public void CloseCamera()
        {
            if(m_pHikCamera.m_bEnabled)
            {
                m_pHikCamera.ShowImage -= new DelegateShowImage(StreamGrabber_ImageGrabbed);
                if (0 != m_pHikCamera.OpenGrap(false))
                {
                    return;
                }
                m_pHikCamera.CloseHIKDevice();
                m_pHikCamera.m_bEnabled = false;
            }
        }

        private void SetCtrlWhenStopGrab()
        {
            //bnStartGrab.IsEnabled = true;
            //bnStopGrab.IsEnabled = false;

            //bnTriggerExec.IsEnabled = false;


            bnSaveJpg.IsEnabled = false;
            //bnSaveJpg.IsEnabled = false;
        }


        public static string imagePath;
        public static string ManualImagePath;
        public static bool ManualImagePathValid = false;
        //public static string imagePathTemp;

        private void bnSaveJpg_Click(object sender, EventArgs e)
        {
            //初始化函数列表状态为init
            if (updateResBeans != null)
            {
                refreshLammyRect(updateResBeans, true);
            }

            ////Check if SN is blank 
            //if (SN.Text == "")
            //{
            //    MessageBox.Show("请扫码获取序列号！");
            //    return;
            //}

            ////Check if SN's formatting is correct //8S 码或者10位序列号
            //if (SN.Text.Length == 10) { }
            //else if (SN.Text.Length > 50 && SN.Text.Length < 56 && SN.Text.IndexOf("8") == 0 && SN.Text.IndexOf("S") == 1) { }
            //else { MessageBox.Show("扫描的二维码格式不正确，请重新扫描！"); return; }

            ////set SN textbox disable
            //SN.IsEnabled = false;
            //string t = SN.Text;

            ////Clear textbox content and get in focus again
            //SN.Clear();
            //SN.Focus();

            //Upload image to the server, 保存成功后，执行算法
            if (isChanageResultMode)
            {
                isChanageResultMode = false;
                btn_modify.Content = "修改";
                updateResultDisplay();
                bnSaveJpg.IsEnabled = true;
                bnOpen.IsEnabled = isOpenCameraEnabledWhenModifying;
                bnClose.IsEnabled = isCloseCameraEnabledWhenModifying;
            }
            ////refreshLammyRect(updateResBeans, true);
            ////if (!exeAlgInMainThread(false))
            ////{
            ////    loadingWait.Visibility = Visibility.Collapsed;
            ////}

            ////打开光源
            //if (m_pSerialController != null && m_pSerialController.m_bValid)
            //{
            //    OpenBarLight(255);
            //    setStep(EStepSelectWork.STEP_AUTO_VS_INFORM_CAM);
            //}
            m_bManual = true;
        }


        public void SaveBitmapWithQRcode(string Sn_num,bool IsThread)
        {
            //Save as JPEG image
            string tttt = getIniResBean.phase;
            ////根据LDA来判断第一站部件的正反面
            //bool isContain = Sn_num.Contains("LDA");
            //if (getIniResBean.phase == "LDA1" && isContain)
            //{
            //    tttt = "LDA2";
            //}
            //if (getIniResBean.phase == "LDA2" && !isContain)

            //{
            //    tttt = "LDA1";
            //}

            if(GlobalValue.ISRESNAP == "1")
            {
                if (phase == 1 || phase == 0)
                {
                    tttt = tttt + "1";
                }
                else
                {
                    tttt = tttt + "2";
                }
            }

            if (!Directory.Exists(GlobalValue.ORIGIN_IMG_FOLDER))
            {
                Directory.CreateDirectory(GlobalValue.ORIGIN_IMG_FOLDER);
            }

            string filename = string.Concat(getIniResBean.project, "_", getIniResBean.build, "_", getIniResBean.sku, "_", tttt + "_" + Sn_num);
            imagePath = Directory.GetCurrentDirectory() + "\\checkImages\\" + filename + "_" + DateTime.Now.ToString("yyMMddHHmmss") + ".jpg";

            //手动模式
            if(ManualImagePathValid && ManualImagePath!=null && File.Exists(ManualImagePath))
            {
                bool renameResult = FileUtils.renameFile(ManualImagePath, imagePath);
                if (renameResult)
                {
                    FileUtils.copyFiles(imagePath, GlobalValue.COMMIT_FAILED_FOLDER); //解决网络太慢导致的线程无法同时上传多个图片导致的漏传问题
                }
                ManualImagePathValid = false;
            }
            else
            {
                if (!Directory.Exists(GlobalValue.ORIGIN_IMG_FOLDER))
                {
                    Directory.CreateDirectory(GlobalValue.ORIGIN_IMG_FOLDER);
                }
                string ImageFilePath = Directory.GetCurrentDirectory() + "\\checkImages\\" + "Snap" + ".jpg";

                bool renameResult = FileUtils.renameFile(ImageFilePath, imagePath);
                if (renameResult)
                {
                    FileUtils.copyFiles(imagePath, GlobalValue.COMMIT_FAILED_FOLDER); //解决网络太慢导致的线程无法同时上传多个图片导致的漏传问题
                }               
            }

            if (IsThread)
                Updatetxt5(filename);
            else
            {
                txt5.Text = filename;
            }
        }


        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            //if ((e.KeyboardDevice.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.Enter:
                        //将要执行代码
                        bnSaveJpg.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        // MessageBox.Show("Saved Image!");

                        break;
                }
            }
        }


        private void BnClearSN_Click(object sender, RoutedEventArgs e)
        {
                SN.Clear();
                SN.IsEnabled = true;
                SN.Focus();
            //openShowImageWindow(GlobalValue.SHOWIMAGE, funRectList, false);
        }

        private BackgroundWorker commitWorker = new BackgroundWorker();
        private BackgroundWorker multCommitWorker = new BackgroundWorker();
        private BackgroundWorker getIniWorker = new BackgroundWorker(); //从服务端拉取配置参数
        private BackgroundWorker updateWorker = new BackgroundWorker(); //从服务端拉取对应配置的函数
        private BackgroundWorker LoadWorker = new BackgroundWorker(); //从服务端拉取对应配置的函数
        private BackgroundWorker updateWorkerOnce= new BackgroundWorker(); //从服务端拉取对应配置的函数
        private BackgroundWorker notiryServerWorker = new BackgroundWorker(); //通知服务器已更新完成
        //private BackgroundWorker algWorker = new BackgroundWorker(); //执行算法
        private BackgroundWorker commitDjWorker = new BackgroundWorker(); //上传点胶结果到服务器
        private BackgroundWorker commitWorkerwithResult = new BackgroundWorker(); //上传点胶结果到服务器

        /************************ 图片地址***********************************/
        // 检测图地址
        //string rootPath = Directory.GetCurrentDirectory() + "\\checkImages";
        // imagePath = Directory.GetCurrentDirectory() + "\\checkImages" + "\\190626142003.bmp";
        //string serialNumberStr = "Set the test serial number:";
        // pass failed 显示的图的地址
        string resultPath = Directory.GetCurrentDirectory() + "\\resultImage";
        string resultPass = Directory.GetCurrentDirectory() + "\\resultImage" + "\\pass.jpg";
        string resultFailed = Directory.GetCurrentDirectory() + "\\resultImage" + "\\fail.jpg";
        string resultCodeError = Directory.GetCurrentDirectory() + "\\resultImage" + "\\CodeError.jpg";
        string resultMesNumError = Directory.GetCurrentDirectory() + "\\resultImage" + "\\mesnum_error.jpg";
        string resultInfo = Directory.GetCurrentDirectory() + "\\resultImage" + "\\Info.jpg";
        string GoldenSurfPath= Directory.GetCurrentDirectory() + "\\zxingtestfail" + "\\GoldenSample.jpg";


        BitmapImage resultPassImage, resultfailedImage, CodeErrorImage, MesNumError, InfoImage, GoldenSurfImage;


        /************************ 图片 窗口显示图片大小配置***********************************/
        private static double checkImageWidth = 5472;
        private static double checkImageHeight = 3648;


        //private double checkImageSmallWidth = checkImageWidth / 4 / 4;    //342; 
        //private double checkImageSmallHeight = checkImageHeight / 4 / 4;    //228;

        //private double checkImageSmallWidth = checkImageWidth / 6 / 3;    //320
        //private double checkImageSmallHeight = checkImageHeight / 4 / 3;    //304;

        //private double checkImageLargeWidth = checkImageWidth / 6 * 0.75;    //684; 
        //private double checkImageLargeHeight = checkImageHeight / 4 * 0.75;    //684;

        private double checkImageSmallWidth = 400;    //320
        private double checkImageSmallHeight = 284;    //304;

        private double checkImageLargeWidth = 800;    //684; 
        private double checkImageLargeHeight = 564;    //684;

        private static double initRatio = 1;
        private double largeRatio = initRatio;

        ////配准图像缩放比例
        //private static double ScaleRatio = 4;

        //private double listViewWidth = checkImageWidth / 4 / 4;
        //private double listViewHeight = checkImageHeight / 4 / 2;

        //private double resultWidth = checkImageWidth / 4 / 2;
        //private double resultHeight = checkImageHeight / 4 / 4;

        //private double listViewWidth = 320;
        //private double listViewHeight = 680;

        //private double resultWidth = checkImageWidth / 4 / 2;
        //private double resultHeight = checkImageHeight / 4 / 4;

        /***************************** ui 相关*************************************/
        private GetIniResBean getIniResBean = null;
        private List<UpdateResBean> updateResBeans = null;  //所有函数列表
        private List<UpdateResBean> updateResBeans1 = new List<UpdateResBean>();  //工站1函数列表
        private List<UpdateResBean> updateResBeans2 = new List<UpdateResBean>();  //工站2函数列表
        private List<ConfigFileRes> ConfigFileResBeans = null; //所有料号配置列表
        //private List<Registration> registration = null; //传递给算法start做配准的对象

        ObservableCollection <Object> LRectList = new ObservableCollection<Object>();
        List <LammyRect> funRectList = new List<LammyRect>();
        List <LammyRect> djRectList = new List<LammyRect>();
        ArrayList largeRectangleList = new ArrayList();
        //ArrayList smallRectangleList = new ArrayList();
        private int checkResultStatus = STATUS_CHECK_RESULT_INIT;
        private static int STATUS_CHECK_RESULT_INIT = 0;
        private static int STATUS_CHECK_RESULT_PASS = 1;
        private static int STATUS_CHECK_RESULT_FAIL = 2;
        private bool isChanageResultMode = false;
        private bool isDjMode = false;
        private int WebUpdateCount = 0;  // 接受推送程序更新


        private void refreshLammyRect(List<UpdateResBean> mUpdateResBean,bool needInitResult)
        {
            tbx_mes.Text = "";
            int[] myArr = new int[] { };
            selectedFunIndex = -1;
            setLargeSmallBtnEnabled(false);
            reInitCheckLargeImageSize();
            LRectList.Clear();
            List<LammyRect> mRects = new List<LammyRect>();
            if (mUpdateResBean == null)
                return;

            if(GlobalValue.ISRESNAP == "0")
            {
                if (inferenceResult != null && ConfigFileResBeans != null && ConfigFileResBeans.Count > 0)
                {
                    foreach (ConfigFileRes C in ConfigFileResBeans)
                    {
                        if (GlobalValue.MES_PARTNUMBER == C.material_num)
                        {
                            myArr = C.function_ids;
                            UploadIR.FunctionID = myArr;//新增函数ID列表
                            break;
                        }
                    }
                }

                if (inferenceResult != null && myArr.Length <= 0 && ConfigFileResBeans != null && ConfigFileResBeans.Count > 0 && !CheckIsDj())
                {
                    LogUtils.d("未匹配到对应物料号");
                    result.Source = MesNumError;
                    return;
                }
            }
            else
            {
                if (inferenceResult != null)
                {
                    //UploadIR.FunctionID = myArr;//新增函数ID列表
                    UploadIR.FunctionID = FunctionIdS.ToArray();
                    myArr = FunctionIdS.ToArray();
                }
            }

            for (int i=0; i< mUpdateResBean.Count;i++ )
            {
                UpdateResBean u = mUpdateResBean[i];
                int functionID = u.function_id;
                MyRect mRect = new MyRect();
                //MyRect mRect = u.rect;
                string functionName = u.function_name;
                string result = u.result;
                if (myArr.Length > 0 && !myArr.Contains(functionID))  
                {
                    continue;
                }

                if (needInitResult)
                {
                    result = GlobalValue.ALG_RESULT_NO;
                }
                else
                {
                    if (inferenceResult != null)
                    {
                        List<FunctionResult> fs = inferenceResult.FunctionResult;
                        foreach (FunctionResult f in fs)
                        {
                            if (f.functionId == u.function_id)
                            {
                                if (myArr.Length > 0)
                                {
                                    if (myArr.Contains(f.functionId))
                                    {
                                        if (f.IsFail == 0)
                                        {
                                            result = GlobalValue.ALG_RESULT_TRUE;
                                        }
                                        else
                                        {
                                            //添加SIM卡托有无检测
                                            if(GlobalValue.IsSIMDUTChecked != "0")
                                            {                                               
                                                ////裁剪图片,获取待解码图像
                                                System.Drawing.Rectangle Rect1 = new System.Drawing.Rectangle(f.roi.left, f.roi.right, f.roi.right - f.roi.left, f.roi.bottom- f.roi.top);
                                                System.Drawing.Bitmap m_BitmapForDUTCheck = QRDecodeUtils.CropBitmap(m_BitmapImageForSave, Rect1);
                                                if (QRDecodeUtils.CheckDUTExist(m_BitmapForDUTCheck))
                                                {
                                                    result = GlobalValue.ALG_RESULT_FALSE;
                                                }
                                                else
                                                {
                                                    result = GlobalValue.ALG_RESULT_NO;
                                                }
                                            }
                                            else
                                            {
                                                result = GlobalValue.ALG_RESULT_FALSE;
                                            }                                           
                                        }
                                    }
                                    else
                                    {
                                        result = GlobalValue.ALG_RESULT_LOAD_FAIL;
                                    }

                                }
                                else
                                {
                                    if (f.IsFail == 0)
                                    {
                                        result = GlobalValue.ALG_RESULT_TRUE;
                                    }
                                    else
                                    {
                                        result = GlobalValue.ALG_RESULT_FALSE;
                                    }
                                }
                                mRect.left = 5472 - f.roi.right;
                                mRect.right = 5472 - f.roi.left;
                                mRect.top = 3648 - f.roi.bottom;
                                mRect.bottom = 3648 - f.roi.top;
                            }
                        }
                    }
                    else
                    {
                        if(u.function_id == 0)
                        {

                        }
                        else
                        {
                            result = GlobalValue.ALG_RESULT_NO;
                            mRect.left = 5472 - u.rect.right;
                            mRect.right = 5472 - u.rect.left;
                            mRect.top = 3648 - u.rect.bottom;
                            mRect.bottom = 3648 - u.rect.top;
                            LammyRect lammyRect = new LammyRect(u.function_id, mRect.left, mRect.top, mRect.right - mRect.left, mRect.bottom - mRect.top, functionID, functionName, result);
                            mRects.Add(lammyRect);
                        }
                    }
                }


                if (mRect != null && result != GlobalValue.ALG_RESULT_LOAD_FAIL && result != GlobalValue.ALG_RESULT_NO)
                {
                    LammyRect lammyRect = new LammyRect(u.function_id, mRect.left, mRect.top, mRect.right - mRect.left, mRect.bottom - mRect.top, functionID, functionName, result);
                    mRects.Add(lammyRect);
                }                
            }

            if (CheckIsDj())
            {
                List<LammyRect> djRois = new List<LammyRect>();
                string djRectResult = GlobalValue.ALG_RESULT_NO;
                if (inferenceResult != null)
                {
                    string result = GlobalValue.ALG_RESULT_NO;

                    List<SegRect> rects = inferenceResult.segResult.rois;
                    if (!needInitResult)
                    {
                        result = GlobalValue.ALG_RESULT_FALSE;
                        if (inferenceResult.segResult.IsFail == 0)
                        {
                            djRectResult = GlobalValue.ALG_RESULT_TRUE;
                        }
                        else
                        {
                            djRectResult = GlobalValue.ALG_RESULT_FALSE;
                        }
                    }
                    else
                    {
                        result = GlobalValue.ALG_RESULT_NO;
                        djRectResult = GlobalValue.ALG_RESULT_NO;
                    }

                    //原始数据
                    //if(rects!=null)
                    //{
                    //    foreach (SegRect r in rects)
                    //    {
                    //        LammyRect l = new LammyRect(0, r.left, r.top, r.right-r.left, r.bottom-r.top,0, "点胶", result);
                    //        djRois.Add(l);
                    //    }
                    //}

                    if (rects != null)
                    {
                        SegRect temp = new SegRect();
                        foreach (SegRect r in rects)
                        {
                            //添加ROI区域进mRect
                            temp.left = 5472 - r.left;
                            temp.right = 5472 - r.right;
                            temp.top = 3648 - r.top;
                            temp.bottom = 3648 - r.bottom;

                            if (temp.right >= temp.left)
                            {
                                LammyRect l = new LammyRect(0, temp.left, temp.top, temp.right - temp.left, temp.bottom - temp.top, 0, "点胶", result);
                                djRois.Add(l);
                            }
                            else
                            {
                                LammyRect l = new LammyRect(0, temp.right, temp.bottom, temp.left - temp.right, temp.top - temp.bottom, 0, "点胶", result);
                                djRois.Add(l);
                            }
                        }
                    }
                }

                DjRect d = new DjRect(0, "点胶", djRectResult, djRois);
                LRectList.Add(d);
                djRectList = d.rects;
                isDjMode = true;
            }

            funRectList = mRects;

            for (int i = 0; i < mRects.Count; i++)
            {
                LRectList.Add(mRects[i]);
            }
            
            updateResultDisplay();
            if (!needInitResult)
            {
                drawLargeRect();
            }
        }
       

        //获取当前Rect列表
        private List<LammyRect> getCurRectList()
        {
            List<LammyRect> rects;
            if (isDjMode)
            {
                rects = djRectList;
            }
            else
            {
                rects = funRectList;
            }
            return rects;
        }

        //private void drawSmallRect()
        //{
        //    List<LammyRect> rects = getCurRectList();
        //    foreach (UIElement rectangle in smallRectangleList)
        //    {
        //        this.Grid1.Children.Remove(rectangle);
        //    }
        //    smallRectangleList.Clear();
        //    int size = rects.Count;
        //    double ratio = CheckImageView.Width / checkImageWidth;
        //    //double ratio1 = CheckImageView.Height / checkImageHeight;
        //    double ShowHeight = (284 - (checkImageHeight / checkImageWidth) * 400) / 2;

        //    for (int i = 0; i < size; i++)
        //    {
        //        LammyRect r = rects[i];
        //        Rectangle curRectangle = r.curRectangle;
        //        Rectangle smallRectangle = new System.Windows.Shapes.Rectangle();
        //        if (r.result == GlobalValue.ALG_RESULT_TRUE)
        //        {
        //            smallRectangle.Stroke = GlobalValue.BRUSH_PASS_COLOR;
        //        }
        //        else if (r.result == GlobalValue.ALG_RESULT_FALSE)
        //        {
        //            smallRectangle.Stroke = GlobalValue.BRUSH_FAIL_COLOR;
        //        }
        //        else
        //        {
        //            smallRectangle.Stroke = GlobalValue.BRUSH_INIT_COLOR;
        //        }

        //        smallRectangle.StrokeThickness = 1;
        //        smallRectangle.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
        //        smallRectangle.VerticalAlignment = VerticalAlignment.Top;
        //        if (GlobalValue.ALG_RESULT_CalibrationRes.global_registration && !CheckIsDj())
        //        {
        //            double Left = curRectangle.Margin.Left - GlobalValue.ALG_RESULT_CalibrationRes.global_roi_rect[0];
        //            if (Left < 0)
        //            {
        //                Left = 0;
        //            }
        //            double TOP = curRectangle.Margin.Top - GlobalValue.ALG_RESULT_CalibrationRes.global_roi_rect[1];
        //            if (TOP < 0)
        //            {
        //                TOP = 0;
        //            }
        //            smallRectangle.Margin = new Thickness(Left * ratio, TOP * ratio + ShowHeight, 0, 0);
        //        }
        //        else
        //        {
        //            smallRectangle.Margin = new Thickness(curRectangle.Margin.Left * ratio, curRectangle.Margin.Top * ratio + ShowHeight, 0, 0);
        //        }

        //       // smallRectangle.Margin = new Thickness(curRectangle.Margin.Left * ratio, curRectangle.Margin.Top * ratio+ ShowHeight, 0, 0);
        //        smallRectangle.Width = curRectangle.Width * ratio;
        //        smallRectangle.Height = curRectangle.Height * ratio;

        //        this.Grid1.Children.Add(smallRectangle);
        //        smallRectangleList.Add(smallRectangle);
        //        Panel.SetZIndex(smallRectangle, 2);
        //    }
        //    Panel.SetZIndex(loadingWait, 4);
        //}


        private void drawLargeRect()
        {
            if(inferenceResult!=null && inferenceResult.requireSeg == 1)
            {
                setDJComposeImage();
            }

            List<LammyRect> rects = getCurRectList();
            foreach (UIElement rectangle in largeRectangleList)
            {
                this.GridLargeView.Children.Remove(rectangle);
            }
            largeRectangleList.Clear();
            double ratio = checkImageLargeWidth / checkImageWidth;
            double ratio1 = checkImageLargeHeight / checkImageHeight;

            int size = rects.Count;
            for (int i = 0; i < size; i++)
            {
                LammyRect r = rects[i];
                Rectangle curRectangle = r.curRectangle;
                Rectangle largeRectangle = new System.Windows.Shapes.Rectangle();
                if (r.result == GlobalValue.ALG_RESULT_TRUE)
                {
                    largeRectangle.Stroke = GlobalValue.BRUSH_PASS_COLOR;
                }
                else if (r.result == GlobalValue.ALG_RESULT_FALSE)
                {
                    largeRectangle.Stroke = GlobalValue.BRUSH_FAIL_COLOR;
                }
                else
                {
                    largeRectangle.Stroke = GlobalValue.BRUSH_INIT_COLOR;
                }
                largeRectangle.StrokeThickness = 3;
                largeRectangle.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                largeRectangle.VerticalAlignment = VerticalAlignment.Top;
                largeRectangle.Margin = new Thickness(curRectangle.Margin.Left * ratio, curRectangle.Margin.Top * ratio1, 0, 0);
                largeRectangle.Width = curRectangle.Width * ratio;
                largeRectangle.Height = curRectangle.Height * ratio1;

                this.GridLargeView.Children.Add(largeRectangle); //屏蔽掉，不画大框
                largeRectangleList.Add(largeRectangle);
                Panel.SetZIndex(largeRectangle, 2);
            }
        }
        
        private void setViewPort(bool Isdjmode,double largeRatio, Rectangle rectangle)
        {
            if(inferenceResult!=null)
            {
                if (Isdjmode && inferenceResult.segResult.IsFail==0)
                    return;
            }

            double centerX = checkImageLargeWidth / 2;
            double centerY = checkImageLargeHeight / 2;
            double rectangleCenterX = 0;
            double rectangleCenterY = 0;

            this.largeRatio = largeRatio;
            double checkImageLargeWidth2 = checkImageLargeWidth * largeRatio;
            int size = largeRectangleList.Count;

            double ratio = checkImageLargeWidth2 / checkImageWidth;
            List<LammyRect> rects = getCurRectList();
            for (int i = 0; i < size; i++)
            {
                LammyRect r = (LammyRect)rects[i];
                Rectangle largeRectangle = (Rectangle)largeRectangleList[i];
                Rectangle curRectangle = r.curRectangle;
                largeRectangle.Margin = new Thickness(curRectangle.Margin.Left * ratio + checkImageSmallWidth, curRectangle.Margin.Top * ratio + checkImageSmallHeight, 0, 0);
                largeRectangle.Width = curRectangle.Width * ratio;
                largeRectangle.Height = curRectangle.Height * ratio;
                if (curRectangle == rectangle)
                {
                    rectangleCenterX = largeRectangle.Margin.Left + largeRectangle.Width / 2;
                    rectangleCenterY = largeRectangle.Margin.Top + largeRectangle.Height / 2;
                    largeRectangle.StrokeThickness = 5;
                }
                else
                {
                    largeRectangle.StrokeThickness = 1;
                }

            }

            CheckImageLargeView.Margin = new Thickness(checkImageSmallWidth, checkImageSmallHeight, 0, 0);
            CheckImageLargeView.Width = checkImageLargeWidth2;
            CheckImageLargeView.Height = checkImageLargeHeight * largeRatio;


            double dx = centerX - rectangleCenterX;
            double dy = centerY - rectangleCenterY;
            for (int i = 0; i < size; i++)
            {
                Rectangle largeRectangle = (Rectangle)largeRectangleList[i];
                largeRectangle.Margin = new Thickness(largeRectangle.Margin.Left + dx, largeRectangle.Margin.Top + dy, 0, 0);
                largeRectangle.Width = largeRectangle.Width;
                largeRectangle.Height = largeRectangle.Height;
            }


            CheckImageLargeView.Margin = new Thickness(CheckImageLargeView.Margin.Left + dx, CheckImageLargeView.Margin.Top + dy, 0, 0);
            CheckImageLargeView.BringIntoView();
            Panel.SetZIndex(CheckImageLargeView, 0);

        }

        // 返回路径下图片
        private BitmapImage getBitmapImage(string imagePath)
        {

            BinaryReader binReader = new BinaryReader(File.Open(imagePath, FileMode.Open));
            FileInfo fileInfo = new FileInfo(imagePath);
            byte[] bytes = binReader.ReadBytes((int)fileInfo.Length);
            binReader.Close();

            // Init bitmap
            BitmapImage CurBitmap = new BitmapImage();
            CurBitmap = new BitmapImage();
            CurBitmap.BeginInit();
            CurBitmap.StreamSource = new MemoryStream(bytes);
            CurBitmap.EndInit();
            return CurBitmap;

            // Uri内部有各种转义case，要避免直接使用。例如把%71转成q字符
            //BitmapImage imagesouce = new BitmapImage(new Uri(imagePath, false));
            //return imagesouce;
        }

        
        private int selectedFunIndex = -1;//选中函数序号
        private int selectedDjIndex = -1;//选中点胶roi序号
        private void LRectListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void LRectListView_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (LRectListView.SelectedItem == null)
            {
                return;
            }
            if (LRectListView.SelectedItem is LammyRect)
            {
                //CheckImageLargeView.Source = CheckImageView.Source;

                isDjMode = false;
                LammyRect rect = (LammyRect)LRectListView.SelectedItem;
                if (CheckIsDj())
                {
                    selectedFunIndex = LRectListView.SelectedIndex-1;
                }
                else
                {
                    selectedFunIndex = LRectListView.SelectedIndex;
                }
                if (imagePath != null)
                {
                    setLargeSmallBtnEnabled(true);
                }
               // drawSmallRect();
                drawLargeRect();
              //  setViewPort(isDjMode,initRatio, rect.curRectangle);
                setGoldenSampleImage(rect.index);
                if (isChanageResultMode)
                {
                    if (rect.result == GlobalValue.ALG_RESULT_FALSE)
                    {
                        foreach(FunctionResult fr in modifyinferenceResult.FunctionResult)
                        {
                            if(fr.functionId == rect.function_ID)
                            {
                                fr.IsFail = 0;
                            }
                        }
                        rect.result = GlobalValue.ALG_RESULT_TRUE;
                    }
                    else if (rect.result == GlobalValue.ALG_RESULT_TRUE)
                    {
                        foreach (FunctionResult fr in modifyinferenceResult.FunctionResult)
                        {
                            if (fr.functionId == rect.function_ID)
                            {
                                fr.IsFail = 1;
                            }
                        }
                        rect.result = GlobalValue.ALG_RESULT_FALSE;
                    }
                }
            }
            else if (LRectListView.SelectedItem is DjRect)
            {
                isDjMode = true;
                DjRect dj = (DjRect)LRectListView.SelectedItem;
                if (inferenceResult != null)
                {
                    if (inferenceResult.segResult.IsFail == 0)
                    {
                        selectedDjIndex = 0;
                        setLargeSmallBtnEnabled(true);
                        reInitCheckLargeImageSize();
                        setDJComposeImage();
                    }
                    else
                    {
                        int countNum = dj.rects.Count;
                        if (dj.rects.Count > 0)
                        {
                            selectedDjIndex = 0;
                            setLargeSmallBtnEnabled(true);
                            //drawSmallRect();
                            drawLargeRect();
                            reInitCheckLargeImageSize();
                        }
                    }
                }      
                
                if (isChanageResultMode)
                {
                    if (dj.result == GlobalValue.ALG_RESULT_FALSE)
                    {
                        modifyinferenceResult.segResult.IsFail = 0;
                        dj.result = GlobalValue.ALG_RESULT_TRUE;
                    }
                    else if (dj.result == GlobalValue.ALG_RESULT_TRUE)
                    {
                        modifyinferenceResult.segResult.IsFail = 1;
                        dj.result = GlobalValue.ALG_RESULT_FALSE;
                    }
                }
            }
        }

        private void BnClearDUTNum_Click(object sender, RoutedEventArgs e)
        {
            GlobalValue.DUTTotal = 0;
            GlobalValue.DUTPass = 0;
            GlobalValue.DUTPassRate = 0;
            DUTNum.Text = GlobalValue.DUTTotal.ToString();
            passnum.Text = GlobalValue.DUTPass.ToString();
            passrate.Text = GlobalValue.DUTPassRate.ToString("P");
            //openShowImageWindow(@"C:\TestImage\2.png");
        }

        private void UpdateProductInfo(bool IsPass,bool IsThread)
        {
            GlobalValue.DUTTotal++;
            if(IsPass)
            {
                GlobalValue.DUTPass++;
            }
            else
            {

            }
            GlobalValue.DUTPassRate = GlobalValue.DUTPass * 1.0 / GlobalValue.DUTTotal;

            string t1 = GlobalValue.DUTTotal.ToString();
            string t2 = GlobalValue.DUTPass.ToString();
            string t3 = GlobalValue.DUTPassRate.ToString("P");

            if (IsThread)
            {
                UpdateDUTNumText(t1);
                UpdateDUTPassText(t2);
                UpdateDUTPassFailText(t3);
            }
            else
            {
                DUTNum.Text = GlobalValue.DUTTotal.ToString();
                passnum.Text = GlobalValue.DUTPass.ToString();
                passrate.Text = GlobalValue.DUTPassRate.ToString("P");
            }

        }



        //private void CheckImageView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    System.Windows.Point p = e.GetPosition(btn_small_iv);
        //    double ratio = CheckImageView.Width / checkImageWidth;
        //    List<LammyRect> rects = getCurRectList();
        //    foreach(LammyRect rect in rects)
        //    {
        //        if(p.X > rect.left *ratio && p.X < (rect.left + rect.width) * ratio && p.Y >rect.top *ratio && p.Y<((rect.top+rect.height)*ratio +30))
        //        {
        //            if (isDjMode)
        //            {
        //                selectedDjIndex = rects.IndexOf(rect);
        //                LRectListView.SelectedIndex = 0;
        //            }
        //            else
        //            {
        //                selectedFunIndex = rects.IndexOf(rect);
        //                if (CheckIsDj())
        //                {
        //                    LRectListView.SelectedIndex = selectedFunIndex+1;
        //                }
        //                else
        //                {
        //                    LRectListView.SelectedIndex = selectedFunIndex;
        //                }
        //            }
                    
        //           // setViewPort(isDjMode,1, rect.curRectangle);
        //            //drawSmallRect();
        //            if (imagePath != null)
        //            {
        //                setLargeSmallBtnEnabled(true);
        //            }
        //            setGoldenSampleImage(rect.index);
        //            break;
        //        }
        //    }
        //}
        private void setGoldenSampleImage(double fun_id)
        {
            string path = GlobalValue.GOLDEN_SAMPLE_FOLDER + fun_id + ".jpg";
            if (File.Exists(path))
            {
                golden_sample_iv.Source = getBitmapImage(path);
            }
        }

        private void setDJComposeImage()
        {
            string path = GlobalValue.DJ_COMPOSEMASK_IMG_PATH;
            if (File.Exists(path))
            {
                BitmapImage bi = getBitmapImage(path);
                checkImageWidth = GlobalValue.SHOWIMAGE.PixelWidth;
                checkImageHeight = GlobalValue.SHOWIMAGE.PixelHeight;

                //2.bitmapimage旋转
                TransformedBitmap tb = new TransformedBitmap();
                tb.BeginInit();
                tb.Source = bi;
                RotateTransform transform = new RotateTransform(180);
                tb.Transform = transform;
                tb.EndInit();
                CheckImageLargeView.Source = tb;
            }
        }

        private bool isOpenCameraEnabledWhenModifying, isCloseCameraEnabledWhenModifying;
        //切换编辑和非编辑状态
        private void modify_Click(object sender, RoutedEventArgs e)
        {
            if (updateResBeans == null || updateResBeans.Count <= 0)
            {
                MessageBox.Show("函数列表为空，无法进入修改状态");
                return;
            }
            foreach (Object o in LRectList)
            {
                if(o is LammyRect)
                {
                    if (((LammyRect)o).result == GlobalValue.ALG_RESULT_NO)
                    {
                        MessageBox.Show("还未执行算法，无法进入修改状态");
                        return;
                    }
                }
                else
                {
                    if (((DjRect)o).result == GlobalValue.ALG_RESULT_NO)
                    {
                        MessageBox.Show("还未执行算法，无法进入修改状态");
                        return;
                    }
                }
                
            }
            if (!isChanageResultMode)
            {
                isChanageResultMode = true;
                btn_modify.Content = "修改中...";
                isOpenCameraEnabledWhenModifying = bnOpen.IsEnabled;
                isCloseCameraEnabledWhenModifying = bnClose.IsEnabled;
                bnSaveJpg.IsEnabled = false; //修改中状态时，拍照保存不可用
                bnOpen.IsEnabled = false;
                bnClose.IsEnabled = false;
            }
            else
            {
                isChanageResultMode = false;
                btn_modify.Content = "修改";
                updateResultDisplay();
                bnSaveJpg.IsEnabled = true;
                bnOpen.IsEnabled = isOpenCameraEnabledWhenModifying;
                bnClose.IsEnabled = isCloseCameraEnabledWhenModifying;
            }
        }

        private void open_file_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog();
            fileDialog.Multiselect = true;
            fileDialog.Title = "请选择文件";
            fileDialog.Filter = "所有文件(*.*)|*.*";
            if (fileDialog.ShowDialog() == true)
            {
                imagePath = fileDialog.FileName;
                m_BitmapImageForSave = getBitmapImage(imagePath);
                UpdateImageSource(imagePath);
                //m_PlCCOMTimer.Start();
                //ImageUtils.GetPicThumbnail(imagePath, @"C:\Users\mali18\Pictures\Camera Roll\picTest\SurfNA_MP_A1_TEST_CodeError_200927095326.jpg", 5472, 3648, 50);
                //LogUtils.d("GetPicThumbnail path time :" + m_PlCCOMTimer.ElapsedTime());


                ////1.镜像180度
                //GlobalValue.SHOWIMAGE = getBitmapImage(imagePath);
                //GlobalValue.SHOWIMAGE_BITMAP = QRDecodeUtils.BitmapImage2Bitmap(GlobalValue.SHOWIMAGE);
                //GlobalValue.SHOWIMAGE_BITMAP.RotateFlip(System.Drawing.RotateFlipType.Rotate180FlipNone);
                //GlobalValue.SHOWIMAGE = QRDecodeUtils.BitmapToBitmapImage(GlobalValue.SHOWIMAGE_BITMAP);
                //checkImageWidth = GlobalValue.SHOWIMAGE.PixelWidth;
                //checkImageHeight = GlobalValue.SHOWIMAGE.PixelHeight;
                //reInitCheckLargeImageSize();
                //this.CheckImageLargeView.Source = GlobalValue.SHOWIMAGE;

                ////2.旋转180度
                //GlobalValue.SHOWIMAGE = getBitmapImage(imagePath);
                //checkImageWidth = GlobalValue.SHOWIMAGE.PixelWidth;
                //checkImageHeight = GlobalValue.SHOWIMAGE.PixelHeight;
                //reInitCheckLargeImageSize();
                ////this.CheckImageView.Source = GlobalValue.SHOWIMAGE;
                //this.CheckImageLargeView.Source = GlobalValue.SHOWIMAGE;

                //执行新算法
                refreshLammyRect(updateResBeans, true);

                if (SN.Text.Length ==0)
                {
                    m_str_DUTSN = "";
                }
                if(GlobalValue.ISPLCUSED == "1")
                {
                    setStep(EStepSelectWork.STEP_AUTO_VS_CALLRCP);
                }
                else
                {
                    setManualStep(EStepManualSelectWork.STEP_AUTO_VS_CALLRCP);
                }
            }
        }

        private void Btn_QRCode_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog();
            fileDialog.Multiselect = true;
            fileDialog.Title = "请选择文件";
            fileDialog.Filter = "所有文件(*.*)|*.*";
            if (fileDialog.ShowDialog() == true)
            {
                ManualImagePathValid = true;
                ManualImagePath = fileDialog.FileName;

                UpdateImageSource(ManualImagePath);

                //GlobalValue.SHOWIMAGE = getBitmapImage(imagePath);
                //GlobalValue.SHOWIMAGE_BITMAP = QRDecodeUtils.BitmapImage2Bitmap(GlobalValue.SHOWIMAGE);
                //GlobalValue.SHOWIMAGE_BITMAP.RotateFlip(System.Drawing.RotateFlipType.Rotate180FlipNone);
                //GlobalValue.SHOWIMAGE = QRDecodeUtils.BitmapToBitmapImage(GlobalValue.SHOWIMAGE_BITMAP);
                //checkImageWidth = GlobalValue.SHOWIMAGE.PixelWidth;
                //checkImageHeight = GlobalValue.SHOWIMAGE.PixelHeight;
                //reInitCheckLargeImageSize();
                //this.CheckImageLargeView.Source = GlobalValue.SHOWIMAGE;

                //GlobalValue.SHOWIMAGE = getBitmapImage(ManualImagePath);
                //checkImageWidth = GlobalValue.SHOWIMAGE.PixelWidth;
                //checkImageHeight = GlobalValue.SHOWIMAGE.PixelHeight;
                //reInitCheckLargeImageSize();
                ////this.CheckImageView.Source = GlobalValue.SHOWIMAGE;
                //this.CheckImageLargeView.Source = GlobalValue.SHOWIMAGE;

                //BitmapImage image = getBitmapImage(ManualImagePath);
                //reInitCheckLargeImageSize();
                //CheckImageView.Source = image;
                //CheckImageLargeView.Source = image;

                //获取待保存图像
                m_BitmapImageForSave = getBitmapImage(ManualImagePath);

                ////裁剪图片,获取待解码图像
                System.Drawing.Rectangle Rect1 = new System.Drawing.Rectangle(GlobalValue.ROI_X, GlobalValue.ROI_Y, GlobalValue.ROI_WIDTH, GlobalValue.ROI_HEIGHT);
                m_BitmapForDecode = QRDecodeUtils.CropBitmap(m_BitmapImageForSave, Rect1);

                string ImageFilePath = Directory.GetCurrentDirectory() + "\\zxingtestfail\\cutimage.jpg";
                m_BitmapForDecode.Save(ImageFilePath);

                if (GlobalValue.ISPLCUSED == "0")
                {
                    if(GlobalValue.ISRESNAP == "1")
                    {
                        phase = 1;
                    }
                    setManualStep(EStepManualSelectWork.STEP_AUTO_VS_DECODE);
                }
                else
                    setStep(EStepSelectWork.STEP_AUTO_VS_DECODE);
            }
        }

        private void BnInitial_Click(object sender, RoutedEventArgs e)
        {
            if (GlobalValue.ISPLCUSED == "0")
            {
                //半自动模式
                setManualStep(EStepManualSelectWork.STEP_SELECTWORK_INIT);
            }
            else
            {
                setStep(EStepSelectWork.STEP_SELECTWORK_INIT);
            }
        }

        //private void BnIsManulCode_Click(object sender, RoutedEventArgs e)
        //{
        //    if (bnIsManulCode.IsChecked == true)
        //    {
        //        PlcCodeResult(2);
        //        GlobalValue.QRCodeInOrder = "1";//相机扫码
        //        SN.Focus();

        //    }
        //    else
        //    {
        //        PlcCodeResult(0);
        //        GlobalValue.QRCodeInOrder = "0";//手动扫码
        //        SN.Focus();
        //    }
        //    IniUtils.IniWriteValue("Station Configuration", "Camera", GlobalValue.QRCodeInOrder, "C:\\config\\config.ini");
        //}

        private void SN_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (SN.Text.Length == 1)
                {
                    if (SN.IsEnabled)
                    {
                        SN.IsEnabled = false;
                    }
                    if (GlobalValue.QRCodeInOrder == "0")
                    {
                        PlcCodeResult(1);
                        PlcRead("D10");
                    }
                }
               else
                {
                    if(GlobalValue.CONFIG_STATION == "LeftBoard" || GlobalValue.CONFIG_STATION == "RightBoard" || GlobalValue.CONFIG_STATION == "Imagers" ||
                        GlobalValue.CONFIG_STATION == "FlipChassis" || GlobalValue.CONFIG_STATION == "ChassisScrews")
                    {
                        if (SN.Text.Length == 23)
                        {
                            if (SN.IsEnabled)
                            {
                                SN.IsEnabled = false;
                            }
                            if (GlobalValue.QRCodeInOrder == "0")
                            {
                                PlcCodeResult(1);
                                PlcRead("D10");
                            }
                        }
                        else
                        {
                            MessageBox.Show("扫描的二维码格式不正确，请重新扫描！");
                            SN.Clear();
                            SN.Focus();
                            return;
                        }
                    }
                    else if(GlobalValue.CONFIG_STATION == "HingeHousing" || GlobalValue.CONFIG_STATION == "CLILens")
                    {
                        if (SN.Text.Length > 50)
                        {
                            if (SN.IsEnabled)
                            {
                                SN.IsEnabled = false;
                            }
                            if (GlobalValue.QRCodeInOrder == "0")
                            {
                                PlcCodeResult(1);
                                PlcRead("D10");
                            }
                        }
                        else
                        {
                            MessageBox.Show("扫描的二维码格式不正确，请重新扫描！");
                            SN.Clear();
                            SN.Focus();
                            return;
                        }
                    }
                    else
                    {
                        if (SN.Text.Length == 10 || SN.Text.Length == 23 || SN.Text.Length > 50)
                        {
                            if (SN.IsEnabled)
                            {
                                SN.IsEnabled = false;
                            }
                            if (GlobalValue.QRCodeInOrder == "0")
                            {
                                PlcCodeResult(1);
                                PlcRead("D10");
                            }
                        }
                        else
                        {
                            MessageBox.Show("扫描的二维码格式不正确，请重新扫描！");
                            SN.Clear();
                            SN.Focus();
                            return;
                        }
                    }               
                }
                string Temp = SN.Text;
                m_str_DUTSN = Temp;
                labelSN.Content = "SN:" + m_str_DUTSN;
            }
        }

        private void updateResultDisplay()
        {
            checkedResultPass();
            if (checkResultStatus == STATUS_CHECK_RESULT_PASS)
            {
                result.Source = resultPassImage;
                m_bFinalResult = true;
            }
            else if(checkResultStatus == STATUS_CHECK_RESULT_FAIL)
            {
                result.Source = resultfailedImage;
                m_bFinalResult = false;
            }
            else if(checkResultStatus == STATUS_CHECK_RESULT_INIT)
            {
                result.Source = null;
                m_bFinalResult = false;
            }
        }

        //判断结果是否通过
        private int checkedResultPass()
        {
            int passCourt=0, initCourt=0,loadFailCourt=0;
            checkResultStatus = STATUS_CHECK_RESULT_INIT; //default
            for (int i = 0; i < LRectList.Count(); i++)
            {
                if (LRectList[i] is DjRect)
                {
                    DjRect r = (DjRect)LRectList[i];
                 
                    if (r.result == GlobalValue.ALG_RESULT_FALSE)
                    {
                        checkResultStatus = STATUS_CHECK_RESULT_FAIL;
                        return checkResultStatus;
                    }
                    else if (r.result == GlobalValue.ALG_RESULT_TRUE)
                    {
                        passCourt++;
                    }
                    else if (r.result == GlobalValue.ALG_RESULT_NO)
                    {
                        initCourt++;
                    }
                    else if (r.result == GlobalValue.ALG_RESULT_LOAD_FAIL)
                    {
                        loadFailCourt++;
                    }
                }

                if (LRectList[i] is LammyRect)
                {
                    LammyRect r = (LammyRect)LRectList[i];
                    Rectangle curRectangle = r.curRectangle;
                    if (r.result == GlobalValue.ALG_RESULT_FALSE)
                    {
                        checkResultStatus = STATUS_CHECK_RESULT_FAIL;
                        return checkResultStatus;
                    }
                    else if (r.result == GlobalValue.ALG_RESULT_TRUE)
                    {
                        passCourt++;
                    }
                    else if (r.result == GlobalValue.ALG_RESULT_NO)
                    {
                        initCourt++;
                    }
                    else if (r.result == GlobalValue.ALG_RESULT_LOAD_FAIL)
                    {
                        loadFailCourt++;
                    }
                }
                
            }
            if(passCourt >0 && passCourt + loadFailCourt == LRectList.Count())
            {
                checkResultStatus = STATUS_CHECK_RESULT_PASS;
            }
            else if(initCourt == LRectList.Count())
            {
                checkResultStatus = STATUS_CHECK_RESULT_INIT;
            }
            
            return checkResultStatus;
        }

        private void startTimer()
        {
            multCommitWorker.RunWorkerAsync();
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Enabled = true;
            timer.Interval = 1000 * 60 * 60 * 12;//12个小时执行间隔时间,单位为毫秒   
            timer.Start();
            timer.Elapsed += new System.Timers.ElapsedEventHandler(Timer_Elapsed);
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            loadingWait.Dispatcher.Invoke(
              new Action(
                delegate
                {

                    //loadingWait.Visibility = Visibility.Visible;
                    multCommitWorker.RunWorkerAsync();

                    FileUtils.DelectDir(GlobalValue.ORIGIN_IMG_FOLDER);
                }
                ));         
        }

        private byte[] readImage(string filePath)
        {
            if (File.Exists(filePath))
            {
                FileStream fs = new FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                byte[] fileBytes = new byte[fs.Length];
                fs.Read(fileBytes, 0, fileBytes.Length);
                fs.Close();
                fs.Dispose();
                return fileBytes;
            }
            else
            {
                return null;
            }
        }

        private void CbDeviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        
        private void large_Imageview(object sender, RoutedEventArgs e)
        {
            if(imagePath != null && inferenceResult != null)
            {
                List<LammyRect> rects = getCurRectList();

                if (CheckIsDj())
                   openShowImageWindow(GlobalValue.DJ_COMPOSEMASK_IMG_PATH, rects,false,true);
                else
                {
                    openShowImageWindow1(GlobalValue.SHOWIMAGE, funRectList, false, false);
                }                
            }
        }
        
        private void small_Imageview(object sender, RoutedEventArgs e)
        {
            if (imagePath != null && File.Exists(imagePath) || (imagePath != null && inferenceResult != null))
            {
                if (largeRatio > initRatio)
                {
                    List<LammyRect> rects = getCurRectList();
                    int index = -1;
                    if (isDjMode)
                    {
                        index = selectedDjIndex;
                    }
                    else
                    {
                        //if (checkIsDj())
                        //{
                        //    index = selectedFunIndex-1;
                        //}
                        //else
                        //{
                        //    index = selectedFunIndex;
                        //}
                        index = selectedFunIndex;
                    }
                    if (index == -1)
                    {
                        if (rects.Count > 0)
                        {
                          //  setViewPort(isDjMode,largeRatio - 2, rects[0].curRectangle);
                        }
                    }
                    else
                    {
                      //  setViewPort(isDjMode,largeRatio - 2, rects[index].curRectangle);
                    }
                }
            }
        }

        private void CheckImageLargeView_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            //if (selectedFunIndex < 0)
            //    return;
            //if (e.Delta > 0)
            //    largeRatio += 0.1f;
            //else
            //    largeRatio -= 0.1f;
            //if (largeRatio >= 8)
            //    largeRatio = 8;
            //if (largeRatio <= initRatio)
            //    largeRatio = initRatio;
            //List<LammyRect> rects = getCurRectList();
            //int index = -1;
            //if (isDjMode)
            //{
            //    index = selectedDjIndex;
            //}
            //else
            //{

            //    index = selectedFunIndex;
            //    setViewPort(isDjMode,largeRatio, rects[index].curRectangle);
            //}
        }


        private void BnWritePLC_Click(object sender, RoutedEventArgs e)
        {
            GlobalValue.PLC_DREGISTER_ADDRESS = cbPLCDRegister.Text;
            short m = -1;
            m = short.Parse(DValue.Text.ToString());
            if(!PlcWrite(GlobalValue.PLC_DREGISTER_ADDRESS,m))
            {
                MessageBox.Show("写入PLC信号失败，请检测PLC连接！");
            }
        }

        private void BnReadPLC_Click(object sender, RoutedEventArgs e)
        {
            GlobalValue.PLC_DREGISTER_ADDRESS = cbPLCDRegister.Text;
            short x = -3;
            x = PlcRead(GlobalValue.PLC_DREGISTER_ADDRESS);

            if(x>=0)
            {
                DValue.Text = x.ToString();
            }
            else
            {
                MessageBox.Show("读取PLC信号失败，请检测PLC连接！");
            }
        }

        private void BnCloseLight_Click(object sender, RoutedEventArgs e)
        {
            int X = int.Parse(Lightness.Text.Trim());
            if (m_pSerialController != null && m_pSerialController.m_bValid)
            {
                OpenBarLight(X);
            }
            else
                MessageBox.Show("未连接上光源控制器！");
        }

        private void setLargeSmallBtnEnabled(bool isEnable)
        {
            large_bt.IsEnabled = isEnable;
          //  small_bt.IsEnabled = isEnable;
        }

        private void BnQuit_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("是否要关闭？", "确认", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Application.Current.MainWindow.Close();
            }
        }

        private void BnSerchSN_Click(object sender, RoutedEventArgs e)
        {
            //if (m_str_DUTSN.Length == 10)
            //{
            //    GlobalValue.MES_L5NUMBER = "";
            //    GlobalValue.MES_HingeHousingNUMBER = "";
            //    GlobalValue.MES_LeftBoardNUMBER = "";
            //    GlobalValue.MES_RightBoardNUMBER = "";
            //    GlobalValue.MES_CLILensNUMBER = "";
            //    List<string> strlist = ReadMesData(m_str_DUTSN);
            //    DisPlayLog(tbx_syslog, "L5SN:" + GlobalValue.MES_L5NUMBER + ",HingeHousingSN:" + GlobalValue.MES_HingeHousingNUMBER + "," +
            //        "LeftBoardSN:" + GlobalValue.MES_LeftBoardNUMBER + ",RightBoardSN:" + GlobalValue.MES_RightBoardNUMBER + ",CLILensSN:" + GlobalValue.MES_CLILensNUMBER, true);
            //}
            string mesStr = ReadMesData(m_str_DUTSN);
            DisPlayLog(tbx_syslog, "从MES中获取SN：" + mesStr, true);
        }

        private void CbNegativeSample_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (cbNegativeSample.SelectedIndex > 0)
            {
                PlcCodeResult(2);    //告知PLC自动扫码信号
            }
            else
            {
                PlcCodeResult(0);    //告知PLC手动扫码信号  正常工作状态
            }
            GlobalValue.DUT_SAMPLE_STATION = cbNegativeSample.SelectedIndex.ToString();
        }

        private void reInitCheckLargeImageSize()
        {
            //checkImageLargeWidth = checkImageWidth / 4 * 0.75;    //1026; 
            //checkImageLargeHeight = checkImageHeight / 4 * 0.75;    //684;
        //    checkImageLargeWidth = 800;    //1026; 
         //   checkImageLargeHeight = 564;    //684;
            CheckImageLargeView.Width = checkImageLargeWidth;
            CheckImageLargeView.Height = checkImageLargeHeight;
            largeRatio = 2;
            CheckImageLargeView.Margin = new Thickness(0, 0, 0, 0);
            CheckImageLargeView.BringIntoView();
        }
        
        private void openShowImageWindow(string imgPath, List<LammyRect> rect,bool TF,bool IsDJStation)
        {
            //正常使用  
            //string path = AppDomain.CurrentDomain.BaseDirectory + CallAlg.getInstance().cutImg(imgPath);
            //ShowImageWindow showImageWindow = new ShowImageWindow(path);

            //点胶合成图
            ShowImageWindow showImageWindow = new ShowImageWindow(imgPath, rect, checkImageWidth, checkImageHeight,TF,IsDJStation);

            ////测试使用
            //ShowImageWindow showImageWindow = new ShowImageWindow(imgPath);
            ////  showImageWindow.Show();
        }

        private void openShowImageWindow1(BitmapImage Temp, List<LammyRect> rect, bool TF,bool IsDJStation)
        {
            //正常使用  
            //string path = AppDomain.CurrentDomain.BaseDirectory + CallAlg.getInstance().cutImg(imgPath);
            //ShowImageWindow showImageWindow = new ShowImageWindow(path);

            //点胶合成图
            ShowImageWindow showImageWindow = new ShowImageWindow(Temp, rect, checkImageWidth, checkImageHeight, TF, IsDJStation);

            ////测试使用
            //ShowImageWindow showImageWindow = new ShowImageWindow(imgPath);
            ////  showImageWindow.Show();
        }


        private void openSocketConnection()
        {
            try
            {
                client.Start();
                Console.WriteLine("链接成功");
                Socket_MessageReceived();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发生异常链接失败{ex.ToString()}");
                throw;
            }
        }

        private void stopSocketConnection()
        {
            try
            {
                client.Dispose();
                Console.WriteLine("链接关闭");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发生异常关闭链接失败{ex.ToString()}");
                throw;
            }
        }

        private void Socket_MessageReceived()
        {
            //注册消息接收事件，接收服务端发送的数据
            client.MessageReceived += (data) => {
                LogUtils.d("Socket_MessageReceived: " + data);
                getIniResBean = JsonConvert.DeserializeObject<GetIniResBean>(data);
                if (getIniResBean == null)
                    return;

                WebUpdateCount++;

                if(WebUpdateCount == 1)
                {
                    ////推送更新程序
                    Updater.CheckUpdateStatus();
                }

                //Task.Factory.StartNew(() => UpdateConfig(),
                // new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
            };
        }

        //********************* 接收点胶算法完成时的推送 ********************************
        static HttpListener httpobj;
        private void startDjListenerConnection()
        {
            if (httpobj != null && httpobj.IsListening)
            {
                httpobj.Close();
            }
            //提供一个简单的、可通过编程方式控制的 HTTP 协议侦听器。此类不能被继承。
            httpobj = new HttpListener();
            //定义url及端口号，通常设置为配置文件
            httpobj.Prefixes.Add("http://127.0.0.1:8889/");
            //启动监听器
            httpobj.Start();
            //异步监听客户端请求，当客户端的网络请求到来时会自动执行Result委托
            //该委托没有返回值，有一个IAsyncResult接口的参数，可通过该参数获取context对象
            httpobj.BeginGetContext(Result, null);
            Console.WriteLine($"服务端初始化完毕，正在等待客户端请求,时间：{DateTime.Now.ToString()}\r\n");

            //if(registration != null)
            //{
            //    ThreadStartPythonRun();//线程开启
            //}
            //else
            //{
            //    if (MessageBox.Show("未获取到配准信息，请关闭程序重新获取") == MessageBoxResult.OK)
            //    {
            //        Task.Factory.StartNew(() => ShutdownApplication(),
            //     new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
            //    }
            //    LogUtils.d("未获取到配准信息，请关闭程序重新获取");
            //}
            ThreadStartPythonRun();//线程开启
        }

        private bool isPythonThreadStarted = false;
        //private void startPythonThread(int a)
        //{
        //    int maxAddModelExeCount = 5, count = 0; //控制最大执行次数限制

        //    //算法加载model
        //    bool isRpcReady = false;
        //    while (!isRpcReady)
        //    {
        //        string loadingText = "", addModelResult = "", golden_sample_path = "";
        //        string startResult = "";
        //        if (a==1)
        //        {
        //            if (GlobalValue.GOLD_SAMPLE_NAME1 == "")
        //            {
        //                golden_sample_path = GlobalValue.TEST_GOLDEN_SAMPLE_PATH;
        //                // golden_sample_path = "";
        //            }
        //            else
        //            {
        //                golden_sample_path = GlobalValue.GOLDEN_SAMPLE_FOLDER + GlobalValue.GOLD_SAMPLE_NAME1;
        //            }
        //            startResult = CallAlg.getInstance().start("FH", golden_sample_path);
        //        }
        //        else
        //        {
        //            if (GlobalValue.GOLD_SAMPLE_NAME2 == "")
        //            {
        //                golden_sample_path = GlobalValue.TEST_GOLDEN_SAMPLE_PATH;
        //                // golden_sample_path = "";
        //            }
        //            else
        //            {
        //                golden_sample_path = GlobalValue.GOLDEN_SAMPLE_FOLDER + GlobalValue.GOLD_SAMPLE_NAME2;
        //            }
        //            startResult = CallAlg.getInstance().start("BAT", golden_sample_path);
        //        }
                

                
                
        //        Task.Factory.StartNew(() => UpdateTb(loading_tv, loadingText),
        //            new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
        //        LogUtils.d("startThread 开启python线程结果：" + startResult);
        //        count++;
        //        if (!startResult.StartsWith("error"))
        //        {
        //            isPythonThreadStarted = true;
        //            isRpcReady = true;
        //            LogUtils.d("rpc已开启");
        //            if (startResult.ToLower().Equals("false"))
        //            {
        //                LogUtils.d("rpc已开启，start python thread失败 ");
        //            }
        //        }
        //        else
        //        {
        //            LogUtils.d("rpc未开启 : " + startResult + "count : " + count);
        //            if (count >= maxAddModelExeCount)
        //            {
        //               if(MessageBox.Show("rpc未开启成功，请重启程序")== MessageBoxResult.OK)
        //                {
        //                    Application.Current.Shutdown();//关闭
        //                }
        //                break;
        //            }
        //            Thread.Sleep(100);
        //        }
        //    }
        //}

        //优化开启算法服务接口
        #region StartPython线程接口
        Thread m_ThreadStartPython = null;
        public bool m_bThreadStartPythonLife = false;
        public bool m_bThreadStartPythonTerminate = false;
        public bool isRpcReady = false;
        public int StartPythonCount = 0;
        public int phase = 0;

        //关闭主程序
        public void ShutdownApplication()
        {
            Application.Current.Shutdown();
        }

        public void ThreadStartPythonFunction()
        {
            // Thread Loop
            while (m_bThreadStartPythonLife)
            {
                doRunStartPythonStep();
                Thread.Sleep(100);
            }
            m_bThreadStartPythonTerminate = true;
        }
        public void ThreadStartPythonRun()
        {
            if (m_bThreadStartPythonLife)
            {
                ThreadStartPythonStop();
                Thread.Sleep(100);
            }
            m_bThreadStartPythonLife = true;
            m_bThreadStartPythonTerminate = false;
            m_ThreadStartPython = new Thread(new ThreadStart(ThreadStartPythonFunction));
            m_ThreadStartPython.Start();
        }

        public void ThreadStartPythonStop()
        {
            m_bThreadStartPythonLife = false;
            Thread.Sleep(100);
            if (null != m_ThreadStartPython)
            {
                Thread.Sleep(100);
                m_ThreadStartPython.Abort();
            }
            m_ThreadStartPython = null;
        }
        public void doRunStartPythonStep()
        {
            
            if (StartPythonCount > 5)
            {
                if (MessageBox.Show("rpc未开启成功，请重启程序") == MessageBoxResult.OK)
                {
                    Task.Factory.StartNew(() => ShutdownApplication(),
                 new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
                }
            }
            //算法加载model
            if (!isRpcReady)
            {

                string loadingText = "";
                string startResult = "";

                List<string> golden_sample_path = new List<string>();
                List<string> station = new List<string>();
                List<string> lines = new List<string>();
                List<string> RegisterList = new List<string>();
                List<string> ClientParamList = new List<string>();

                if (GlobalValue.GOLD_SAMPLE_NAME1 != "")
                {
                    if (GlobalValue.ISRESNAP == "1")
                    {
                        golden_sample_path.Add(GlobalValue.GOLDEN_SAMPLE_FOLDER + GlobalValue.GOLD_SAMPLE_NAME1);
                        golden_sample_path.Add(GlobalValue.GOLDEN_SAMPLE_FOLDER + GlobalValue.GOLD_SAMPLE_NAME2);
                        station.Add(GlobalValue.CONFIG_STATION + "1");
                        station.Add(GlobalValue.CONFIG_STATION + "2");
                        lines.Add(GlobalValue.CONFIG_LINE);
                        RegisterList.Add(GlobalValue.HParam1);
                        RegisterList.Add(GlobalValue.HParam2);
                        ClientParamList.Add(GlobalValue.ModelParam);
                    }
                    else
                    {
                        golden_sample_path.Add(GlobalValue.GOLDEN_SAMPLE_FOLDER + GlobalValue.GOLD_SAMPLE_NAME1);
                        station.Add(GlobalValue.CONFIG_STATION);
                        lines.Add(GlobalValue.CONFIG_LINE);
                        RegisterList.Add(GlobalValue.HParam1);
                        ClientParamList.Add(GlobalValue.ModelParam);
                    }
                }
                else
                {
                    station.Add("Default");
                    lines.Add(GlobalValue.CONFIG_LINE);
                    golden_sample_path.Add(GlobalValue.TEST_GOLDEN_SAMPLE_PATH);
                    RegisterList.Add("{\"method\": \"SURF\", \"scale\": 4, \"region\": [2392,901,1797,1456], \"delta\": 0.7}");
                    ClientParamList.Add("{\"System\":{\"local_config\":false,\"debug\":false,\"client_url\":\"http://127.0.0.1:8889/\",\"gluepath_inspection_registration\":true,\"normal_inspection_registration\":true,\"gluepath_inspection_use_gpu\":true,\"normal_inspection_use_gpu\":false,\"new_project\":false},\"Detector\":{\"CenterNet\":{},\"YOLOv3\":{\"BACK_BONE\":\"Darknet\",\"DEVICE\":\"cpu\",\"YOLO_INPUT_SIZE\":320,\"ORIGINAL_HEIGHT\":320,\"ORIGINAL_WIDTH\":320,\"CONF_THRESHOLD\":0.5,\"NMS_THRESHOLD\":0.5,\"NUMBER_CLASS\":2}},\"FunctionThreshold\":{\"default\":0.4},\"GluePathInspection\":{\"roi_list\":[[290,700,360,2450],[2700,700,360,2450],[290,2810,360,2450],[2700,2810,360,2450],[290,700,2450,360],[4790,700,2450,360]],\"position_list\":[\"tl\",\"tr\",\"bl\",\"br\",\"l\",\"r\"]}}");
                }

                startResult = CallAlg.getInstance().start(ClientParamList,RegisterList, golden_sample_path);

                Task.Factory.StartNew(() => UpdateTb(loading_tv, loadingText),
                    new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
                LogUtils.d("startThread 开启python线程结果：" + startResult);
                if (!startResult.StartsWith("error"))
                {
                    isPythonThreadStarted = true;
                    isRpcReady = true;
                    LogUtils.d("rpc已开启");
                    if (startResult.ToLower().Equals("false"))
                    {
                        LogUtils.d("rpc已开启，start python thread失败 ");
                        if (MessageBox.Show("rpc已开启，start python thread失败,请重启程序") == MessageBoxResult.OK)
                        {
                            Task.Factory.StartNew(() => ShutdownApplication(),
                            new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
                        }
                    }
                    else
                    {
                        ThreadStartPythonStop();
                    }
                }
                
            }
            StartPythonCount++;
        }
        #endregion

        private void stopPythonThread()
        {
            CallAlg.getInstance().stop();
        }
        private void Result(IAsyncResult ar)
        {
            //当接收到请求后程序流会走到这里
            if(httpobj.IsListening)
            {
                //继续异步监听
                httpobj.BeginGetContext(Result, null);
                var guid = Guid.NewGuid().ToString();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"接到新的请求:{guid},时间：{DateTime.Now.ToString()}");
                //获得context对象
                var context = httpobj.EndGetContext(ar);
                var request = context.Request;
                var response = context.Response;
                ////如果是js的ajax请求，还可以设置跨域的ip地址与参数
                context.Response.ContentType = "text/plain;charset=UTF-8";//告诉客户端返回的ContentType类型为纯文本格式，编码为UTF-8
                context.Response.AddHeader("Content-type", "text/plain");//添加响应头信息
                context.Response.ContentEncoding = Encoding.UTF8;
                string returnObj = null;//定义返回客户端的信息
                if (request.HttpMethod == "POST" && request.InputStream != null)
                {
                    //处理客户端发送的请求并返回处理信息
                    returnObj = HandleRequest(request, response);
                }
                else
                {
                    returnObj = $"不是post请求或者传过来的数据为空";
                }
                var returnByteArr = Encoding.UTF8.GetBytes(returnObj);//设置客户端返回信息的编码
                try
                {
                    using (var stream = response.OutputStream)
                    {
                        //把处理信息返回到客户端
                        stream.Write(returnByteArr, 0, returnByteArr.Length);
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"网络崩了：{ex.ToString()}");
                }
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"请求处理完成：{guid},时间：{ DateTime.Now.ToString()}\r\n");
            }
           
        }

        private string HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            string data = null;
            try
            {
                var byteList = new List<byte>();
                var byteArr = new byte[2048];
                int readLen = 0;
                int len = 0;
                //接收客户端传过来的数据并转成字符串类型
                do
                {
                    readLen = request.InputStream.Read(byteArr, 0, byteArr.Length);
                    len += readLen;
                    byteList.AddRange(byteArr);
                } while (readLen != 0);
                data = Encoding.UTF8.GetString(byteList.ToArray(), 0, len);

                Task.Factory.StartNew(() => exeAlgComplete(data),
                 new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
                
                //获取得到数据data可以进行其他操作
                Console.WriteLine(data);
            }
            catch (Exception ex)
            {
                response.StatusDescription = "404";
                response.StatusCode = 404;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"在接收数据时发生错误:{ex.ToString()}");
                return $"在接收数据时发生错误:{ex.ToString()}";//把服务端错误信息直接返回可能会导致信息不安全，此处仅供参考
            }
            response.StatusDescription = "200";//获取或设置返回给客户端的 HTTP 状态代码的文本说明。
            response.StatusCode = 200;// 获取或设置返回给客户端的 HTTP 状态代码。
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"接收数据完成:{data.Trim()},时间：{DateTime.Now.ToString()}");
            return $"接收数据完成";
        }

        #region  //串口通讯 光源控制和PLC通讯
        public Serial m_pSerialController = null;
        public MelsecFxSerial melsecSerial = null;
        public bool m_bLightOpened = false;

        //初始化串口
        public bool InitSerial()
        {
            bool b1 = false;
            m_pSerialController = new Serial();
            if(m_pSerialController != null)
            {
                if(b1 = m_pSerialController.OpenSerialPort(GlobalValue.LIGHTCOMPORT, 19200, 8, "1", "NONE"))
                {
                    CloseBarLight(0);
                    m_pSerialController.ReceiveMesg += new DelegateReceiveMesg(_receiveControllerMsg);
                }
                else
                {
                    MessageBox.Show(GlobalValue.LIGHTCOMPORT +"未打开");
                }
            }

            try
            {
                melsecSerial = new MelsecFxSerial();

                melsecSerial.SerialPortInni(sp =>
                {
                    sp.PortName = GlobalValue.PLCCOMPORT;
                    sp.BaudRate = 9600;
                    sp.DataBits = 7;
                    sp.StopBits = System.IO.Ports.StopBits.One;
                    sp.Parity = System.IO.Ports.Parity.Even;
                });

                melsecSerial.Open();
            }
            catch(Exception EX)
            {
                MessageBox.Show("Error:"+EX.Message);
                return false;
            }
         
            return b1;

        }
        //反初始化串口
        public bool UnitSerial()
        {
            bool b1 = false;
            if (m_pSerialController != null && m_pSerialController.m_bValid)
               if( b1 = m_pSerialController.CloseSerialPort())
                {
                    m_pSerialController.ReceiveMesg -= new DelegateReceiveMesg(_receiveControllerMsg);
                }

           if(melsecSerial != null && melsecSerial.IsOpen())
            {
                melsecSerial.Close();
            }
            return b1;
        }

        //接收控制器传过来的数据
        public void _receiveControllerMsg(string Message)
        {
            try
            {
              if(Message != "")
                {
                    LogUtils.d("controller response :" + Message);
                    m_bLightOpened = true;
                }
            }
            catch (Exception ex)
            {
                LogUtils.e("controller response error :" + ex.Message);
            }
        }
        //开灯
        public void OpenBarLight(int i_lightness)
        {
            string Lightness = i_lightness.ToString();
            Lightness = string.Format("{0:0000}", int.Parse(Lightness));

            string strTemp = "";
            strTemp = "SA" + Lightness /*+ "#SB" + Lightness + "#SC" + Lightness + "#SD" + Lightness*/ + "#";
            m_pSerialController.SendData(strTemp);

        }  //亮度从0到255
        //关灯
        public void CloseBarLight(int i_lightness)
        {
            string Lightness = i_lightness.ToString();
            Lightness = string.Format("{0:0000}", int.Parse(Lightness));

            string strTemp = "";
            strTemp = "SA" + Lightness /*+ "#SB" + Lightness + "#SC" + Lightness + "#SD" + Lightness*/ + "#";
            m_pSerialController.SendData(strTemp);
        }

        #region  读写三菱寄存器
        public short PlcRead(string address)
        {
            if (melsecSerial != null && melsecSerial.IsOpen())
            {
                OperateResult<short> M = melsecSerial.ReadInt16(address);
                if(M.IsSuccess)
                {
                    LogUtils.d("读取PLC"+ address+":"+M.Content.ToString());
                    return M.Content;
                }
                return -1;
            }
            else
            {
                return -2;
            }
        }
        public bool PlcWrite(string address, short x)
        {
            if (melsecSerial!=null && melsecSerial.IsOpen())
            {
               return melsecSerial.Write(address, x).IsSuccess;
            }
            return false;
        }
        public bool PlcReadInput(string address)
        {
            if (melsecSerial != null && melsecSerial.IsOpen())
            {
                OperateResult<bool> M = melsecSerial.ReadBool(address);
                if (M.IsSuccess)
                {
                    return M.Content;
                }
                return false;
            }
            else
            {
                return false;
            }
        }

        //写入结果
        public bool InformPlcResult(bool TF)
        {
            if (GlobalValue.ISUSEDMESMANUAL == "1")
            {
                // string Station_type = getIniResBean.phase;
                if (GlobalValue.MES_RESULT == "0")
                {
                    return PlcWrite("D30", 1);
                }
                else
                {
                    return PlcWrite("D30", 2);
                }
            }
            else
            {
                if (TF)
                {
                    return PlcWrite("D30", 1);
                }
                else
                {
                    return PlcWrite("D30", 2);
                }
            }    
        }

        //重测程序
        public bool InformPlcAgain()
        {
            return PlcWrite("D30", 3);
        }

        //重新扫码
        public bool InformPlcScanBarcode()
        {
            return PlcWrite("D30", 4);
        }


        //0 左进右出 右进左出   PLC方向
        public bool PlcStartDir(short x)
        {
            return PlcWrite("D40", x);
        }


        //获取拍照信号
        public short GetSnap()
        {
            return PlcRead("D20");
        }

        //获取拍照信号
        public bool ResetSnap()
        {
            return PlcWrite("D20", 0);
        }


        //获取左启动信号
        public bool GetStartSignelLeft()
        {
            return PlcReadInput("X0");
        }

        //获取右启动信号
        public bool GetStartSignelRight()
        {
            return PlcReadInput("X4");
        }

        public bool CheckDUT()
        {
            return PlcReadInput("X6");
        }

        //扫码枪禁用 0未扫码 1扫码完成 2扫码枪禁用
        public bool PlcCodeResult(short x)
        {
            return PlcWrite("D10", x);
        }


        #endregion

        #endregion

        #region 刷新时间线程
        #region 线程处理
        public Thread UpdateCTtimeThread = null;
        public bool m_bThreadUpdateCTtimeThreadLife = false;
        public bool m_bThreadUpdateCTtimeThreadTerminate = false;

        public void UpdateCTtimeFunction()
        {
            // Thread Loop
            while (m_bThreadUpdateCTtimeThreadLife)
            {
                DoUpdateCTtimeThreadJob();
                Thread.Sleep(100);
            }
            m_bThreadUpdateCTtimeThreadTerminate = true;
        }
        public void UpdateCTtimeRun()
        {
            if (m_bThreadUpdateCTtimeThreadLife)
            {
                UpdateCTtimeStop();
                Thread.Sleep(100);
            }
            m_bThreadUpdateCTtimeThreadLife = true;
            m_bThreadUpdateCTtimeThreadTerminate = false;
            UpdateCTtimeThread = new Thread(new ThreadStart(UpdateCTtimeFunction));
            UpdateCTtimeThread.IsBackground = false;
            UpdateCTtimeThread.Start();
        }
        public void UpdateCTtimeStop()
        {
            m_bThreadUpdateCTtimeThreadLife = false;

            if (null != UpdateCTtimeThread)
            {
                UpdateCTtimeThread.Abort();
            }

            UpdateCTtimeThread = null;
        }
        public void DoUpdateCTtimeThreadJob()
        {
            string x = m_pWatchTime.ElapsedTimeSecond().ToString("F1");
            UpdateCTTimeText(x);
        }
        #endregion
        #endregion

        #region 自动模式
        //自动程序线程相关参数
        public bool m_bFinalResult = false;        //最终结果判断
        public EStepSelectWork m_estepCurrent;
        public EStepSelectWork m_estepPrevious;
        Thread m_ThreadSelectWork = null;
        public bool m_bThreadSelectWorkLife = false;
        public bool m_bThreadSelectWorkTerminate = false;
        public int m_iRetryTimes = 0;   //重新尝试次数
        public int m_iRetryCOMTimes = 0; //通讯重试次数
        public CStopWatch m_pWatchTime = null;
        public CStopWatch m_PlCCOMTimer = null;
        public CStopWatch m_ControllerCOMTimer = null;
        public string m_str_DUTSN = "NoCode";
        public bool m_nRPCSucessed = false;
        public System.Drawing.Bitmap bMap; //待解码图像


        #region   //跨线程调用控件
        private delegate void outputDelegate(string msg);   //定义控件委托

        private void Output(string msg)   //刷新textbox SN方法
        {
            this.SN.Dispatcher.Invoke(new outputDelegate(OutputAction), msg);

        }

        private void Updatetxt5(string msg) //刷新txt5方法
=> this.txt5.Dispatcher.Invoke(new outputDelegate(Txt5Action), msg);

        private void UpdateImageSource(string ImagePath) //刷新imagesource方法
        {
            //this.CheckImageView.Dispatcher.Invoke(new outputDelegate(UpdateImageSourceAction), ImagePath);
            this.CheckImageLargeView.Dispatcher.Invoke(new outputDelegate(UpdateImageSourceAction), ImagePath);
        }

        private void UpdateLoadingWindow(string msg) //显示窗体
=> this.loadingWait.Dispatcher.Invoke(new outputDelegate(UpdateLoadingWindowShowAction), msg);

        private void UpdateLoadingWindowText(string msg) //显示窗体上文字
=> this.loading_tv.Dispatcher.Invoke(new outputDelegate(UpdateLoadingWindowTextAction), msg);

        private void UpdateDUTNumText(string msg) //显示总数目
=> this.DUTNum.Dispatcher.Invoke(new outputDelegate(UpdateDUTNumTextAction), msg);

        private void UpdateDUTPassText(string msg) //显示pass数目
=> this.passnum.Dispatcher.Invoke(new outputDelegate(UpdateDUTPassTextAction), msg);

        private void UpdateDUTPassFailText(string msg) //显示良率
=> this.passrate.Dispatcher.Invoke(new outputDelegate(UpdateDUTPassFailTextAction), msg);

        private void UpdateCTTimeText(string msg) //刷新时间
=> this.CycleTime.Dispatcher.Invoke(new outputDelegate(UpdateCTTimeTextAction), msg);

        private void OutputAction(string msg)
        {
            if (msg == "clear")
            {
                this.SN.Clear();
                this.SN.IsEnabled = true;
                this.SN.Focus();
            }
        }
        private void Txt5Action(string msg) => this.txt5.Text = msg;
        private void UpdateImageSourceAction(string msg)
        {
            GlobalValue.SHOWIMAGE = getBitmapImage(msg);

            checkImageWidth = GlobalValue.SHOWIMAGE.PixelWidth;
            checkImageHeight = GlobalValue.SHOWIMAGE.PixelHeight;

            //1.bitmap旋转
            //GlobalValue.SHOWIMAGE_BITMAP = QRDecodeUtils.BitmapImage2Bitmap(GlobalValue.SHOWIMAGE);
            //GlobalValue.SHOWIMAGE_BITMAP.RotateFlip(System.Drawing.RotateFlipType.Rotate180FlipNone);
            //GlobalValue.SHOWIMAGE = QRDecodeUtils.BitmapToBitmapImage(GlobalValue.SHOWIMAGE_BITMAP);

            //2.bitmapimage旋转
            TransformedBitmap tb = new TransformedBitmap();
            tb.BeginInit();
            tb.Source = GlobalValue.SHOWIMAGE;
            RotateTransform transform = new RotateTransform(180);
            tb.Transform = transform;
            tb.EndInit();

            //this.CheckImageView.Source = GlobalValue.SHOWIMAGE;
            this.CheckImageLargeView.Source = tb;
        }

        private void UpdateLoadingWindowShowAction(string msg)
        {
            if (msg == "show")
                loadingWait.Visibility = Visibility;
            else
                loadingWait.Visibility = Visibility.Collapsed;
        }
        private void UpdateLoadingWindowTextAction(string msg) => loading_tv.Text = msg;
        private void UpdateDUTNumTextAction(string msg) => DUTNum.Text = msg;
        private void UpdateDUTPassTextAction(string msg) => passnum.Text = msg;
        private void UpdateDUTPassFailTextAction(string msg) => passrate.Text = msg;
        private void UpdateCTTimeTextAction(string msg) => CycleTime.Text = msg;


        #endregion


        //初始化自动参数
        public void InitAutoModePara()
        {
            //1.初始化串口
            InitSerial();

            //2.初始化定时器
            InitWatchObject();

            //3.运行自动模式
            AutoModeRun();
        }
        //反初始化自动参数
        public void UnitAutoModePara()
        {

            //停止自动模式
            AutoModeStop();

            //2.反初始化串口
            UnitSerial();

            //关闭相机
            CloseCamera();

        }
        public void InitWatchObject()
        {
                m_pWatchTime = new CStopWatch();
                m_PlCCOMTimer = new CStopWatch();
                m_ControllerCOMTimer = new CStopWatch();
        }

        //步骤显示
        public void DisPlayLog(TextBox tb,string Log,bool TF)
        {
            if (tb.Text.Length > 500)
            {
                tb.Clear();
            }

            if (TF)
            {
                String StrTemp = DateTime.Now.ToString("HH:mm:ss:fff  ") + Log + "\r\n";
                tb.AppendText(StrTemp);
                tb.ScrollToEnd();
                LogUtils.d(StrTemp);
            }
            else
            {
                String StrTemp = DateTime.Now.ToString("HH:mm:ss:fff  ") + Log + "\r\n";
                tb.AppendText(StrTemp);
                tb.ScrollToEnd();
                LogUtils.d(StrTemp);
            }
        }


        public void ShowGoldenSampleImage()
        {
            result.Source = InfoImage;
        }

        public void UpdateGoldenSampleImage()
        {
            Task.Factory.StartNew(() => ShowGoldenSampleImage(),
                new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
        }

        public void ShowFailImage()
        {
            result.Source = CodeErrorImage;
        }


        public void UpdateFailImage()
        {
            Task.Factory.StartNew(() => ShowFailImage(),
                new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
        }

        //步骤显示
        public void UpdateSysDisplayLog(string Log)
        {
            Task.Factory.StartNew(() => DisPlayLog(tbx_syslog, Log,true),
                new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
        }

        //自动步骤显示
        public void UpdateStepLog(string Log)
        {
            Task.Factory.StartNew(() => UpdateLabelText(label_Step, Log),
                new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
        }

        //SN显示
        public void UpdateSNDisplay(string Log)
        {
            Task.Factory.StartNew(() => UpdateLabelText(labelSN, "SN:"+Log),
                new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
        }

        public void UpdateMESDisplayLog(string Log)
        {
            Task.Factory.StartNew(() => DisPlayLog(tbx_mes, Log,false),
                new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
        }

        //状态复位
        public void ResetAllRCVState()
        {
            m_bFinalResult = false;
            m_iRetryTimes = 0;   //重新尝试次数
            m_iRetryCOMTimes = 0; //通讯重试次数
            m_nRPCSucessed = false;
        }

        //自动模式
        public void AutoModeRun()
        {
            ThreadSelectWorkRun();
        }
        public void AutoModeStop()
        {
            ThreadSelectWorkStop();
        }
        public void setStep(EStepSelectWork estepSelectWork)
        {
            m_estepPrevious = m_estepCurrent;
            m_estepCurrent = estepSelectWork;
        }
        public void ThreadSelectWorkFunction()
        {
            // Thread Loop
            while (m_bThreadSelectWorkLife)
            {
                doRunSelectWorkStep();
                Thread.Sleep(5);
            }
            m_bThreadSelectWorkTerminate = true;
        }
        public void ThreadSelectWorkRun()
        {
            if (m_bThreadSelectWorkLife)
            {
                ThreadSelectWorkStop();
                Thread.Sleep(100);
            }
            m_bThreadSelectWorkLife = true;
            m_bThreadSelectWorkTerminate = false;
            m_ThreadSelectWork = new Thread(new ThreadStart(ThreadSelectWorkFunction));
            m_ThreadSelectWork.Priority = ThreadPriority.Highest;
            m_ThreadSelectWork.IsBackground = true;
            m_ThreadSelectWork.Start();
        }
        public void ThreadSelectWorkStop()
        {
            m_bThreadSelectWorkLife = false;

            if (null != m_ThreadSelectWork)
            {
                Thread.Sleep(100);
                m_ThreadSelectWork.Abort();
            }
            m_ThreadSelectWork = null;
        }      
        //自动模式主体程序
        public void doRunSelectWorkStep()
        {
            switch (m_estepCurrent)
            {
                case EStepSelectWork.STEP_SELECTWORK_INIT: UpdateStepLog("STEP_SELECTWORK_INIT");
                    {
                        UpdateSysDisplayLog("初始化完成");
                        CloseBarLight(0);
                        UpdateCTtimeStop();//结束计时
                        ResetAllRCVState();
                        phase = 0;
                        GlobalValue.AutoBarcode_Error = false;
                        setStep(EStepSelectWork.STEP_SELECTWORK_PLC_UPDATEIO);
                        break;
                    }

                case EStepSelectWork.STEP_SELECTWORK_PLC_UPDATEIO: UpdateStepLog("STEP_SELECTWORK_PLC_UPDATEIO");
                    {
                        if (CheckDUT() || GlobalValue.ISDUTCHECK =="0")
                        {
                            //重新刷新结果数据
                            Task.Factory.StartNew(() => refreshLammyRect(updateResBeans, true),
                            new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();

                            m_pWatchTime.Start(); //开始计时
                            UpdateCTtimeRun();
                            if(GlobalValue.IsShowIndicationImage == "0")
                            {
                                setStep(EStepSelectWork.STEP_AUTO_INFORM_CONTROLLER_OPENLIGHT);
                            }
                            else
                            {
                                setStep(EStepSelectWork.STEP_SELECTWORK_IMAGEDISPLAY);
                            }
                            break;
                        }                   
                        break;
                    }


                case EStepSelectWork.STEP_SELECTWORK_IMAGEDISPLAY: UpdateStepLog("STEP_SELECTWORK_IMAGEDISPLAY");
                    {
                        string DisplayPath = "";
                        if (GlobalValue.ISRESNAP == "1")
                        {
                            DisplayPath = GlobalValue.DISPLAY_IMG_FOLDER + GlobalValue.CONFIG_STATION + "1.jpg";
                        }
                        else
                        {
                            DisplayPath = GlobalValue.DISPLAY_IMG_FOLDER + GlobalValue.CONFIG_STATION + ".jpg";
                        }
                        if (!File.Exists(DisplayPath))
                        {
                            DisplayPath = GlobalValue.DISPLAY_IMG_FOLDER + "defaultDisplay.jpg";
                        }
                        //显示正常放置方式
                        UpdateImageSource(DisplayPath);

                        //上方提示
                        UpdateGoldenSampleImage();

                        setStep(EStepSelectWork.STEP_AUTO_INFORM_CONTROLLER_OPENLIGHT);
                        break;
                    }


                case EStepSelectWork.STEP_AUTO_INFORM_CONTROLLER_OPENLIGHT:UpdateStepLog("STEP_AUTO_INFORM_CONTROLLER_OPENLIGHT");
                    {
                       // UpdateSysDisplayLog("开始打开光源");
                        m_iRetryTimes = 0;   //重新尝试次数
                        m_ControllerCOMTimer.Start();
                        setStep(EStepSelectWork.STEP_AUTO_INFORM_CONTROLLER_OPENLIGHT_RESPONSE);
                        break;
                    }

                case EStepSelectWork.STEP_AUTO_INFORM_CONTROLLER_OPENLIGHT_RESPONSE:UpdateStepLog("STEP_AUTO_INFORM_CONTROLLER_OPENLIGHT_RESPONSE");
                    {                   
                            UpdateSysDisplayLog("打开光源");
                            if (m_pSerialController != null && m_pSerialController.m_bValid)
                            {
                                m_bLightOpened = false;
                                OpenBarLight(GlobalValue.LIGHTNESS);                             
                                setStep(EStepSelectWork.STEP_AUTO_INFORM_CONTROLLER_OPENLIGHT_DONE);
                                break;
                            }
                            else
                            {
                                Output("clear");//清空SN控件
                                UpdateSysDisplayLog("光源控制器通讯超时，请检查光源控制器连接状态！");
                                setStep(EStepSelectWork.STEP_SELECTWORK_INIT);
                                break;
                            }
                    }

                case EStepSelectWork.STEP_AUTO_INFORM_CONTROLLER_OPENLIGHT_DONE: UpdateStepLog("STEP_AUTO_INFORM_CONTROLLER_OPENLIGHT_DONE");
                    {
                        if (m_bLightOpened)
                        {
                            UpdateSysDisplayLog("光源已打开");
                            m_bLightOpened = false;
                            setStep(EStepSelectWork.STEP_AUTO_WAIT_PLC_SNAP_RESPONSE);
                            break;
                        }
                        else
                        {
                            if (m_ControllerCOMTimer.ElapsedTime() > 2000)
                            {
                                Output("clear");//清空SN控件
                                UpdateCTtimeStop();//结束计时
                                UpdateSysDisplayLog("光源控制器通讯超时，请检查光源控制器连接状态！");
                                setStep(EStepSelectWork.STEP_SELECTWORK_INIT);
                                break;
                            }
                            else
                            {
                                setStep(EStepSelectWork.STEP_AUTO_INFORM_CONTROLLER_OPENLIGHT_DONE);
                                break;
                            }
                        }
                    }


                case EStepSelectWork.STEP_AUTO_SHOW_SECOND_IMAGE:UpdateStepLog("STEP_AUTO_SHOW_SECOND_IMAGE");
                    {
                        if(!CheckDUT())
                        {
                            string DisplayPath = GlobalValue.DISPLAY_IMG_FOLDER + GlobalValue.CONFIG_STATION + "2.jpg";
                            if (!File.Exists(DisplayPath))
                            {
                                DisplayPath = GlobalValue.DISPLAY_IMG_FOLDER + "defaultDisplay.jpg";
                            }
                            //显示第二次放置方式
                            UpdateImageSource(DisplayPath);

                            //上方提示
                            UpdateGoldenSampleImage();

                            setStep(EStepSelectWork.STEP_AUTO_WAIT_PLC_SNAP_RESPONSE);
                            break;
                        }
                        break;
                    }

                case EStepSelectWork.STEP_AUTO_INFORM_SCAN_BARCODE: UpdateStepLog("STEP_AUTO_INFORM_SCAN_BARCODE");
                    {
                        m_iRetryTimes = 0;   //重新尝试次数
                        m_PlCCOMTimer.Start();
                        GlobalValue.AutoBarcode_Error = true;
                        UpdateSysDisplayLog("请重新扫码并重新启动");
                        setStep(EStepSelectWork.STEP_AUTO_INFORM_SCAN_BARCODE_RESPONSE);
                        break;
                    }

                case EStepSelectWork.STEP_AUTO_INFORM_SCAN_BARCODE_RESPONSE: UpdateStepLog("STEP_AUTO_INFORM_SCAN_BARCODE_RESPONSE");
                    {
                        if (m_iRetryTimes > 3 || m_PlCCOMTimer.ElapsedTime() > 2000)
                        {
                            Output("clear");//清空SN控件
                            UpdateSysDisplayLog("PLC通讯超时，请检查PLC连接状态！");
                            setStep(EStepSelectWork.STEP_SELECTWORK_INIT);
                            break;
                        }
                        else
                        {
                            if (melsecSerial != null && melsecSerial.IsOpen())
                            {
                                if(InformPlcScanBarcode())//通知PLC重新扫码（复位）
                                {
                                    setStep(EStepSelectWork.STEP_AUTO_INFORM_SCAN_BARCODE_DONE);
                                    break;
                                }
                                else
                                {
                                    m_iRetryTimes++;
                                    setStep(EStepSelectWork.STEP_AUTO_INFORM_SCAN_BARCODE_RESPONSE);
                                    break;
                                }
                            }
                            else
                            {
                                Output("clear");//清空SN控件
                                UpdateSysDisplayLog("PLC通讯超时，请检查PLC连接状态！");
                                setStep(EStepSelectWork.STEP_SELECTWORK_INIT);
                                break;
                            }
                        }
                    }

                case EStepSelectWork.STEP_AUTO_INFORM_SCAN_BARCODE_DONE:UpdateStepLog("STEP_AUTO_INFORM_SCAN_BARCODE_DONE");
                    {
                        if (CheckDUT())
                        {
                            setStep(EStepSelectWork.STEP_AUTO_WAIT_PLC_SNAP_RESPONSE);
                            break;
                        }
                        break;
                    }

                case EStepSelectWork.STEP_AUTO_WAIT_PLC_SNAP_RESPONSE: UpdateStepLog("STEP_AUTO_WAIT_PLC_SNAP_RESPONSE");
                    {
                        if (melsecSerial != null && melsecSerial.IsOpen())
                        {
                            if (GetSnap() == 1)
                            {
                                Thread.Sleep(500);
                                setStep(EStepSelectWork.STEP_AUTO_PLC_SNAP_RESPONSE_DONE);
                                break;
                            }
                            break;
                        }
                        else
                        {
                            Output("clear");//清空SN控件
                            UpdateSysDisplayLog("未连接上PLC，请检查PLC连接状态！");
                            setStep(EStepSelectWork.STEP_SELECTWORK_INIT);
                            break;
                        }                                                       
                    }

                case EStepSelectWork.STEP_AUTO_PLC_SNAP_RESPONSE_DONE:UpdateStepLog("STEP_AUTO_PLC_SNAP_RESPONSE_DONE");
                    {

                        UpdateSysDisplayLog("收到PLC触发拍照指令");
                        setStep(EStepSelectWork.STEP_AUTO_VS_INFORM_CAM);
                        break;
                    }

                case EStepSelectWork.STEP_AUTO_VS_INFORM_CAM:
                    {
                        UpdateSysDisplayLog("相机开始拍照");
                        m_bIsSvaebmpSuccessed = true;
                        m_iRetryTimes = 0;
                        m_PlCCOMTimer.Start();
                        setStep(EStepSelectWork.STEP_AUTO_VS_CAM_RESPOND);
                        break;
                    }

                case EStepSelectWork.STEP_AUTO_VS_CAM_RESPOND:UpdateStepLog("STEP_AUTO_VS_CAM_RESPOND");
                    {
                        UpdateSysDisplayLog("相机拍照中");
                        if (m_iRetryTimes > 2 || m_PlCCOMTimer.ElapsedTime() > 5000)
                        {
                            Output("clear");//清空SN控件
                            UpdateSysDisplayLog("相机拍照超时，请检查相机连接状态！");
                            setStep(EStepSelectWork.STEP_SELECTWORK_INIT);
                            break;
                        }
                        else
                        {
                            if (null != m_pHikCamera && m_pHikCamera.m_bEnabled)
                            {
                               if(m_pHikCamera.GetOneImage()==0)
                                {
                                    setStep(EStepSelectWork.STEP_AUTO_VS_CAM_RESPOND_DONE);
                                    break;
                                }
                               else
                                {
                                    m_iRetryTimes++;
                                    setStep(EStepSelectWork.STEP_AUTO_VS_CAM_RESPOND);
                                    break;
                                }
                            }
                            else
                            {
                                Output("clear");//清空SN控件
                                CloseBarLight(0);
                                UpdateSysDisplayLog("相机掉线，请检查相机连接状态！");
                                setStep(EStepSelectWork.STEP_SELECTWORK_INIT);
                                break;
                            }
                        }
                    }

                case EStepSelectWork.STEP_AUTO_VS_CAM_RESPOND_DONE:UpdateStepLog("STEP_AUTO_VS_CAM_RESPOND_DONE");
                    {
                        if(m_bIsSvaebmpSuccessed)
                        {
                            break;
                        }
                        else
                        {
                            if(GlobalValue.ISRESNAP == "1")
                            {
                                phase++;
                                if (phase > 2)
                                {
                                    phase = 1;
                                }
                            }

                            UpdateSysDisplayLog("相机拍照完成");
                            ResetSnap();//重置拍照寄存器
                            if (GlobalValue.QRCodeInOrder == "0" || GlobalValue.AutoBarcode_Error)
                            {
                                GlobalValue.AutoBarcode_Error = false;
                                if (GlobalValue.DUT_SAMPLE_STATION == "1")
                                {
                                    m_str_DUTSN = "PositiveSample";
                                    UpdateSNDisplay(m_str_DUTSN);
                                }
                                else if (GlobalValue.DUT_SAMPLE_STATION == "2")
                                {
                                    m_str_DUTSN = "NegativeSample";
                                    UpdateSNDisplay(m_str_DUTSN);
                                }

                                if (GlobalValue.ISRESNAP == "1")
                                {
                                    if (phase == 2)
                                    {
                                        FunctionIdS.Clear();
                                        foreach (UpdateResBean x in updateResBeans2)
                                        {
                                            FunctionIdS.Add(x.function_id);
                                        }
                                    }
                                    else
                                    {
                                        FunctionIdS.Clear();
                                        foreach (UpdateResBean x in updateResBeans1)
                                        {
                                            FunctionIdS.Add(x.function_id);
                                        }
                                    }
                                }
                                SaveBitmapWithQRcode(m_str_DUTSN, true);
                                setStep(EStepSelectWork.STEP_AUTO_VS_CALLRCP);
                                break;
                            }
                            else
                            {
                                if (GlobalValue.ISRESNAP == "1")
                                {
                                    if (phase == 2)
                                    {
                                        FunctionIdS.Clear();
                                        foreach (UpdateResBean x in updateResBeans2)
                                        {
                                            FunctionIdS.Add(x.function_id);
                                        }
                                        SaveBitmapWithQRcode(m_str_DUTSN, true);
                                        setStep(EStepSelectWork.STEP_AUTO_VS_CALLRCP);
                                        break;
                                    }
                                    else
                                    {
                                        FunctionIdS.Clear();
                                        foreach (UpdateResBean x in updateResBeans1)
                                        {
                                            FunctionIdS.Add(x.function_id);
                                        }
                                        setStep(EStepSelectWork.STEP_AUTO_VS_DECODE);
                                        break;
                                    }
                                }
                                setStep(EStepSelectWork.STEP_AUTO_VS_DECODE);
                                break;
                            }
                        }
                    }

                case EStepSelectWork.STEP_AUTO_VS_DECODE:UpdateStepLog("STEP_AUTO_VS_DECODE");
                    {
                        UpdateSysDisplayLog("开始解二维码");
                        TestThreadStart();
                        resultStr = string.Empty;
                        DMresults.Clear();
                        bMap = null;

                        if (GlobalValue.PART_NUMBER_TYPE == "2")
                        {
                            string str_DUTSNTime = DateTime.Now.ToString("MMdd") + DateTime.Now.ToString("HHmmss");
                            resultStr = str_DUTSNTime;
                            UpdateSysDisplayLog("二维码：" + resultStr);
                            m_str_DUTSN = resultStr;
                            UpdateSNDisplay(m_str_DUTSN);
                            SaveBitmapWithQRcode(m_str_DUTSN, true);
                            setStep(EStepSelectWork.STEP_AUTO_VS_CALLRCP);
                            break;
                        }

                        if (GlobalValue.ISLOCATEDUSED == "0")
                        {
                            //UpdateSysDisplayLog("初始化图片");
                            //获取GoldenSample
                            m_PlCCOMTimer.Start();
                            bMap = QRDecodeUtils.MatchPicBySurf(QRDecodeUtils.BitmapImage2Bitmap(GoldenSurfImage), QRDecodeUtils.BitmapImage2Bitmap(m_BitmapImageForSave),S: GlobalValue.ScaleRatio);
                            
                            if (bMap != null)
                            {
                                UpdateSysDisplayLog("配准成功:" + m_PlCCOMTimer.ElapsedTime().ToString());
                                string ImageFilePath = Directory.GetCurrentDirectory() + "\\Zbardecode\\ZbarDecode\\bin\\Debug\\cutimage.jpg";
                                System.Drawing.Bitmap CropBitmap = crop(bMap, new System.Drawing.Rectangle(GlobalValue.ROI_X, GlobalValue.ROI_Y, GlobalValue.ROI_WIDTH, GlobalValue.ROI_HEIGHT));
                                using (MemoryStream memory = new MemoryStream())
                                {
                                    using (FileStream fs = new FileStream(ImageFilePath, FileMode.Create, FileAccess.ReadWrite))
                                    {
                                        CropBitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Jpeg);
                                        byte[] bytes = memory.ToArray();
                                        fs.Write(bytes, 0, bytes.Length);
                                    }
                                }
                                InitThreadBitmap(CropBitmap, 2);

                                if (GlobalValue.ISZBARCODEUSED == "0")
                                {
                                    InformZbarDecode();//通知Zbar定位失败
                                }
                                else
                                {
                                    StartZbarDecode();//调用zbar解码
                                }
                            }
                            else
                            {
                                UpdateSysDisplayLog("配准失败:" + m_PlCCOMTimer.ElapsedTime().ToString());
                                InitThreadBitmap(m_BitmapForDecode, 1);
                                if (GlobalValue.ISZBARCODEUSED == "0")
                                {
                                    InformZbarDecode();//通知Zbar定位失败
                                }
                                else
                                {
                                    string ImageFilePath = Directory.GetCurrentDirectory() + "\\Zbardecode\\ZbarDecode\\bin\\Debug\\cutimage.jpg";
                                    using (MemoryStream memory = new MemoryStream())
                                    {
                                        using (FileStream fs = new FileStream(ImageFilePath, FileMode.Create, FileAccess.ReadWrite))
                                        {
                                            m_BitmapForDecode.Save(memory, System.Drawing.Imaging.ImageFormat.Jpeg);
                                            byte[] bytes = memory.ToArray();
                                            fs.Write(bytes, 0, bytes.Length);
                                        }
                                    }
                                    StartZbarDecode();//调用zbar解码
                                }
                            }
                        }
                        else
                        {
                            bMap = QRDecodeUtils.LocateBarcode(GlobalValue.CONFIG_STATION,m_BitmapForDecode, GlobalValue.ISLARGEQRCODE);

                            try
                            {
                                if (bMap != null && bMap.Width < 400 )
                                {
                                    string ImageFilePath = Directory.GetCurrentDirectory() + "\\Zbardecode\\ZbarDecode\\bin\\Debug\\cutimage.jpg";
                                    using (MemoryStream memory = new MemoryStream())
                                    {
                                        using (FileStream fs = new FileStream(ImageFilePath, FileMode.Create, FileAccess.ReadWrite))
                                        {
                                            bMap.Save(memory, System.Drawing.Imaging.ImageFormat.Jpeg);
                                            byte[] bytes = memory.ToArray();
                                            fs.Write(bytes, 0, bytes.Length);
                                        }
                                    }
                                    //bMap.Save(ImageFilePath);
                                    InitThreadBitmap(bMap, 2);

                                    if (GlobalValue.ISZBARCODEUSED == "0")
                                    {
                                        InformZbarDecode();//通知Zbar定位失败
                                    }
                                    else
                                    {
                                        StartZbarDecode();//调用zbar解码
                                    }
                                }
                                else
                                {
                                    InitThreadBitmap(m_BitmapForDecode, 1);
                                    InformZbarDecode();//通知Zbar定位失败
                                }

                            }
                            catch
                            {
                                InitThreadBitmap(m_BitmapForDecode, 1);
                                InformZbarDecode();//通知Zbar定位失败
                            }                           
                        }                    
                        StartDecode();
                        setStep(EStepSelectWork.STEP_AUTO_VS_DECODE_RESPOND);
                        UpdateSysDisplayLog("解码中...");
                        break;
                    }


                case EStepSelectWork.STEP_AUTO_VS_DECODE_RESPOND:UpdateStepLog("STEP_AUTO_VS_DECODE_RESPOND");
                    {
                        if (IsDecodeDone())
                        {
                            UpdateSysDisplayLog("线程解码完成");
                            foreach (var obj in threadDecodesList)
                            {
                                if (obj.Result != null && obj.Result.Count > 0)
                                {
                                    foreach (var result in obj.Result)
                                    {
                                        resultStr = resultStr + result.Text;
                                        Console.WriteLine(resultStr);
                                    }
                                    break;
                                }
                            }
                            setStep(EStepSelectWork.STEP_AUTO_VS_DECODE_RESPOND_DATAMATRIC);
                            TestThreadStop();
                            break;
                        }
                        break;
                    }

                case EStepSelectWork.STEP_AUTO_VS_DECODE_RESPOND_DATAMATRIC:UpdateStepLog("STEP_AUTO_VS_DECODE_RESPOND_DATAMATRIC");
                    {
                        if (resultStr != string.Empty)
                        {
                            m_PlCCOMTimer.Start();
                            setStep(EStepSelectWork.STEP_AUTO_VS_DECODE_RESPOND_DONE);
                            break;
                        }
                        else
                        {
                            if(bMap != null && GlobalValue.CODETYPE =="1")
                            {
                                Mat mat = OpenCvSharp.Extensions.BitmapConverter.ToMat(bMap);
                                //转成灰度图
                                Mat src_gray = mat.CvtColor(ColorConversionCodes.BGR2GRAY);
                                OpenCvSharp.Size size = new OpenCvSharp.Size(300, 300);
                                Mat SizeMat = new Mat();
                                Cv2.Resize(src_gray, SizeMat, size);

                                foreach (var blocksize in BlockSizeList)
                                {
                                    Mat SizeMatThreshold = new Mat();
                                    Cv2.AdaptiveThreshold(SizeMat, SizeMatThreshold, 255, AdaptiveThresholdTypes.MeanC, ThresholdTypes.Binary, blocksize, 2);
                                    System.Drawing.Bitmap bitmap12 = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(SizeMatThreshold); // mat 转 bitmap
                                    List<string> strtemp = DMdecoder.DecodeImage(bitmap12, 1, new TimeSpan(0, 0, 0, 0, 200));
                                    if (strtemp.Count > 0)
                                    {
                                        foreach (string str in strtemp)
                                        {
                                            if (str.Length == 10 || str.Length == 23)
                                            {
                                                if (IsNumAndEnCh(str))
                                                {
                                                    DMresults.Add(str);
                                                    break;
                                                }
                                            }
                                        }

                                        if (DMresults.Count > 0)
                                        {
                                            break;
                                        }
                                    }
                                }
                            }
                            m_PlCCOMTimer.Start();
                            setStep(EStepSelectWork.STEP_AUTO_VS_DECODE_RESPOND_DONE);
                            break;
                        }
                    }

                case EStepSelectWork.STEP_AUTO_VS_DECODE_RESPOND_DONE:UpdateStepLog("STEP_AUTO_VS_DECODE_RESPOND_DONE");
                    {
                        if (resultStr != string.Empty)
                        {
                            UpdateSysDisplayLog("二维码："+ resultStr);
                            m_str_DUTSN = resultStr;
                            UpdateSNDisplay(m_str_DUTSN);
                            SaveBitmapWithQRcode(m_str_DUTSN, true);
                            setStep(EStepSelectWork.STEP_AUTO_VS_CALLRCP);
                            break;
                        }
                        else
                        {
                            if (m_PlCCOMTimer.ElapsedTime() > 5000)
                            {
                                Output("clear");//清空SN控件
                                UpdateSysDisplayLog("zbarcode掉线！");
                                setStep(EStepSelectWork.STEP_SELECTWORK_INIT);
                                break;
                            }
                            else
                            {
                                if (DMresults.Count > 0)
                                {
                                    UpdateSysDisplayLog("DM二维码：" + DMresults[0]);
                                    m_str_DUTSN = DMresults[0];
                                    UpdateSNDisplay(m_str_DUTSN);
                                    SaveBitmapWithQRcode(m_str_DUTSN, true);
                                    setStep(EStepSelectWork.STEP_AUTO_VS_CALLRCP);
                                    break;
                                }
                                else
                                {
                                    if (m_bTCPDecodeDone)
                                    {
                                        if (StrReceiveZbarcodeMsg == "CodeError")
                                        {
                                            UpdateSysDisplayLog("二维码识别失败");
                                            m_str_DUTSN = "CodeError";
                                            UpdateSNDisplay(m_str_DUTSN);
                                            //CloseBarLight(0);
                                            SaveBitmapWithQRcode(m_str_DUTSN, true);
                                            UpdateMESDisplayLog("二维码识别失败");

                                        //    UpdateFailImage(); //扫码失败提示

                                            //if (GlobalValue.ISUSEDMESMANUAL == "1")
                                            //{
                                            //    setStep(EStepSelectWork.STEP_AUTO_VS_CALLRCP);  //调用算法
                                            //}
                                            //else
                                            //{
                                            //    phase = 0;
                                            //    setStep(EStepSelectWork.STEP_AUTO_INFORM_SCAN_BARCODE);
                                            //}
                                            ////else
                                            ////{
                                            ////    //重新刷新结果数据
                                            ////    Task.Factory.StartNew(() => refreshLammyRect(updateResBeans, true),
                                            ////   new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
                                            ////    setStep(EStepSelectWork.STEP_AUTO_INFORM_PLC_RESULT);
                                            ////}
                                            setStep(EStepSelectWork.STEP_AUTO_VS_CALLRCP);  //调用算法
                                            break;
                                        }
                                        else
                                        {
                                            UpdateSysDisplayLog("D二维码：" + StrReceiveZbarcodeMsg);
                                            m_str_DUTSN = StrReceiveZbarcodeMsg;
                                            UpdateSNDisplay(m_str_DUTSN);
                                            SaveBitmapWithQRcode(m_str_DUTSN, true);
                                            setStep(EStepSelectWork.STEP_AUTO_VS_CALLRCP);
                                            break;
                                        }
                                    }
                                }
                            }
                            setStep(EStepSelectWork.STEP_AUTO_VS_DECODE_RESPOND_DONE);
                            break;
                        }                    
                    }

                case EStepSelectWork.STEP_AUTO_VS_CALLRCP:UpdateStepLog("STEP_AUTO_VS_CALLRCP");
                    {
                        UpdateSysDisplayLog("更新料号");
                        if(GlobalValue.PART_NUMBER_TYPE == "0")
                        {
                            if (m_str_DUTSN.Length == 23)
                            {
                                GlobalValue.MES_PARTNUMBER = m_str_DUTSN.Substring(2, 10);
                                
                            }
                            else if (m_str_DUTSN.Length == 53)
                            {
                                GlobalValue.MES_PARTNUMBER = m_str_DUTSN.Substring(3, 10);
                            }
                            else
                            {
                                GlobalValue.MES_PARTNUMBER = "";
                            }
                        }
                        else if(GlobalValue.PART_NUMBER_TYPE == "1")
                        {
                            GlobalValue.MES_PARTNUMBER = ReadMesData(m_str_DUTSN);
                        }
                        UpdateSysDisplayLog("查询料号：" + GlobalValue.MES_PARTNUMBER);
                        if (GlobalValue.ISRESNAP == "0")
                        {
                            if (ConfigFileResBeans != null && ConfigFileResBeans.Count > 0)
                            {
                                foreach (ConfigFileRes C in ConfigFileResBeans)
                                {
                                    if (GlobalValue.MES_PARTNUMBER == C.material_num)
                                    {
                                        FunctionIdS.Clear();
                                        FunctionIdS = (C.function_ids).ToList<int>();
                                        //UploadIR.FunctionID = C.function_ids;//新增函数ID列表
                                        break;
                                    }
                                    else
                                    {
                                        FunctionIdS.Clear();
                                    }

                                }
                            }
                        }
                        else
                        {
                            if (ConfigFileResBeans != null && ConfigFileResBeans.Count > 0)
                            {
                                foreach (ConfigFileRes C in ConfigFileResBeans)
                                {
                                    if (GlobalValue.MES_PARTNUMBER == C.material_num)
                                    {
                                        //FunctionIdS.Clear();
                                        FunctionPartNumberID.Clear();
                                        foreach (int f in (C.function_ids).ToList<int>())
                                        {
                                            if (FunctionIdS.Contains(f))
                                            {
                                                FunctionPartNumberID.Add(f);
                                            }
                                        }
                                        FunctionIdS.Clear();
                                        FunctionIdS = FunctionPartNumberID;
                                        //FunctionIdS = (C.function_ids).ToList<int>();
                                        //UploadIR.FunctionID = C.function_ids;//新增函数ID列表
                                        break;
                                    }
                                    else
                                    {
                                        FunctionIdS.Clear();
                                    }
                                }
                            }
                        }
                       
                        Task.Factory.StartNew(() => refreshLammyRect(updateResBeans, true),
                            new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
                        UpdateSysDisplayLog("开始调用算法");
                        m_iRetryTimes = 0;
                        m_nRPCSucessed = false;
                        m_bFinalResult = false;
                        setStep(EStepSelectWork.STEP_AUTO_VS_CALLRCP_RESPOND); 
                        break;
                    }

                case EStepSelectWork.STEP_AUTO_VS_CALLRCP_RESPOND:UpdateStepLog("STEP_AUTO_VS_CALLRCP_RESPOND");
                    {
                        UpdateSysDisplayLog("开始执行算法");
                        exeAlgInMainThread(true);

                        if(GlobalValue.ISRESNAP == "1")
                        {
                            if (phase == 1)
                            {
                                setStep(EStepSelectWork.STEP_AUTO_VS_CALLRCP_RESPOND_DONE);
                                break;
                            }
                            else
                            {
                                setStep(EStepSelectWork.STEP_AUTO_INFORM_PLC_READY2MOVEOUT_DONE);
                                break;
                            }
                        }
                        setStep(EStepSelectWork.STEP_AUTO_INFORM_PLC_READY2MOVEOUT_DONE);
                        break;
                    }

                case EStepSelectWork.STEP_AUTO_INFORM_PLC_READY2MOVEOUT_DONE:UpdateStepLog("STEP_AUTO_INFORM_PLC_READY2MOVEOUT_DONE");
                    {
                        UpdateSysDisplayLog("关闭光源");
                        CloseBarLight(0);
                        m_PlCCOMTimer.Start();
                        setStep(EStepSelectWork.STEP_AUTO_VS_CALLRCP_RESPOND_DONE);
                        break;
                    }
             
                case EStepSelectWork.STEP_AUTO_VS_CALLRCP_RESPOND_DONE:UpdateStepLog("STEP_AUTO_VS_CALLRCP_RESPOND_DONE");
                    {
                        if(!m_nRPCSucessed)
                        {
                            if (m_PlCCOMTimer.ElapsedTime() > 10000)
                            {
                                Output("clear");//清空SN控件
                                UpdateLoadingWindow("hide");
                                UpdateSysDisplayLog("算法超时，请检查算法库连接状态！");
                                setStep(EStepSelectWork.STEP_AUTO_INFORM_PLC_RESULT);
                                break;
                            }
                        }
                        else
                        {
                            UpdateSysDisplayLog("算法执行完成");
                            setStep(EStepSelectWork.STEP_AUTO_INFORM_PLC_RESULT);
                            break;
                        }
                        break;
                    }

                case EStepSelectWork.STEP_AUTO_INFORM_PLC_RESULT:UpdateStepLog("STEP_AUTO_INFORM_PLC_RESULT");
                    {
                        m_iRetryTimes = 0;   //重新尝试次数
                        m_PlCCOMTimer.Start();
                        if (m_str_DUTSN == "CodeError")
                        {
                            UpdateFailImage();
                            m_bFinalResult = false;
                        }
                        UpdateSysDisplayLog("告知PLC执行结果:" + m_bFinalResult.ToString());
                        setStep(EStepSelectWork.STEP_AUTO_INFORM_PLC_RESULT_RESPONSE);
                        break;
                    }

                case EStepSelectWork.STEP_AUTO_INFORM_PLC_RESULT_RESPONSE:UpdateStepLog("STEP_AUTO_INFORM_PLC_RESULT_RESPONSE");
                    {                       
                        if (m_iRetryTimes > 3 || m_PlCCOMTimer.ElapsedTime() > 2000)
                        {
                            Output("clear");//清空SN控件
                            UpdateSysDisplayLog("PLC通讯超时，请检查PLC连接状态！");
                            setStep(EStepSelectWork.STEP_SELECTWORK_INIT);
                            break;
                        }
                        else
                        {
                            if (melsecSerial != null && melsecSerial.IsOpen())
                            {
                                if(GlobalValue.ISRESNAP == "1")
                                {
                                    if (phase == 1)
                                    {
                                        if (m_bFinalResult)
                                        {
                                            InformPlcAgain(); //在测试一遍
                                            setStep(EStepSelectWork.STEP_AUTO_SHOW_SECOND_IMAGE);
                                            break;
                                        }
                                        else
                                        {
                                            if (GlobalValue.ISUSEDMESMANUAL == "1")
                                            {
                                                InformPlcAgain(); //在测试一遍
                                                setStep(EStepSelectWork.STEP_AUTO_SHOW_SECOND_IMAGE);
                                                break;
                                            }
                                            else
                                            {
                                                phase = 0;
                                                InformPlcResult(m_bFinalResult);
                                                setStep(EStepSelectWork.STEP_AUTO_INFORM_PLC_RESULT_DONE);
                                                break;
                                            }
                                        }                           
                                    }
                                    else
                                    {
                                        if (InformPlcResult(m_bFinalResult))
                                        {
                                            setStep(EStepSelectWork.STEP_AUTO_INFORM_PLC_RESULT_DONE);
                                            break;
                                        }
                                        else
                                        {
                                            m_iRetryTimes++;
                                            setStep(EStepSelectWork.STEP_AUTO_INFORM_PLC_RESULT_RESPONSE);
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    if (InformPlcResult(m_bFinalResult))
                                    {
                                        setStep(EStepSelectWork.STEP_AUTO_INFORM_PLC_RESULT_DONE);
                                        break;
                                    }
                                    else
                                    {
                                        m_iRetryTimes++;
                                        setStep(EStepSelectWork.STEP_AUTO_INFORM_PLC_RESULT_RESPONSE);
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                Output("clear");//清空SN控件
                                UpdateSysDisplayLog("PLC通讯超时，请检查PLC连接状态！");
                                setStep(EStepSelectWork.STEP_SELECTWORK_INIT);
                                break;
                            }
                        }
                    }

                case EStepSelectWork.STEP_AUTO_INFORM_PLC_RESULT_DONE:UpdateStepLog("STEP_AUTO_INFORM_PLC_RESULT_DONE");
                    {                     
                       // UpdateProductInfo(m_bFinalResult, true);
                        //if(!m_bFinalResult)
                        //{
                        //    UpdateCTtimeStop();//结束计时
                        //}
                        Output("clear");//清空SN控件
                        setStep(EStepSelectWork.STEP_AUTO_SHOW_FAILWINDOW);
                        break;
                    }

                case EStepSelectWork.STEP_AUTO_SHOW_FAILWINDOW:UpdateStepLog("STEP_AUTO_SHOW_FAILWINDOW");
                    {                  
                        if (!m_bFinalResult)
                        {
                            ThreadStartingPoint(CheckIsDj()); //显示失败大图
                        }
                        m_PlCCOMTimer.Start();
                        setStep(EStepSelectWork.STEP_AUTO_WAIT_CYCLE_END);
                        break;
                    }

                case EStepSelectWork.STEP_AUTO_WAIT_CYCLE_END:UpdateStepLog("STEP_AUTO_WAIT_CYCLE_END");
                    {
                        if(GlobalValue.ISDUTCHECK == "0")
                        {
                            if( m_PlCCOMTimer.ElapsedTime() > GlobalValue.ShowLimitTime || GetStartSignelLeft() || GetStartSignelRight())
                            {
                                Output("clear");//清空SN控件
                                UpdateCTtimeStop();//结束计时
                                setStep(EStepSelectWork.STEP_SELECTWORK_PLC_UPDATEIO);
                                break;
                            }
                        }
                        else
                        {
                            if (!CheckDUT())
                            {
                                Thread.Sleep(1000);
                                Output("clear");//清空SN控件
                                UpdateCTtimeStop();//结束计时
                                setStep(EStepSelectWork.STEP_SELECTWORK_PLC_UPDATEIO);
                                break;
                            }
                        }
                        break;
                    }                  

                default:
                    {
                        UpdateSysDisplayLog("等待初始化...");
                        setStep(EStepSelectWork.STEP_SELECTWORK_INIT);
                        break;
                    }
            }
        }

        #endregion

        #region 半自动模式

        public EStepManualSelectWork m_eManualstepCurrent;
        public EStepManualSelectWork m_eManualstepPrevious;
        Thread m_ThreadManualSelectWork = null;
        public bool m_bThreadManualSelectWorkLife = false;
        public bool m_bThreadManualSelectWorkTerminate = false;
        public bool m_bManual = false;

        //初始化自动参数
        public void InitManualModePara()
        {
            //2.初始化定时器
            InitWatchObject();

            //3.运行自动模式
            AutoManualModeRun();

        }
        //反初始化自动参数
        public void UnitManualModePara()
        {
            //停止自动模式
            AutoManualModeStop();

            //关闭相机
            CloseCamera();

        }
        //设置手动模式
        public void setManualStep(EStepManualSelectWork estepManualSelectWork)
        {
            m_eManualstepPrevious = m_eManualstepCurrent;
            m_eManualstepCurrent = estepManualSelectWork;
        }

        public void AutoManualModeRun()
        {
            ThreadManualSelectWorkRun();
        }
        public void AutoManualModeStop()
        {
            ThreadManualSelectWorkStop();
        }
        public void ThreadManualSelectWorkFunction()
        {
            // Thread Loop
            while (m_bThreadManualSelectWorkLife)
            {
                doRunManualSelectWorkStep();
                Thread.Sleep(10);
            }
            m_bThreadManualSelectWorkTerminate = true;
        }
        public void ThreadManualSelectWorkRun()
        {
            if (m_bThreadManualSelectWorkLife)
            {
                ThreadSelectWorkStop();
                Thread.Sleep(100);
            }
            m_bThreadManualSelectWorkLife = true;
            m_bThreadManualSelectWorkTerminate = false;
            m_ThreadManualSelectWork = new Thread(new ThreadStart(ThreadManualSelectWorkFunction));
            m_ThreadManualSelectWork.Priority = ThreadPriority.Highest;
            m_ThreadManualSelectWork.IsBackground = true;
            m_ThreadManualSelectWork.Start();
        }
        public void ThreadManualSelectWorkStop()
        {
            m_bThreadManualSelectWorkLife = false;

            if (null != m_ThreadManualSelectWork)
            {
                Thread.Sleep(100);
                m_ThreadManualSelectWork.Abort();
            }
            m_ThreadManualSelectWork = null;
        }
        //自动模式主体程序
        public void doRunManualSelectWorkStep()
        {
            switch (m_eManualstepCurrent)
            {
                case EStepManualSelectWork.STEP_SELECTWORK_INIT:UpdateStepLog("STEP_SELECTWORK_INIT");
                    {
                        UpdateSysDisplayLog("初始化完成");
                        UpdateCTtimeStop();//结束计时
                        ResetAllRCVState();
                        phase = 0;
                        setManualStep(EStepManualSelectWork.STEP_SELECTWORK_DONORMALWORK);
                        break;
                    }

                case EStepManualSelectWork.STEP_SELECTWORK_DONORMALWORK:UpdateStepLog("STEP_SELECTWORK_DONORMALWORK");
                    {
                        if(m_bManual)
                        {
                            Task.Factory.StartNew(() => refreshLammyRect(updateResBeans, true),
                            new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
                            m_pWatchTime.Start(); //开始计时
                            UpdateCTtimeRun();
                            setManualStep(EStepManualSelectWork.STEP_AUTO_VS_INFORM_CAM);
                            break;
                        }
                        //setManualStep(EStepManualSelectWork.STEP_SELECTWORK_DONORMALWORK);
                        break;
                    }

                case EStepManualSelectWork.STEP_AUTO_VS_INFORM_CAM:UpdateStepLog("STEP_AUTO_VS_INFORM_CAM");
                    {
                        m_bManual = false;
                        UpdateSysDisplayLog("相机开始拍照");
                        m_bIsSvaebmpSuccessed = true;
                        m_iRetryTimes = 0;
                        m_PlCCOMTimer.Start();
                        setManualStep(EStepManualSelectWork.STEP_AUTO_VS_CAM_RESPOND);
                        break;
                    }

                case EStepManualSelectWork.STEP_AUTO_VS_CAM_RESPOND:UpdateStepLog("STEP_AUTO_VS_CAM_RESPOND");
                    {
                        UpdateSysDisplayLog("相机拍照中");
                        if (m_iRetryTimes > 2 || m_PlCCOMTimer.ElapsedTime() > 5000)
                        {
                            Output("clear");//清空SN控件
                            UpdateSysDisplayLog("相机拍照超时，请检查相机连接状态！");
                            setManualStep(EStepManualSelectWork.STEP_SELECTWORK_INIT);
                            break;
                        }
                        else
                        {
                            if (null != m_pHikCamera && m_pHikCamera.m_bEnabled)
                            {
                                if (m_pHikCamera.GetOneImage() == 0)
                                {
                                    setManualStep(EStepManualSelectWork.STEP_AUTO_VS_CAM_RESPOND_DONE);
                                    break;
                                }
                                else
                                {
                                    m_iRetryTimes++;
                                    setManualStep(EStepManualSelectWork.STEP_AUTO_VS_CAM_RESPOND);
                                    break;
                                }
                            }
                            else
                            {
                                Output("clear");//清空SN控件
                                UpdateSysDisplayLog("相机掉线，请检查相机连接状态！");
                                setManualStep(EStepManualSelectWork.STEP_SELECTWORK_INIT);
                                break;
                            }
                        }
                    }

                case EStepManualSelectWork.STEP_AUTO_VS_CAM_RESPOND_DONE:UpdateStepLog("STEP_AUTO_VS_CAM_RESPOND_DONE");
                    {
                        if (m_bIsSvaebmpSuccessed)
                        {
                            break;
                        }
                        else
                        {
                            if(GlobalValue.ISRESNAP =="1")
                            {
                                phase++;
                                if (phase > 2)
                                {
                                    phase = 1;
                                }
                            }
                            UpdateSysDisplayLog("相机拍照完成");
                            if (GlobalValue.QRCodeInOrder == "0")
                            {
                                if (GlobalValue.DUT_SAMPLE_STATION == "1")
                                {
                                    m_str_DUTSN = "PositiveSample";
                                    UpdateSNDisplay(m_str_DUTSN);
                                }
                                else if (GlobalValue.DUT_SAMPLE_STATION == "2")
                                {
                                    m_str_DUTSN = "NegativeSample";
                                    UpdateSNDisplay(m_str_DUTSN);
                                }
                                if(GlobalValue.ISRESNAP =="1")
                                {
                                    if (phase == 2)
                                    {
                                        FunctionIdS.Clear();
                                        foreach (UpdateResBean x in updateResBeans2)
                                        {
                                            FunctionIdS.Add(x.function_id);
                                        }
                                    }
                                    else
                                    {
                                        FunctionIdS.Clear();
                                        foreach (UpdateResBean x in updateResBeans1)
                                        {
                                            FunctionIdS.Add(x.function_id);
                                        }
                                    }
                                }
                                SaveBitmapWithQRcode(m_str_DUTSN, true);
                                setManualStep(EStepManualSelectWork.STEP_AUTO_VS_CALLRCP);
                                break;
                            }  //手动扫码
                            else
                            {
                                if (GlobalValue.ISRESNAP == "1")
                                {
                                    if (phase == 2)
                                    {
                                        FunctionIdS.Clear();
                                        foreach (UpdateResBean x in updateResBeans2)
                                        {
                                            FunctionIdS.Add(x.function_id);
                                        }
                                        SaveBitmapWithQRcode(m_str_DUTSN, true);
                                        setManualStep(EStepManualSelectWork.STEP_AUTO_VS_CALLRCP);
                                        break;
                                    }
                                    else
                                    {
                                        FunctionIdS.Clear();
                                        foreach (UpdateResBean x in updateResBeans1)
                                        {
                                            FunctionIdS.Add(x.function_id);
                                        }
                                        setManualStep(EStepManualSelectWork.STEP_AUTO_VS_DECODE);
                                        break;
                                    }
                                }
                                else
                                {
                                    setManualStep(EStepManualSelectWork.STEP_AUTO_VS_DECODE);
                                    break;
                                }
                            } //自动扫码
                        }
                    }

                case EStepManualSelectWork.STEP_AUTO_VS_DECODE:UpdateStepLog("STEP_AUTO_VS_DECODE");
                    {
                        UpdateSysDisplayLog("开始解二维码");
                        TestThreadStart();
                        resultStr = string.Empty;
                        DMresults.Clear();

                        if (GlobalValue.ISLOCATEDUSED == "0")
                        {                          
                                InitThreadBitmap(m_BitmapForDecode, 2);
                                if (GlobalValue.ISZBARCODEUSED == "0")
                                {
                                    InformZbarDecode();//通知Zbar定位失败
                                }
                                else
                                {
                                    string ImageFilePath = Directory.GetCurrentDirectory() + "\\Zbardecode\\ZbarDecode\\bin\\Debug\\cutimage.jpg";
                                    m_BitmapForDecode.Save(ImageFilePath);
                                    StartZbarDecode();//调用zbar解码
                                }                                                 
                        }
                        else
                        {
                            bMap = QRDecodeUtils.LocateBarcode(GlobalValue.CONFIG_STATION, m_BitmapForDecode, GlobalValue.ISLARGEQRCODE);

                            if (bMap.Width < 300)
                            {
                                string ImageFilePath = Directory.GetCurrentDirectory() + "\\Zbardecode\\ZbarDecode\\bin\\Debug\\cutimage.jpg";
                                bMap.Save(ImageFilePath);
                                InitThreadBitmap(bMap, 2);

                                if (GlobalValue.ISZBARCODEUSED == "0")
                                {
                                    InformZbarDecode();//通知Zbar定位失败
                                }
                                else
                                {
                                    StartZbarDecode();//调用zbar解码
                                }
                            }
                            else
                            {
                                InitThreadBitmap(m_BitmapForDecode, 1);
                                InformZbarDecode();//通知Zbar定位失败
                            }
                        }
                        StartDecode();
                        setManualStep(EStepManualSelectWork.STEP_AUTO_VS_DECODE_RESPOND);
                        break;
                    }           

                case EStepManualSelectWork.STEP_AUTO_VS_DECODE_RESPOND:UpdateStepLog("STEP_AUTO_VS_DECODE_RESPOND");
                    {
                        if (IsDecodeDone())
                        {
                            foreach (var obj in threadDecodesList)
                            {
                                if (obj.Result != null && obj.Result.Count > 0)
                                {
                                    foreach (var result in obj.Result)
                                    {
                                        resultStr = resultStr + result.Text;
                                        Console.WriteLine(resultStr);
                                    }
                                    break;
                                }
                            }
                            setManualStep(EStepManualSelectWork.STEP_AUTO_VS_DECODE_RESPOND_DATAMATRIC);
                            TestThreadStop();
                            break;
                        }
                        break;
                    }


                case EStepManualSelectWork.STEP_AUTO_VS_DECODE_RESPOND_DATAMATRIC:UpdateStepLog("STEP_AUTO_VS_DECODE_RESPOND_DATAMATRIC");
                    {
                        if (resultStr != string.Empty)
                        {
                            m_PlCCOMTimer.Start();
                            setStep(EStepSelectWork.STEP_AUTO_VS_DECODE_RESPOND_DONE);
                            break;
                        }
                        else
                        {
                           if(bMap != null)
                            {
                                List<string> STR = DMdecoder.DecodeImage(bMap, 1, new TimeSpan(0, 0, 0, 0, 200));
                                if (STR.Count > 0)
                                {
                                    foreach (string str in STR)
                                    {
                                        if (str.Length == 10 || str.Length == 23 || str.Length > 50)
                                        {
                                            if (IsNumAndEnCh(str))
                                            {
                                                DMresults.Add(str);
                                            }
                                        }
                                    }
                                }
                            }                         
                            m_PlCCOMTimer.Start();
                            setStep(EStepSelectWork.STEP_AUTO_VS_DECODE_RESPOND_DONE);
                            break;
                        }
                    }


                case EStepManualSelectWork.STEP_AUTO_VS_DECODE_RESPOND_DONE:UpdateStepLog("STEP_AUTO_VS_DECODE_RESPOND_DONE");
                    {
                        if (resultStr != string.Empty)
                        {
                            UpdateSysDisplayLog("二维码：" + resultStr);
                            m_str_DUTSN = resultStr;
                            UpdateSNDisplay(m_str_DUTSN);
                            SaveBitmapWithQRcode(m_str_DUTSN, true);
                            setManualStep(EStepManualSelectWork.STEP_AUTO_VS_CALLRCP);
                            break;
                        }
                        else
                        {
                            if(m_PlCCOMTimer.ElapsedTime() > 5000)
                            {
                                Output("clear");//清空SN控件
                                UpdateSysDisplayLog("zbarcode掉线！");
                                setManualStep(EStepManualSelectWork.STEP_SELECTWORK_INIT);
                                break;
                            }
                            else
                            {
                                if(DMresults.Count>0)
                                {
                                    UpdateSysDisplayLog("DM二维码：" + DMresults[0]);
                                    m_str_DUTSN = DMresults[0];
                                    UpdateSNDisplay(m_str_DUTSN);
                                    SaveBitmapWithQRcode(m_str_DUTSN, true);
                                    setManualStep(EStepManualSelectWork.STEP_AUTO_VS_CALLRCP);
                                    break;
                                }
                                else
                                {
                                    if (m_bTCPDecodeDone)
                                    {
                                        if (StrReceiveZbarcodeMsg == "CodeError")
                                        {
                                            UpdateSysDisplayLog("二维码识别失败");
                                            m_str_DUTSN = "CodeError";
                                            UpdateSNDisplay(m_str_DUTSN);
                                            SaveBitmapWithQRcode(m_str_DUTSN, true);
                                            UpdateMESDisplayLog("二维码识别失败");

                                            //if(GlobalValue.ISUSEDMESMANUAL == "1")
                                            //{
                                                setManualStep(EStepManualSelectWork.STEP_AUTO_VS_CALLRCP);
                                                break;
                                            //}
                                            //else
                                            //{
                                            //    //重新刷新结果数据
                                            //    Task.Factory.StartNew(() => refreshLammyRect(updateResBeans, true),
                                            //   new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
                                            //    setManualStep(EStepManualSelectWork.STEP_AUTO_INFORM_PLC_RESULT_DONE);
                                            //    break;
                                            //}
                                        }
                                        else
                                        {
                                            UpdateSysDisplayLog("D二维码：" + StrReceiveZbarcodeMsg);
                                            m_str_DUTSN = StrReceiveZbarcodeMsg;
                                            UpdateSNDisplay(m_str_DUTSN);
                                            SaveBitmapWithQRcode(m_str_DUTSN, true);
                                            setManualStep(EStepManualSelectWork.STEP_AUTO_VS_CALLRCP);
                                            break;
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }

                case EStepManualSelectWork.STEP_AUTO_VS_CALLRCP:UpdateStepLog("STEP_AUTO_VS_CALLRCP");
                    {
                        UpdateSysDisplayLog("开始调用算法");
                        m_iRetryTimes = 0;
                        m_nRPCSucessed = false;
                        m_bFinalResult = false;
                        setManualStep(EStepManualSelectWork.STEP_AUTO_VS_CALLRCP_RESPOND);
                        break;
                    }

                case EStepManualSelectWork.STEP_AUTO_VS_CALLRCP_RESPOND:UpdateStepLog("STEP_AUTO_VS_CALLRCP_RESPOND");
                    {
                        UpdateSysDisplayLog("开始执行算法");
                        m_PlCCOMTimer.Start();
                        exeAlgInMainThread(true);
                        setManualStep(EStepManualSelectWork.STEP_AUTO_VS_CALLRCP_RESPOND_DONE);
                        break;
                    }

                case EStepManualSelectWork.STEP_AUTO_VS_CALLRCP_RESPOND_DONE:UpdateStepLog("STEP_AUTO_VS_CALLRCP_RESPOND_DONE");
                    {
                        if (!m_nRPCSucessed)
                        {
                            if (m_PlCCOMTimer.ElapsedTime() > 10000)
                            {
                                Output("clear");//清空SN控件
                                UpdateLoadingWindow("hide");
                                UpdateSysDisplayLog("算法超时，请检查算法库连接状态！");
                                setManualStep(EStepManualSelectWork.STEP_AUTO_INFORM_PLC_RESULT_DONE);
                                break;
                            }
                        }
                        else
                        {
                            UpdateSysDisplayLog("算法执行完成");
                            setManualStep(EStepManualSelectWork.STEP_AUTO_INFORM_PLC_RESULT_DONE);
                            break;
                        }
                        break;
                    }

                case EStepManualSelectWork.STEP_AUTO_INFORM_PLC_RESULT_DONE:UpdateStepLog("STEP_AUTO_INFORM_PLC_RESULT_DONE");
                    {
                        if (m_str_DUTSN == "CodeError")
                        {
                            UpdateFailImage();
                            m_bFinalResult = false;
                        }
                        UpdateSysDisplayLog("测试结果:" + m_bFinalResult.ToString());
                        Output("clear");//清空SN控件
                        UpdateCTtimeStop();//结束计时
                        setManualStep(EStepManualSelectWork.STEP_AUTO_SHOW_FAILWINDOW);
                        break;
                    }

                case EStepManualSelectWork.STEP_AUTO_SHOW_FAILWINDOW:UpdateStepLog("STEP_AUTO_SHOW_FAILWINDOW");
                    {
                        if (!m_bFinalResult)
                        {
                            ThreadStartingPoint(CheckIsDj());
                        }
                        setManualStep(EStepManualSelectWork.STEP_SELECTWORK_DONORMALWORK);
                        break;
                    }

                default:
                    {
                        UpdateSysDisplayLog("等待初始化...");
                        setManualStep(EStepManualSelectWork.STEP_SELECTWORK_INIT);
                        break;
                    }
            }
        }


        #endregion

        #region 解码程序
        string UserNameTemp = "";
        string PasswordTemp = "";


        //设置参数变量
        public QRDecodeUtils m_ThreadDecode1 = new QRDecodeUtils(0, 0, 1, GlobalValue.CODETYPE, 0, 3);
        public QRDecodeUtils m_ThreadDecode2 = new QRDecodeUtils(0, 1, 1, GlobalValue.CODETYPE, 90, 5);
        public QRDecodeUtils m_ThreadDecode3 = new QRDecodeUtils(0, 2, 1, GlobalValue.CODETYPE, 180, 7);
        public QRDecodeUtils m_ThreadDecode4 = new QRDecodeUtils(0, 3, 1, GlobalValue.CODETYPE, 270, 9);
        public QRDecodeUtils m_ThreadDecode5 = new QRDecodeUtils(10, 1, 1, GlobalValue.CODETYPE, 0, 11);
        public QRDecodeUtils m_ThreadDecode6 = new QRDecodeUtils(10, 0, 1, GlobalValue.CODETYPE, 90, 13);
        public QRDecodeUtils m_ThreadDecode7 = new QRDecodeUtils(10, 0, 1, GlobalValue.CODETYPE, 180, 15);
        public QRDecodeUtils m_ThreadDecode8 = new QRDecodeUtils(10, 3, 1, GlobalValue.CODETYPE, 270, 17);
        public QRDecodeUtils m_ThreadDecode9 = new QRDecodeUtils(20, 0, 1, GlobalValue.CODETYPE, 0, 19);
        public QRDecodeUtils m_ThreadDecode10 = new QRDecodeUtils(20, 3, 1, GlobalValue.CODETYPE, 90, 21);
        public QRDecodeUtils m_ThreadDecode11 = new QRDecodeUtils(20, 0, 1, GlobalValue.CODETYPE, 180, 23);
        public QRDecodeUtils m_ThreadDecode12 = new QRDecodeUtils(20, 1, 1, GlobalValue.CODETYPE, 270, 25);
        public QRDecodeUtils m_ThreadDecode13 = new QRDecodeUtils(30, 1, 1.5, GlobalValue.CODETYPE, 180, 27);
        public QRDecodeUtils m_ThreadDecode14 = new QRDecodeUtils(0, 0, 1, GlobalValue.CODETYPE, 0, 29);

        public List<QRDecodeUtils> threadDecodesList = new List<QRDecodeUtils>();
        public List<int> BlockSizeList = new List<int>();
        //public List<OpenCvSharp.Point> points = new List<OpenCvSharp.Point>();
        public string resultStr = string.Empty;


        public void InitThreadBitmap(System.Drawing.Bitmap _bitmap, int n)
        {
            //string ImageFilePath = Directory.GetCurrentDirectory() + "\\zxingtestfail\\" + DateTime.Now.ToString("yyMMddHHmmss") + ".png";
            //_bitmap.Save(ImageFilePath);

            foreach (var a in threadDecodesList)
            {
                a.BitmapOrigin = (System.Drawing.Bitmap)_bitmap.Clone();
                a.ZoomNumber = n;
            }
        }

        public void TestThreadStart()
        {
            foreach (var a in threadDecodesList)
            {
                a.QRDecodeUtilsRun();
            }
        }

        public void TestThreadStop()
        {
            foreach (var a in threadDecodesList)
            {
                a.QRDecodeUtilsStop();
            }
        }

        public void InitThreadObject()
        {
            threadDecodesList.Add(m_ThreadDecode1);
            threadDecodesList.Add(m_ThreadDecode2);
            threadDecodesList.Add(m_ThreadDecode3);
            threadDecodesList.Add(m_ThreadDecode4);
            threadDecodesList.Add(m_ThreadDecode5);
            threadDecodesList.Add(m_ThreadDecode6);
            threadDecodesList.Add(m_ThreadDecode7);
            threadDecodesList.Add(m_ThreadDecode8);
            threadDecodesList.Add(m_ThreadDecode9);
            threadDecodesList.Add(m_ThreadDecode10);
            threadDecodesList.Add(m_ThreadDecode11);
            threadDecodesList.Add(m_ThreadDecode12);
            threadDecodesList.Add(m_ThreadDecode13);
            threadDecodesList.Add(m_ThreadDecode14);
        }

        public void InitBlockSizeObject()
        {
            BlockSizeList.Add(3);
            BlockSizeList.Add(5);
            BlockSizeList.Add(7);
            BlockSizeList.Add(9);
            BlockSizeList.Add(11);
            BlockSizeList.Add(13);
            BlockSizeList.Add(15);
            BlockSizeList.Add(17);
            BlockSizeList.Add(21);
            BlockSizeList.Add(23);
            BlockSizeList.Add(27);
            BlockSizeList.Add(31);
        }

        public void StartDecode()
        {
            foreach (var obj in threadDecodesList)
            {
                obj.StartIndex = 1;
            }
        }


        private void CbPlcDirList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int X = cbPlcDirList.SelectedIndex;
            GlobalValue.PLC_COMBOBOX_VALUE = X.ToString();
            switch (X)
            {
                case 0:
                    if (PlcStartDir(0))
                    {
                        GlobalValue.PlcDirection = "0";
                    }
                    else
                        MessageBox.Show("PLC未连接上！");
                    break;
                case 1:
                    if (PlcStartDir(1))
                    {
                        GlobalValue.PlcDirection = "1";
                    }
                    else
                        MessageBox.Show("PLC未连接上！");
                    break;
                case 2:
                    if (PlcStartDir(2))
                    {
                        GlobalValue.PlcDirection = "0";
                    }
                    else
                        MessageBox.Show("PLC未连接上！");
                    break;
                case 3:
                    if (PlcStartDir(3))
                    {
                        GlobalValue.PlcDirection = "1";
                    }
                    else
                        MessageBox.Show("PLC未连接上！");
                    break;
                default:
                    if (PlcStartDir(0))
                    {
                        GlobalValue.PlcDirection = "0";
                    }
                    else
                        MessageBox.Show("PLC未连接上！");
                    break;
            }

            IniUtils.IniWriteValue("Station Configuration", "PLC", GlobalValue.PLC_COMBOBOX_VALUE, "C:\\config\\config.ini");
        }

        private void CbManulCode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int X = cbManulCode.SelectedIndex;
            GlobalValue.QRCodeInOrder = X.ToString();

            switch (X)
            {
                case 0:
                    if (PlcCodeResult(0))
                    {
                        GlobalValue.QRCodeInOrder = "0";
                    }
                    else
                        MessageBox.Show("PLC未连接上！");
                    break;
                case 1:
                    if (PlcCodeResult(2))
                    {
                        GlobalValue.QRCodeInOrder = "1";
                    }
                    else
                        MessageBox.Show("PLC未连接上！");
                    break;          
                default:
                    if (PlcCodeResult(0))
                    {
                        GlobalValue.QRCodeInOrder = "0";
                    }
                    else
                        MessageBox.Show("PLC未连接上！");
                    break;
            }

            SN.Focus();
            IniUtils.IniWriteValue("Station Configuration", "Camera", GlobalValue.QRCodeInOrder, "C:\\config\\config.ini");
        }



        private void Btn_upload_Click(object sender, RoutedEventArgs e)
        {
            if (updateResBeans == null || updateResBeans.Count <= 0)
            {
                MessageBox.Show("函数列表为空，无法进入修改状态");
                return;
            }
            foreach (Object o in LRectList)
            {
                if (o is LammyRect)
                {
                    if (((LammyRect)o).result == GlobalValue.ALG_RESULT_NO)
                    {
                        MessageBox.Show("还未执行算法，无法进入修改状态");
                        return;
                    }
                }
                else
                {
                    if (((DjRect)o).result == GlobalValue.ALG_RESULT_NO)
                    {
                        MessageBox.Show("还未执行算法，无法进入修改状态");
                        return;
                    }
                }
            }
            if (isChanageResultMode)
            {
                return;
            }
            //smartCheckPF.LoginWindow _loginWindow = new smartCheckPF.LoginWindow();
            //_loginWindow.ShowDialog();
            //if (!_loginWindow.IsValid)
            //    return;
            GlobalValue.USERNAME = UserNameTemp;   //获取用户名和密码
            GlobalValue.PASSWORD = PasswordTemp;
            GlobalValue.IsNTFMODE = true;  //确认为NTF模式
            UploadIR.IR = modifyinferenceResult;
            // loadingWait.Visibility = Visibility.Visible;
            commitWorkerwithResult.RunWorkerAsync(UploadIR);
        }


        private void Btn_Login_Click(object sender, RoutedEventArgs e)
        {
            if((string)btn_Login.Content == "登 录")
            {
                smartCheckPF.LoginWindow _loginWindow = new smartCheckPF.LoginWindow();
                _loginWindow.ShowDialog();
                if (!_loginWindow.IsValid)
                    return;
                UserNameTemp = _loginWindow.UserName;   //获取用户名和密码
                PasswordTemp = _loginWindow.UserPassword;

                HttpRequestClient httpRequestClient = new HttpRequestClient();
                string response = "-1";
                httpRequestClient.SetFieldValue("userName", UserNameTemp);
                httpRequestClient.SetFieldValue("userPwd", PasswordTemp);
                httpRequestClient.Upload(GlobalValue.COMMIT_IP_CHECKUSER, out response);
                LogUtils.d("用户登录信息: " + response);
                UploadRes res = JsonConvert.DeserializeObject<UploadRes>(response);

                if (res.code != 200)
                {
                    MessageBox.Show("未查询到用户信息,请联系ME获取账号密码");
                    return;
                }
                else
                {
                    btn_Login.Content = "登 出";
                    SetBtnEnable(true);
                    labelUserInfo.Foreground = Brushes.AliceBlue;
                    labelUserInfo.Content = "欢迎用户:" + UserNameTemp;
                } 
            }
            else
            {
                UserNameTemp = "";
                PasswordTemp = "";
                btn_Login.Content = "登 录";
                labelUserInfo.Foreground = Brushes.Black;
                labelUserInfo.Content = "用户未登录";
                SetBtnEnable(false);
            }          
        }

        /// <summary>
        /// 调试按钮是否可以点击
        /// </summary>
        /// <param name="TF">bool变量</param>
        /// <returns>none</returns>
        private void SetBtnEnable(bool TF)
        {
            btn_modify.IsEnabled = TF;
            btn_open.IsEnabled = TF;
            btn_upload.IsEnabled = TF;
            btn_QRCode.IsEnabled = TF;
        }

        private void BnOpenLight_Click(object sender, RoutedEventArgs e)
        {          
            if (bnOpenLight.IsChecked == true)
            {
                if (m_pSerialController!=null && m_pSerialController.m_bValid)
                {
                    OpenBarLight(GlobalValue.LIGHTNESS);
                }
                else
                    MessageBox.Show("未连接上光源控制器！");
            }
            else
            {
                if (m_pSerialController != null && m_pSerialController.m_bValid)
                {
                    OpenBarLight(0);
                }
                else
                    MessageBox.Show("未连接上光源控制器！");
            }
        }

        private void BnIsFront_Click(object sender, RoutedEventArgs e)
        {
            if(GlobalValue.ISRESNAP == "1")
            {
                if (bnIsFront.IsChecked == true)
                {
                    phase = 2;
                    FunctionIdS.Clear();
                    foreach (UpdateResBean x in updateResBeans2)
                    {
                        FunctionIdS.Add(x.function_id);
                    }
                }
                else
                {
                    phase = 1;
                    FunctionIdS.Clear();
                    foreach (UpdateResBean x in updateResBeans1)
                    {
                        FunctionIdS.Add(x.function_id);
                    }
                }
            }
        }


        public bool IsDecodeDone()
        {
            if (m_ThreadDecode1.StartIndex == 2 && m_ThreadDecode2.StartIndex == 2 && m_ThreadDecode3.StartIndex == 2 && m_ThreadDecode4.StartIndex == 2 && m_ThreadDecode5.StartIndex == 2
                && m_ThreadDecode6.StartIndex == 2 && m_ThreadDecode7.StartIndex == 2 && m_ThreadDecode8.StartIndex == 2 && m_ThreadDecode9.StartIndex == 2 && m_ThreadDecode10.StartIndex == 2
                && m_ThreadDecode11.StartIndex == 2 && m_ThreadDecode12.StartIndex == 2 && m_ThreadDecode13.StartIndex == 2 && m_ThreadDecode14.StartIndex == 2)
                return true;
            else
                return false;
        }

        //裁剪Bitmap
        public System.Drawing.Bitmap crop(System.Drawing.Bitmap oriBmp,System.Drawing.Rectangle rect)
        {

            System.Drawing.Bitmap target = new System.Drawing.Bitmap(rect.Width, rect.Height);

            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(target))
            {
                g.DrawImage(oriBmp, new System.Drawing.Rectangle(0, 0, target.Width, target.Height),
                      rect,
                      System.Drawing.GraphicsUnit.Pixel);
            }
            return target;
        }

        //判断字符串是否乱码
        public static bool IsNumAndEnCh(string input)
        {
            string pattern = @"^[A-Z0-9]+$";
            Regex regex = new Regex(pattern);
            return regex.IsMatch(input);
        }

        #endregion

        #region MES交互
        public string MesError;

        public string ReadMesData(string SerialNum)
        {
            string[] strArr = MES.ReadData(m_str_DUTSN, out MesError);
            string mesMunber = "";
            if (strArr.Count() > 0)
            {
                foreach (string s in strArr)
                {
                    string[] str1 = s.Split('=');
                    if (str1.Count() > 1)
                    {
                        if (str1[0] == "back_end_pn") //获取料号编码
                        {
                            mesMunber = str1[1];
                        }
                        
                    }
                }
            }
            return mesMunber;
        }

        /// <summary>
        ///往Mes系统中写入数据
        /// </summary>
        /// <returns>对象描述，此处是产品名称</returns>
        public bool WriteMESwithResult(string SerialNum, string Station_type,bool PassFail)
        {
            try
            {
                if (GlobalValue.ISUSEDMESMANUAL == "1")
                {
                    // string Station_type = getIniResBean.phase;
                    if (GlobalValue.MES_RESULT == "0")
                    {
                        return MESinterface.MES.AddData(SerialNum, "0", Station_type, GlobalValue.MES_PCNAME, out MesError);
                    }
                    else
                    {
                        return MESinterface.MES.AddData(SerialNum, "1", Station_type, GlobalValue.MES_PCNAME, out MesError);
                    }
                }
                else
                {
                    // string Station_type = getIniResBean.phase;
                    if (PassFail)
                    {
                        return MESinterface.MES.AddData(SerialNum, "0", Station_type, GlobalValue.MES_PCNAME, out MesError);
                    }
                    else
                    {
                        return MESinterface.MES.AddData(SerialNum, "1", Station_type, GlobalValue.MES_PCNAME, out MesError);
                    }
                }
            }
            catch (Exception ex)
            {
                DisPlayLog(tbx_syslog, ex.Message,true);
                return false;
            }

           
        }

        #endregion

        #region  进程间通讯

        public TCPSever m_pTCPSever = new TCPSever();
        public bool m_bTCPDecodeDone = false;
        public string StrReceivePythonMsg = "";
        public string StrReceiveZbarcodeMsg = "";

        public bool InitTCPsever(string ip, string port)
        {
            try
            {
                if (!m_pTCPSever.initTcpSever(ip, port))
                {
                    MessageBox.Show("初始化服务器失败，请检查IP地址和端口号！");
                    return false;
                }

                m_pTCPSever.SocketWatching += new DelegateSocketWatching(_SocketWatching);
                return true;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        public void _SocketWatching(EndPoint RemoteEndPoint, int userID)
        {
            if (userID > 4)
            {
                MessageBox.Show("最多只允许连接四个客户端！");
                return;
            }

            //添加客户端信息
            m_pTCPSever.ClientName.Add(RemoteEndPoint.ToString());

            LogUtils.d(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ": " + RemoteEndPoint.ToString() + "客户端连接成功！");
            m_pTCPSever.ReceiveMsg += new DelegateReceiveMsg(_ReceiveMsg);
        }


        public void _ReceiveMsg(Socket sokClient, string message, int length)
        {
            if (length != 0)
            {
                if(m_pTCPSever.ClientName[0] == sokClient.RemoteEndPoint.ToString())
                {
                    StrReceiveZbarcodeMsg = message;
                    m_bTCPDecodeDone = true;
                }
                else
                {
                    StrReceivePythonMsg = message;
                }
                string T = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff");
                string strMsg = sokClient.RemoteEndPoint.ToString() + " " + T + "-->" + message;
                LogUtils.d(strMsg);
            }
            else
            {
                m_pTCPSever.ClientName.Remove(sokClient.RemoteEndPoint.ToString());
                // 从列表中移除被中断的连接IP
                m_pTCPSever.ReceiveMsg -= new DelegateReceiveMsg(_ReceiveMsg);
                LogUtils.d(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ": " + sokClient.RemoteEndPoint.ToString() + "客户端失去连接！");
            }
        }

        public bool VS2PythonMsg(string msg)
        {
            if (m_pTCPSever.m_IsValid)
            {
                if (m_pTCPSever.ClientName.Count > 1)
                {
                    if (m_pTCPSever.SendMsg(m_pTCPSever.ClientName[1], msg))
                    {
                        string strMsg = "服务器" + "-->" + msg;
                        LogUtils.d(strMsg);
                        return true;
                    }
                }
            }
            return false;
        }

        public void StartZbarDecode()
        {
            if(m_pTCPSever.m_IsValid)
            {
                m_pTCPSever.SendMsg(m_pTCPSever.ClientName[0], "start");
                string strMsg = "服务器" + "-->" + m_pTCPSever.ClientName[0]+ ":start";
                LogUtils.d(strMsg);
                m_bTCPDecodeDone = false;
            }
        }

        public void InformZbarDecode()
        {
            if (m_pTCPSever.m_IsValid)
            {
                m_pTCPSever.SendMsg(m_pTCPSever.ClientName[0], "error");
                string strMsg = "服务器" + "-->" + m_pTCPSever.ClientName[0] + ":error";
                LogUtils.d(strMsg);
                m_bTCPDecodeDone = false;
            }
        }



        #endregion

        #region  弹出新窗体
        private void ThreadStartingPoint(bool IsDjMode)
        {
            Action act = () =>
            {     
                if(IsDjMode)
                {
                    ShowImageWindow showImageWindow = new ShowImageWindow(GlobalValue.DJ_COMPOSEMASK_IMG_PATH, djRectList, checkImageWidth, checkImageHeight, true, IsDjMode);
                }
                else
                {
                     ShowImageWindow showImageWindow = new ShowImageWindow(GlobalValue.SHOWIMAGE, funRectList, checkImageWidth, checkImageHeight, true, IsDjMode);
                }

            };
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, act);
        }


        #endregion

        #region 刷新IO信息
        Thread m_ThreadScanIO = null;
        public bool m_bThreadScanIOLife = false;
        public bool m_bThreadScanIOTerminate = false;

        public void ThreadScanIOFunction()
        {
            // Thread Loop
            while (m_bThreadScanIOLife)
            {
                doRunScanIOStep();
                Thread.Sleep(1000);
            }
            m_bThreadScanIOTerminate = true;
        }
        public void ThreadScanIORun()
        {
            if (m_bThreadScanIOLife)
            {
                ThreadScanIOStop();
                Thread.Sleep(100);
            }
            m_bThreadScanIOLife = true;
            m_bThreadScanIOTerminate = false;
            m_ThreadScanIO = new Thread(new ThreadStart(ThreadScanIOFunction));
            m_ThreadScanIO.Start();
        }


        public void ThreadScanIOStop()
        {
            m_bThreadScanIOLife = false;

            if (null != m_ThreadScanIO)
            {
                Thread.Sleep(100);
                m_ThreadScanIO.Abort();
            }

            m_ThreadScanIO = null;
        }
        public void doRunScanIOStep()
        {
            string str_Time = DateTime.Now.ToString("yyyy-MM-dd ") + DateTime.Now.ToString("HH:mm:ss ");
            Task.Factory.StartNew(() => UpdateLabelText(label_datetime, str_Time),
          new CancellationTokenSource().Token, TaskCreationOptions.None, _syncContextTaskScheduler).Wait();
        }
        #endregion

        #region 读写Mysql数据库
        public MySqlConnection MySqlClient;


        //初始化oracle数据库连接
        public int InitConnectMysqlSever()
        {
            string connStr = String.Format("server={0};user id={1}; password={2}; port={3}; database=INTERFACE; pooling=true; charset=utf8", "10.114.130.191", "FH_USER", "Lenovo2020", 3306);

            MySqlClient = new MySqlConnection(connStr);
            try
            {
                MySqlClient.Open();
                Console.WriteLine("已经建立连接");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return -1;
            }
        }


        //关闭数据库连接
        public void UnitConnectMysqlSever()
        {
            if (MySqlClient != null)
            {
                MySqlClient.Close();	//关闭连接
                MySqlClient = null;
            }
        }

        //插入数据
        public int InsertMysqlData(string DUTSN, bool testresult1, string StationType, string StationName)
        {
            if (MySqlClient.State == ConnectionState.Open)
            {
                int testresult = -1;
                if (testresult1)
                {
                    testresult = 0;
                }
                else
                {
                    testresult = 1;
                }
                string s = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                MySqlCommand cmd = MySqlClient.CreateCommand();
                cmd.CommandText = "INSERT INTO INTERFACE_FH_TEST(SN,test_result,station_type,station_code,create_time)VALUES(@SN,@test_result,@station_type,@station_code,@create_time)";
                cmd.Parameters.AddWithValue("@SN", DUTSN);
                cmd.Parameters.AddWithValue("@test_result", testresult);
                cmd.Parameters.AddWithValue("@station_type", StationType);
                cmd.Parameters.AddWithValue("@station_code", StationName);
                cmd.Parameters.AddWithValue("@create_time", s);
                cmd.ExecuteNonQuery();
                return 0;
            }
            else
            {
                return -1;
            }

        }

        #endregion

        #region  将上传失败的图片检测数据写入文件中
        /// <summary> 
        /// 将上传失败的算法结果写入INI文件 
        /// </summary> 
        /// <param name="iniFilePath">文件地址 </param> 
        /// <param name="ImageName">图片名称 </param> 
        /// <param name="TestData">算法结果 </param> 
        private void Write_UploadFailFile(string iniFilePath,string ImageName,string TestData)
        {
            var pat = System.IO.Path.GetDirectoryName(iniFilePath);
            if (Directory.Exists(pat) == false)
            {
                Directory.CreateDirectory(pat);
            }
            if (File.Exists(iniFilePath) == false)
            {
                File.Create(iniFilePath).Close();
            }

            IniUtils.IniWriteValue("TestData", ImageName, TestData, iniFilePath);
        }

        /// <summary> 
        /// 从INI文件中读取上传失败的算法结果 
        /// </summary> 
        /// <param name="iniFilePath">文件地址 </param> 
        /// <param name="ImageName">图片名称 </param> 
        private string Read_UploadFailFile(string iniFilePath, string ImageName)
        {
            string str = string.Empty;
            if (File.Exists(iniFilePath))
            {
                str = IniUtils.IniReadValue("TestData", ImageName, iniFilePath);
            }
            return str;
        }
        #endregion

    }
}
