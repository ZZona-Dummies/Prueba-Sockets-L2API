using System;
using System.IO;

namespace DeltaSockets
{
    public static class SocketGlobals
    {
        public const int gBufferSize = 1024;

        public class AsyncReceiveState
        {
            public System.Net.Sockets.Socket Socket;
            public byte[] Buffer = new byte[gBufferSize];

            internal MemoryStream _packetBuff;

            // a buffer for appending received data to build the packet
            public MemoryStream PacketBufferStream
            {
                get
                {
                    if (_packetBuff == null)
                    {
                        Console.WriteLine("Creating a new MemoryStream");
                        _packetBuff = new MemoryStream();
                    }
                    return _packetBuff;
                }
                set
                {
                    _packetBuff = value;
                }
            }

            public object Packet;

            // the size (in bytes) of the Packet
            public int ReceiveSize;

            // the total bytes received for the Packet so far
            public int TotalBytesReceived;
        }

        public class AsyncSendState
        {
            public System.Net.Sockets.Socket Socket;

            //Public Buffer(Carcassonne.Library.PacketBufferSize - 1) As Byte ' a buffer to store the currently received chunk of bytes
            public byte[] BytesToSend;

            public int Progress;

            public AsyncSendState(System.Net.Sockets.Socket argSocket)
            {
                Socket = argSocket;
            }

            public int NextOffset()
            {
                return Progress;
            }

            public int NextLength()
            {
                if (BytesToSend.Length - Progress > gBufferSize)
                {
                    return gBufferSize;
                }
                else
                {
                    return BytesToSend.Length - Progress;
                }
            }
        }

        public class MessageQueue
        {
            public System.Collections.Queue Messages = new System.Collections.Queue();
            public bool Processing;

            public event MessageQueuedEventHandler MessageQueued;

            public delegate void MessageQueuedEventHandler();

            public void Add(AsyncSendState argState)
            {
                Messages.Enqueue(argState);
                MessageQueued?.Invoke();
            }
        }
    }
}