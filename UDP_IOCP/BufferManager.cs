/*
    作者：niniBoom
    CSDN:http://blog.csdn.net/nini_boom?viewmode=contents
    163邮箱:13063434858@163.com
-----------------------------------------------------------------
    创建一个大型缓冲区可以划分并分配给SocketAsyncEventArgs 对象，
    用在每个套接字I/O 操作。 这使缓冲区可以轻松地重复使用，可防止
    堆内存碎片化
-----------------------------------------------------------------
    注：这块的代码不多，逻辑简单，但是不建议全部去消化，很多地方需
    要花费不小的学习成本去理解，只记住它的功能即可
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
    class BufferManager
    {
        int numBytes;
        byte[] buffer;
        Stack<int> freeIndexPool;
        int currentIndex;
        int bufferSize;
        

        
        /// <summary>
        /// 缓存管理，创建缓存块
        /// </summary>
        /// <param name="totalBytes">缓存的总大小</param>
        /// <param name="bufferSize">每块缓存的大小</param>
        public BufferManager(int totalBytes, int bufferSize)
        {
            numBytes = totalBytes;
            currentIndex = 0;
            this.bufferSize = bufferSize;
            freeIndexPool = new Stack<int>();        
        }


        /// <summary>
        /// 为buffer创建空间
        /// </summary>
        public void InitBuffer()
        {
            buffer = new byte[numBytes];
            
        }

        
        /// <summary>
        /// 为一个SocketAsyncEventArgs对象设置缓存区大小
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            if (freeIndexPool.Count > 0)
            {
                //如果freeIndexPool不为空，说明已经被用了，那么就在目前的栈顶部的偏移位开始Set，
                args.SetBuffer(buffer, freeIndexPool.Pop(), bufferSize);
            }
            else
            {
                if ((numBytes - bufferSize) < currentIndex)
                {
                    return false;
                }
                args.UserToken = currentIndex;
                args.SetBuffer(buffer, currentIndex, bufferSize);
                currentIndex += bufferSize;
            }
            return true;
        }

        public void SetBufferValue(SocketAsyncEventArgs args, byte[] value)
        {
            //取出SocketAsyncEventArgs的对象
            int offsize = (int)args.UserToken;

            for (int i = offsize; i < bufferSize + offsize; ++i)
            {
                if (i >= value.Length)
                {
                    break;
                }
                buffer[i] = value[i - offsize];
            }
        }

        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            freeIndexPool.Push(args.Offset);
            args.SetBuffer(null,0,0);
        }

    }
}
