using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Dummy_Socket
{
    static class Program
    {
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmMain());
        }

        public static string AppFolder
        {
            get
            {
                return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }
        }
    }
    public class EnhancedForm<T> : Form
    {
        private static T _eForm;
        public static T Me
        {
            get
            {
                if (_eForm == null)
                    _eForm = (T)Activator.CreateInstance(typeof(T));
                return _eForm;
            }
        }

        public T1 CreateInstance<T1>() where T1 : T
        {
            _eForm = (T)Activator.CreateInstance(typeof(T1));
            return (T1)_eForm;
        }

        public static Configuration cfg
        {
            get
            {
                return frmOptions.config;
            }
        }

        //No tiene mayor uso
        public new void Show()
        {
            if (!Application.OpenForms.Cast<Form>().Contains(this))
                base.Show();
            else
                Console.WriteLine("Form '{0}' is already opened!", base.Text);
        }

        public void ShowMulti()
        {
            base.Show();
        }
    }

    public class SocketInstance
    {
        public frmSocket instance;
        public bool isClient;

        public SocketInstance(frmSocket i, bool ic)
        {
            instance = i;
            isClient = ic;
        }
    }
}
