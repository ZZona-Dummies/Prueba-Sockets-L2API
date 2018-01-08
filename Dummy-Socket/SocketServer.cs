using System;
using System.Collections.Generic;
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

        private SocketState _state;
        public SocketState myState
        {
            get
            {
                return _state;
            }
        }

        private IPEndPoint _endpoint;
        private byte[] byteData = new byte[SocketManager.minBufferSize];

        /// <summary>
        /// All done
        /// </summary>
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        /// <summary>
        /// The routing table
        /// </summary>
        public static Dictionary<ulong, Socket> routingTable = new Dictionary<ulong, Socket>(); //With this we can assume that ulong.MaxValue clients can connect to the Socket (2^64 - 1)

        private readonly static List<ulong> closedClients = new List<ulong>();

        private static Dictionary<ushort, List<byte>> dataBuffer = new Dictionary<ushort, List<byte>>();
        private static Dictionary<ushort, ulong> requestIDs = new Dictionary<ushort, ulong>();

        private static bool debug;

        private static ushort nextReqID;

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
                ComeAlive();
        }

        /// <summary>
        /// Comes the alive.
        /// </summary>
        public void ComeAlive()
        {
            if (IPEnd != null)
            {
                try
                {
                    try
                    {
                        ServerSocket.Bind(IPEnd);
                        ServerSocket.Listen(10); //El servidor se prepara para recebir la conexion de 10 clientes simultaneamente

                        Console.WriteLine("Waiting for a connection...");
                        ServerSocket.BeginAccept(new AsyncCallback(OnAccept), ServerSocket);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception ocurred while starting CLIENT: " + ex);
                        return;
                    }
                    _state = SocketState.ServerStarted;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else Console.WriteLine("Destination IP isn't defined!");
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
                Console.WriteLine("Exception ocurred while accepting in async server: " + ex.Message);
            }
        }

        private void OnReceive(IAsyncResult ar)
        {
            try
            {
                Socket handler = (Socket)ar.AsyncState;
                int bytesRead = handler.EndReceive(ar);

                bool serialized = SocketManager.Deserialize(byteData, byteData.Length, out SocketMessage sm, SocketDbgType.Server); //Aquí hay un problema gordo si ni se puede deserializar un "<conn>"

                if (bytesRead > 0 && bytesRead < SocketManager.minBufferSize)
                {
                    //string str = Encoding.Unicode.GetString(byteData, 0, bytesRead); //Obtiene la longitud en bytes de los datos pasados y los transforma en una string
                    Console.WriteLine("Server readed block of {0} bytes", bytesRead);

                    if (sm != null)
                    {
                        if (sm.Type == typeof(SocketCommand)) //Si el mensaje es del tipo steing entonces será un comando
                            HandleAction(sm, handler);
                        else if (sm.Type == typeof(SocketBuffer))
                        {
                            SocketBuffer buf = sm.TryGetObject<SocketBuffer>();

                            //Create a new request id while receiving packets...
                            CreateNextReqId(handler, sm.id);

                            ushort reqId = buf.requestID;

                            //Aqui ni se deberia almacenar, deberia almacenarse en los otros clientes segun va pasando por el servidor, para luego hacer lo que quiero hacer mas abajo...
                            dataBuffer[reqId].AddRange(buf.splittedData);
                            --requestIDs[reqId];

                            if(requestIDs[reqId] == 0)
                            {
                                //Remove from dictionary and deserialize
                                requestIDs.Remove(reqId);

                                //aqui viene lo interesante porque si le mandamos todo de golpe a los otros clientes...
                            }
                        }
                        //Aqui lo que hay que hacer es ir sumando a un buffer general los distintos fragmentos secuenciales que vayan llegando
                        //elif (tipo == SocketBuffer) //Entonces quiere decir que es para todos los otros clientes ... Aquí lo que haré será una clase en la cual serialice info en paquetes
                        //Para ello comprobaré el tamaño del paquete que se va a enviar con todos los metadatos qu ya contiene para ver cuantos datos se puede meter más... (El buffer lo haré de 4096 bytes)
                        else
                            DoServerError("Not supported type!", sm.id);
                    }
                    else
                    {
                        if (serialized)
                            myLogger.Log("Null object passed though the socket! Ignore...");
                        else
                            DoServerError("Cannot deserialize!");
                    }
                }
                else if (bytesRead > SocketManager.minBufferSize)
                {
                    Console.WriteLine("Cannot deserialize something that is bigger from the buffer it can contain!");
                    //Check if deserialize is true && sm != null

                    /*Console.WriteLine("Server is reading big block of {0} bytes!", bytesRead);
                    SendToOtherClients(sm, bytesRead);
                    Array.Resize(ref byteData, minBufferSize); //byteData =  new byte[minBufferSize]; //Reset the buffer*/
                }

                //Continua escuchando, para listar el próximo mensaje, recursividad asíncrona.
                handler.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None,
                    new AsyncCallback(OnReceive), handler);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void HandleAction(SocketMessage sm, Socket handler)
        {
            //string val = sm.StringValue;
            SocketCommand cmd = sm.TryGetObject<SocketCommand>();
            if (cmd != null)
            {
                switch (cmd.Command)
                {
                    case SocketCommands.Conn: //Para que algo se añade aqui debe ocurrir algo...
                        Console.WriteLine("Adding #{0} client to routing table!", sm.id);
                        //Give an id for a client before we add it to the routing table
                        //and create a request id for the next action that needs it

                        nextReqID = requestIDs.Keys.ObtainFreeID();
                        GiveId(handler);
                        break;

                    case SocketCommands.CreateRequestId:
                        //We add a request with an id for a number of numbers
                        //BufferLen = bufferSplitted.Length
                        AddRequest(nextReqID, (ulong)((float)cmd.Metadata["BufferLen"] / (int)cmd.Metadata["BlockLen"])); //Aqui lo que tengo que hacer es crear una clase llamada SocketCommand y dentro meterle el StringValue y metadatos
                        break;

                    case SocketCommands.ConfirmConnId:
                        routingTable.Add(sm.id, handler);
                        break;

                    case SocketCommands.CloseClients:
                        CloseAllClients(sm.id);
                        break;

                    case SocketCommands.ClosedClient:
                        closedClients.Add(sm.id);
                        CloseServerAfterClientsClose();
                        break;

                    case SocketCommands.Stop:
                        CloseAllClients(sm.id);
                        break;

                    case SocketCommands.UnpoliteStop:
                        Stop();
                        break;

                    default:
                        DoServerError(string.Format("Cannot de-encrypt the message! Unrecognized 'enum' case: {0}", cmd.Command), sm.id);
                        break;
                }
                /*}
                else
                {
                    string blockSizeId = "Block_Size:";
                    if (sm.StringValue.StartsWith(blockSizeId))
                    {
                        int bytesToRead = int.Parse(sm.StringValue.Substring(blockSizeId.Length));

                        Console.WriteLine("Preparing array to its next value length of {0} bytes!", bytesToRead);

                        //byteData = new byte[bytesToRead]; //The next message will be of this size...
                        Array.Resize(ref byteData, bytesToRead);
                    }
                    else
                    {
                        DoServerError(string.Format("Cannot de-encrypt the message! Unrecognized 'string' case: {0}", sm.StringValue), sm.id);
                    }
                }*/
            }
        }

        private void GiveId(Socket handler)
        {
            //First, we have to assure that there are free id on the current KeyValuePair to optimize the process...
            ulong genID = routingTable.Keys.ObtainFreeID();

            handler.Send(SocketManager.SendConnId(genID));
            CreateNextReqId(handler, genID);
        }

        private void CreateNextReqId(Socket handler, ulong id)
        {
            handler.Send(SocketManager.SendCommand(SocketCommands.CreateNextReqId, new SocketCommandData(new Dictionary<string, object>() { { "reqId", nextReqID } }), id));
        }

        private static void AddRequest(ushort id, ulong blockNum)
        { //We add the number of blocks that we will add with the same id
            requestIDs.Add(id, blockNum);
        }

        private void CloseAllClients(ulong id = 0)
        {
            if (id > 0) routingTable[id].Send(SocketManager.PoliteClose(id)); //First, close the client that has send make the request...
            //Console.WriteLine("Routing: "+routingTable.Count);
            foreach (KeyValuePair<ulong, Socket> soc in routingTable)
            { //Aqui hay otro problema, el routing table no alcanza a coger ningun valor
                if (soc.Key != id) //Then, close the others one
                {
                    Console.WriteLine("Sending to CLIENT #{0} order to CLOSE", soc.Key);
                    soc.Value.Send(SocketManager.PoliteClose(soc.Key)); //Encoding.Unicode.GetBytes("<close>")
                }
            }
        }

        private void CloseServerAfterClientsClose()
        {
            if (closedClients.Count == routingTable.Count)
                Stop(); //Close the server, when all the clients has been closed.
        }

        private void DoServerError(string msg, ulong id = 0)
        {
            PoliteStop(id);
            Console.WriteLine("{0} CLOSING SERVER due to: " + msg,
                id == 0 ? "" : string.Format("(FirstClient: #{0})", id));
        }

        private void SendToOtherClients(SocketMessage sm, int bytesRead)
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
                Console.WriteLine(ex.Message);
            }
        }

        public void PoliteStop(ulong id = 0)
        {
            CloseAllClients(id); //And then, the server will autoclose itself...
        }

        /// <summary>
        /// Closes the server.
        /// </summary>
        private void Stop()
        {
            if (_state == SocketState.ServerStarted)
            {
                try
                {
                    Console.WriteLine("Closing server");

                    _state = SocketState.ServerStopped;
                    if (ServerSocket.Connected) //Aqui lo que tengo que hacer es que se desconecten los clientes...
                        ServerSocket.Shutdown(SocketShutdown.Both);
                    ServerSocket.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception ocurred while trying to stop server: " + ex);
                }
            }
            else
                Console.WriteLine("Server cannot be stopped because it hasn't been started!");
        }

        public void Dispose()
        {
            Console.WriteLine("Disposing server");
            Stop();
        }
    }
}