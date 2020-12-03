using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace samrtCheckPF.http
{
    public delegate void DelegateSocketWatching(EndPoint RemoteEndPoint, int userID);
    public delegate void DelegateReceiveMsg(Socket sokClient, string Message, int length);

    public class TCPSever
    {
        Thread threadWatch = null; // 负责监听客户端连接请求的 线程；
        Socket socketWatch = null;
        public bool m_IsValid = false;
        public int m_ConnectNum = 0;
        public List<string> ClientName = new List<string>();//最多连接4个客户端
        public string StrReceiveMessage = "";

        public DelegateSocketWatching SocketWatching;
        public DelegateReceiveMsg ReceiveMsg;

        //字典检索
        public Dictionary<string, Socket> dict = new Dictionary<string, Socket>();
        public Dictionary<string, Thread> dictThread = new Dictionary<string, Thread>();

        public TCPSever()
        {

        }

        /// 获取本地IP地址信息
        public string GetAddressIP()
        {
            ///获取本地的IP地址
            string AddressIP = string.Empty;
            foreach (IPAddress _IPAddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (_IPAddress.AddressFamily.ToString() == "InterNetwork")
                {
                    AddressIP = _IPAddress.ToString();
                }
            }
            return AddressIP;
        }

        public bool initTcpSever(string IPaddress, string Port)
        {
            // 创建负责监听的套接字，注意其中的参数；
            socketWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // 获得文本框中的IP对象；
            IPAddress address = IPAddress.Parse(IPaddress);
            // 创建包含ip和端口号的网络节点对象；
            IPEndPoint endPoint = new IPEndPoint(address, int.Parse(Port));
            try
            {
                // 将负责监听的套接字绑定到唯一的ip和端口上；
                socketWatch.Bind(endPoint);
            }
            catch (SocketException se)
            {
                return false;
            }
            // 设置监听队列的长度；
            socketWatch.Listen(10);


            // 创建负责监听的线程；
            threadWatch = new Thread(WatchConnecting);
            threadWatch.IsBackground = true;
            threadWatch.Start();
            m_IsValid = true;

            if (m_IsValid)
            {
                return true;
            }
            else
                return false;

        }


        /// <summary>
        /// 监听客户端请求的方法；
        /// </summary>
        void WatchConnecting()
        {
            while (true)  // 持续不断的监听客户端的连接请求；
            {
                // 开始监听客户端连接请求，Accept方法会阻断当前的线程；
                Socket sokConnection = socketWatch.Accept(); // 一旦监听到一个客户端的请求，就返回一个与该客户端通信的 套接字；

                // 想列表控件中添加客户端的IP信息；
                m_ConnectNum++;
                if (SocketWatching != null)
                {
                    SocketWatching(sokConnection.RemoteEndPoint, m_ConnectNum);
                }

                // 将与客户端连接的 套接字 对象添加到集合中；
                dict.Add(sokConnection.RemoteEndPoint.ToString(), sokConnection);
                Thread thr = new Thread(RecMsg);
                thr.IsBackground = true;
                thr.Start(sokConnection);
                dictThread.Add(sokConnection.RemoteEndPoint.ToString(), thr);  //  将新建的线程 添加 到线程的集合中去。
            }
        }

        void RecMsg(object sokConnectionparn)
        {
            Socket sokClient = sokConnectionparn as Socket;
            while (true)
            {
                try
                {
                    List<byte> data = new List<byte>();
                    byte[] buffer = new Byte[1024];
                    int len = 0;
                    if ((len = sokClient.Receive(buffer)) != 0)
                    {
                        for (int j = 0; j < len; j++)
                        {
                            data.Add(buffer[j]);
                        }

                        if (data.Count > 0)
                        {
                            StrReceiveMessage = Encoding.UTF8.GetString(data.ToArray(), 0, data.Count);
                        }

                        if (ReceiveMsg != null)
                        {
                            ReceiveMsg(sokClient, StrReceiveMessage, len);
                        }
                    }
                    else
                    {
                        // 从 通信套接字 集合中删除被中断连接的通信套接字；
                        dict.Remove(sokClient.RemoteEndPoint.ToString());
                        // 从通信线程集合中删除被中断连接的通信线程对象；
                        dictThread.Remove(sokClient.RemoteEndPoint.ToString());
                        // 从列表中移除被中断的连接IP
                        m_ConnectNum--;

                        if (ReceiveMsg != null)
                        {
                            ReceiveMsg(sokClient, StrReceiveMessage, len);
                        }
                        break;
                    }

                }

                catch (Exception e)
                {
                    break;
                }
            }

        }

        public bool SendMsg(string StrClient, string msg)
        {
            try
            {
                if (string.IsNullOrEmpty(StrClient))   // 判断是不是选择了发送的对象；
                {
                    return false;
                }
                else
                {
                    byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(msg);
                    dict[StrClient].Send(byteArray);// 解决了 sokConnection是局部变量，不能再本函数中引用的问题
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                return false;
            }

        }

        public void CloseTcpSever()
        {
            try
            {
                foreach (Socket S in dict.Values)
                {
                    S.Close();
                }

                foreach (Thread T in dictThread.Values)
                {
                    T.Abort();
                }

            }
            catch (System.Exception ex)
            {
            }


        }    //关闭服务器主要操作

    }
}
