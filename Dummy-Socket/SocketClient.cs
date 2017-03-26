using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Dummy_Socket
{

    public class SocketClient
    { //Hacer IDisposable?
        public Socket ClientSocket;
        public IPAddress IP;
        public int Port;
        //private Timer thTimer;
        private IPEndPoint _endpoint;
        private byte[] socketBuffer; //I will keep this static, but I think I will have problems
        private Timer task;
        private Action act;
        private int period = 1;

        public frmSocket ins;

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
            }
            else ins.WriteClientLog("Destination IP isn't defined!");
        }

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
            int bytesSend = ClientSocket.Send(Encoding.Unicode.GetBytes(msg));
            if (breakLine) BreakLine();
            return bytesSend;
        }

        private void BreakLine()
        {
            ClientSocket.Send(Encoding.Unicode.GetBytes("<Client Quit>"));
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
                ins.WriteClientLog("Remember that you're in a Client, you, you can't only close Both connections or only your connection.");
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