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
    // State object for receiving data from remote device.
    public class StateObject
    {
        // Client socket.
        public Socket workSocket = null;

        // Size of receive buffer.
        public const short BufferSize = short.MaxValue; //32KB

        // Receive buffer.
        public byte[] buffer;

        public StateObject()
        {
            Console.WriteLine("StateObject constructor called!");
            buffer = new byte[BufferSize];
        }
    }

    /// <summary>
    /// Class SocketClientSocket.
    /// </summary>
    public class SocketClient
    {
        #region "Fields"
        // ManualResetEvent instances signal completion.
        private ManualResetEvent connectDone = new ManualResetEvent(false);
        private ManualResetEvent sendDone = new ManualResetEvent(false);
        private ManualResetEvent receiveDone = new ManualResetEvent(false);

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

        private IPEndPoint _endpoint;

        private StateObject stateObject = new StateObject();

        //Obsolete
        private readonly Timer task;
        private readonly Action<object> ClientCallback;
        private readonly int period = 1;

        private static Lazy<Dictionary<ulong, SocketStorer>> dataBuffer;

        //Flag to final deserialization
        //public bool deserialize;

        //Its own requestID list
        internal List<ulong> requestIDs = new List<ulong>();
        #endregion

        #region "Properties"
        private SocketState _state;
        public SocketState myState
        {
            get
            {
                return _state;
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

        public ulong maxReqId
        {
            get
            {
                return Id + ushort.MaxValue;
            }
        }
        #endregion

        #region "Constructors"
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
        public SocketClient(Action<object> everyFunc, bool doConnection = false) :
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

        public SocketClient(IPAddress ip, ushort port, Action<object> everyFunc, int readEvery = 100, bool doConnection = false) :
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
        public SocketClient(string ip, ushort port, Action<object> everyFunc, int readEvery = 100, bool doConnection = false) :
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
        public SocketClient(IPAddress ipAddr, ushort port, SocketType sType, ProtocolType pType, int readEvery, Action<object> everyFunc, bool doConnection = false)
        {
            period = readEvery;

            ClientCallback = everyFunc;
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
        #endregion

        #region "Socket Methods"

        #region "Timering Methods"
        /// <summary>
        /// Starts the receiving.
        /// </summary>
        [Obsolete]
        protected void StartReceiving()
        {
            _Receiving(period);
        }

        /// <summary>
        /// Stops the receiving.
        /// </summary>
        [Obsolete]
        protected void StopReceiving()
        {
            _Receiving();
        }

        [Obsolete]
        private void _Receiving(int p = 0)
        {
            if (task != null)
                task.Change(5, p);
        }

        [Obsolete]
        private void Timering(object stateInfo)
        {
            Receive();
            //ClientCallback(null);
            //if (deserialize) deserialize = false;
        }
        #endregion

        public void DoConnection()
        {
            if (IPEnd != null)
            {
                // Connect to a remote device.
                try
                {
                    // Connect to the remote endpoint.
                    ClientSocket.BeginConnect(_endpoint,
                        new AsyncCallback(ConnectCallback), ClientSocket);
                    connectDone.WaitOne();

                    // Send test data to the remote device.
                    ClientSocket.Send(SocketManager.ManagedConn());
                    sendDone.WaitOne();

                    // Receive the response from the remote device.
                    //StartReceiving(); //I will apply this later

                    //La cosa curiosa esq el metodo Receive se llama de forma ciclica sin necesitar un timer
                    //Aun asi voy a ver si adivino como funciona esto
                    Receive();
                    //receiveDone.WaitOne();

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

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.
                client.EndConnect(ar);

                Console.WriteLine("Socket connected to {0}",
                    client.RemoteEndPoint.ToString());

                // Signal that the connection has been made.
                connectDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void Receive()
        {
            try
            {
                // Create the state object.
                stateObject.workSocket = ClientSocket;

                // Begin receiving the data from the remote device.
                ClientSocket.BeginReceive(stateObject.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), stateObject);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket 
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.
                int bytesRec = ClientSocket.EndReceive(ar);

                SocketMessage sm = null;
                //if(bytesRec > 0)
                //Comment the condition to show messages from clients
                SocketManager.Deserialize(state.buffer, StateObject.BufferSize, out sm, SocketDbgType.Client);
                ///*else
                //{
                //    //Nothing received from server
                //    o = null;
                //}

                if (!(sm != null && sm.msg != null))
                    return; //Empty message received

                //Ya no hace falta devolver ningun bool, simplemente con llamar o no llamar al callback es suficiente
                //bool b = true;
                object o = null;

                if (sm.Type == typeof(SocketCommand))
                    HandleAction(sm);
                else if (sm.Type == typeof(SocketBuffer))
                    HandleBuffer(sm, out o);

                if(o != null)
                    ClientCallback(o);

                // Get the rest of the data.
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception ex)
            { //Forced connection close...
                Console.WriteLine("Exception ocurred while receiving data! " + ex.ToString());
                //o = null; //Dead silence.
                Stop();
            }
        }

        public int Send(object msg)
        {
            // Convert the string data to byte data using ASCII encoding.

            if (!ClientSocket.Equals(null) && ClientSocket.Connected)
            {
                IEnumerable<byte[]> byteData = null;

                int //bytesSend = 0
                    bytesLength = 0;

                if (!(msg is byte[]))
                {
                    byteData = SocketManager.SerializeForClients(this, Id, msg).AsEnumerable();
                    foreach (byte[] b in byteData)
                    {
                        //bytesSend += ClientSocket.Send(b);
                        bytesLength += b.Length;
                    }
                }
                else
                {
                    byte[] m = (byte[])msg;
                    //bytesSend = ClientSocket.Send(m);
                    bytesLength += m.Length;
                }

                Console.WriteLine("Sending on client serialized object of type: {0} and length of: {1}/{2}", msg.GetType().Name, bytesLength, bytesLength);

                // Begin sending the data to the remote device.
                // Aqui se usa ClientSocket al igual que en Receive
                ClientSocket.BeginSend(byteData.JoinMultipleArray().ToArray(), 0, bytesLength, 0,
                    new AsyncCallback(SendCallback), ClientSocket);

                return bytesLength;
            }
            else
            {
                Console.WriteLine("Error happened while sending data through a client!");
                return 0;
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = client.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.
                //sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        #endregion

        #region "Class Methods"
        private void HandleAction(SocketMessage sm)
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

                        Send(SocketManager.ConfirmConnId(Id));
                        break;

                    case SocketCommands.CloseInstance:
                        Console.WriteLine("Client is closing connection...");
                        Stop();
                        break;

                    default:
                        Console.WriteLine("Unknown ClientCallbackion to take! Case: {0}", cmd);
                        break;
                }
            }
            else
            {
                Console.WriteLine("Empty string received by client!");
            }
        }

        private void HandleBuffer(SocketMessage sm, out object o)
        {
            SocketBuffer buf = sm.TryGetObject<SocketBuffer>();
            if (buf != null)
            {
                try
                {
                    ulong reqId = sm.RequestID;

                    if (dataBuffer == null)
                        dataBuffer = new Lazy<Dictionary<ulong, SocketStorer>>(() => new Dictionary<ulong, SocketStorer>());

                    if (dataBuffer.Value.ContainsKey(reqId))
                    {
                        dataBuffer.Value[reqId].buffer.Value.AddRange(buf.splittedData);
                        ++dataBuffer.Value[reqId].PacketCount;
                    }
                    else
                    {
                        List<byte> bytes = new List<byte>(sm.MessageSize);
                        //bytes.AddRange(buf.splittedData);
                        dataBuffer.Value.Add(reqId, new SocketStorer()
                        {
                            buffer = new Lazy<List<byte>>(() =>
                            {
                                int le = buf.splittedData.Count();
                                for (int i = 0; i < le; ++i)
                                    bytes[buf.myOrder * StateObject.BufferSize + i] = buf.splittedData.ElementAt(i);
                                return bytes;
                            }),
                            PacketCount = 1
                        });
                    }

                    Console.WriteLine("Receiving {0}/{1} packets...", dataBuffer.Value[reqId].PacketCount, buf.blockNum);

                    if (dataBuffer.Value[reqId].PacketCount == buf.blockNum)
                    {
                        SocketManager.Deserialize(dataBuffer.Value[reqId].buffer.Value.ToArray(), 0, out o, SocketDbgType.Client);
                        dataBuffer.Value.Remove(reqId);
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Buffer couldn't add bytes due to an exception! Ex: "+ex);
                }
            }
            else
            {
                Console.WriteLine("Empty buffer received by client!");
            }
            o = null;
        }

        #region "Error & Close & Stop & Dispose" 
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

                    Send(SocketManager.ClientClosed(Id));
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
        #endregion

        #endregion
    }
}