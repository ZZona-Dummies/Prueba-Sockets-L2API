using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

        [Obsolete("Use IPEnd instead.")]
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

        #endregion "Fields"

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

        #endregion "Properties"

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

        #endregion "Constructors"

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
            //Receive();
            //ClientCallback(null);
            //if (deserialize) deserialize = false;
        }

        #endregion "Timering Methods"

        /*public void DoConnection()
        {
            if (IPEnd != null)
            {
                // Connect to a remote device.
                try
                {
                    // Connect to the remote endpoint.
                    ClientSocket.BeginConnect(IPEnd,
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
                    myLogger.Log("Exception ocurred while starting CLIENT: " + ex);
                    return;
                }
                _state = SocketState.ClientStarted;
            }
            else
                myLogger.Log("Destination IP isn't defined!");
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.
                client.EndConnect(ar);

                myLogger.Log("Socket connected to {0}",
                    client.RemoteEndPoint.ToString());

                // Signal that the connection has been made.
                connectDone.Set();
            }
            catch (Exception e)
            {
                myLogger.Log(e.ToString());
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
                myLogger.Log(e.ToString());
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

                if (o != null)
                    ClientCallback(o);

                // Get the rest of the data.
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception ex)
            { //Forced connection close...
                myLogger.Log("Exception ocurred while receiving data! " + ex.ToString());
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

                myLogger.Log("Sending on client serialized object of type: {0} and length of: {1}/{2}", msg.GetType().Name, bytesLength, bytesLength);

                // Begin sending the data to the remote device.
                // Aqui se usa ClientSocket al igual que en Receive
                ClientSocket.BeginSend(byteData.JoinMultipleArray().ToArray(), 0, bytesLength, 0,
                    new AsyncCallback(SendCallback), ClientSocket);

                return bytesLength;
            }
            else
            {
                myLogger.Log("Error happened while sending data through a client!");
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
                myLogger.Log("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.
                //sendDone.Set();
            }
            catch (Exception e)
            {
                myLogger.Log(e.ToString());
            }
        }*/

        private SocketGlobals.MessageQueue cSendQueue = new SocketGlobals.MessageQueue();

        public event MessageSentToServerEventHandler MessageSentToServer;

        public delegate void MessageSentToServerEventHandler(string argCommandString);

        public void DoConnection()
        {
            // create the TcpListener which will listen for and accept new client connections asynchronously
            /*cClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // convert the server address and port into an ipendpoint
            IPAddress[] mHostAddresses = Dns.GetHostAddresses(cServerAddress);
            IPEndPoint mEndPoint = null;
            foreach (IPAddress mHostAddress in mHostAddresses)
            {
                if (mHostAddress.AddressFamily == AddressFamily.InterNetwork)
                {
                    mEndPoint = new IPEndPoint(mHostAddress, cServerPort);
                }
            }*/

            // connect to server async
            try
            {
                ClientSocket.BeginConnect(IPEnd, new AsyncCallback(ConnectToServerCompleted), new SocketGlobals.AsyncSendState(ClientSocket));
            }
            catch (Exception ex)
            {
                myLogger.Log("ConnectToServer error: " + ex.Message);
            }
        }

        public void DisconnectFromServer()
        {
            ClientSocket.Disconnect(false);
        }

        /// <summary>
        /// Fires right when a client is connected to the server.
        /// </summary>
        /// <param name="ar"></param>
        /// <remarks></remarks>
        public void ConnectToServerCompleted(IAsyncResult ar)
        {
            // get the async state object which was returned by the async beginconnect method
            SocketGlobals.AsyncSendState mState = (SocketGlobals.AsyncSendState)ar.AsyncState;

            // end the async connection request so we can check if we are connected to the server
            try
            {
                // call the EndConnect method which will succeed or throw an error depending on the result of the connection
                mState.Socket.EndConnect(ar);
                // at this point, the EndConnect succeeded and we are connected to the server!
                // send a welcome message
                SendMessageToServer("/say What? My name is...");
                // start waiting for messages from the server
                SocketGlobals.AsyncReceiveState mReceiveState = new SocketGlobals.AsyncReceiveState();
                mReceiveState.Socket = mState.Socket;
                mReceiveState.Socket.BeginReceive(mReceiveState.Buffer, 0, SocketGlobals.gBufferSize, SocketFlags.None, new AsyncCallback(ServerMessageReceived), mReceiveState);
            }
            catch (Exception ex)
            {
                // at this point, the EndConnect failed and we are NOT connected to the server!
                myLogger.Log("Connect error: " + ex.Message);
            }
        }

        public void ServerMessageReceived(IAsyncResult ar)
        {
            // get the async state object from the async BeginReceive method
            SocketGlobals.AsyncReceiveState mState = (SocketGlobals.AsyncReceiveState)ar.AsyncState;
            using (mState.PacketBufferStream = new System.IO.MemoryStream())
            {
                // call EndReceive which will give us the number of bytes received
                int numBytesReceived = 0;
                numBytesReceived = mState.Socket.EndReceive(ar);
                // determine if this is the first data received
                if (mState.ReceiveSize == 0)
                {
                    // this is the first data recieved, so parse the receive size which is encoded in the first four bytes of the buffer
                    mState.ReceiveSize = BitConverter.ToInt32(mState.Buffer, 0);
                    // write the received bytes thus far to the packet data stream
                    mState.PacketBufferStream.Write(mState.Buffer, 4, numBytesReceived - 4);
                }
                else
                {
                    // write the received bytes thus far to the packet data stream
                    mState.PacketBufferStream.Write(mState.Buffer, 0, numBytesReceived);
                }
                // increment the total bytes received so far on the state object
                mState.TotalBytesReceived += numBytesReceived;
                // check for the end of the packet
                // bytesReceived = Carcassonne.Library.PacketBufferSize Then
                if (mState.TotalBytesReceived < mState.ReceiveSize)
                {
                    // ## STILL MORE DATA FOR THIS PACKET, CONTINUE RECEIVING ##
                    // the TotalBytesReceived is less than the ReceiveSize so we need to continue receiving more data for this packet
                    mState.Socket.BeginReceive(mState.Buffer, 0, SocketGlobals.gBufferSize, SocketFlags.None, new AsyncCallback(ServerMessageReceived), mState);
                }
                else
                {
                    // ## FINAL DATA RECEIVED, PARSE AND PROCESS THE PACKET ##
                    // the TotalBytesReceived is equal to the ReceiveSize, so we are done receiving this Packet...parse it!
                    System.Runtime.Serialization.Formatters.Binary.BinaryFormatter mSerializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    // rewind the PacketBufferStream so we can de-serialize it
                    mState.PacketBufferStream.Position = 0;
                    // de-serialize the PacketBufferStream which will give us an actual Packet object
                    mState.Packet = (string)mSerializer.Deserialize(mState.PacketBufferStream);
                    // parse the complete message that was received from the server
                    ParseReceivedServerMessage(mState.Packet, mState.Socket);
                    // call BeginReceive again, so we can start receiving another packet from this client socket
                    SocketGlobals.AsyncReceiveState mNextState = new SocketGlobals.AsyncReceiveState();
                    mNextState.Socket = mState.Socket;
                    mNextState.Socket.BeginReceive(mNextState.Buffer, 0, SocketGlobals.gBufferSize, SocketFlags.None, new AsyncCallback(ServerMessageReceived), mNextState);
                }
            }
        }

        public void ParseReceivedServerMessage(string argCommandString, Socket argClient)
        {
            myLogger.Log(argCommandString);
            //Select Case argDat
            //    Case "hi"
            //        Send("hi", argClient)
            //End Select
            //RaiseEvent MessageReceived(argMsg & " | " & argDat)
        }

        public void SendMessageToServer(string argCommandString)
        {
            // create a Packet object from the passed data; this packet can be any object type because we use serialization!
            //Dim mPacket As New Dictionary(Of String, String)
            //mPacket.Add("CMD", argCommandString)
            //mPacket.Add("MSG", argMessageString)
            string mPacket = argCommandString;

            byte[] mPacketBytes = null;
            // serialize the Packet into a stream of bytes which is suitable for sending with the Socket
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter mSerializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            using (System.IO.MemoryStream mSerializerStream = new System.IO.MemoryStream())
            {
                mSerializer.Serialize(mSerializerStream, mPacket);

                // get the serialized Packet bytes
                mPacketBytes = mSerializerStream.GetBuffer();
            }

            // convert the size into a byte array
            byte[] mSizeBytes = BitConverter.GetBytes(mPacketBytes.Length + 4);

            // create the async state object which we can pass between async methods
            SocketGlobals.AsyncSendState mState = new SocketGlobals.AsyncSendState(ClientSocket);

            // resize the BytesToSend array to fit both the mSizeBytes and the mPacketBytes
            // ERROR: Not supported in C#: ReDimStatement
            Array.Resize(ref mState.BytesToSend, mPacketBytes.Length + mSizeBytes.Length);

            // copy the mSizeBytes and mPacketBytes to the BytesToSend array
            Buffer.BlockCopy(mSizeBytes, 0, mState.BytesToSend, 0, mSizeBytes.Length);
            Buffer.BlockCopy(mPacketBytes, 0, mState.BytesToSend, mSizeBytes.Length, mPacketBytes.Length);

            ClientSocket.BeginSend(mState.BytesToSend, mState.NextOffset(), mState.NextLength(), SocketFlags.None, new AsyncCallback(MessagePartSent), mState);
        }

        ///' <summary>
        ///' QueueMessage prepares a Message object containing our data to send and queues this Message object in the OutboundMessageQueue.
        ///' </summary>
        ///' <param name="argCommandMessage"></param>
        ///' <param name="argCommandData"></param>
        ///' <remarks></remarks>
        //Sub QueueMessage(ByVal argCommandMessage As String, ByVal argCommandData As Object)

        //End Sub

        private void cSendQueue_MessageQueued()
        {
            // when a message is queued, we need to check whether or not we are currently processing the queue before allowing the top item in the queue to start sending
            if (cSendQueue.Processing == false)
            {
                // process the top message in the queue, which in turn will process all other messages until the queue is empty
                SocketGlobals.AsyncSendState mState = (SocketGlobals.AsyncSendState)cSendQueue.Messages.Dequeue();
                // we must send the correct number of bytes, which must not be greater than the remaining bytes
                ClientSocket.BeginSend(mState.BytesToSend, mState.NextOffset(), mState.NextLength(), SocketFlags.None, new AsyncCallback(MessagePartSent), mState);
            }
        }

        public void MessagePartSent(IAsyncResult ar)
        {
            // get the async state object which was returned by the async beginsend method
            SocketGlobals.AsyncSendState mState = (SocketGlobals.AsyncSendState)ar.AsyncState;
            try
            {
                int numBytesSent = 0;
                // call the EndSend method which will succeed or throw an error depending on if we are still connected
                numBytesSent = mState.Socket.EndSend(ar);
                // increment the total amount of bytes processed so far
                mState.Progress += numBytesSent;
                // determine if we havent' sent all the data for this Packet yet
                if (mState.NextLength() > 0)
                {
                    // we need to send more data
                    mState.Socket.BeginSend(mState.BytesToSend, mState.NextOffset(), mState.NextLength(), SocketFlags.None, new AsyncCallback(MessagePartSent), mState);
                }
                // at this point, the EndSend succeeded and we are ready to send something else!
                // TODO: use the queue to determine what message was sent and show it in the local chat buffer
                //RaiseEvent MessageSentToServer()
            }
            catch (Exception ex)
            {
                myLogger.Log("DataSent error: " + ex.Message);
            }
        }

        #endregion "Socket Methods"

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
                        myLogger.Log("Starting new CLIENT connection with ID: {0}", sm.id);
                        Id = sm.id;

                        //Send(SocketManager.ConfirmConnId(Id)); //???
                        break;

                    case SocketCommands.CloseInstance:
                        myLogger.Log("Client is closing connection...");
                        Stop();
                        break;

                    default:
                        myLogger.Log("Unknown ClientCallbackion to take! Case: {0}", cmd);
                        break;
                }
            }
            else
            {
                myLogger.Log("Empty string received by client!");
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

                    myLogger.Log("Receiving {0}/{1} packets...", dataBuffer.Value[reqId].PacketCount, buf.blockNum);

                    if (dataBuffer.Value[reqId].PacketCount == buf.blockNum)
                    {
                        SocketManager.Deserialize(dataBuffer.Value[reqId].buffer.Value.ToArray(), 0, out o, SocketDbgType.Client);
                        dataBuffer.Value.Remove(reqId);
                    }
                }
                catch (Exception ex)
                {
                    myLogger.Log("Buffer couldn't add bytes due to an exception! Ex: " + ex);
                }
            }
            else
            {
                myLogger.Log("Empty buffer received by client!");
            }
            o = null;
        }

        #region "Error & Close & Stop & Dispose"

        private void CloseConnection(SocketShutdown soShutdown)
        {
            if (soShutdown == SocketShutdown.Receive)
            {
                myLogger.Log("Remember that you're in a Client, you can't only close Both connections or only your connection.");
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
                    myLogger.Log("Closing client (#{0})", Id);

                    //Send(SocketManager.ClientClosed(Id)); //???
                    _state = SocketState.ClientStopped;
                    CloseConnection(SocketShutdown.Both); //No hace falta comprobar si estamos connected
                    Dispose();
                }
                catch (Exception ex)
                {
                    myLogger.Log("Exception ocurred while trying to stop client: " + ex);
                }
            }
            else
                myLogger.Log("Client cannot be stopped because it hasn't been started!");
        }

        #endregion "Error & Close & Stop & Dispose"

        #endregion "Class Methods"
    }
}