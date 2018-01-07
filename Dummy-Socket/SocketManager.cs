using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;

namespace DeltaSockets
{
    public enum SocketDbgType
    {
        Client,
        Server
    }

    public enum SocketState
    {
        NonStarted,
        ClientStarted,
        ServerStarted,
        ClientStopped,
        ServerStopped
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
        public ulong id; //0 is never used, because 0 is for all clients...

        public Type Type
        {
            get
            { //If this is not equal to SocketBuffer we can assume that this is a command for the server.
                return msg.GetType();
            }
        }

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
        public SocketMessage(ulong i, object m)
        {
            id = i;
            msg = m;
        }
    }

    [Serializable]
    public class SocketBuffer
    {
        public ulong[] destsId = new ulong[1] { 0 };
        public byte[] splittedData;

        private SocketBuffer()
        { }

        public SocketBuffer(byte[] data)
            : this(data, null)
        { }

        public SocketBuffer(byte[] data, params ulong[] dests)
        {
            if (dests != null) destsId = dests;
            splittedData = data;
        }
    }

    public class SocketManager
    {
        public static byte[] SerializeForClients(ulong Id, object toBuffer, SocketDbgType type = SocketDbgType.Client)
        {
            //Prepare here the buffer, calculating the restant size (for this we have to serialize and calculate how many bytes we can introduce on the buffer of SocketBuffer)
            //Yes, this requires a lot of serialization (3-steps)
            //I have to test how many free bytes has a message, to see how many bytes occupies the instance of the splittedData field.
            return Serialize(new SocketMessage(Id, null), type);
        }

        public static byte[] Serialize(SocketMessage msg, SocketDbgType type = SocketDbgType.Client)
        {
            return Serialize(msg, type);
        }

        private static byte[] Serialize(object obj, SocketDbgType type = SocketDbgType.Client)
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    //memoryStream.Seek(0, SeekOrigin.Begin);
                    (new BinaryFormatter()).Serialize(memoryStream, obj);

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

        public static bool Deserialize<T>(byte[] message, long size, out T sm, SocketDbgType type)
        {
            if (message == null || message.Length == 0 || (message.Length > 0 && message.Sum(x => x) == 0))
            {
                Console.WriteLine(type == SocketDbgType.Client ? "Nothing new from the server..." : "Empty message to deserialize sended to the server!");
                sm = default(T);
                return false;
            }

            Console.WriteLine("[{1}] Trying to deserializing {0} bytes: {2}", message.Length, type.ToString().ToUpper(), ""); // string.Join(" ", message.Select(x => x.ToString())));
            try
            {
                using (MemoryStream memoryStream = new MemoryStream(message))
                {
                    //memoryStream.Seek(0, SeekOrigin.Begin);
                    memoryStream.SetLength(size);
                    sm = (T)(new BinaryFormatter()).Deserialize(memoryStream);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception deserializing in {1}: {0}\n\nBytes:\n\n{2}", ex.ToString(), type.ToString().ToUpper(), string.Join(" ", message.Select(x => x.ToString())));
                sm = default(T);
                return false;
            }
        }

        //0 means that the message is not for any client, is a broadcast message sended to the server, so, we have to handle errors when we don't manage it correctly.
        private static byte[] SendString(string msg, ulong id = 0)
        {
            return Serialize(new SocketMessage(id, msg));
        }

        //Server actions that doesn't need to be sended to the other clients and maybe that need also any origin id

        public static byte[] SendId(ulong id)
        {
            return SendString("<give_id>", id);
        }

        public static byte[] ConfirmId(ulong id)
        {
            return SendString("<confirm_id>", id);
        }

        public static byte[] ManagedConn(ulong id)
        {
            return SendString("<conn>", id);
        }

        public static byte[] PoliteClose(ulong id = 0)
        {
            return SendString("<close>");
        }

        public static byte[] ClientClosed(ulong id = 0)
        {
            return SendString("<client_closed>");
        }
    }

    public class SocketServerConsole
    {
        private readonly Control printer;

        private SocketServerConsole()
        {
        }

        public SocketServerConsole(Control c)
        {
            printer = c;
        }

        public void Log(string str, params object[] str0)
        {
            Log(string.Format(str, str0));
        }

        public void Log(string str)
        {
            Console.WriteLine(str);
#if LOG_SERVER
            if (printer != null)
            {
                if (printer.InvokeRequired) //De esto hice una versión mejorada
                    printer.Invoke(new MethodInvoker(() => { printer.Text += str + Environment.NewLine; }));
            }
            else
                Console.WriteLine("You must define 'myLogger' field of type 'SocketServerConsole' inside 'SocketServer' in order to use this feature.");
#endif
        }
    }

    public class SocketClientConsole
    {
        public Control errorPrinter;

        private readonly Control printer;
        private readonly bool writeLines = true;

        private SocketClientConsole()
        {
        }

        public SocketClientConsole(Control c, bool wl = true)
        {
            printer = c;
            writeLines = wl;
        }

        public void Log(string str, params object[] str0)
        {
            Log(string.Format(str, str0));
        }

        public void Log(string str)
        {
            if (writeLines)
                Console.WriteLine("Client Message: " + str);
#if LOG_CLIENT
            if (printer != null)
            {
                if (printer.InvokeRequired) //De esto hice una versión mejorada
                    printer.Invoke(new MethodInvoker(() => { printer.Text += str + Environment.NewLine; }));
            }
            else
                Console.WriteLine("You must define 'myLogger' field of type 'SocketCientConsole' inside 'SocketClient' in order to use this feature.");
#endif
        }

        public void LogError(string str, params object[] str0)
        {
            LogError(string.Format(str, str0));
        }

        public void LogError(string str)
        {
            if (writeLines)
                Console.WriteLine("Client Error Message: " + str);
#if LOG_CLIENT
            if (errorPrinter != null)
            {
                if (errorPrinter.InvokeRequired) //De esto hice una versión mejorada
                    errorPrinter.Invoke(new MethodInvoker(() => { errorPrinter.Text += str + Environment.NewLine; }));
            }
            else
                Console.WriteLine("You must define 'myLogger.errorPrinter' field of type 'SocketCientConsole' inside 'SocketClient' in order to use this feature.");
#endif
        }
    }
}
