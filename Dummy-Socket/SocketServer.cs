using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace DeltaSockets
{
    /// <summary>
    /// Class SocketServer.
    /// </summary>
    public class SocketServer
    {
        #region "Fields"

        public SocketServerConsole myLogger = new SocketServerConsole(null);

        /// <summary>
        /// The lerped port
        /// </summary>
        public const int DefPort = 7776;

        /// <summary>
        /// The server socket
        /// </summary>
        public Socket ServerSocket;

        /// <summary>
        /// The permision
        /// </summary>
        public SocketPermission Permision;

        /// <summary>
        /// The ip
        /// </summary>
        public IPAddress IP;

        /// <summary>
        /// The port
        /// </summary>
        public int Port;

        [Obsolete("Use IPEnd instead.")]
        private IPEndPoint _endpoint;

        private byte[] byteData = new byte[StateObject.BufferSize];

        /// <summary>
        /// All done
        /// </summary>
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        /// <summary>
        /// The routing table
        /// </summary>
        public static Dictionary<ulong, Socket> routingTable = new Dictionary<ulong, Socket>(); //With this we can assume that ulong.MaxValue clients can connect to the Socket (2^64 - 1)

        private static bool dispose, debug;

        #endregion "Fields"

        #region "Propierties"

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

        #endregion "Propierties"

        #region "Socket Constructors"

        /// <summary>
        /// Initializes a new instance of the <see cref="SocketServer"/> class.
        /// </summary>
        /// <param name="debug">if set to <c>true</c> [debug].</param>
        /// <param name="doConnection">if set to <c>true</c> [do connection].</param>
        public SocketServer(bool debug, bool doConnection = false) :
            this(new SocketPermission(NetworkAccess.Accept, TransportType.Tcp, "", SocketPermission.AllPorts), Dns.GetHostEntry("").AddressList[0], DefPort, SocketType.Stream, ProtocolType.Tcp, debug, doConnection)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SocketServer"/> class.
        /// </summary>
        /// <param name="ip">The ip.</param>
        /// <param name="port">The port.</param>
        /// <param name="debug">if set to <c>true</c> [debug].</param>
        /// <param name="doConnection">if set to <c>true</c> [do connection].</param>
        public SocketServer(string ip, int port, bool debug, bool doConnection = false) :
            this(new SocketPermission(NetworkAccess.Accept, TransportType.Tcp, "", SocketPermission.AllPorts), IPAddress.Parse(ip), port, SocketType.Stream, ProtocolType.Tcp, debug, doConnection)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SocketServer"/> class.
        /// </summary>
        /// <param name="permission">The permission.</param>
        /// <param name="ipAddr">The ip addr.</param>
        /// <param name="port">The port.</param>
        /// <param name="sType">Type of the s.</param>
        /// <param name="pType">Type of the p.</param>
        /// <param name="curDebug">if set to <c>true</c> [current debug].</param>
        /// <param name="doConnection">if set to <c>true</c> [do connection].</param>
        public SocketServer(SocketPermission permission, IPAddress ipAddr, int port, SocketType sType, ProtocolType pType, bool curDebug, bool doConnection = false)
        {
            permission.Demand();

            IP = ipAddr;
            Port = port;

            debug = curDebug;

            ServerSocket = new Socket(ipAddr.AddressFamily, sType, pType);

            if (doConnection)
                StartServer(); // ??? --> ComeAlive
        }

        #endregion "Socket Constructors"

        #region "Socket Methods"

        /// <summary>
        /// Comes the alive.
        /// </summary>
        /*public void ComeAlive()
        {
            if (IPEnd != null)
            {
                try
                {
                    try
                    {
                        ServerSocket.Bind(IPEnd);
                        ServerSocket.Listen(100); //El servidor se prepara para recebir la conexion de 100 clientes simultaneamente

                        myLogger.Log("Waiting for a connection...");
                        ServerSocket.BeginAccept(new AsyncCallback(OnAccept), ServerSocket);
                    }
                    catch (Exception ex)
                    {
                        myLogger.Log("Exception ocurred while starting CLIENT: " + ex);
                        return;
                    }
                    _state = SocketState.ServerStarted;
                }
                catch (Exception ex)
                {
                    myLogger.Log(ex.Message);
                }
            }
            else myLogger.Log("Destination IP isn't defined!");
        }

        private void OnAccept(IAsyncResult ar)
        {
            try
            {
                Socket clientSocket = ServerSocket.EndAccept(ar);

                //Start listening for more clients
                ServerSocket.BeginAccept(new AsyncCallback(OnAccept), null);

                //Once the client connects then start receiving the commands from it
                clientSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None,
                    new AsyncCallback(OnReceive), clientSocket);
            }
            catch (Exception ex)
            {
                myLogger.Log("Exception ocurred while accepting in async server: " + ex.Message);
            }
        }

        private void OnReceive(IAsyncResult ar)
        {
            try
            {
                Socket handler = (Socket)ar.AsyncState;
                int bytesRead = handler.EndReceive(ar);

                bool serialized = SocketManager.Deserialize(byteData, byteData.Length, out SocketMessage sm, SocketDbgType.Server);

                if (bytesRead > 0 && bytesRead <= StateObject.BufferSize)
                {
                    //string str = Encoding.Unicode.GetString(byteData, 0, bytesRead); //Obtiene la longitud en bytes de los datos pasados y los transforma en una string
                    myLogger.Log("Server readed block of {0} bytes", bytesRead);

                    if (sm != null)
                    {
                        if (sm.Type == typeof(SocketCommand)) //Si el mensaje es del tipo steing entonces será un comando
                            HandleAction(sm, handler);
                        else if (sm.Type == typeof(SocketBuffer))
                        {
                            //La excepcion 'System.Collections.Generic.KeyNotFoundException' ocurre basicamente porque la parte que asigna las IDs que vana ser utilizadas en el futuro no se llama
                            //Ya tengo que ver si es porque se ejecuta después o porque no llega a ejecutarse por algun problema

                            SocketBuffer buf = sm.TryGetObject<SocketBuffer>();
                            SendToClient(sm, bytesRead, buf.destsId);

                            //Esta parte ya no hace falta, con reenviar los datos al cliente, este tiene que saber cuando deserializar
                            //Borrada...
                        }
                        else
                            DoServerError("Not supported type!", sm.id);
                    }
                    else
                    {
                        if (serialized)
                            if (bytesRead > StateObject.BufferSize)
                                myLogger.Log("Overpassing {0} bytes to {1} bytes.", StateObject.BufferSize, bytesRead);
                            else if (bytesRead == 0)
                                myLogger.Log("Null object passed though the socket! Ignore...");
                            else
                                DoServerError("Cannot deserialize!");
                    }
                }
                else if (bytesRead > StateObject.BufferSize)
                {
                    myLogger.Log("Cannot deserialize something that is bigger from the buffer it can contain!");
                }

                //Continua escuchando, para listar el próximo mensaje, recursividad asíncrona.
                handler.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None,
                    new AsyncCallback(OnReceive), handler);
            }
            catch (Exception ex)
            {
                myLogger.Log(ex.Message);
            }
        }

        /// <summary>
        /// Called when [send].
        /// </summary>
        /// <param name="ar">The ar.</param>
        public void OnSend(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;
                client.EndSend(ar);
            }
            catch (Exception ex)
            {
                myLogger.Log(ex.Message);
            }
        }*/

        private bool cStopRequested;

        private System.Collections.ArrayList cClients = new System.Collections.ArrayList();

        public event MessageReceivedEventHandler MessageReceived;

        public delegate void MessageReceivedEventHandler(string argMessage, Socket argClientSocket);

        public event ClientConnectedEventHandler ClientConnected;

        public delegate void ClientConnectedEventHandler(Socket argClientSocket);

        public event ClientDisconnectedEventHandler ClientDisconnected;

        public delegate void ClientDisconnectedEventHandler(Socket argClientSocket);

        public void InitializeServer()
        {
        }

        /// <summary>
        /// StartServer starts the server by listening for new client connections with a TcpListener.
        /// </summary>
        /// <remarks></remarks>
        public void StartServer()
        {
            // create the TcpListener which will listen for and accept new client connections asynchronously
            /*ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // convert the server address and port into an ipendpoint
            IPAddress[] mHostAddresses = Dns.GetHostAddresses(ServerAddress);
            IPEndPoint mEndPoint = null;
            foreach (IPAddress mHostAddress in mHostAddresses)
            {
                if (mHostAddress.AddressFamily == AddressFamily.InterNetwork)
                {
                    mEndPoint = new IPEndPoint(mHostAddress, cServerPort);
                }
            }*/

            // bind to the server's ipendpoint
            ServerSocket.Bind(IPEnd);

            // configure the listener to allow 1 incoming connection at a time
            ServerSocket.Listen(1000);

            // accept client connection async
            ServerSocket.BeginAccept(new AsyncCallback(ClientAccepted), ServerSocket);
        }

        public void StopServer()
        {
            //cServerSocket.Disconnect(True)
            ServerSocket.Close();
            //cStopRequested = True
        }

        /// <summary>
        /// ClientConnected is a callback that gets called when the server accepts a client connection from the async BeginAccept method.
        /// </summary>
        /// <param name="ar"></param>
        /// <remarks></remarks>
        public void ClientAccepted(IAsyncResult ar)
        {
            // get the async state object from the async BeginAccept method, which contains the server's listening socket
            Socket mServerSocket = (Socket)ar.AsyncState;
            // call EndAccept which will connect the client and give us the the client socket
            Socket mClientSocket = null;
            try
            {
                mClientSocket = mServerSocket.EndAccept(ar);
            }
            catch (ObjectDisposedException ex)
            {
                // if we get an ObjectDisposedException it that means the server socket terminated while this async method was still active
                return;
            }
            // instruct the client to begin receiving data
            SocketGlobals.AsyncReceiveState mState = new SocketGlobals.AsyncReceiveState();
            mState.Socket = mClientSocket;
            ClientConnected?.Invoke(mState.Socket);
            mState.Socket.BeginReceive(mState.Buffer, 0, SocketGlobals.gBufferSize, SocketFlags.None, new AsyncCallback(ClientMessageReceived), mState);
            // begin accepting another client connection
            mServerSocket.BeginAccept(new AsyncCallback(ClientAccepted), mServerSocket);
        }

        /// <summary>
        /// BeginReceiveCallback is an async callback method that gets called when the server receives some data from a client socket after calling the async BeginReceive method.
        /// </summary>
        /// <param name="ar"></param>
        /// <remarks></remarks>
        public void ClientMessageReceived(IAsyncResult ar)
        {
            // get the async state object from the async BeginReceive method
            SocketGlobals.AsyncReceiveState mState = (SocketGlobals.AsyncReceiveState)ar.AsyncState;
            // call EndReceive which will give us the number of bytes received
            int numBytesReceived = 0;
            try
            {
                numBytesReceived = mState.Socket.EndReceive(ar);
            }
            catch (SocketException ex)
            {
                // if we get a ConnectionReset exception, it could indicate that the client has disconnected
                if (ex.SocketErrorCode == SocketError.ConnectionReset)
                {
                    ClientDisconnected?.Invoke(mState.Socket);
                    return;
                }
            }
            // if we get numBytesReceived equal to zero, it could indicate that the client has disconnected
            if (numBytesReceived == 0)
            {
                ClientDisconnected?.Invoke(mState.Socket);
                return;
            }
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
                mState.Socket.BeginReceive(mState.Buffer, 0, SocketGlobals.gBufferSize, SocketFlags.None, new AsyncCallback(ClientMessageReceived), mState);
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
                // handle the message
                ParseReceivedClientMessage(mState.Packet, mState.Socket);
                // call BeginReceive again, so we can start receiving another packet from this client socket
                SocketGlobals.AsyncReceiveState mNextState = new SocketGlobals.AsyncReceiveState();
                mNextState.Socket = mState.Socket;
                mNextState.Socket.BeginReceive(mNextState.Buffer, 0, SocketGlobals.gBufferSize, SocketFlags.None, new AsyncCallback(ClientMessageReceived), mNextState);
            }
        }

        public void ParseReceivedClientMessage(string argCommandString, Socket argClient)
        {
            myLogger.Log("ParseReceivedClientMessage: " + argCommandString);

            // parse the command string
            string argCommand = null;
            string argText = null;

            if (argCommandString.StartsWith("/"))
            {
                argCommand = argCommandString.Substring(0, argCommandString.IndexOf(" "));
                argText = argCommandString.Remove(0, argCommand.Length + 1);
            }
            else
                argText = argCommandString;

            switch (argText)
            {
                case "hi server":
                    SendMessageToClient("/say Server replied.", argClient);
                    break;
            }

            MessageReceived?.Invoke(argCommandString, argClient);

            //' respond back to the client on certain messages
            //Select Case argMessageString
            //    Case "hi"
            //        SendMessageToClient("\say", "hi received", argClient)
            //End Select
            //RaiseEvent MessageReceived(argCommandString & " | " & argMessageString)
        }

        /// <summary>
        /// QueueMessage prepares a Message object containing our data to send and queues this Message object in the OutboundMessageQueue.
        /// </summary>
        /// <remarks></remarks>
        public void SendMessageToClient(string argCommandString, Socket argClient)
        {
            // parse the command string
            string argCommand = null;
            string argText = null;
            argCommand = argCommandString.Substring(0, argCommandString.IndexOf(" "));
            argText = argCommandString.Remove(0, argCommand.Length);

            //' create a Packet object from the passed data
            //Dim mPacket As New Dictionary(Of String, String)
            //mPacket.Add("CMD", argCommandMessage)
            //mPacket.Add("MSG", argCommandData)

            string mPacket = argCommandString;

            // serialize the Packet into a stream of bytes which is suitable for sending with the Socket
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter mSerializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            System.IO.MemoryStream mSerializerStream = new System.IO.MemoryStream();
            mSerializer.Serialize(mSerializerStream, mPacket);

            // get the serialized Packet bytes
            byte[] mPacketBytes = mSerializerStream.GetBuffer();

            // convert the size into a byte array
            byte[] mSizeBytes = BitConverter.GetBytes(mPacketBytes.Length + 4);

            // create the async state object which we can pass between async methods
            SocketGlobals.AsyncSendState mState = new SocketGlobals.AsyncSendState(argClient);

            // resize the BytesToSend array to fit both the mSizeBytes and the mPacketBytes
            // TODO: ReDim mState.BytesToSend(mPacketBytes.Length + mSizeBytes.Length - 1)
            Array.Resize(ref mState.BytesToSend, mPacketBytes.Length + mSizeBytes.Length - 1);

            // copy the mSizeBytes and mPacketBytes to the BytesToSend array
            Buffer.BlockCopy(mSizeBytes, 0, mState.BytesToSend, 0, mSizeBytes.Length);
            Buffer.BlockCopy(mPacketBytes, 0, mState.BytesToSend, mSizeBytes.Length, mPacketBytes.Length);

            // queue the Message
            argClient.BeginSend(mState.BytesToSend, mState.NextOffset(), mState.NextLength(), SocketFlags.None, new AsyncCallback(MessagePartSent), mState);
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
            }
            catch (Exception ex)
            {
                myLogger.Log("DataSent error: " + ex.Message);
            }
        }

        #endregion "Socket Methods"

        #region "Class Methods"

        private void HandleAction(SocketMessage sm, Socket handler)
        {
            //string val = sm.StringValue;
            SocketCommand cmd = sm.TryGetObject<SocketCommand>();
            if (cmd != null)
            {
                switch (cmd.Command)
                {
                    case SocketCommands.Conn:
                        //Para que algo se añade aqui debe ocurrir algo...
                        //Give an id for a client before we add it to the routing table
                        //and create a request id for the next action that needs it

                        //First, we have to assure that there are free id on the current KeyValuePair to optimize the process...
                        ulong genID = 1;

                        //Give id in a range...
                        bool b = routingTable.Keys.FindFirstMissingNumberFromSequence(out genID, new MinMax<ulong>(1 + (ulong)routingTable.Count * ushort.MaxValue, 1 + (ulong)routingTable.Count * ushort.MaxValue + ushort.MaxValue));
                        myLogger.Log("Adding #{0} client to routing table!", genID); //Esto ni parece funcionar bien

                        handler.Send(SocketManager.SendConnId(genID));
                        break;

                    case SocketCommands.ConfirmConnId:
                        routingTable.Add(sm.id, handler);
                        break;

                    case SocketCommands.CloseClients:
                        CloseAllClients(sm.id);
                        break;

                    case SocketCommands.ClosedClient:
                        //closedClients.Add(sm.id);
                        routingTable.Remove(sm.id);
                        CloseServerAfterClientsClose(dispose);
                        break;

                    case SocketCommands.Stop:
                        CloseAllClients(sm.id);
                        break;

                    case SocketCommands.UnpoliteStop:
                        object d = cmd.Metadata["Dispose"];
                        Stop(d != null && ((bool)d));
                        break;

                    default:
                        DoServerError(string.Format("Cannot de-encrypt the message! Unrecognized 'enum' case: {0}", cmd.Command), sm.id);
                        break;
                }
            }
        }

        #region "Send Methods"

        private void SendToAllClients(SocketMessage sm, int bytesRead)
        {
            myLogger.Log("---------------------------");
            myLogger.Log("Client with ID {0} sent {1} bytes (JSON).", sm.id, bytesRead);
            myLogger.Log("Sending to the other clients.");
            myLogger.Log("---------------------------");
            myLogger.Log("");

            //Send to the other clients
            foreach (KeyValuePair<ulong, Socket> soc in routingTable)
                if (soc.Key != sm.id)
                    soc.Value.Send(byteData);
        }

        private void SendToClient(SocketMessage sm, int readBytes, params ulong[] dests)
        {
            SendToClient(sm, readBytes, dests.AsEnumerable());
        }

        private void SendToClient(SocketMessage sm, int readBytes, IEnumerable<ulong> dests)
        {
            if (dests.Count() == 1)
            {
                if (dests.First() == 0)
                { //Send to all users
                    foreach (KeyValuePair<ulong, Socket> soc in routingTable)
                        if (soc.Key != sm.id)
                            soc.Value.Send(byteData);
                }
            }
            else if (dests.Count() > 1)
            { //Select dictionary keys that contains dests
                foreach (KeyValuePair<ulong, Socket> soc in routingTable.Where(x => dests.Contains(x.Key)))
                    if (soc.Key != sm.id)
                        soc.Value.Send(byteData);
            }
            else
            {
                //Error
            }
        }

        #endregion "Send Methods"

        #region "Error & Close & Stop & Dispose"

        private void DoServerError(string msg, ulong id = 0, bool dis = false)
        {
            PoliteStop(dis, id);
            myLogger.Log("{0} CLOSING SERVER due to: " + msg,
                id == 0 ? "" : string.Format("(FirstClient: #{0})", id));
        }

        private void CloseAllClients(ulong id = 0)
        {
            if (id > 0) routingTable[id].Send(SocketManager.PoliteClose(id)); //First, close the client that has send make the request...
            myLogger.Log("Closing all {0} clients connected!", routingTable.Count);
            foreach (KeyValuePair<ulong, Socket> soc in routingTable)
            {
                if (soc.Key != id) //Then, close the others one
                {
                    myLogger.Log("Sending to CLIENT #{0} order to CLOSE", soc.Key);
                    soc.Value.Send(SocketManager.PoliteClose(soc.Key)); //Encoding.Unicode.GetBytes("<close>")
                }
            }
        }

        private void CloseServerAfterClientsClose(bool dis)
        {
            if (routingTable.Count == routingTable.Count)
                Stop(dis); //Close the server, when all the clients has been closed.
        }

        public void PoliteStop(bool dis = false, ulong id = 0)
        {
            dispose = dis;
            CloseAllClients(id); //And then, the server will autoclose itself...
        }

        /// <summary>
        /// Closes the server.
        /// </summary>
        private void Stop(bool dis = true)
        {
            if (_state == SocketState.ServerStarted)
            {
                try
                {
                    myLogger.Log("Closing server");

                    _state = SocketState.ServerStopped;
                    if (ServerSocket.Connected) //Aqui lo que tengo que hacer es que se desconecten los clientes...
                        ServerSocket.Shutdown(SocketShutdown.Both);
                    ServerSocket.Close();
                    if (dis) ServerSocket = null; //Dispose
                }
                catch (Exception ex)
                {
                    myLogger.Log("Exception ocurred while trying to stop server: " + ex);
                }
            }
            else
                myLogger.Log("Server cannot be stopped because it hasn't been started!");
        }

        public void Dispose()
        {
            myLogger.Log("Disposing server");
            PoliteStop(true);
        }

        #endregion "Error & Close & Stop & Dispose"

        #endregion "Class Methods"
    }
}