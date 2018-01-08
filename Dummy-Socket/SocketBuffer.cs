using System;
using System.Collections.Generic;
using System.Linq;

namespace DeltaSockets
{
    [Serializable]
    public class SocketBuffer
    {
        //public static Dictionary<ushort, ulong> requestIDs = new Dictionary<ushort, ulong>(); //ulong = Numeros de bloques
        //Esto lo tengo que meter en el servidor puesto que no sabe nada sobre lo que hay en cliente de forma local

        public ushort requestID;
        public IEnumerable<ulong> destsId;
        public string OriginalType;
        public IEnumerable<byte> splittedData;

        private SocketBuffer()
        { }

        private SocketBuffer(string type)
            : this(type, null)
        { }

        private SocketBuffer(string type, params ulong[] dests)
        {
            if (dests != null) destsId = dests;
            OriginalType = type;
        }

        public static IEnumerable<byte[]> GetBuffers(ulong ClientId, ushort RequestId, object toBuffer, params ulong[] dests)
        {
            string strType = toBuffer.GetType().Name;

            byte[] bufferSplitted = SocketManager.Serialize(toBuffer);

            //We need to calculate the length of the buffers
            SocketBuffer sb = new SocketBuffer(strType, dests);

            //We create an instance and we get an id & the block length
            int blockLen = GetBlockLength(sb);

            for (int i = 0; i < bufferSplitted.Length; i += blockLen)
            {
                if (i > 0) sb = new SocketBuffer(strType, dests);

                sb.requestID = RequestId;
                sb.splittedData = bufferSplitted.Skip(i).Take(blockLen - 1);

                yield return SocketManager.Serialize(new SocketMessage(ClientId, sb));
            }
        }

        private static int GetBlockLength(SocketBuffer sb)
        {
            return SocketManager.minBufferSize - (sb.GetObjectSize() + 5); //5 of margin??
        }
    }
}
