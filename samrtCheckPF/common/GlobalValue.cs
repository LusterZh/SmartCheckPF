using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using samrtCheckPF.utils;
using System.Windows.Media;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using samrtCheckPF.json;
using System.Windows.Media.Imaging;

namespace samrtCheckPF.common
{
    class GlobalValue
    {
        // log相关
        public static bool isDebugMode = true;
        public static string COMMIT_FAILED_FOLDER = AppDomain.CurrentDomain.BaseDirectory + "commitFailedImages\\";
        public static string GOLD_SAMPLE_FOLDER = AppDomain.CurrentDomain.BaseDirectory + "function\\";
        public static string DATA_BASE_FOLDER = AppDomain.CurrentDomain.BaseDirectory + "DataBase\\";

        ////public static string SYSTEM_LOCAL_IP = GetLocalIP(); //系统本地IP地址
        //public static string IP = IniUtils.IniReadValue("Station Configuration", "IP", Directory.GetCurrentDirectory() + "\\config.ini");
        //public static string PORT = IniUtils.IniReadValue("Station Configuration", "PORT", Directory.GetCurrentDirectory() + "\\config.ini");
        //public static string LIGHTCOMPORT = IniUtils.IniReadValue("Station Configuration", "lIGHTPORT", Directory.GetCurrentDirectory() + "\\config.ini");
        //public static string PLCCOMPORT = IniUtils.IniReadValue("Station Configuration", "PLCPORT", Directory.GetCurrentDirectory() + "\\config.ini");
        //public static string ISPLCUSED = IniUtils.IniReadValue("Station Configuration", "ISPLCUSED", Directory.GetCurrentDirectory() + "\\config.ini");
        //public static string ISLOCATEDUSED = IniUtils.IniReadValue("Station Configuration", "ISLOCATEDUSED", Directory.GetCurrentDirectory() + "\\config.ini");
        //public static string CAMERAEXPOSURE = IniUtils.IniReadValue("Station Configuration", "Exposure Time", Directory.GetCurrentDirectory() + "\\config.ini");
        //public static string CAMERAGAIN = IniUtils.IniReadValue("Station Configuration", "Gain", Directory.GetCurrentDirectory() + "\\config.ini");
        //public static string CAMERASN= IniUtils.IniReadValue("Station Configuration", "CameraSN", Directory.GetCurrentDirectory() + "\\config.ini");
        ////裁剪区域
        //public static int ROI_X = int.Parse(IniUtils.IniReadValue("Station Configuration", "ROI_X", Directory.GetCurrentDirectory() + "\\config.ini"));
        //public static int ROI_Y = int.Parse(IniUtils.IniReadValue("Station Configuration", "ROI_Y", Directory.GetCurrentDirectory() + "\\config.ini"));
        //public static int ROI_WIDTH = int.Parse(IniUtils.IniReadValue("Station Configuration", "ROI_WIDTH", Directory.GetCurrentDirectory() + "\\config.ini"));
        //public static int ROI_HEIGHT = int.Parse(IniUtils.IniReadValue("Station Configuration", "ROI_HEIGHT", Directory.GetCurrentDirectory() + "\\config.ini"));

        ////测试mes写入
        //public static string ISUSEDMESMANUAL = IniUtils.IniReadValue("Station Configuration", "ISUSEDMESMANUAL", Directory.GetCurrentDirectory() + "\\config.ini");
        //public static string MES_RESULT = IniUtils.IniReadValue("Station Configuration", "MES_RESULT", Directory.GetCurrentDirectory() + "\\config.ini");
        //public static string MES_STATIONTYPE= IniUtils.IniReadValue("Station Configuration", "MES_STATIONTYPE", Directory.GetCurrentDirectory() + "\\config.ini");
        //public static string MES_PCNAME = GetLocalPCName();

        ////识别大条码或者小条码
        //public static string ISLARGEQRCODE = IniUtils.IniReadValue("Station Configuration", "ISLARGEQRCODE", Directory.GetCurrentDirectory() + "\\config.ini");


        //物料相关参数

        //public static string SYSTEM_LOCAL_IP = GetLocalIP(); //系统本地IP地址
        public static string IP = "";
        public static string PORT = "";
        public static string LIGHTCOMPORT = "";
        public static string PLCCOMPORT = "";
        public static int LIGHTNESS = 0;
        public static string ISPLCUSED = "";
        public static string ISLOCATEDUSED = "";
        public static string CAMERAEXPOSURE = "";
        public static string CAMERAGAIN = "";
        public static string CAMERASN = "";
        //裁剪区域
        public static int ROI_X = 0;
        public static int ROI_Y = 0;
        public static int ROI_WIDTH = 0;
        public static int ROI_HEIGHT = 0;

