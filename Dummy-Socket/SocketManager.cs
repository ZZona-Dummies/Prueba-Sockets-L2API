using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace DeltaSockets
{
    public enum SocketDbgType
    {
        Client, Server
    }

    /// <summary>
    /// Class SocketMessage.
    /// </summary>
    [Serializable]
    public class SocketMessage
    {
        public string StringValue
        {
            get
            {
                try
                {
                    return (string)msg;
                }
                catch
                {
                    return "";
                }
            }
        }

        public int IntValue
        {
            get
            {
                try
                {
                    return (int)msg;
                }
                catch
                {
                    return -1;
                }
            }
        }

        /// <summary>
        /// The identifier
        /// </summary>
        public int id;

        public string TypeString;

        //Name??
        /// <summary>
        /// The MSG
        /// </summary>
        public object msg;

        /// <summary>
        /// Initializes a new instance of the <see cref="SocketMessage"/> class.
        /// </summary>
        /// <param name="i">The i.</param>
        /// <param name="m">The m.</param>
        public SocketMessage(int i, object m)
        {
            id = i;
            msg = m;
        }
    }

    public class SocketManager
    {
        public static byte[] Serialize<T>(int Id, T anySerializableObject, SocketDbgType type = SocketDbgType.Client)
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    (new BinaryFormatter()).Serialize(memoryStream, new SocketMessage(Id, anySerializableObject) { TypeString = typeof(T).Name });

                    byte[] bytes = memoryStream.ToArray();
                    Console.WriteLine("[{1}] Serialized {0} bytes", bytes.Length, type.ToString().ToUpper());

                    return bytes;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception serializing in {0}: " + ex, type.ToString().ToUpper());
                return null;
            }
        }

        public static bool Deserialize<T>(byte[] message, out T sm, SocketDbgType type)
        {
            if (message == null || message.Length == 0 || (message.Length == 1 && message[0] == 0))
            {
                //Microsoft.VisualBasic.Interaction.MsgBox(Convert.ToString("ERROR CONTROLADO?"));
                Console.WriteLine("Empty message to deserialize!");
                sm = (T)Activator.CreateInstance(typeof(T));
                return false;
            }

            Console.WriteLine("[{1}] Trying to deserializing {0} bytes", message.Length, type.ToString().ToUpper());
            try
            {
                using (MemoryStream memoryStream = new MemoryStream(message))
                {
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    sm = (T)(new BinaryFormatter()).Deserialize(memoryStream);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception deserializing in {0}: " + ex.ToString(), type.ToString().ToUpper());
                sm = default(T);
                return false;
            }

            sm = default(T);
            return false;
        }
    }
}
