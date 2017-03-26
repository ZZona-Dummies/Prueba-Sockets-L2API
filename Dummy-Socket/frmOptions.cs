using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Dummy_Socket
{
    public partial class frmOptions : EnhancedForm<frmOptions>
    {
        private static Configuration _config;
        public static Configuration config
        {
            get
            {
                if (_config == null)
                    _config = Configuration.Load();
                return _config;
            }
        }
        public frmOptions()
        {
            InitializeComponent();
        }

        private void frmOptions_Load(object sender, EventArgs e)
        {
            numericUpDown1.Value = _config.clientInstances;
            numericUpDown2.Value = _config.serverInstances;
            checkBox1.Checked = _config.startClient;
            checkBox2.Checked = _config.startServer;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Configuration.Save(_config);
        }
    }
}
