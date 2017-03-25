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

        public frmMain()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            socketForm.Show();
            if (socketForm.tabControl1.SelectedTab != socketForm.tabPage1)
                socketForm.tabControl1.SelectedTab = socketForm.tabPage1;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            socketForm.Show();
            if(socketForm.tabControl1.SelectedTab != socketForm.tabPage2)
                socketForm.tabControl1.SelectedTab = socketForm.tabPage2;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            socketForm.Show();
        }

        private void opcionesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            optionsForm.Show();
        }
    }
}
