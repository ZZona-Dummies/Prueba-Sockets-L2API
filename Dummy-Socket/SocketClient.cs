using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Timer = System.Threading.Timer;

namespace DeltaSockets
{
    /// <summary>
    /// Class SocketClient.
    /// </summary>
    public class SocketClient
    {
        public SocketClientConsole myLogger = new SocketClientConsole(null);

        /// <summary>
        /// The client socket
        /// </summary>
        public Socket ClientSocket;

        /// <summary>
        /// The ip
        /// </summary>
        public IPAddress IP;

        /// <summary>
        /// The port
        /// </summary>
        public ushort Port;
        public ulong Id;

        private SocketState _state;
        public SocketState myState
        {
            get
            {
                return _state;
            }
        }

        private IPEndPoint _endpoint;

        private bool autoConnect;

        //Obsolete
        private readonly Timer task;
        private readonly Action act;
        private readonly int period = 1;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="SocketClient"/> class.
        /// </summary>
        /// <param name="doConnection">if set to <c>true</c> [do connection].</param>
        public SocketClient(bool doConnection = false) :
            this(IPAddress.Loopback, SocketServer.DefPort, SocketType.Stream, ProtocolType.Tcp, -1, null, doConnection)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SocketClient"/> class.
        /// </summary>
        /// <param name="everyFunc">The every function.</param>
        /// <param name="doConnection">if set to <c>true</c> [do connection].</param>
        public SocketClient(Action everyFunc, bool doConnection = false) :
            this(IPAddress.Loopback, SocketServer.DefPort, SocketType.Stream, ProtocolType.Tcp, -1, everyFunc, doConnection)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SocketClient"/> class.
        /// </summary>
        /// <param name="ip">The ip.</param>
        /// <param name="port">The port.</param>
        /// <param name="doConnection">if set to <c>true</c> [do connection].</param>
        public SocketClient(string ip, ushort port, bool doConnection = false) :
            this(ip, port, null, 100, doConnection)
        { }

        public SocketClient(IPAddress ip, ushort port, bool doConnection = false) :
            this(ip, port, SocketType.Stream, ProtocolType.Tcp, 100, null, doConnection)
        { }

        public SocketClient(IPAddress ip, ushort port, Action everyFunc, int readEvery = 100, bool doConnection = false) :
            this(ip, port, SocketType.Stream, ProtocolType.Tcp, readEvery, everyFunc, doConnection)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SocketClient"/> class.
        /// </summary>
        /// <param name="ip">The ip.</param>
        /// <param name="port">The port.</param>
        /// <param name="readEvery">The read every.</param>
        /// <param name="everyFunc">The every function.</param>
        /// <param name="doConnection">if set to <c>true</c> [do connection].</param>
        public SocketClient(string ip, ushort port, Action everyFunc, int readEvery = 100, bool doConnection = false) :
            this(IPAddress.Parse(ip), port, SocketType.Stream, ProtocolType.Tcp, readEvery, everyFunc, doConnection)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SocketClient"/> class.
        /// </summary>
        /// <param name="ipAddr">The ip addr.</param>
        /// <param name="port">The port.</param>
        /// <param name="sType">Type of the s.</param>
        /// <param name="pType">Type of the p.</param>
        /// <param name="readEvery">The read every.</param>
        /// <param name="everyFunc">The every function.</param>
        /// <param name="doConnection">if set to <c>true</c> [do connection].</param>
        public SocketClient(IPAddress ipAddr, ushort port, SocketType sType, ProtocolType pType, int readEvery, Action everyFunc, bool doConnection = false)
        {
            //bytes = new byte[1024 * 1024 * 10];

            period = readEvery;

            act = everyFunc;
            TimerCallback timerDelegate = new TimerCallback(Timering);

            if (everyFunc != null)
                task = new Timer(timerDelegate, null, 5, readEvery);

            IP = ipAddr;
            Port = port;

            ClientSocket = new Socket(ipAddr.AddressFamily, sType, pType)
            {
                NoDelay = false
            };

            if (doConnection)
                DoConnection();
        }

        /// <summary>
        /// Starts the receiving.
        /// </summary>
        protected void StartReceiving()
        {
            _Receiving(period);
        }

        /// <summary>
        /// Stops the receiving.
        /// </summary>
        protected void StopReceiving()
        {
            _Receiving();
        }

        private void _Receiving(int p = 0)
        {
            if (task != null)
                task.Change(5, p);
        }

