/*
    作者：niniBoom
    CSDN:http://blog.csdn.net/nini_boom?viewmode=contents
    163邮箱:13063434858@163.com
-----------------------------------------------------------------
注：创建SocketAsyncEventArgs对象为元素,Stack为管理容器的线程池
    提供取出与加入以及线程池大小的操作   
-----------------------------------------------------------------
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UDP_IOCP.UIOCP_Kernel
{
    class UIOCP_Thread_Pool
    {
        private Stack<SocketAsyncEventArgs> threadPool;
        
        /*为lock提供参数*/
        private static readonly object thislock = new object();

        /*初始化线程池的容量*/
        public UIOCP_Thread_Pool(int capacity)
        {
            threadPool = new Stack<SocketAsyncEventArgs>(capacity);
        }

        public void Push(SocketAsyncEventArgs item)
        {
            if (item == null)
            {
                throw new ArgumentException("Item不能为Null");
            }
            lock (thislock)
            {
                threadPool.Push(item);
            }
        }

        public SocketAsyncEventArgs Pop()
        {
            lock (thislock)
            {
                return threadPool.Pop();
            }
        }

        public int Count
        {
            get { return threadPool.Count; }
        }

    }
}
