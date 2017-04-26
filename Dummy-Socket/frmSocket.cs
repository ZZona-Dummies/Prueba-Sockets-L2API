using System;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Linq;
using Lerp2API.SafeECalls;

namespace Dummy_Socket
{
    public enum SocketState { NotStarted, ClientStarted, ServerStarted }
    public partial class frmSocket : EnhancedForm<frmSocket>
    {
        public SocketClient client;
        public SocketServer server;

        public bool disableAutoName;

        public int socketID;

        private SocketState _state;
        public SocketState state
        {
            get
            {
                return _state;
            }
            set
            {
                SocketState oldstate = _state;
                _state = value;
                if (_state == SocketState.ClientStarted)
                {
                    clientConnect.Text = "¡Conectado!";
                    DisableControls(false);
                }
                else if (_state == SocketState.ServerStarted)
                {
                    startServer.Text = "¡Server arrancado!";
                    DisableControls(true);
                }
                else
                {
                    if (oldstate == SocketState.ClientStarted)
                    {
                        clientConnect.Text = "Conectarse";
                        EnableControls(true);
                    }
                    else if (oldstate == SocketState.ServerStarted)
                    {
                        startServer.Text = "Arrancar servidor";
                        EnableControls(false);
                    }
                }
            }
        }

        public const string notValidClientConn = "Por favor, revisa que los campos IP y puerto sean válidos en la pestaña clientes.",
                            notValidServerConn = "Por favor, revisa que los campos IP y puerto sean válidos en la pestaña servidores.";

        private string svLog, clLog;

        public frmSocket()
        {
            InitializeComponent();
        }

        private void frmSocket_Load(object sender, EventArgs e)
        {
            if (!disableAutoName)
                clientName.Text = string.Format("Client{0}", new Random().Next(0, 9999));
        }

        public void ShowServerTab()
        {
            if (tabControl1.SelectedTab != tabPage1)
                tabControl1.SelectedTab = tabPage1;
        }

        public void ShowClientTab()
        {
            if (tabControl1.SelectedTab != tabPage2)
                tabControl1.SelectedTab = tabPage2;
        }

        public void Start(bool isClient)
        {
            bool succ = false;
            socketID = ++frmMain.lastID;
            if (isClient)
            {
                if (client == null)
                {
                    if (ValidateClient())
                    {
                        client = new SocketClient(clientIP.Text, (int)clientPort.Value, ClientAction());
                        client.socketID = socketID;
                        client.DoConnection();

                        succ = true;
                    }
                    else
                        WriteClientLog(notValidClientConn);
                }
            }
            else
            {
                if (ValidateServer())
                {
                    server = new SocketServer(new SocketPermission(NetworkAccess.Accept, TransportType.Tcp, "", SocketPermission.AllPorts), IPAddress.Parse(serverIP.Text), (int)serverPort.Value, SocketType.Stream, ProtocolType.Tcp, true);
                    server.socketID = socketID;

                    server.ComeAlive();
                    //server.StartListening();

                    //server.ServerCallback = new AsyncCallback(server.AcceptCallback);

                    succ = true;
                }
                else
                    WriteServerLog(notValidServerConn);
            }
            if (succ)
            {
                state = isClient ? SocketState.ClientStarted : SocketState.ServerStarted;
                frmMain.socketIns.Add(socketID, new SocketInstance(this, isClient));
            }
        }

        private Action ClientAction()
        {
            return () => {
                byte[] bytes = new byte[1024];
                string str = client.ReceiveMessage(bytes);
                SocketMessage sm = JsonUtility.FromJson<SocketMessage>(str);
                Console.WriteLine("My ID: {0}, Id received: {1}\nMessage: {2}", client.Id, sm.id, sm.msg);
                if (receivedMsgs.InvokeRequired)
                    receivedMsgs.Invoke(new MethodInvoker(() => { receivedMsgs.Text += sm.msg; }));
            };
        }

        private bool ValidateClient()
        {
            return !string.IsNullOrWhiteSpace(clientIP.Text);
        }

        private bool ValidateServer()
        {
            return !string.IsNullOrWhiteSpace(serverIP.Text);
        }

        private void startServer_Click(object sender, EventArgs e)
        {
            Start(false);
        }

        private void clientConnect_Click(object sender, EventArgs e)
        {
            Start(true);
        }

        private void sendMsg_Click(object sender, EventArgs e)
        {
            client.WriteLine(clientMsg.Text);
            clientMsg.Text = "";
        }

#if STATIC_LOG
        public static void WriteClientLog(string str)
        {
            WriteLog(str, true);
        }
        public static void WriteServerLog(string str)
        {
            WriteLog(str, false);
        }
        private static void WriteLog(string str, bool isClient)
        {
            Console.WriteLine(str);
            str += Environment.NewLine;
            if (isClient)
            {
                foreach (frmSocket so in frmMain.socketIns.Values.Where(x => x.isClient).Select(x => x.instance))
                    so.clientLog.Text += str;
            }
            else
            {
                foreach (frmSocket so in frmMain.socketIns.Values.Where(x => !x.isClient).Select(x => x.instance))
                    so.serverLog.Text += str;
            }
        }
#else
        public void WriteClientLog(string str, params object[] pars)
        {
            WriteLog(str, true, pars);
        }
        public void WriteServerLog(string str, params object[] pars)
        {
            WriteLog(str, false, pars);
        }
        private void WriteLog(string str, bool isClient, params object[] pars)
        {
            Console.WriteLine(str);
            str += Environment.NewLine;
            if (isClient)
            {
                clLog += pars != null ? string.Format(str, pars) : str;
                if (clientLog.InvokeRequired)
                    clientLog.Invoke(new MethodInvoker(() => { clientLog.Text = clLog; }));
            }
            else
            {
                svLog += pars != null ? string.Format(str, pars) : str;
                if (serverLog.InvokeRequired)
                    serverLog.Invoke(new MethodInvoker(() => { serverLog.Text = svLog; }));
            }
        }
#endif
        public void SetName(string name)
        {
            clientName.Text = name;
        }

        private void frmSocket_Closing(object sender, FormClosingEventArgs e)
        {
            if (state == SocketState.ClientStarted)
            {
                client.CloseConnection(SocketShutdown.Both);
                client.DisposeSocket();
            }
            if(state == SocketState.ServerStarted)
                server.CloseServer();
        }

        private void EnableControls(bool isClient)
        {
            ControlControls(isClient, true);
        }

        private void DisableControls(bool isClient)
        {
            ControlControls(isClient, false);
        }

        internal void ControlControls(bool isClient, bool enable)
        {
            TabPage tb = tabPage2;
            if (!isClient) tb = tabPage1;

            foreach (Control c in tb.Controls)
                c.Enabled = enable;
        }
    }
}
