using System;
using System.Collections.Generic;
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

    public enum SocketCommands
    {
        //Server
        Conn,
        ConfirmConnId,
        CloseClients,
        ClosedClient,
        Stop,
        UnpoliteStop,
        CustomCommand,

        //Client
        CreateConnId,
        CloseInstance,
        //DeserializeData
    }

    public class SocketManager
    {
        //public const int minBufferSize = 4096;

        public static IEnumerable<byte[]> SerializeForClients(SocketClient client, ulong Id, object toBuffer)
        {
            return SerializeForClients(client, Id, toBuffer, SocketDbgType.Client, 0);
        }

        public static IEnumerable<byte[]> SerializeForClients(SocketClient client, ulong Id, object toBuffer, SocketDbgType type)
        {
            return SerializeForClients(client, Id, toBuffer, type, 0);
        }

        public static IEnumerable<byte[]> SerializeForClients(SocketClient client, ulong Id, object toBuffer, params ulong[] dests)
        {
            return SerializeForClients(client, Id, toBuffer, SocketDbgType.Client, dests);
        }

        //Heavy method
        public static IEnumerable<byte[]> SerializeForClients(SocketClient client, ulong Id, object toBuffer, SocketDbgType type, params ulong[] dests)
        {
            //Prepare here the buffer, calculating the restant size (for this we have to serialize and calculate how many bytes we can introduce on the buffer of SocketBuffer)
            //Yes, this requires a lot of serialization (3-steps)
            //I have to test how many free bytes has a message, to see how many bytes occupies the instance of the splittedData field.

            //Before, we send any data we have to request an id...
            //My best attemp would be creating a request id for the next use

            return SocketBuffer.GetBuffers(client, Id, toBuffer, dests);
        }

        public static byte[] Serialize(SocketMessage msg)
        {
            return Serialize(msg, SocketDbgType.Client);
        }

        public static byte[] Serialize(SocketMessage msg, SocketDbgType type)
        {
            return Serialize((object)msg, type);
        }

        internal static byte[] Serialize(object obj, SocketDbgType type = (SocketDbgType)(-1))
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    try
                    {
                        memoryStream.Seek(0, SeekOrigin.Begin);
                        (new BinaryFormatter()).Serialize(memoryStream, obj);

                        byte[] bytes = memoryStream.ToArray();

                        if (type != (SocketDbgType)(-1))
                            Console.WriteLine("[{1}] Serialized {0} bytes", bytes.Length, type.ToString().ToUpper());

                        return bytes;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Internal serialization error [Type: {0}] => " + ex, obj.GetType().Name);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                if (type != (SocketDbgType)(-1))
                    Console.WriteLine("Exception serializing in {0}: " + ex, type.ToString().ToUpper());
                return null;
            }
        }

        public static bool Deserialize<T>(byte[] message, long size, out T sm, SocketDbgType type)
        {
            if (message == null || message.Length == 0 || (message.Length > 0 && message.Sum(x => x) == 0))
            { //Esto ya lo hago en el otro lado pero por si las moscas
                Console.WriteLine(type == SocketDbgType.Client ? "Nothing new from the server..." : "Empty message to deserialize sended to the server!");
                sm = default(T);
                return false;
            }

            Console.WriteLine("[{1}] Trying to deserializing {0} bytes: {2} ...", message.Length, type.ToString().ToUpper(), string.Join("-", message.Select(x => x.ToString()).Take(10)));
            try
            {
                using (MemoryStream memoryStream = new MemoryStream(message))
                {
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    if(size > 0) memoryStream.SetLength(size);
                    sm = (T)(new BinaryFormatter()).Deserialize(memoryStream);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception deserializing in {1}: {0}; Bytes: {2} ...", ex.ToString(), type.ToString().ToUpper(), string.Join("-", message.Select(x => x.ToString()).Take(10)));
                sm = default(T);
                return false;
            }
        }

        //0 means that the message is not for any client, is a broadcast message sended to the server, so, we have to handle errors when we don't manage it correctly.
        public static byte[] SendCommand(SocketCommands cmd, SocketCommandData data, ulong OriginClientId = 0)
        {
            return Serialize(new SocketMessage(OriginClientId, 0, new SocketCommand(cmd, data)));
        }

        private static byte[] SendCommand(SocketCommands cmd, ulong OriginClientId = 0)
        {
            return Serialize(new SocketMessage(OriginClientId, 0, new SocketCommand(cmd)));
        }

        public static byte[] SendBuffer(ulong OriginClientId, ulong RequestId, SocketBuffer buffer)
        {
            return Serialize(new SocketMessage(OriginClientId, RequestId, buffer));
        }

        //Server actions that doesn't need to be sended to the other clients and maybe that need also any origin id

        public static byte[] SendConnId(ulong clientId)
        {
            return SendCommand(SocketCommands.CreateConnId, clientId);
        }

        public static byte[] ConfirmConnId(ulong id)
        {
            return SendCommand(SocketCommands.ConfirmConnId, id);
        }

        //Id is obtained later
        public static byte[] ManagedConn()
        {
            return SendCommand(SocketCommands.Conn);
        }

        public static byte[] PoliteClose(ulong id = 0)
        {
            return SendCommand(SocketCommands.CloseInstance);
        }

        public static byte[] ClientClosed(ulong id = 0)
        {
            return SendCommand(SocketCommands.ClosedClient);
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
