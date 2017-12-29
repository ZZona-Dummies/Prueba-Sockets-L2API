using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Timer = System.Threading.Timer;

namespace DeltaSockets
{
    /// <summary>
    /// Class SocketMessage.
    /// </summary>
    [Serializable]
    public class SocketMessage
    {
        public string StringValue
        {
            get
            {
                try
                {
                    return (string) msg;
                }
                catch
                {
                    return "";
                }
            }
        }

        public int IntValue
        {
            get
            {
                try
                {
                    return (int) msg;
                }
                catch
                {
                    return -1;
                }
            }
        }

        /// <summary>
        /// The identifier
        /// </summary>
        public int id;

        //Name??
        /// <summary>
        /// The MSG
        /// </summary>
        public object msg;

        /// <summary>
        /// Initializes a new instance of the <see cref="SocketMessage"/> class.
        /// </summary>
        /// <param name="i">The i.</param>
        /// <param name="m">The m.</param>
        public SocketMessage(int i, object m)
        {
            id = i;
            msg = m;
        }

        public static byte[] Serialize<T>(int Id, T anySerializableObject)
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    (new BinaryFormatter()).Serialize(memoryStream, new SocketMessage(Id, anySerializableObject));
                    return memoryStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception serializing: " + ex.ToString());
                return null;
            }
        }

        public static T Deserialize<T>(byte[] message)
        {
            if (message == null || message.Length == 0 || (message.Length == 1 && message[0] == 0))
            {
                //Microsoft.VisualBasic.Interaction.MsgBox(Convert.ToString("ERROR CONTROLADO?"));
                Console.WriteLine("Error controlado");
                return (T) Activator.CreateInstance(typeof(T));
            }

            Console.WriteLine("Deserializing {0} bytes", message.Length);
            try
            {
                using (MemoryStream memoryStream = new MemoryStream(message))
                {
                    return (T) (new BinaryFormatter()).Deserialize(memoryStream);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception deserializing: " + ex.ToString());
                return default(T);
            }
        }
    }

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
        public int Port, Id;

        private IPEndPoint _endpoint;

        //I think I will delete this later
        private readonly byte[] socketBuffer;

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
        public SocketClient(string ip, int port, bool doConnection = false) :
            this(ip, port, null, 100, doConnection)
        { }

        public SocketClient(IPAddress ip, int port, bool doConnection = false) :
            this(ip, port, SocketType.Stream, ProtocolType.Tcp, 100, null, doConnection)
        { }

        public SocketClient(IPAddress ip, int port, Action everyFunc, int readEvery = 100, bool doConnection = false) :
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
        public SocketClient(string ip, int port, Action everyFunc, int readEvery = 100, bool doConnection = false) :
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
        public SocketClient(IPAddress ipAddr, int port, SocketType sType, ProtocolType pType, int readEvery, Action everyFunc, bool doConnection = false)
        {
            socketBuffer = new byte[1024];

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

            Id = ClientSocket.GetHashCode();

            if (doConnection)
            {
                ClientSocket.Connect(IPEnd);
                StartReceiving();
            }
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
            IPEndPoint end = IPEnd;
            if (end != null)
            {
                ClientSocket.Connect(end);
                StartReceiving();
                ClientSocket.Send(SocketMessage.Serialize(Id, "<conn>"));
            }
            else Console.WriteLine("Destination IP isn't defined!");
        }

        public int SendData(object msg)
        {
            if (!ClientSocket.Equals(null) && ClientSocket.Connected)
            {
                int bytesSend = ClientSocket.Send(SocketMessage.Serialize(Id, msg));
                return bytesSend;
            }
            else
            {
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
                byte[] bytes = new byte[1024];

                // Receives data from a bound Socket.
                int bytesRec = 0;

                if (!ClientSocket.Equals(null))
                {
                    bytesRec = ClientSocket.Receive(bytes);
                }

                // Continues to read the data till data isn't available
                while (ClientSocket.Available > 0)
                    bytesRec = ClientSocket.Receive(bytes);

                SocketMessage sm = (SocketMessage) SocketMessage.Deserialize<object>(bytes);
                msg = sm;

                if (sm.StringValue == "<close>")
                {
                    Console.WriteLine("Closing connection...");
                    SendData("<client_closed>");
                    End();
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            { //Forced connection close...
                Console.WriteLine("Exception ocurred while receiving data! " + ex.ToString());
                msg = null; //Dead silence.
                SendData("<client_closed>");
                End();
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
        public void End()
        {
            CloseConnection(SocketShutdown.Both);
            Dispose();
        }

        private void Timering(object stateInfo)
        {
            act();
        }
    }

    public class SocketClientConsole
    {
        public Control errorPrinter;

        private readonly Control printer;
        private readonly bool writeLines = true;

        private SocketClientConsole()
        {
        }

        public SocketClientConsole(Control c, bool wl = true)
        {
            printer = c;
            writeLines = wl;
        }

        public void Log(string str, params object[] str0)
        {
            Log(string.Format(str, str0));
        }

        public void Log(string str)
        {
            if (writeLines)
                Console.WriteLine("Client Message: " + str);
#if LOG_CLIENT
            if (printer != null)
            {
                if (printer.InvokeRequired) //De esto hice una versión mejorada
                    printer.Invoke(new MethodInvoker(() => { printer.Text += str + Environment.NewLine; }));
            }
            else
                Console.WriteLine("You must define 'myLogger' field of type 'SocketCientConsole' inside 'SocketClient' in order to use this feature.");
#endif
        }

        public void LogError(string str, params object[] str0)
        {
            LogError(string.Format(str, str0));
        }

        public void LogError(string str)
        {
            if (writeLines)
                Console.WriteLine("Client Error Message: " + str);
#if LOG_CLIENT
            if (errorPrinter != null)
            {
                if (errorPrinter.InvokeRequired) //De esto hice una versión mejorada
                    errorPrinter.Invoke(new MethodInvoker(() => { errorPrinter.Text += str + Environment.NewLine; }));
            }
            else
                Console.WriteLine("You must define 'myLogger.errorPrinter' field of type 'SocketCientConsole' inside 'SocketClient' in order to use this feature.");
#endif
        }
    }
}