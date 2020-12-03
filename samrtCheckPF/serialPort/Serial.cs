using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.Text.RegularExpressions;


namespace samrtCheckPF.serialPort
{
    public delegate void DelegateReceiveMesg(string Message);

    public class Serial
    {
        public SerialPort sp;
        public bool b_HEXReceive = false;
        public bool b_HEXSend = false;
        public string StrMessage = "";
        public DelegateReceiveMesg ReceiveMesg = null;
        public bool m_bValid = false;

        public Serial()
        {
            sp = new SerialPort();
        }

        public bool OpenSerialPort(string portname, int iBaudRate, int iDateBits, StopBits iStopBits, Parity iParity)
        {
            try
            {
                sp.PortName = portname;
                sp.BaudRate = iBaudRate;
                sp.DataBits = iDateBits;
                sp.StopBits = iStopBits;
                sp.Parity = iParity;
                sp.Open();
                sp.DataReceived += new SerialDataReceivedEventHandler(sp1_DataReceived);
                m_bValid = true;
            }

            catch (System.Exception ex)
            {
                MessageBox.Show("Error:" + ex.Message, "Error");
                m_bValid = false;
            }

            return m_bValid;
        }

        public bool CloseSerialPort()
        {
            bool _breturn = false;
            try
            {
                sp.Close();
                sp.DataReceived -= new SerialDataReceivedEventHandler(sp1_DataReceived);
                m_bValid = false;
                _breturn = true;
            }

            catch (System.Exception ex)
            {
                MessageBox.Show("Error:" + ex.Message, "Error");
                _breturn = false;

            }
            return _breturn;
        }

        public bool OpenSerialPort(string portname, int iBaudRate, int iDateBits, string sStopBits, string sParity)
        {

            try
            {
                sp.PortName = portname;
                sp.BaudRate = iBaudRate;
                sp.DataBits = iDateBits;

                switch (sStopBits)
                {
                    case "1":
                        sp.StopBits = StopBits.One;
                        break;
                    case "1.5":
                        sp.StopBits = StopBits.OnePointFive;
                        break;
                    case "2":
                        sp.StopBits = StopBits.Two;
                        break;
                    default:
                        // MessageBox.Show("Error：参数不正确!", "Error");
                        break;
                }

                switch (sParity)             //校验位
                {
                    case "None":
                        sp.Parity = Parity.None;
                        break;
                    case "Odd":
                        sp.Parity = Parity.Odd;
                        break;
                    case "Even":
                        sp.Parity = Parity.Even;
                        break;
                    default:
                        break;
                }
                sp.Encoding = System.Text.Encoding.GetEncoding("GB2312");//此行非常重要 可解决接收中文乱码问题
                sp.Open();
                sp.DataReceived += new SerialDataReceivedEventHandler(sp1_DataReceived);
                m_bValid = true;
            }

            catch (System.Exception ex)
            {
                MessageBox.Show("Error:" + ex.Message, "Error");
                m_bValid = false;
            }

            return m_bValid;
        }

        public void SendData(String strData)
        {

            if (!sp.IsOpen) //如果没打开
            {
               // MessageBox.Show("请先打开串口！", "Error");
                return;
            }

            if (b_HEXSend == true)	//“HEX发送” 按钮 
            {
                //处理数字转换
                string sendBuf = strData;
                string sendnoNull = sendBuf.Trim();
                string sendNOComma = sendnoNull.Replace(',', ' ');    //去掉英文逗号
                string sendNOComma1 = sendNOComma.Replace('，', ' '); //去掉中文逗号
                string strSendNoComma2 = sendNOComma1.Replace("0x", "");   //去掉0x
                strSendNoComma2.Replace("0X", "");   //去掉0X
                string[] strArray = strSendNoComma2.Split(' ');

                int byteBufferLength = strArray.Length;
                for (int i = 0; i < strArray.Length; i++)
                {
                    if (strArray[i] == "")
                    {
                        byteBufferLength--;
                    }
                }
                // int temp = 0;
                byte[] byteBuffer = new byte[byteBufferLength];
                int ii = 0;
                for (int i = 0; i < strArray.Length; i++)        //对获取的字符做相加运算
                {

                    Byte[] bytesOfStr = Encoding.Default.GetBytes(strArray[i]);

                    int decNum = 0;
                    if (strArray[i] == "")
                    {
                        //ii--;     //加上此句是错误的，下面的continue以延缓了一个ii，不与i同步
                        continue;
                    }
                    else
                    {
                        decNum = Convert.ToInt32(strArray[i], 16); //atrArray[i] == 12时，temp == 18 
                    }

                    try    //防止输错，使其只能输入一个字节的字符
                    {
                        byteBuffer[ii] = Convert.ToByte(decNum);
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error");
                        return;
                    }

                    ii++;
                }
                sp.Write(byteBuffer, 0, byteBuffer.Length);
            }
            else		//以字符串形式发送时 
            {
                sp.WriteLine(strData);    //写入数据
            }
        }

        void sp1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string strTemp = "";
            if (sp.IsOpen)     //此处可能没有必要判断是否打开串口，但为了严谨性，我还是加上了
            {
                System.Threading.Thread.Sleep(30);
                if (!b_HEXReceive)                          //'发送字符串'单选按钮
                {
                    //byte[] buffer = new Byte[1024];
                    //int len = 0;
                    //if ((len = sp.BytesToRead) != 0)   //BytesToRead:sp1接收的字符个数
                    //{

                    //    byte[] buf = new byte[len];
                    //    sp.Read(buf, 0, buf.Length);         //读取数据
                    //    strTemp = System.Text.Encoding.ASCII.GetString(buf);
                    //    sp.DiscardInBuffer();                      //清空SerialPort控件的Buffer 
                    //}

                    strTemp = sp.ReadExisting();
                    sp.DiscardInBuffer();
                }

                else                                            //'接受16进制按钮'
                {
                    try
                    {
                        Byte[] receivedData = new Byte[sp.BytesToRead];        //创建接收字节数组
                        sp.Read(receivedData, 0, receivedData.Length);         //读取数据
                        //string text = sp1.Read();   //Encoding.ASCII.GetString(receivedData);
                        sp.DiscardInBuffer();                                  //清空SerialPort控件的Buffer
                        //这是用以显示字符串
                        //    string strRcv = null;
                        //    for (int i = 0; i < receivedData.Length; i++ )
                        //    {
                        //        strRcv += ((char)Convert.ToInt32(receivedData[i])) ;
                        //    }
                        //    txtReceive.Text += strRcv + "\r\n";             //显示信息
                        //}
                        //int decNum = 0;//存储十进制
                        for (int i = 0; i < receivedData.Length; i++) //窗体显示
                        {

                            strTemp += receivedData[i].ToString("X2") + " ";  //16进制显示
                        }
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show(ex.Message, "出错提示");
                    }
                }

                StrMessage = strTemp; //注意：回车换行必须这样写，单独使用"\r"和"\n"都不会有效果
                ReceiveMesg?.Invoke(StrMessage);
            }
            else
            {
                MessageBox.Show("请打开某个串口", "错误提示");
            }
        }
    }
}
