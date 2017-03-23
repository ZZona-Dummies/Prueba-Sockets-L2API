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
    public partial class frmMain : EnhancedForm
    {
        private frmSocket socketForm = GetForm<frmSocket>();
        private frmOptions optionsForm = GetForm<frmOptions>();

        public frmMain()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            socketForm.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            socketForm.Show();
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
