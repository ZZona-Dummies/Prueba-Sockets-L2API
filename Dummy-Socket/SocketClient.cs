using Lerp2API.SafeECalls;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
//using Lerp2API.SafeECalls;

namespace Dummy_Socket
{
    public class SocketMessage
    {
        public int id;
        //Name??
        public string msg;

        public SocketMessage(int i, string m)
        {
            id = i;
            msg = m;
        }
    }

    public class SocketClient
    { //Hacer IDisposable?
        public Socket ClientSocket;
        public IPAddress IP;
        public int Port, Id;
        //private Timer thTimer;
        private IPEndPoint _endpoint;
        private byte[] socketBuffer; //I will keep this static, but I think I will have problems
        private Timer task;
        private Action act;
        private int period = 1;

        public int socketID;
        private frmSocket ins
        {
            get
            {
                return frmMain.socketIns[socketID].instance;
            }
        }

        internal IPEndPoint IPEnd
        {
            get
            {
                if (IP != null)
                {
                    if (_endpoint == null)
                        _endpoint = new IPEndPoint(IP, Port);
                    return _endpoint;
                }
                else return null;
            }
        }

        public SocketClient(bool doConnection = false) :
            this(IPAddress.Loopback, SocketServer.lerpedPort, SocketType.Stream, ProtocolType.Tcp, 1, null, doConnection)
        { }

        public SocketClient(Action everyFunc, bool doConnection = false) :
            this(IPAddress.Loopback, SocketServer.lerpedPort, SocketType.Stream, ProtocolType.Tcp, 1, everyFunc, doConnection)
        { }

        public SocketClient(string ip, int port, bool doConnection = false) :
            this(ip, port, -1, null, doConnection)
        { }

        public SocketClient(string ip, int port, Action everyFunc, bool doConnection = false) :
            this(ip, port, 1, everyFunc, doConnection)
        { }


        public SocketClient(string ip, int port, int readEvery, Action everyFunc, bool doConnection = false) :
            this(IPAddress.Parse(ip), port, SocketType.Stream, ProtocolType.Tcp, readEvery, everyFunc, doConnection)
        { }

        public SocketClient(IPAddress ipAddr, int port, SocketType sType, ProtocolType pType, int readEvery, Action everyFunc, bool doConnection = false)
        {
            socketBuffer = new byte[1024];

            //thTimer = new Timer(cbTimer != null ? cbTimer : SocketCallback, obj != null ? obj : socketBuffer, Timeout.Infinite, Timeout.Infinite);

            period = readEvery;

            act = everyFunc;
            TimerCallback timerDelegate = new TimerCallback(Timering);

            if (everyFunc != null)
                task = new Timer(timerDelegate, null, 5, readEvery); //CronTask.CreateInstance(everyFunc, readEvery);

            IP = ipAddr;
            Port = port;

            ClientSocket = new Socket(ipAddr.AddressFamily, sType, pType);
            ClientSocket.NoDelay = false;

            Id = ClientSocket.GetHashCode();

            if (doConnection)
            {
                ClientSocket.Connect(IPEnd);
                //if (cbTimer != null) 
                StartReceiving();
            }
        }

        public void StartReceiving()
        {
            if (task != null)
                task.Change(5, period);
        }

        public void StopReceiving()
        {
            if (task != null)
                task.Change(5, 0);
        }

        public void DoConnection()
        {
            IPEndPoint end = IPEnd;
            if (end != null)
            {
                ClientSocket.Connect(end);
                StartReceiving();
                ClientSocket.Send(Encoding.Unicode.GetBytes(JsonUtility.ToJson(new SocketMessage(Id, "<conn>"))));
            }
#if STATIC_LOG
            else frmSocket.WriteClientLog("Destination IP isn't defined!");
#else
            else ins.WriteClientLog("Destination IP isn't defined!");
#endif
        }

        /*private void Send(byte[] buffer, int offset, int size, int timeout)
        {
            int startTickCount = Environment.TickCount;
            int sent = 0;  // how many bytes is already sent
            do
            {
                if (Environment.TickCount > startTickCount + timeout)
                    throw new Exception("Timeout.");
                try
                {
                    sent += ClientSocket.Send(buffer, offset + sent, size - sent, SocketFlags.None);
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.WouldBlock ||
                        ex.SocketErrorCode == SocketError.IOPending ||
                        ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                    {
                        // socket buffer is probably full, wait and try again
                        Thread.Sleep(30);
                    }
                    else
                        throw ex;  // any serious error occurr
                }
            } while (sent < size);
        }

        private void Receive(byte[] buffer, int offset, int size, int timeout)
        {
            int startTickCount = Environment.TickCount;
            int received = 0;  // how many bytes is already received
            do
            {
                if (Environment.TickCount > startTickCount + timeout)
                    throw new Exception("Timeout.");
                try
                {
                    received += ClientSocket.Receive(buffer, offset + received, size - received, SocketFlags.None);
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.WouldBlock ||
                        ex.SocketErrorCode == SocketError.IOPending ||
                        ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                    {
                        // socket buffer is probably empty, wait and try again
                        Thread.Sleep(30);
                    }
                    else
                        throw ex;  // any serious error occurr
                }
            } while (received < size);
        }

        public string ReceiveMessage()
        {
            byte[] buffer = new byte[1024];  // length of the text "Hello world!"
            //try
            //{ // receive data with timeout 10s
                Receive(buffer, 0, buffer.Length, 10000);
                string str = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
                return str;
            //}
        }

        public void WriteLine(string msg)
        {
            //try
            //{ // sends the text with timeout 10s
                Send(Encoding.UTF8.GetBytes(msg), 0, msg.Length, 10000);
            //}
        }*/

        //Esto lo tengo que arreglar
        public int Write(string msg)
        {
            return SendMessage(msg, false);
        }

        public int WriteLine(string msg)
        {
            return SendMessage(msg, true);
        }

        private int SendMessage(string msg, bool breakLine)
        {
            string message = JsonUtility.ToJson(new SocketMessage(Id, msg));
            int bytesSend = ClientSocket.Send(Encoding.Unicode.GetBytes(message));
            //if (breakLine) BreakLine(); //Voy a desactivar esto temporalmente
            return bytesSend;
        }

        private void BreakLine()
        {
            ClientSocket.Send(Encoding.Unicode.GetBytes("<stop>"));
        }

        public string ReceiveMessage(byte[] bytes)
        {
            // Receives data from a bound Socket.
            int bytesRec = ClientSocket.Receive(bytes);

            // Converts byte array to string
            string msg = Encoding.Unicode.GetString(bytes, 0, bytesRec);

            // Continues to read the data till data isn't available
            while (ClientSocket.Available > 0)
            {
                bytesRec = ClientSocket.Receive(bytes);
                msg += Encoding.Unicode.GetString(bytes, 0, bytesRec);
            }
            return msg;
        }

        private void SocketCallback(object obj)
        {
            ReceiveMessage((byte[])obj);
        }

        public void CloseConnection(SocketShutdown soShutdown)
        {
            if (soShutdown == SocketShutdown.Receive)
            {
#if STATIC_LOG
                frmSocket.WriteClientLog("Remember that you're in a Client, you, you can't only close Both connections or only your connection.");
#else
                ins.WriteClientLog("Remember that you're in a Client, you, you can't only close Both connections or only your connection.");
#endif
                return;
            }
            ClientSocket.Shutdown(soShutdown);
        }

        public void DisposeSocket()
        {
            ClientSocket.Close();
        }

        private void Timering(object stateInfo)
        {
            act();
        }

    }
}