using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Dummy_Socket
{
    internal static class Program
    {
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        [STAThread]
        private static void Main()
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
                    _eForm = (T) Activator.CreateInstance(typeof(T));
                return _eForm;
            }
        }

        public T1 CreateInstance<T1>() where T1 : T
        {
            _eForm = (T) Activator.CreateInstance(typeof(T1));
            return (T1) _eForm;
        }

        public static Configuration cfg
        {
            get
            {
                return frmOptions.config;
            }
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

    public static class JSONHandler
    {
        public static void SerializeToFile<T>(string path, T obj)
        {
            try
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(obj));
            }
            catch (Exception ex)
            {
                Console.WriteLine("JSONHandler Serializing Exception: " + ex);
            }
        }

        public static T DeserializeFromFile<T>(string path)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                Console.WriteLine("JSONHandler Deserializing Exception: " + ex);
                return default(T);
            }
        }

        public static object DeserializeFromFile(string path)
        {
            try
            {
                return JsonConvert.DeserializeObject(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                Console.WriteLine("JSONHandler Deserializing Exception: " + ex);
                return null;
            }
        }

        public static bool IsJson(this string strInput)
        {
            strInput = strInput.Trim();
            if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || //For object
                (strInput.StartsWith("[") && strInput.EndsWith("]"))) //For array
            {
                try
                {
                    var obj = JToken.Parse(strInput);
                    return true;
                }
                catch (JsonReaderException jex)
                {
                    //Exception in parsing json
                    Console.WriteLine(jex.Message);
                    return false;
                }
                catch (Exception ex) //some other exception
                {
                    Console.WriteLine(ex.ToString());
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}