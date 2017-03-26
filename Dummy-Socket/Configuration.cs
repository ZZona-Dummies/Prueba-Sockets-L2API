using System.IO;

namespace Dummy_Socket
{
    public class Configuration
    {
        public int clientInstances = 2,
                   serverInstances = 1;
        public bool startClient,
                    startServer;

        public static string configFile = Path.Combine(Program.AppFolder, "SocketConfig.xml");

        public static Configuration Load()
        {
            Configuration ins = XMLTools.DeserializeFromFile<Configuration>(configFile);
            if(ins != null)
                return ins;
            return new Configuration();
        }

        public static void Save(Configuration ins)
        {
            XMLTools.SerializeToFile(ins, configFile);
        }
    }
}
