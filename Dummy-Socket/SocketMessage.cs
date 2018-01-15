using System;

namespace DeltaSockets
{
    /// <summary>
    /// Class SocketMessage.
    /// </summary>
    [Serializable]
    public class SocketMessage
    {
        //Aqui no se devuelve ninguna excpecion ... OJO CUIDAO!
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

        [Obsolete]
        public ulong id
        {
            get
            {
                return ClientOriginId;
            }
        }

        /// <summary>
        /// The identifier
        /// </summary>
        public ulong ClientOriginId; //0 is never used, because 0 is for all clients...
        public ulong RequestID;

        public int MessageSize
        {
            get
            { //This can contain up to (2^32 / 2^12 = 2^20) bytes only or 1GB only
                if (msg is SocketBuffer)
                    return ((SocketBuffer)msg).blockNum * StateObject.BufferSize;
                return -1;
            }
        }

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
        public SocketMessage(ulong oi, ulong rid, object m)
        {
            ClientOriginId = oi;
            RequestID = rid;
            msg = m;
        }

        public T TryGetObject<T>()
        {
            return (T)msg;
        }
    }
}