        /// <summary>
        /// Does the connection.
        /// </summary>
        public void DoConnection()
        {
            if (IPEnd != null)
            {
                try
                {
                    ClientSocket.Connect(IPEnd);
                    StartReceiving();
                    Console.WriteLine("Starting new CLIENT connection with ID: {0}", Id);
                    ClientSocket.Send(SocketManager.ManagedConn(Id));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception ocurred while starting CLIENT: " + ex);
                    return;
                }
                _state = SocketState.ClientStarted;
            }
            else
                Console.WriteLine("Destination IP isn't defined!");
        }

        public int SendData(object msg)
        {
            if (!ClientSocket.Equals(null) && ClientSocket.Connected)
            {
                //bytesSend = 0;
                byte[] bytes = SocketManager.SerializeForClients(Id, msg);

                //if(bytes.Length > 1024)
                //    bytesSend += ClientSocket.Send(SocketManager.Serialize(Id, "Block_Size:" + bytes.Length));

                int bytesSend = ClientSocket.Send(bytes);

                return bytesSend;
            }
            else
            {
                Console.WriteLine("Error happened while sending data through a client!");
                return 0;
            }
        }

        private void BreakLine()
        {
            ClientSocket.Send(Encoding.Unicode.GetBytes("<stop>"));
        }

        /// <summary>
        /// Receives the message.
        /// </summary>
        /// <param name="msg">The MSG.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool ReceiveData(out SocketMessage msg) //No entiendo porque este es sincrono, deberia ser asincrono... Copy & paste rulez!
        { //Esto solo devolverá falso cuando se cierre la conexión...
            try
            {
                // Receives data from a bound Socket.
                int bytesRec = 0;
                byte[] bytes = new byte[1024];

                if (!ClientSocket.Equals(null))
                {
                    // Continues to read the data till data isn't available
                    while (ClientSocket.Available > 0)
                        bytesRec = ClientSocket.Receive(bytes);
                }
                else
                {
                    Console.WriteLine("Server requesting this client to stop, stopping this client! (#{0})", Id);

                    Stop(); //Don't stop, the server will do it for this client.
                    msg = null;
                    return false;
                }

                SocketManager.Deserialize(bytes, 1024, out msg, SocketDbgType.Client);

                if (msg == null)
                    return false; //Empty message received

                if (msg.StringValue == typeof(string).Name)
                    return HandleAction(msg);

                return true;
            }
            catch (Exception ex)
            { //Forced connection close...
                Console.WriteLine("Exception ocurred while receiving data! " + ex.ToString());
                msg = null; //Dead silence.
                Stop();
                return false;
            }
        }

        private bool HandleAction(SocketMessage sm)
        {
            //before we connect we request an id to the master server...
            string val = sm.StringValue;
            if (!string.IsNullOrWhiteSpace(val))
            {
                switch (val)
                {
                    case "<give_id>":
                        Id = sm.id;
                        SendData(SocketManager.ConfirmId(Id));
                        return true;
                    case "<close>":
                        Console.WriteLine("Client is closing connection...");
                        Stop();
                        return false;
                    default:
                        Console.WriteLine("Unknown action to take! Case: {0}", val);
                        return true;
                }
            }
            else
            {
                Console.WriteLine("Empty string received by client!");
                return false;
            }
        }

        private void CloseConnection(SocketShutdown soShutdown)
        {
            if (soShutdown == SocketShutdown.Receive)
            {
                Console.WriteLine("Remember that you're in a Client, you can't only close Both connections or only your connection.");
                return;
            }
            if (ClientSocket.Connected)
            {
                ClientSocket.Disconnect(false);
                if (ClientSocket.Connected)
                    ClientSocket.Shutdown(soShutdown);
            }
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            ClientSocket.Close();
        }

        /// <summary>
        /// Ends this instance.
        /// </summary>
        public void Stop()
        {
            if (_state == SocketState.ClientStarted)
            {
                try
                {
                    Console.WriteLine("Closing client (#{0})", Id);

                    SendData(SocketManager.ClientClosed(Id));
                    _state = SocketState.ClientStopped;
                    CloseConnection(SocketShutdown.Both); //No hace falta comprobar si estamos connected
                    Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception ocurred while trying to stop client: " + ex);
                }
            }
            else
                Console.WriteLine("Client cannot be stopped because it hasn't been started!");
        }

        private void Timering(object stateInfo)
        {
            act();
        }
    }
}