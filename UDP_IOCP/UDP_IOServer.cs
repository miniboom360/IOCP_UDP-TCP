/*
作者：niniBoom
CSDN:http://blog.csdn.net/nini_boom?viewmode=contents
163邮箱:13063434858@163.com
-----------------------------------------------------------------

介绍：这里是处理高并发队列的核心部分，总所周知，UDP在c#中是没有提供
异步处理接口的，这里使用SocketAsyncEventArgs异步操作套接字和对象池
技术，异步处理使用UDP协议传输的大量数据，同时支持socket发送和接收
动作后的SocketAsyncEventArgs对象和缓存的回收管理.ProcessSend和
processReceive接口中可添加自定义动作

技术参数：
    监听端口：10099
    单个数据缓存大小：1024

-----------------------------------------------------------------
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UDP_IOCP.UIOCP_Kernel
{
    class UDP_IOServer
    {
        /*
          Socket是IOCP的核心之一，在这里统一管理
          */

        //创建的线程池
        private static UIOCP_Thread_Pool socketArgsPool;

        private static Socket receiveSocket;

        private static Socket sendSocket;

        private static SocketAsyncEventArgs receiveSocketArgs;

        private static SocketAsyncEventArgs sendSocketArgs;

        private static IPEndPoint localEndPoint;

        private BufferManager bfManager;

        private static UdpClient receive_udp;

        private static byte[] receivebuffer;

        private int poolSize;

        private int bufferSize;

        private int listen_port;

        
        public UDP_IOServer()
        {            
            listen_port = 10099;
            bufferSize = 1024;
            poolSize = 20;
            receivebuffer = new byte[1024];

            /*创建供IO缓存区*/
            bfManager = new BufferManager(poolSize * bufferSize * 2, bufferSize);
            bfManager.InitBuffer();

            socketArgsPool = new UIOCP_Thread_Pool(poolSize);

            //初始化receiveSocket属性
            receiveSocket = new Socket(AddressFamily.InterNetwork,
                                        SocketType.Dgram, ProtocolType.Udp);

            localEndPoint = new IPEndPoint(IPAddress.Parse(Get_Local_IP()), listen_port);

            receive_udp = new UdpClient(localEndPoint);

            //初始化sendSocket属性
            sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            /*为线程池填入SocketAsyncEventArgs对象*/
            for (int i = 0; i < poolSize; ++i)
            {
                sendSocketArgs = new SocketAsyncEventArgs();
                /*为每个SocketAsyncEventArgs对象增加一个套接字结束回调事件*/
                sendSocketArgs.Completed += new EventHandler<SocketAsyncEventArgs>(Completed);
                /*为每个SocketAsyncEventArgs的buffer进行填充*/
                bfManager.SetBuffer(sendSocketArgs);
                socketArgsPool.Push(sendSocketArgs);
            }
        }

        public void Recv()
        {
            try
            {
                while (true)
                {
                    IAsyncResult iar = receive_udp.BeginReceive(null, null);
                    byte[] receiveData = receive_udp.EndReceive(iar, ref localEndPoint);

                    /*此处是其他模块接收到数据后的处理接口*/

                }
            }
            catch (Exception e)
            {
                Console.WriteLine("receove failed {0}", e);
            }           
        }

        /*供其他模块调用的send接口*/
        public static void Send(byte[] content, EndPoint remoteEndPoint)
        {
            /*从中取出一个SocketAsyncEventArgs对象使用*/
            sendSocketArgs = socketArgsPool.Pop();
            sendSocketArgs.RemoteEndPoint = remoteEndPoint;

            /*设置发送内容*/
            Array.Copy(content, sendSocketArgs.Buffer, content.Length);

            sendSocketArgs.SetBuffer(0,content.Length);

            if (sendSocketArgs.RemoteEndPoint != null)
            {
                if (!sendSocket.SendToAsync(sendSocketArgs))
                {
                    ProcessSend(sendSocketArgs);
                }
            }

        }

        /*发送数据后的动作接口，可添加自定义的动作接口*/
        private static void ProcessSend(SocketAsyncEventArgs e)
        {
            /*放入socketAsync池中*/
            socketArgsPool.Push(e);
        }

        public static void StartReceive()
        {
            if (!receiveSocket.ReceiveFromAsync(receiveSocketArgs))
            {
                processReceive(receiveSocketArgs);
            }
        }

        /*接收数据后的动作接口，可添加自定义的动作接口*/
        private static void processReceive(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred >0 && e.SocketError == SocketError.Success)
            {
                byte[] receiveData = e.Buffer;                
            }

            StartReceive();
        }


        private void Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)              
            {
                case SocketAsyncOperation.ReceiveFrom:
                    break;

                case SocketAsyncOperation.SendTo:
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not success!");
            }
        }

        /*获取本地的IP*/
        private static string Get_Local_IP()
        {
            IPAddress[] addressList = Dns.GetHostEntry(Environment.MachineName).AddressList;

            string my_ip = addressList[1].ToString();

            return my_ip;
        }

    }
}
