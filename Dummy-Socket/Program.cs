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

    public class EnhancedForm : Form
    {
        private static EnhancedForm _eForm;
        public static EnhancedForm Me
        {
            get
            {
                if (_eForm == null)
                    _eForm = new EnhancedForm();
                return _eForm;
            }
        }

        public static T GetForm<T>() where T : EnhancedForm
        {
            return (T)Me;
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