        //测试mes写入
        public static string ISUSEDMESMANUAL = "";
        public static string MES_RESULT = "";
        public static string MES_STATIONTYPE = "";
        public static string MES_PCNAME = GetLocalPCName();

        //识别大条码或者小条码
        public static string ISLARGEQRCODE = "";

        //条码类别
        public static string CODETYPE = "";

        //是否需要zbarcode
        public static string ISZBARCODEUSED = "";

        //是否需要拍照两次
        public static string ISRESNAP = "";

        //是否需要产品感应器
        public static string ISDUTCHECK = "";
        
        //是否需要显示示意图 
        public static string IsShowIndicationImage = "";

        //获取料号方式 0:序列号获取 1:MES系统获取 2:机台配置文件获取
        public static string PART_NUMBER_TYPE = "";


        public static string MES_PARTNUMBER = "";//物料编码
        public static string MES_LeftBoardNUMBER = "";
        public static string MES_RightBoardNUMBER = "";
        public static string MES_HingeHousingNUMBER = "";
        public static string MES_L5NUMBER = "";
        public static string MES_CLILensNUMBER = "";


        public static string QRCodeInOrder = "0";
        public static string PlcDirection = "0";
        public static string SERVER_IP = "http://" + IP+":"+PORT;
        public static string SOCKET_IP = "ws://"+IP+ ":8888/ws";
        //public static string COMMIT_IP = SERVER_IP + "/image/upload"; //上传图片url
        public static string COMMIT_IP = SERVER_IP + "/image/uploadImageWithResult"; //上传图片url
        public static string COMMIT_IP_PASSFAIL = SERVER_IP + "/image/newUpload"; //上传图片结果url
        public static string COMMIT_IP_CHECKUSER = SERVER_IP + "/user/verifyUserNtf"; //检查用户权限
        public static string COMMIT_DJ_IP = SERVER_IP + "/image/uploadImageWithResult"; //上传点胶结果url
        public static string GETINI_IP = SERVER_IP + "/config/getConfig"; //拉取最新配置信息
        public static string UPDATE_IP = SERVER_IP + "/station/getNewStationFunction"; //拉取更新url
        public static string NOTIFY_SERVER_IP = SERVER_IP + "/config/pcCompleteUpdate"; //通知服务器更新完成
        //public static string GOLDEN_SAMPLE_DOWNLOAD_URL = SERVER_IP + "/file/downloadImage?functionId="; //下载golden_sample的url
        public static string GOLDEN_SAMPLE_NEW_DOWNLOAD_URL = SERVER_IP + "/file/downloadImage?imageId="; //下载golden_sample的url
        public static string ALG_FOLDER = AppDomain.CurrentDomain.BaseDirectory + "alg\\"; //算法运行时相关文件夹

        public static string GOLDEN_SAMPLE_FOLDER = AppDomain.CurrentDomain.BaseDirectory + "alg\\golden_sample\\"; //下载golden_sample的保存路径
        public static string CAPTURE_CROP_FOLDER = AppDomain.CurrentDomain.BaseDirectory + "alg\\capture_crop\\"; //裁剪拍摄照片的函数roi图片
        public static string MODEL_DOWNLOAD_URL = SERVER_IP + "/file/downloadModel?functionId="; //下载golden_sample的url
        public static string MODEL_NEWDOWNLOAD_URL = SERVER_IP + "/file/downloadModelByUrl?url="; //下载golden_sample的新url
        public static string MODEL_FOLDER = AppDomain.CurrentDomain.BaseDirectory + "function\\model\\"; //下载golden_sample的保存路径
        public static string CUT_IMG_FOLDER = AppDomain.CurrentDomain.BaseDirectory + "temp\\"; //下载golden_sample的保存路径
        public static string ORIGIN_IMG_FOLDER = AppDomain.CurrentDomain.BaseDirectory + "checkImages"; //原始图片保存路径
        public static string DISPLAY_IMG_FOLDER = AppDomain.CurrentDomain.BaseDirectory + "zxingtestfail\\"; //展示图片路径
        public static string USERNAME = "lishizhen"; //上传者身份
        public static string PASSWORD = "LISHIZHEN"; //上传者密码
        public static string IMAGENAME = "1"; //图片结果名称
        public static bool IsNTFMODE = false; //NTF模式


        //待显示的图片
        public static BitmapImage SHOWIMAGE = null;
        public static System.Drawing.Bitmap SHOWIMAGE_BITMAP = null;

