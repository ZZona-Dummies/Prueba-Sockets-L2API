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
            Configuration ins = File.Exists(configFile) ? JSONHandler.DeserializeFromFile<Configuration>(configFile) : null;
            if (ins != null)
                return ins;
            return new Configuration();
        }

        public static void Save(Configuration ins)
        {
            JSONHandler.SerializeToFile(configFile, ins);
        }
    }
}