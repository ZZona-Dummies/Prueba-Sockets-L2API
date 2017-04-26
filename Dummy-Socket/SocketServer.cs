using Lerp2API;
using Lerp2API.SafeECalls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Dummy_Socket
{
    //The commands for interaction between the server and the client
    public enum Command
    {
        Login,      //Log into the server
        Logout,     //Logout of the server
        Message,    //Send a text message to all the chat clients
        List,       //Get a list of users in the chat room from the server
        Null        //No command
    }
    public class SocketServer
    {
        //The ClientInfo structure holds the required information about every
        //client connected to the server
        struct ClientInfo
        {
            public Socket socket;   //Socket of the client
            public string strName;  //Name by which the user logged into the chat room
        }

        //The collection of all clients logged into the room (an array of type ClientInfo)
        ArrayList clientList;

        public const int lerpedPort = 22222;

        public Socket ServerSocket;
        public SocketPermission Permision;
        public IPAddress IP;
        public int Port;
        private IPEndPoint _endpoint;
        private byte[] byteData = new byte[1024];
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        public static Dictionary<int, Socket> routingTable = new Dictionary<int, Socket>();

        public int socketID;
        private frmSocket ins
        {
            get
            {
                return frmMain.socketIns[socketID].instance;
            }
        }

        private static bool debug;

        private AsyncCallback _callback;
        /*public AsyncCallback ServerCallback
        {
            set
            {
                _callback = value;
                AssignCallback(_callback);
            }
        }*/

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

        public SocketServer(bool debug, bool doConnection = false) :
            this(new SocketPermission(NetworkAccess.Accept, TransportType.Tcp, "", SocketPermission.AllPorts), Dns.GetHostEntry("").AddressList[0], lerpedPort, SocketType.Stream, ProtocolType.Tcp, debug, doConnection)
        { }

        public SocketServer(string ip, int port, bool debug, bool doConnection = false) :
            this(new SocketPermission(NetworkAccess.Accept, TransportType.Tcp, "", SocketPermission.AllPorts), IPAddress.Parse(ip), port, SocketType.Stream, ProtocolType.Tcp, debug, doConnection)
        { }

        public SocketServer(SocketPermission permission, IPAddress ipAddr, int port, SocketType sType, ProtocolType pType, bool curDebug, bool doConnection = false)
        {
            permission.Demand();

            IP = ipAddr;
            Port = port;

            debug = curDebug;

            ServerSocket = new Socket(ipAddr.AddressFamily, sType, pType);

            if (doConnection) ServerSocket.Bind(IPEnd);
        }

        public void ComeAlive()
        {
            if (IPEnd != null)
            {
                try
                {
                    ServerSocket.Bind(IPEnd);
                    ServerSocket.Listen(10); //El servidor se prepara para recebir la conexion de 10 clientes simultaneamente

                    //while (true)
                    //{
                        // Set the event to nonsignaled state.
                        //allDone.Reset();

                        // Start an asynchronous socket to listen for connections.
                        Console.WriteLine("Waiting for a connection...");
                        ServerSocket.BeginAccept(new AsyncCallback(OnAccept), ServerSocket);

                        // Wait until a connection is made before continuing.
                        //allDone.WaitOne();
                    //}
                }
                catch(Exception ex)
                {
                    ins.WriteServerLog(ex.Message);
                }
            }
#if STATIC_LOG
            else frmSocket.WriteServerLog("Destination IP isn't defined!");
#else
            else ins.WriteServerLog("Destination IP isn't defined!");
#endif
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
                ins.WriteServerLog(ex.Message);
            }
        }

        private void OnReceive(IAsyncResult ar)
        {
            try
            {
                Socket handler = (Socket)ar.AsyncState;
                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                { //handler.Receive(byteData)
                    string str = Encoding.Unicode.GetString(byteData, 0, bytesRead); //Obtiene la longitud en bytes de los datos pasados y los transforma en una string
                    SocketMessage sm = null;
                    if (str.IsJson())
                        sm = JsonUtility.FromJson<SocketMessage>(str);

                    if (sm != null)
                    {
                        if (sm.msg == "<conn>")
                            routingTable.Add(sm.id, handler);
                        else
                        {
                            ins.WriteServerLog("---------------------------");
                            ins.WriteServerLog("Client with ID {0} sent {1} bytes (JSON).", sm.id, bytesRead);
                            ins.WriteServerLog("Message: {0}", sm.msg);
                            ins.WriteServerLog("Sending to the other clients.");
                            ins.WriteServerLog("---------------------------");
                            ins.WriteServerLog("");
                            //ins.WriteServerLog(string.Format("Read {0} bytes from client.\n Data: {1}", str.Length, str));

                            //Send to the other clients
                            foreach (KeyValuePair<int, Socket> soc in routingTable)
                                if (soc.Key != sm.id)
                                    soc.Value.Send(Encoding.Unicode.GetBytes(str));
                        }
                    }
                    else
                    {
                        if (str == "<stop>") //Si recibe FINCONN sale
                            CloseServer();
                        //else if (str.IndexOf("<conn>") > -1)
                        //    routingTable.Add();
                        else
                            ins.WriteServerLog("Cannot de-encrypt the message!");
                    }

                }

                handler.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None,
                    new AsyncCallback(OnReceive), handler);

                /*//Transform the array of bytes received from the user into an
                //intelligent form of object Data
                Data msgReceived = new Data(byteData);

                //We will send this object in response the users request
                Data msgToSend = new Data();

                byte[] message;

                //If the message is to login, logout, or simple text message
                //then when send to others the type of the message remains the same
                msgToSend.cmdCommand = msgReceived.cmdCommand;
                msgToSend.strName = msgReceived.strName;

                switch (msgReceived.cmdCommand)
                {
                    case Command.Login:

                        //When a user logs in to the server then we add her to our
                        //list of clients

                        ClientInfo clientInfo = new ClientInfo();
                        clientInfo.socket = clientSocket;
                        clientInfo.strName = msgReceived.strName;

                        clientList.Add(clientInfo);

                        //Set the text of the message that we will broadcast to all users
                        msgToSend.strMessage = "<<<" + msgReceived.strName + " has joined the room>>>";
                        break;

                    case Command.Logout:

                        //When a user wants to log out of the server then we search for her 
                        //in the list of clients and close the corresponding connection

                        int nIndex = 0;
                        foreach (ClientInfo client in clientList)
                        {
                            if (client.socket == clientSocket)
                            {
                                clientList.RemoveAt(nIndex);
                                break;
                            }
                            ++nIndex;
                        }

                        clientSocket.Close();

                        msgToSend.strMessage = "<<<" + msgReceived.strName + " has left the room>>>";
                        break;

                    case Command.Message:

                        //Set the text of the message that we will broadcast to all users
                        msgToSend.strMessage = msgReceived.strName + ": " + msgReceived.strMessage;
                        break;

                    case Command.List:

                        //Send the names of all users in the chat room to the new user
                        msgToSend.cmdCommand = Command.List;
                        msgToSend.strName = null;
                        msgToSend.strMessage = null;

                        //Collect the names of the user in the chat room
                        foreach (ClientInfo client in clientList)
                        {
                            //To keep things simple we use asterisk as the marker to separate the user names
                            msgToSend.strMessage += client.strName + "*";
                        }

                        message = msgToSend.ToByte();

                        //Send the name of the users in the chat room
                        clientSocket.BeginSend(message, 0, message.Length, SocketFlags.None,
                                new AsyncCallback(OnSend), clientSocket);
                        break;
                }

                if (msgToSend.cmdCommand != Command.List)   //List messages are not broadcasted
                {
                    message = msgToSend.ToByte();

                    foreach (ClientInfo clientInfo in clientList)
                    {
                        if (clientInfo.socket != clientSocket ||
                            msgToSend.cmdCommand != Command.Login)
                        {
                            //Send the message to all users
                            clientInfo.socket.BeginSend(message, 0, message.Length, SocketFlags.None,
                                new AsyncCallback(OnSend), clientInfo.socket);
                        }
                    }

                    ins.WriteServerLog(msgToSend.strMessage + "\r\n");
                }

                //If the user is logging out then we need not listen from her
                if (msgReceived.cmdCommand != Command.Logout)
                {
                    //Start listening to the message send by the user
                    clientSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(OnReceive), clientSocket);
                }*/
            }
            catch (Exception ex)
            {
                ins.WriteServerLog(ex.Message);
            }
        }

        public void OnSend(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;
                client.EndSend(ar);
            }
            catch (Exception ex)
            {
                ins.WriteServerLog(ex.Message);
            }
        }

        /*public static void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.
            //allDone.Set();

            // Get the socket that handles the client request.
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Create the state object.
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
        }

        public static void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;

            // Retrieve the state object and the handler socket
            // from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket. 
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.
                state.sb.Append(Encoding.ASCII.GetString(
                    state.buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read 
                // more data.
                content = state.sb.ToString();
                if (content.IndexOf("<EOF>") > -1)
                {
                    // All the data has been read from the 
                    // client. Display it on the console.
                    Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                        content.Length, content);
                    // Echo the data back to the client.
                    Send(handler, content);
                }
                else
                {
                    // Not all data received. Get more.
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
                }
            }
        }

        private static void Send(Socket handler, String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), handler);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }*/

        //Este metodo se llama cada vez que una nueva conexion entra en funcionamiento
        /*private void BeginConnection(IAsyncResult ar)
        {
            try
            {
                Socket listener = (Socket)ar.AsyncState; //Este es el propio socket del servidor
                Socket handler = listener.EndAccept(ar); //Este es el socket del cliente

                string str = Encoding.Default.GetString(bytes, 0, handler.Receive(bytes)); //Obtiene la longitud en bytes de los datos pasados y los transforma en una string

#if STATIC_LOG
                frmSocket.WriteServerLog(string.Format("Read {0} bytes from client.\n Data: {1}", str.Length * 2, str));
#else
                    ins.WriteServerLog(string.Format("Read {0} bytes from client.\n Data: {1}", str.Length, str));
#endif

                    if (str.IndexOf("FINCONN") > -1) //Si recibe FINCONN sale
                        CloseServer();
            }
            catch(Exception ex)
            {
                ins.WriteServerLog(ex.Message);
            }

            return;
        }*/

        /*public void StartListening()
        {
            if (ServerSocket.IsBound) ServerSocket.Listen(10);
#if STATIC_LOG
            else frmSocket.WriteServerLog("You have to make alive your Server socket first! (Call 'ComeAlive' method)");
#else
            else ins.WriteServerLog("You have to make alive your Server socket first! (Call 'ComeAlive' method)");
#endif
        }*/

        /*private void AssignCallback(AsyncCallback aCallback)
        {
            if (aCallback == null)
            {
#if STATIC_LOG
                frmSocket.WriteServerLog("Server callback cannot be null");
#else
                ins.WriteServerLog("Server callback cannot be null");
#endif
                return;
            }
            //aCallback = new AsyncCallback(AcceptCallback);
            ServerSocket.BeginAccept(aCallback, ServerSocket);
        }*/

        public void CloseServer()
        {
            if (ServerSocket.Connected)
            {
                ServerSocket.Shutdown(SocketShutdown.Receive);
                ServerSocket.Close();
            }
#if STATIC_LOG
            else frmSocket.WriteServerLog("If you want to close something, you have to be first connected!");
#else
            else ins.WriteServerLog("If you want to close something, you have to be first connected!");
#endif
        }

        /// <summary>
        /// Asynchronously accepts an incoming connection attempt and creates
        /// a new Socket to handle remote host communication.
        /// </summary>     
        /// <param name="ar">the status of an asynchronous operation
        /// </param> 
        /*public void AcceptCallback(IAsyncResult ar)
        {
            Socket listener = null;

            // A new Socket to handle remote host communication
            Socket handler = null;
            try
            {
                // Receiving byte array
                byte[] buffer = new byte[1024];
                // Get Listening Socket object
                listener = (Socket)ar.AsyncState;
                // Create a new socket
                handler = listener.EndAccept(ar);

                // Using the Nagle algorithm
                handler.NoDelay = false;

                // Creates one object array for passing data
                object[] obj = new object[2];
                obj[0] = buffer;
                obj[1] = handler;

                // Begins to asynchronously receive data
                handler.BeginReceive(
                    buffer,        // An array of type Byt for received data
                    0,             // The zero-based position in the buffer 
                    buffer.Length, // The number of bytes to receive
                    SocketFlags.None,// Specifies send and receive behaviors
                    new AsyncCallback(ReceiveCallback),//An AsyncCallback delegate
                    obj            // Specifies infomation for receive operation
                    );

                // Begins an asynchronous operation to accept an attempt
                AsyncCallback aCallback = new AsyncCallback(AcceptCallback);
                listener.BeginAccept(aCallback, listener);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: {0}", ex.ToString());
            }
        }

        /// <summary>
        /// Asynchronously receive data from a connected Socket.
        /// </summary>
        /// <param name="ar">
        /// the status of an asynchronous operation
        /// </param> 
        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Fetch a user-defined object that contains information
                object[] obj = new object[2];
                obj = (object[])ar.AsyncState;

                // Received byte array
                byte[] buffer = (byte[])obj[0];

                // A Socket to handle remote host communication.
                Socket handler = (Socket)obj[1];

                // Received message
                string content = string.Empty;

                // The number of bytes received.
                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    content += Encoding.Unicode.GetString(buffer, 0,
                        bytesRead);
                    // If message contains "<Client Quit>", finish receiving
                    if (content.IndexOf("<Client Quit>") > -1)
                    {
                        // Convert byte array to string
                        string str =
                            content.Substring(0, content.LastIndexOf("<Client Quit>"));
                        if (debug)
#if STATIC_LOG
                            frmSocket.WriteServerLog(string.Format("Read {0} bytes from client.\n Data: {1}", str.Length * 2, str));
#else
                            ins.WriteServerLog(string.Format("Read {0} bytes from client.\n Data: {1}", str.Length * 2, str));
#endif

                        // Prepare the reply message
                        byte[] byteData =
                            Encoding.Unicode.GetBytes(str);

                        // Sends data asynchronously to a connected Socket
                        handler.BeginSend(byteData, 0, byteData.Length, 0,
                            new AsyncCallback(SendCallback), handler);
                    }
                    else
                    {
                        // Continues to asynchronously receive data
                        byte[] buffernew = new byte[1024];
                        obj[0] = buffernew;
                        obj[1] = handler;
                        handler.BeginReceive(buffernew, 0, buffernew.Length,
                            SocketFlags.None,
                            new AsyncCallback(ReceiveCallback), obj);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: {0}", ex.ToString());
            }
        }

        /// <summary>
        /// Sends data asynchronously to a connected Socket.
        /// </summary>
        /// <param name="ar">
        /// The status of an asynchronous operation
        /// </param> 
        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // A Socket which has sent the data to remote host
                Socket handler = (Socket)ar.AsyncState;

                // The number of bytes sent to the Socket
                int bytesSend = handler.EndSend(ar);
                if (debug)
#if STATIC_LOG
                    frmSocket.WriteServerLog(string.Format("Sent {0} bytes to Client", bytesSend));
#else
                    ins.WriteServerLog(string.Format("Sent {0} bytes to Client", bytesSend));
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: {0}", ex.ToString());
            }
        }*/
    }

    public class StateObject
    {
        // Client  socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 1024;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();
    }

    class Data
    {
        //Default constructor
        public Data()
        {
            this.cmdCommand = Command.Null;
            this.strMessage = null;
            this.strName = null;
        }

        //Converts the bytes into an object of type Data
        public Data(byte[] data)
        {
            //The first four bytes are for the Command
            this.cmdCommand = (Command)BitConverter.ToInt32(data, 0);

            //The next four store the length of the name
            int nameLen = BitConverter.ToInt32(data, 4);

            //The next four store the length of the message
            int msgLen = BitConverter.ToInt32(data, 8);

            //This check makes sure that strName has been passed in the array of bytes
            if (nameLen > 0)
                this.strName = Encoding.UTF8.GetString(data, 12, nameLen);
            else
                this.strName = null;

            //This checks for a null message field
            if (msgLen > 0)
                this.strMessage = Encoding.UTF8.GetString(data, 12 + nameLen, msgLen);
            else
                this.strMessage = null;
        }

        //Converts the Data structure into an array of bytes
        public byte[] ToByte()
        {
            List<byte> result = new List<byte>();

            //First four are for the Command
            result.AddRange(BitConverter.GetBytes((int)cmdCommand));

            //Add the length of the name
            if (strName != null)
                result.AddRange(BitConverter.GetBytes(strName.Length));
            else
                result.AddRange(BitConverter.GetBytes(0));

            //Length of the message
            if (strMessage != null)
                result.AddRange(BitConverter.GetBytes(strMessage.Length));
            else
                result.AddRange(BitConverter.GetBytes(0));

            //Add the name
            if (strName != null)
                result.AddRange(Encoding.UTF8.GetBytes(strName));

            //And, lastly we add the message text to our array of bytes
            if (strMessage != null)
                result.AddRange(Encoding.UTF8.GetBytes(strMessage));

            return result.ToArray();
        }

        public string strName;      //Name by which the client logs into the room
        public string strMessage;   //Message text
        public Command cmdCommand;  //Command type (login, logout, send message, etcetera)
    }
}
