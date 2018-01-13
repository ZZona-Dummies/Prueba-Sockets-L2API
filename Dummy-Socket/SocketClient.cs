using System;
using System.Collections.Generic;
using System.Linq;
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

        private static Dictionary<ulong, Dictionary<uint, IEnumerable<byte>>> dataBuffer = new Dictionary<ulong, Dictionary<uint, IEnumerable<byte>>>();

        //Flag to final deserialization
        public bool deserialize;

        //Its own requestID list
        internal List<ulong> requestIDs = new List<ulong>();

        public ulong maxReqId
        {
            get
            {
                return Id + ushort.MaxValue;
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
                    ClientSocket.Send(SocketManager.ManagedConn());
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Exception ocurred while starting CLIENT: "+ex);
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
                IEnumerable<byte[]> bytes = null;

                int bytesSend = 0,
                    bytesLength = 0;

                if (!(msg is byte[]))
                {
                    bytes = SocketManager.SerializeForClients(this, Id, msg).AsEnumerable();
                    foreach (byte[] b in bytes)
                    {
                        bytesSend += ClientSocket.Send(b);
                        bytesLength += b.Length;
                    }
                }
                else
                {
                    byte[] m = (byte[])msg;
                    bytesSend = ClientSocket.Send(m);
                    bytesLength += m.Length;
                }

                Console.WriteLine("Sending on client serialized object of type: {0} and length of: {1}/{2}", msg.GetType().Name, bytesLength, bytesSend);

                return bytesSend;
            }
            else
            {
                Console.WriteLine("Error happened while sending data through a client!");
                return 0;
            }
        }

        /// <summary>
        /// Receives the message.
        /// </summary>
        /// <param name="msg">The MSG.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool ReceiveData(out object o) //No entiendo porque este es sincrono, deberia ser asincrono... Copy & paste rulez!
        { //Esto solo devolverá falso cuando se cierre la conexión...
            try
            {
                // Receives data from a bound Socket.
                int bytesRec = 0;
                byte[] bytes = new byte[SocketManager.minBufferSize];

                try
                {
                    // Continues to read the data till data isn't available
                    while (ClientSocket.Available > 0)
                        bytesRec = ClientSocket.Receive(bytes);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Server exception ocurred, stopping this client! (#{0}) => " + ex, Id);

                    Stop(); //Don't stop, the server will do it for this client.
                    o = null;
                    return false;
                }

                SocketMessage sm = null;
                //if(bytesRec > 0)
                //Comment the condition to show messages from clients
                    SocketManager.Deserialize(bytes, SocketManager.minBufferSize, out sm, SocketDbgType.Client);
                /*else
                {
                    //Nothing received from server
                    o = null;
                }*/

                if (!(sm != null && sm.msg != null))
                {
                    o = null;
                    return false; //Empty message received
                }

                bool b = true;

                if (sm.Type == typeof(SocketCommand))
                    b = HandleAction(ref sm);
                else if (sm.Type == typeof(SocketBuffer))
                    b = HandleBuffer(ref sm);

                o = sm.msg;
                return b;
            }
            catch (Exception ex)
            { //Forced connection close...
                Console.WriteLine("Exception ocurred while receiving data! " + ex.ToString());
                o = null; //Dead silence.
                Stop();
                return false;
            }
        }

        private bool HandleAction(ref SocketMessage sm)
        {
            //Before we connect we request an id to the master server...
            SocketCommand cmd = sm.TryGetObject<SocketCommand>();
            if (cmd != null)
            {
                switch (cmd.Command)
                {
                    case SocketCommands.CreateConnId:
                        Console.WriteLine("Starting new CLIENT connection with ID: {0}", sm.id);
                        Id = sm.id;

                        SendData(SocketManager.ConfirmConnId(Id));
                        return true;

                    case SocketCommands.CloseInstance:
                        Console.WriteLine("Client is closing connection...");
                        Stop();
                        return false;

                    default:
                        Console.WriteLine("Unknown action to take! Case: {0}", cmd);
                        return true;
                }
            }
            else
            {
                Console.WriteLine("Empty string received by client!");
                return false;
            }
        }

        private bool HandleBuffer(ref SocketMessage sm)
        {
            SocketBuffer buf = sm.TryGetObject<SocketBuffer>();
            if (buf != null)
            {
                try
                {
                    ulong reqId = sm.RequestID;

                    //Tengo que optimizar esto
                    if (dataBuffer.ContainsKey(reqId))
                    {
                        Dictionary<uint, IEnumerable<byte>> d = new Dictionary<uint, IEnumerable<byte>>();
                        d.Add(buf.myOrder, buf.splittedData);
                        dataBuffer[reqId] = d; //.AddRange(buf.splittedData);
                    }
                    else
                    {
                        Dictionary<uint, IEnumerable<byte>> d = new Dictionary<uint, IEnumerable<byte>>();
                        d.Add(buf.myOrder, buf.splittedData);
                        dataBuffer.Add(reqId, d);
                    }

                    if(buf.end)
                    {
                        object o = null;
                        SocketManager.Deserialize(dataBuffer[reqId].Select(x => x.Value), 0, out o, SocketDbgType.Client);
                        sm = new SocketMessage(sm.id, 0, o);
                        deserialize = true;
                        dataBuffer.Remove(reqId);
                    }
                    return true;
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Buffer couldn't add bytes due to an exception! Ex: "+ex);
                    return false;
                }
            }
            else
            {
                Console.WriteLine("Empty buffer received by client!");
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
            if (deserialize) deserialize = false;
        }
    }
}