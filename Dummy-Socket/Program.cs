using System;
using System.Collections.Generic;
using System.Linq;
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

        //No tiene mayor uso
        public new void Show()
        {
            if (!Application.OpenForms.Cast<Form>().Contains(this))
                base.Show();
            else
                Console.WriteLine("Form '{0}' is already opened!", base.Text);
        }
    }
}
