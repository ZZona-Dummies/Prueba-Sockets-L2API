using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Dummy_Socket
{
    public class SocketServer
    {
        public const int lerpedPort = 22222;

        public Socket ServerSocket;
        public SocketPermission Permision;
        public IPAddress IP;
        public int Port;
        private IPEndPoint _endpoint;
        private byte[] bytes;

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
        public AsyncCallback ServerCallback
        {
            set
            {
                _callback = value;
                AssignCallback(_callback);
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

            bytes = new byte[1024];

            if (doConnection) ServerSocket.Bind(IPEnd);
        }

        public void ComeAlive()
        {
            IPEndPoint end = IPEnd;
            if (end != null) ServerSocket.Bind(end);
#if STATIC_LOG
            else frmSocket.WriteServerLog("Destination IP isn't defined!");
#else
            else ins.WriteServerLog("Destination IP isn't defined!");
#endif
        }

        public void StartListening()
        {
            if (ServerSocket.IsBound) ServerSocket.Listen(10);
#if STATIC_LOG
            else frmSocket.WriteServerLog("You have to make alive your Server socket first! (Call 'ComeAlive' method)");
#else
            else ins.WriteServerLog("You have to make alive your Server socket first! (Call 'ComeAlive' method)");
#endif
        }

        private void AssignCallback(AsyncCallback aCallback)
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
        }

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
        public void AcceptCallback(IAsyncResult ar)
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
        }
    }
}