        //Alg计算结果状态
        public static string ALG_RESULT_NO = ""; //未执行算法
        public static string ALG_RESULT_TRUE = "PASS"; //执行结果 true
        public static string ALG_RESULT_FALSE = "FAIL"; //执行结果 false
        public static string ALG_RESULT_LOAD_FAIL = "加载失败"; //执行加载模型时失败
        public static GetCalibrationRes ALG_RESULT_CalibrationRes = null; //加载算法需要的json

        //点胶mask的路径
        public static string DJ_MASK_PATH = AppDomain.CurrentDomain.BaseDirectory + "function\\";
        public static string DJ_COMPOSEMASK_IMG_PATH = DJ_MASK_PATH + "merge.jpg";
        public static string DJ_MASK_IMG_PATH = DJ_MASK_PATH + "mask.png";

        //未能成过获取网页的图片
        public static string TEST_GOLDEN_SAMPLE_PATH = DISPLAY_IMG_FOLDER + "Reg_golden.jpg";

        //配准之后的图片
      //  public static string REGISTER_IMG_PATH = DJ_MASK_PATH + "registration.jpg";
        public static string GOLD_SAMPLE_NAME1 = "";
        public static string GOLD_SAMPLE_NAME2 = "";
        public static string HParam1 = "";
        public static string HParam2 = "";
        public static string ModelParam = "";

        //不同状态下的画笔颜色
        public static SolidColorBrush BRUSH_INIT_COLOR = new SolidColorBrush(Color.FromArgb(255,255, 255, 255));
       // public static SolidColorBrush BRUSH_INIT_COLOR = Brushes.Blue;
        public static SolidColorBrush BRUSH_FAIL_COLOR = Brushes.Red;
        public static SolidColorBrush BRUSH_PASS_COLOR = Brushes.Green;
        public static SolidColorBrush BRUSH_SELECTED_COLOR = new SolidColorBrush(Color.FromArgb(255, 20, 68, 106));

        //获取配置文件信息并发送给服务器
        public static string CONFIG_PROJECT = "";
        public static string CONFIG_SKU = "";
        public static string CONFIG_BULID = "";
        public static string CONFIG_STATION = "";
        public static string CONFIG_LINE = "";
        public static string CONFIG_PARTNUM = "";
        public static string CONFIG_VERSION = "";
        public static string CONFIG_PCUUID = "";

        //产品数量和良率统计
        public static int DUTTotal = 0;
        public static int DUTPass = 0;
        public static double DUTPassRate = 0;


        //正负样本的值
        public static string DUT_SAMPLE_STATION = "-1"; //0 正常模式  1.正样本 2.负样本


        //PLC寄存器的值
        public static string PLC_DREGISTER_ADDRESS = "-1"; //D10,D20,D30,D40
        public static string PLC_COMBOBOX_VALUE = "0";


        //扫码失败标志位
        public static bool AutoBarcode_Error = false;

        //配准缩放比例
        public static double ScaleRatio = 4;

        //无感应器显示结果时间
        public static int ShowLimitTime = 3000;

        //检测Sim卡托有无
        public static string IsSIMDUTChecked = "0";

        //是否压缩图片
        public static string IsJPEGZIPED = "1";

        /// <summary>
        /// 获取本机IP地址
        /// </summary>
        /// <returns>本机IP地址</returns>
        public static string GetLocalIP()
        {
            try
            {
                string HostName = Dns.GetHostName(); //得到主机名
                IPHostEntry IpEntry = Dns.GetHostEntry(HostName);
                for (int i = 0; i < IpEntry.AddressList.Length; i++)
                {
                    //从IP地址列表中筛选出IPv4类型的IP地址
                    //AddressFamily.InterNetwork表示此IP为IPv4,
                    //AddressFamily.InterNetworkV6表示此地址为IPv6类型
                    if (IpEntry.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                    {
                        return IpEntry.AddressList[i].ToString();
                    }
                }
                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        /// <summary>
        /// 获取本机电脑名称
        /// </summary>
        /// <returns>本机电脑名称</returns>
        /// 
        public static string GetLocalPCName()
        {
            try
            {
                return Dns.GetHostName(); //得到主机名
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        //读取json配置文件
        public static void  ReadJsonConfigFile()
        {
            try
            {
                StreamReader file = File.OpenText(AppDomain.CurrentDomain.BaseDirectory + "function\\" + "config.json");
                JsonTextReader reader = new JsonTextReader(file);
                JToken S = JToken.ReadFrom(reader);
              
                ALG_RESULT_CalibrationRes = S.Root.ToObject<GetCalibrationRes>();
                string STR = S.Root.ToString();
                file.Close();
            }
            catch
            {

            }
        }

    }
}
