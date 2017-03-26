using Lerp2API.Communication.Sockets;
using System;
using System.Net;
using System.Net.Sockets;

namespace Dummy_Socket
{
    public partial class frmSocket : EnhancedForm<frmSocket>
    {
        public SocketClient client;
        public SocketServer server;

        public const string notValidClientConn = "Por favor, revisa que los campos IP y puerto sean válidos en la pestaña clientes.",
                            notValidServerConn = "Por favor, revisa que los campos IP y puerto sean válidos en la pestaña servidores.";

        public frmSocket()
        {
            InitializeComponent();
        }

        private void frmSocket_Load(object sender, EventArgs e)
        {

        }

        public void ShowClientTab()
        {
            if (tabControl1.SelectedTab != tabPage1)
                tabControl1.SelectedTab = tabPage1;
        }

        public void ShowServerTab()
        {
            if (tabControl1.SelectedTab != tabPage2)
                tabControl1.SelectedTab = tabPage2;
        }

        public void Start(bool isClient)
        {
            if(isClient)
            {
                if (client == null)
                {
                    if (ValidateClient())
                    {
                        client = new SocketClient(clientIP.Text, (int)clientPort.Value, ClientAction());
                        client.DoConnection();
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

                    server.ComeAlive();
                    server.StartListening();

                    server.ServerCallback = new AsyncCallback(SocketServer.AcceptCallback);

                }
                else
                    WriteServerLog(notValidServerConn);
            }
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
            Start(true);
        }

        private void clientConnect_Click(object sender, EventArgs e)
        {
            Start(false);
        }

        private void sendMsg_Click(object sender, EventArgs e)
        {
            client.WriteLine(clientMsg.Text);
        }

        private void WriteClientLog(string str)
        {
            clientLog.Text += str + Environment.NewLine;
        }
        private void WriteServerLog(string str)
        {
            serverLog.Text += str + Environment.NewLine;
        }
    }
}
