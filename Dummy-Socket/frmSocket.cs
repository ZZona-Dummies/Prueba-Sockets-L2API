using System;
using System.Net;
using System.Net.Sockets;

namespace Dummy_Socket
{
    public enum SocketState { NotStarted, ClientStarted, ServerStarted }
    public partial class frmSocket : EnhancedForm<frmSocket>
    {
        public SocketClient client;
        public SocketServer server;

        public bool disableAutoName;

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
                    clientConnect.Text = "¡Conectado!";
                else if (_state == SocketState.ServerStarted)
                    startServer.Text = "¡Server arrancado!";
                else
                {
                    if (oldstate == SocketState.ClientStarted)
                        clientConnect.Text = "Conectarse";
                    else if (oldstate == SocketState.ServerStarted)
                        startServer.Text = "Arrancar servidor";
                }
            }
        }

        public const string notValidClientConn = "Por favor, revisa que los campos IP y puerto sean válidos en la pestaña clientes.",
                            notValidServerConn = "Por favor, revisa que los campos IP y puerto sean válidos en la pestaña servidores.";

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
            if(isClient)
            {
                if (client == null)
                {
                    if (ValidateClient())
                    {
                        client = new SocketClient(clientIP.Text, (int)clientPort.Value, ClientAction());
                        client.ins = this;

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
                    server = new SocketServer(new SocketPermission(NetworkAccess.Accept, TransportType.Tcp, "", SocketPermission.AllPorts), IPAddress.Parse(serverIP.Text), (int)serverPort.Value, SocketType.Stream, ProtocolType.Tcp, false);
                    server.ins = this;

                    server.ComeAlive();
                    server.StartListening();

                    server.ServerCallback = new AsyncCallback(server.AcceptCallback);

                    succ = true;
                }
                else
                    WriteServerLog(notValidServerConn);
            }
            if (succ)
                state = isClient ? SocketState.ClientStarted : SocketState.ServerStarted;
        }

        private Action ClientAction()
        {
            return () => {
                byte[] bytes = new byte[1024];
                string str = client.ReceiveMessage(bytes);
                receivedMsgs.Text += str + Environment.NewLine;
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
        }

        public void WriteClientLog(string str)
        {
            clientLog.Text += str + Environment.NewLine;
        }
        public void WriteServerLog(string str)
        {
            serverLog.Text += str + Environment.NewLine;
        }
        public void SetName(string name)
        {
            clientName.Text = name;
        }
    }
}
