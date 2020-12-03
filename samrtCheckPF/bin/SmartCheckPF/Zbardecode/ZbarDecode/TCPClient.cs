using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace ZbarDecode
{
    public delegate void DelegateReceiveMessage(string Message, int length);
    public class TCPClient
    {
        public Socket newclient;
        public bool Connected = false;
        public Thread myThread;
        public delegate void MyInvoke(string str);
        public string StrReceiveMessage = "";
        public DelegateReceiveMessage ReceiveMessage = null;

        public TCPClient()
        {

        }

        public bool ConnectSever(string ip, string IPport)
        {
            byte[] data = new byte[1024];

            int port = Convert.ToInt32(IPport);//将端口号强制为32位整型，存放在port中

            //创建一个套接字 

            IPEndPoint ie = new IPEndPoint(IPAddress.Parse(ip), port);
            newclient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //将套接字与远程服务器地址相连
            try
            {
                newclient.Connect(ie);
                Connected = true;
                ThreadStart myThreaddelegate = new ThreadStart(ReceiveMsg);
                myThread = new Thread(myThreaddelegate);
                myThread.Start();
                return true;
            }
            catch (SocketException e)
            {
                Console.WriteLine("连接服务器失败  " + e.Message);
                return false;
            }
        }

        public void ReceiveMsg()
        {
            while (Connected)
            {
                try
                {
                    List<byte> data = new List<byte>();
                    byte[] buffer = new byte[1024];
                    int length = 0;
                    if ((length = newclient.Receive(buffer)) > 0)
                    {
                        for (int j = 0; j < length; j++)
                        {
                            data.Add(buffer[j]);
                        }
                        if (data.Count > 0)
                        {
                            StrReceiveMessage = Encoding.UTF8.GetString(data.ToArray(), 0, data.Count);
                        }
                    }

                    else
                    {
                        Connected = false;
                    }

                    if (ReceiveMessage != null)
                    {
                        ReceiveMessage(StrReceiveMessage, length);
                    }


                }
                catch (System.Exception ex)
                {
                   
                    newclient.Dispose();
                }
            }

        }

        public bool SendMessage(string SendData)
        {
            if (!newclient.Connected)
            {
                Connected = false;

                if (ReceiveMessage != null)
                {
                    ReceiveMessage("", 0);
                }

                return false;
            }
            if (Connected)
            {
                try
                {
                    if (SendData != "")
                    {
                        byte[] byteArray = System.Text.Encoding.Default.GetBytes(SendData);
                        newclient.Send(byteArray);
                        return true;
                    }
                    return false;
                }
                catch (System.Exception ex)
                {
                    return false;
                }
            }

            else
            {
                newclient.Dispose();
                return false;
            }
        }


    }
}
