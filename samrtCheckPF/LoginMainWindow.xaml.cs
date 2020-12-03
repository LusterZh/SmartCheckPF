using samrtCheckPF.common;
using samrtCheckPF.utils;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace smartCheckPF
{
    /// <summary>
    /// LoginMainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginMainWindow : Window
    {
        List<string> listData = new List<string>();
        string Project = "";
        string Build = "";
        string SKU = "";
        string Station = "";
        string Line = "";
        string PartNum = "";
        string IniPath = "C:\\config\\config.ini";

        public LoginMainWindow()
        {
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            // 设置全屏
            this.WindowState = System.Windows.WindowState.Normal;
            //  this.WindowStyle = System.Windows.WindowStyle.None;
            this.ResizeMode = System.Windows.ResizeMode.NoResize;

            InitializeComponent();
            FileUtils.deleteFile(Directory.GetCurrentDirectory() + "\\StationConfig.txt");
            //ReadTxt2ComboxText(Directory.GetCurrentDirectory() + "\\StationConfig.txt");
            ReadConfig2ComboxText(Directory.GetCurrentDirectory() + "\\StationConfig.ini");
            Read_RsstoreFile(IniPath);//读取记忆位置
            //DeleteItem();
            SetComboTextIndex();
        }

        //从StationConfig.ini中读取初始配置信息
        private void ReadConfig2ComboxText(string filePath)
        {
            updateComnoText(cbProject, lbProject, "project", filePath);
            updateComnoText(cbSKU, lbSKU, "sku", filePath);
            updateComnoText(cbBulid, lbBulid, "bulid", filePath);
            updateComnoText(cbStation, lbStation, "station", filePath);
            updateComnoText(cbLine, lbLine, "line", filePath);
            //料号获取方式 0:SN获取 1:MES系统获取 2:机台配置文件获取
            GlobalValue.PART_NUMBER_TYPE = getIniValue("Station Config", "PART_NUMBER_TYPE", "1", filePath);
            if (GlobalValue.PART_NUMBER_TYPE == "2")
            {
                updateComnoText(cbPartNum, lbPartNum, "partNumber", filePath);
            }
            else
            {
                cbPartNum.Visibility = Visibility.Collapsed;
                lbPartNum.Visibility = Visibility.Collapsed;
            }
            
        }

        private void updateComnoText(ComboBox cb, Label lb, string key, string filePath)
        {
            List<string> datas = IniUtils.IniReadValue("Station Config", key, filePath).Split(',').ToList<string>();
            foreach (string data in datas)
            {
                if (data != "")
                {
                    cb.Items.Add(data);
                }
            }
        }

        //public void ReadTxt2ComboxText(string Path)
        //{
        //    try
        //    {
        //        int i = 0;
        //        //读入文件
        //        using (StreamReader reader = new StreamReader(Path, Encoding.Default))
        //        {
        //            //循环读取所有行
        //            while (!reader.EndOfStream)
        //            {
        //                //将每行数据，用“Tab”分割成6段
        //                char[] separator = { ',' };
        //                string[] data = reader.ReadLine().Split(separator);
        //                UpdateComboText(data);
        //                i++;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        string s = ex.Message;
        //    }

        //}

        //public void UpdateComboText(string[] data)
        //{
        //    if(data.Length>4)
        //    {
        //            cbProject.Items.Add(data[0].Split(':')[1].Trim());
        //            cbSKU.Items.Add(data[1].Split(':')[1].Trim());
        //            cbBulid.Items.Add(data[2].Split(':')[1].Trim());
        //            cbStation.Items.Add(data[3].Split(':')[1].Trim());
        //            cbLine.Items.Add(data[4].Split(':')[1].Trim());
        //    }
        //}

        public void SetComboTextIndex()
        {
            // ch:选择第一项 | en:Select the first item
            if (cbProject.Items.Count != 0)
            {
                if (cbProject.Items.Contains(Project))
                    cbProject.SelectedItem = Project;
                else
                    cbProject.SelectedIndex = 0;
            }
            if (cbSKU.Items.Count != 0)
            {
                if (cbSKU.Items.Contains(SKU))
                    cbSKU.SelectedItem = SKU;
                else
                    cbSKU.SelectedIndex = 0;
            }

            if (cbBulid.Items.Count != 0)
            {
                if (cbBulid.Items.Contains(Build))
                    cbBulid.SelectedItem = Build;
                else
                    cbBulid.SelectedIndex = 0;

            }
            if (cbStation.Items.Count != 0)
            {
                if (cbStation.Items.Contains(Station))
                    cbStation.SelectedItem = Station;
                else
                    cbStation.SelectedIndex = 0;
            }
            if (cbLine.Items.Count != 0)
            {
                if (cbLine.Items.Contains(Line))
                    cbLine.SelectedItem = Line;
                else
                    cbLine.SelectedIndex = 0;
            }
            if(cbPartNum.Items.Count != 0)
            {
                if (cbPartNum.Items.Contains(PartNum))
                    cbPartNum.SelectedItem = PartNum;
                else
                    cbPartNum.SelectedIndex = 0;
            }
        }

        //public void DeleteSameString(ComboBox cb)
        //{
        //    int count = cb.Items.Count;//获取combobox1中所有行的数量
        //    int i, j;
        //    for (i = 0; i < count; i++)
        //    {
        //        String str1 = cb.Items[i].ToString();
        //        for (j = i + 1; j < count; j++)
        //        {
        //            String str2 = cb.Items[j].ToString();
        //            if (str1 == str2)
        //            {
        //                cb.Items.RemoveAt(j);
        //                j--;
        //                count--;
        //             }
        //        }
        //    }
        //}

        ////字符串排序
        //public void SortCombobox(ComboBox cb)
        //{
        //    int count = cb.Items.Count;//获取combobox1中所有行的数量
        //    List<string> array = new List<string>();

        //    int i;
        //    for (i = 0; i < count; i++)
        //    {
        //        array.Add(cb.Items[i].ToString());
        //    }
        //    array.Sort();

        //    for (i = 0; i < count; i++)
        //    {
        //        cb.Items.RemoveAt(i);
        //        i--;
        //        count--;
        //    }

        //    foreach(string s in array)
        //    {
        //        cbStation.Items.Add(s);
        //    }
        //}


        //public void DeleteItem()
        //{
        //    DeleteSameString(cbProject);
        //    DeleteSameString(cbSKU);
        //    DeleteSameString(cbBulid);
        //    DeleteSameString(cbStation);
        //    DeleteSameString(cbLine);
        //    //SortCombobox(cbStation);
        //}

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Project = cbProject.Text;
            SKU = cbSKU.Text;
            Station = cbStation.Text;
            Build = cbBulid.Text;
            Line = cbLine.Text;
            PartNum = cbPartNum.Text;

            GlobalValue.CONFIG_PROJECT = cbProject.Text;
            GlobalValue.CONFIG_SKU = cbSKU.Text;
            GlobalValue.CONFIG_STATION = cbStation.Text;
            GlobalValue.CONFIG_BULID = cbBulid.Text;
            GlobalValue.CONFIG_LINE = cbLine.Text;
            GlobalValue.CONFIG_PARTNUM = cbPartNum.Text;
            if(GlobalValue.PART_NUMBER_TYPE == "2")
            {
                GlobalValue.MES_PARTNUMBER = cbPartNum.Text;
            }


            Write_RsstoreFile(IniPath);//记忆文件写入

            string configFilePath = Directory.GetCurrentDirectory() + "\\ConfigFiles\\config_" + GlobalValue.CONFIG_STATION + ".ini";
            if (File.Exists(configFilePath))
            {
                //按照站点读取配置文件
                Read_ConfigFile(GlobalValue.CONFIG_STATION);
            }
            else
            {
                Read_ConfigFile("Default");
            }
            
            this.DialogResult = Convert.ToBoolean(1);
            this.Close();
        }

        private void Read_ConfigFile(string strtemp)
        {
            if (strtemp.Contains("2"))
                strtemp = "LDA1";
            string str = "config_" + strtemp;
            GlobalValue.IP = IniUtils.IniReadValue("Station Configuration", "IP", Directory.GetCurrentDirectory() + "\\ConfigFiles\\" + str + ".ini");
            IniUtils.IniWriteValue("Station Configuration", "IP", GlobalValue.IP, "C:\\config\\config.ini");
            GlobalValue.PORT = IniUtils.IniReadValue("Station Configuration", "PORT", Directory.GetCurrentDirectory() + "\\ConfigFiles\\" + str + ".ini");
            GlobalValue.LIGHTCOMPORT = IniUtils.IniReadValue("Station Configuration", "lIGHTPORT", Directory.GetCurrentDirectory() + "\\ConfigFiles\\" + str + ".ini");
            GlobalValue.PLCCOMPORT = IniUtils.IniReadValue("Station Configuration", "PLCPORT", Directory.GetCurrentDirectory() + "\\ConfigFiles\\" + str + ".ini");
            GlobalValue.LIGHTNESS = int.Parse(IniUtils.IniReadValue("Station Configuration", "LIGHTNESS", Directory.GetCurrentDirectory() + "\\ConfigFiles\\" + str + ".ini"));
            GlobalValue.ISPLCUSED = IniUtils.IniReadValue("Station Configuration", "ISPLCUSED", Directory.GetCurrentDirectory() + "\\ConfigFiles\\" + str + ".ini");
            GlobalValue.ISLOCATEDUSED = IniUtils.IniReadValue("Station Configuration", "ISLOCATEDUSED", Directory.GetCurrentDirectory() + "\\ConfigFiles\\" + str + ".ini");
            GlobalValue.CAMERAEXPOSURE = IniUtils.IniReadValue("Station Configuration", "Exposure Time", Directory.GetCurrentDirectory() + "\\ConfigFiles\\" + str + ".ini");
            GlobalValue.CAMERAGAIN = IniUtils.IniReadValue("Station Configuration", "Gain", Directory.GetCurrentDirectory() + "\\ConfigFiles\\" + str + ".ini");
            GlobalValue.CAMERASN = IniUtils.IniReadValue("Station Configuration", "CameraSN", Directory.GetCurrentDirectory() + "\\ConfigFiles\\" + str + ".ini");
            //裁剪区域
            GlobalValue.ROI_X = int.Parse(IniUtils.IniReadValue("Station Configuration", "ROI_X", Directory.GetCurrentDirectory() + "\\ConfigFiles\\" + str + ".ini"));
            GlobalValue.ROI_Y = int.Parse(IniUtils.IniReadValue("Station Configuration", "ROI_Y", Directory.GetCurrentDirectory() + "\\ConfigFiles\\" + str + ".ini"));
            GlobalValue.ROI_WIDTH = int.Parse(IniUtils.IniReadValue("Station Configuration", "ROI_WIDTH", Directory.GetCurrentDirectory() + "\\ConfigFiles\\" + str + ".ini"));
            GlobalValue.ROI_HEIGHT = int.Parse(IniUtils.IniReadValue("Station Configuration", "ROI_HEIGHT", Directory.GetCurrentDirectory() + "\\ConfigFiles\\" + str + ".ini"));

            //测试mes写入
            GlobalValue.ISUSEDMESMANUAL = IniUtils.IniReadValue("Station Configuration", "ISUSEDMESMANUAL", Directory.GetCurrentDirectory() + "\\ConfigFiles\\" + str + ".ini");
            GlobalValue.MES_RESULT = IniUtils.IniReadValue("Station Configuration", "MES_RESULT", Directory.GetCurrentDirectory() + "\\ConfigFiles\\" + str + ".ini");
            GlobalValue.MES_STATIONTYPE = IniUtils.IniReadValue("Station Configuration", "MES_STATIONTYPE", Directory.GetCurrentDirectory() + "\\ConfigFiles\\" + str + ".ini");

            //识别大条码或者小条码
            GlobalValue.ISLARGEQRCODE = IniUtils.IniReadValue("Station Configuration", "ISLARGEQRCODE", Directory.GetCurrentDirectory() + "\\ConfigFiles\\" + str + ".ini");

            //条码类型
            GlobalValue.CODETYPE = IniUtils.IniReadValue("Station Configuration", "CODETYPE", Directory.GetCurrentDirectory() + "\\ConfigFiles\\" + str + ".ini");

            //是否使用zbar辅助读码
            GlobalValue.ISZBARCODEUSED = IniUtils.IniReadValue("Station Configuration", "ISZBARCODEUSED", Directory.GetCurrentDirectory() + "\\ConfigFiles\\" + str + ".ini");

            //是否需要扫描两次
            GlobalValue.ISRESNAP = IniUtils.IniReadValue("Station Configuration", "ISRESNAP", Directory.GetCurrentDirectory() + "\\ConfigFiles\\" + str + ".ini");

            //是否使用产品感应器
            GlobalValue.ISDUTCHECK = IniUtils.IniReadValue("Station Configuration", "ISDUTCHECK", Directory.GetCurrentDirectory() + "\\ConfigFiles\\" + str + ".ini");
            
            GlobalValue.IsShowIndicationImage = getIniValue("Station Configuration", "IsShowIndicationImage", "1", Directory.GetCurrentDirectory() + "\\ConfigFiles\\" + str + ".ini");

            //配准缩放比例
            GlobalValue.ScaleRatio = double.Parse(getIniValue("Station Configuration", "RegScaleRatio", "4", Directory.GetCurrentDirectory() + "\\ConfigFiles\\" + str + ".ini"));

            //流程结束之后显示时间限制
            GlobalValue.ShowLimitTime = int.Parse(getIniValue("Station Configuration", "ShowLimitTime", "3000", Directory.GetCurrentDirectory() + "\\ConfigFiles\\" + str + ".ini"));

            //是否检测SIM卡托产品有无 ，默认不检测
            GlobalValue.IsSIMDUTChecked = getIniValue("Station Configuration", "IsSIMDUTChecked", "0", Directory.GetCurrentDirectory() + "\\ConfigFiles\\" + str + ".ini");
            //IniUtils.IniWriteValue("Station Config", "PART_NUMBER_TYPE", GlobalValue.PART_NUMBER_TYPE, filePath);

            //是否压缩图片
            GlobalValue.IsJPEGZIPED = getIniValue("Station Configuration", "IsJPEGZIPED", "1", Directory.GetCurrentDirectory() + "\\ConfigFiles\\" + str + ".ini");


            GlobalValue.SERVER_IP = "http://" + GlobalValue.IP + ":" + GlobalValue.PORT;
            GlobalValue.SOCKET_IP = "ws://" + GlobalValue.IP + ":8888/ws";
            GlobalValue.COMMIT_IP = GlobalValue.SERVER_IP + "/image/uploadImageWithResult"; //上传图片url
            GlobalValue.COMMIT_IP_PASSFAIL = GlobalValue.SERVER_IP + "/image/newUpload"; //上传图片结果url
            GlobalValue.COMMIT_IP_CHECKUSER = GlobalValue.SERVER_IP + "/user/verifyUserNtf"; //检查用户权限
            GlobalValue.COMMIT_DJ_IP = GlobalValue.SERVER_IP + "/image/uploadImageWithResult"; //上传点胶结果url
            GlobalValue.GETINI_IP = GlobalValue.SERVER_IP + "/config/getConfig"; //拉取最新配置信息
            GlobalValue.UPDATE_IP = GlobalValue.SERVER_IP + "/station/getNewStationFunction"; //拉取更新url
            GlobalValue.NOTIFY_SERVER_IP = GlobalValue.SERVER_IP + "/config/pcCompleteUpdate"; //通知服务器更新完成
            //GlobalValue.GOLDEN_SAMPLE_DOWNLOAD_URL = GlobalValue.SERVER_IP + "/file/downloadImage?functionId="; //下载golden_sample的url
            GlobalValue.GOLDEN_SAMPLE_NEW_DOWNLOAD_URL = GlobalValue.SERVER_IP + "/file/downloadImage?imageId="; //下载golden_sample的url
            GlobalValue.ALG_FOLDER = AppDomain.CurrentDomain.BaseDirectory + "alg\\"; //算法运行时相关文件夹
            GlobalValue.MODEL_DOWNLOAD_URL = GlobalValue.SERVER_IP + "/file/downloadModel?functionId="; //下载golden_sample的url
            GlobalValue.MODEL_NEWDOWNLOAD_URL = GlobalValue.SERVER_IP + "/file/downloadModelByUrl?url="; //下载golden_sample的新url
    }

        private string getIniValue(string section, string key,string defaultValue,string iniFilePath)
        {
            string Temp = IniUtils.IniReadValue(section, key, iniFilePath);
            if (Temp == string.Empty)
            {
                IniUtils.IniWriteValue(section, key, defaultValue, iniFilePath);
                Temp = defaultValue;
            }
            return Temp;
        }

        /// <summary> 
        /// 读取INI文件 
        /// </summary> 
        /// <param name="iniFilePath">文件地址 )</param> 
        private void Read_RsstoreFile(string iniFilePath)
        {
            if (File.Exists(iniFilePath))
            {
               Project = IniUtils.IniReadValue("Station Configuration", "Project", iniFilePath);
               Build = IniUtils.IniReadValue("Station Configuration", "Build", iniFilePath);
               SKU = IniUtils.IniReadValue("Station Configuration", "SKU", iniFilePath);
               Station = IniUtils.IniReadValue("Station Configuration", "Station", iniFilePath);
               Line = IniUtils.IniReadValue("Station Configuration", "Line", iniFilePath);
               PartNum = IniUtils.IniReadValue("Station Configuration", "PartNum", iniFilePath);
                GlobalValue.PLC_COMBOBOX_VALUE = IniUtils.IniReadValue("Station Configuration", "PLC", iniFilePath);
               GlobalValue.QRCodeInOrder = IniUtils.IniReadValue("Station Configuration", "Camera", iniFilePath);
            }
        }


        /// <summary> 
        /// 写入INI文件 
        /// </summary> 
        /// <param name="iniFilePath">文件地址 )</param> 
        private void Write_RsstoreFile(string iniFilePath)
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

             IniUtils.IniWriteValue("Station Configuration", "Project", Project, iniFilePath);
             IniUtils.IniWriteValue("Station Configuration", "Build", Build, iniFilePath);
             IniUtils.IniWriteValue("Station Configuration", "SKU", SKU, iniFilePath);
             IniUtils.IniWriteValue("Station Configuration", "Station", Station, iniFilePath);
             IniUtils.IniWriteValue("Station Configuration", "Line", Line, iniFilePath);
             IniUtils.IniWriteValue("Station Configuration", "PartNum", PartNum, iniFilePath);
             IniUtils.IniWriteValue("Station Configuration", "PLC", GlobalValue.PLC_COMBOBOX_VALUE, iniFilePath);
             IniUtils.IniWriteValue("Station Configuration", "Camera", GlobalValue.QRCodeInOrder, iniFilePath);
        }
    }
}
