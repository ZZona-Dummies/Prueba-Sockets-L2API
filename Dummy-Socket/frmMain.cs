using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Dummy_Socket
{
    public partial class frmMain : EnhancedForm<frmMain>
    {
        private frmSocket socketForm
        {
            get
            {
                return frmSocket.Me;
            }
        }

        private frmOptions optionsForm
        {
            get
            {
                return frmOptions.Me;
            }
        }

        public static Dictionary<int, SocketInstance> socketIns = new Dictionary<int, SocketInstance>();

        public frmMain()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            socketForm.Show();
            socketForm.ShowClientTab();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            socketForm.Show();
            socketForm.ShowServerTab();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ExecuteInstance(true, true);
        }

        private void ExecuteInstance(bool serverIns, bool clientIns)
        {
            if (clientIns || serverIns)
            {
                int cin = cfg.clientInstances,
                    sin = cfg.serverInstances;

                if (serverIns)
                    for (int i = 0; i < sin; ++i)
                    {
                        frmSocket so = frmSocket.Me.CreateInstance<frmSocket>();
                        so.Show();
                        so.ShowServerTab();
                        so.Start(false);
                        socketIns.Add(i, new SocketInstance(so, false));
                    }

                if (clientIns)
                    for (int i = 0; i < cin; ++i)
                    {
                        frmSocket so = frmSocket.Me.CreateInstance<frmSocket>();
                        so.Show();
                        so.ShowClientTab();
                        so.Start(true);
                        socketIns.Add(i + sin, new SocketInstance(so, true));
                    }
            }
        }

        private void opcionesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            optionsForm.Show();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            ExecuteInstance(cfg.startServer, cfg.startClient);
        }
    }
}
