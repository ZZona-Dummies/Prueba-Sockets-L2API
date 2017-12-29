using DeltaSockets;
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
        /*private frmSocket socketForm
        {
            get
            {
                return frmSocket.Me;
            }
        }*/

        private frmOptions optionsForm
        {
            get
            {
                return frmOptions.Me;
            }
        }

        public static Dictionary<int, SocketInstance> socketIns = new Dictionary<int, SocketInstance>();
        public static int lastID = -1;

        public frmMain()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //if (SocketServerConsole.printer != null)
            //    throw new Exception("You must not open more than one server! If you do, please modify the source code.");

            frmSocket socketForm = new frmSocket();
            socketForm.Show();
            socketForm.ShowServerTab();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            frmSocket socketForm = new frmSocket();
            socketForm.Show();
            socketForm.ShowClientTab();
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
                    }

                if (clientIns)
                    for (int i = 0; i < cin; ++i)
                    {
                        frmSocket so = frmSocket.Me.CreateInstance<frmSocket>();
                        so.disableAutoName = true;
                        so.Show();
                        so.ShowClientTab();
                        so.SetName("Client" + i);
                        so.Start(true);
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