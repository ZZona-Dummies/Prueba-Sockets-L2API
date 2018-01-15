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

        private static bool debug;
        #endregion

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
        #endregion

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
                ComeAlive();
        }
        #endregion

        #region "Socket Methods"
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
                        ServerSocket.Listen(100); //El servidor se prepara para recebir la conexion de 100 clientes simultaneamente

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

                bool serialized = SocketManager.Deserialize(byteData, byteData.Length, out SocketMessage sm, SocketDbgType.Server);

                if (bytesRead > 0 && bytesRead <= StateObject.BufferSize)
                {
                    //string str = Encoding.Unicode.GetString(byteData, 0, bytesRead); //Obtiene la longitud en bytes de los datos pasados y los transforma en una string
                    Console.WriteLine("Server readed block of {0} bytes", bytesRead);

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
                    Console.WriteLine("Cannot deserialize something that is bigger from the buffer it can contain!");
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
        #endregion

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
                        Console.WriteLine("Adding #{0} client to routing table!", genID); //Esto ni parece funcionar bien

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
            if(dests.Count() == 1)
            {
                if (dests.First() == 0)
                { //Send to all users
                    foreach (KeyValuePair<ulong, Socket> soc in routingTable)
                        if (soc.Key != sm.id)
                            soc.Value.Send(byteData);
                }
            }
            else if(dests.Count() > 1)
            { //Select dictionary keys that contains dests
                foreach(KeyValuePair<ulong, Socket> soc in routingTable.Where(x => dests.Contains(x.Key)))
                    if (soc.Key != sm.id)
                        soc.Value.Send(byteData);
            }
            else
            {
                //Error
            }
        }
        #endregion

        #region "Error & Close & Stop & Dispose" 
        private void DoServerError(string msg, ulong id = 0)
        {
            PoliteStop(id);
            Console.WriteLine("{0} CLOSING SERVER due to: " + msg,
                id == 0 ? "" : string.Format("(FirstClient: #{0})", id));
        }

        private void CloseAllClients(ulong id = 0)
        {
            if (id > 0) routingTable[id].Send(SocketManager.PoliteClose(id)); //First, close the client that has send make the request...
            Console.WriteLine("Closing all {0} clients connected!", routingTable.Count);
            foreach (KeyValuePair<ulong, Socket> soc in routingTable)
            {
                if (soc.Key != id) //Then, close the others one
                {
                    Console.WriteLine("Sending to CLIENT #{0} order to CLOSE", soc.Key);
                    soc.Value.Send(SocketManager.PoliteClose(soc.Key)); //Encoding.Unicode.GetBytes("<close>")
                }
            }
        }

        private void CloseServerAfterClientsClose()
        {
            if (routingTable.Count == routingTable.Count)
                Stop(); //Close the server, when all the clients has been closed.
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
        #endregion

        #endregion
    }
}