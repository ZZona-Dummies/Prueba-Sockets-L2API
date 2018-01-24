using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace DeltaSockets
{
    [Serializable]
    public class SocketBuffer : ISerializable
    {
        //Quizás tenga que añadirle un int para ver el orden de los paquetes

        public IEnumerable<ulong> destsId;
        public string OriginalType;
        public IEnumerable<byte> splittedData; //Esto tampoco...

        //With this you can transfer 8TB, it only ocuppies 4 bytes as maximum
        public int myOrder; //This can store to 8TB... (4 KB * int.MaxValue)

        public int blockNum; //Replicate all this info in all blocks

        private SocketBuffer()
        { }

        protected SocketBuffer(SerializationInfo info, StreamingContext context)
        {
            // Reset the property value using the GetValue method.
            destsId = (IEnumerable<ulong>)info.GetValue("dI", typeof(ulong[]));
            OriginalType = info.GetString("OT");
            splittedData = (IEnumerable<byte>)info.GetValue("sD", typeof(byte[]));
            blockNum = info.GetInt32("bN");
        }

        private SocketBuffer(string type)
            : this(type, null)
        { }

        private SocketBuffer(string type, params ulong[] dests)
        {
            if (dests != null) destsId = dests;
            OriginalType = type;
        }

        public static IEnumerable<byte[]> GetBuffers(SocketClient client, ulong ClientId, object toBuffer, params ulong[] dests)
        {
            string strType = toBuffer.GetType().Name;

            byte[] bufferSplitted = SocketManager.Serialize(toBuffer);

            //We need to calculate the length of the buffers
            SocketBuffer sb = new SocketBuffer(strType, dests);
            sb.myOrder = 0;

            //We create an instance and we get an id & the block length
            int blockLen = GetBlockLength(sb);

            int w = 1;
            for (int i = 0; i < bufferSplitted.Length; i += blockLen)
            {
                if (i > 0) sb = new SocketBuffer(strType, dests);

                sb.myOrder = w;
                sb.splittedData = bufferSplitted.Skip(i).Take(blockLen - 1);
                sb.blockNum = (int)((float)bufferSplitted.Length / blockLen); //We get the number of packets we will send

                ulong requestId = client.Id;

                if (client.requestIDs == null) client.requestIDs = new List<ulong>();
                if (client.requestIDs.Count > 0)
                    client.requestIDs.FindFirstMissingNumberFromSequence(out requestId, new MinMax<ulong>(client.Id, client.maxReqId));

                client.requestIDs.Add(requestId);

                //if(i >= bufferSplitted.Length - blockLen - 1)
                //    sb.end = true;

                ++w;

                yield return SocketManager.SendBuffer(client.Id, requestId, sb);
            }
        }

        private static int GetBlockLength(SocketBuffer sb)
        {
            return StateObject.BufferSize - (sb.GetObjectSize() + 144); //144 of margin?? Esto está cogido con pinzas
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");

            info.AddValue("dI", destsId.ToArray());
            info.AddValue("OT", OriginalType);
            info.AddValue("bN", blockNum);

            if (splittedData != null)
                info.AddValue("sD", splittedData.ToArray());
        }
    }

    public class SocketStorer
    {
        public Lazy<List<byte>> buffer = new Lazy<List<byte>>(() => new List<byte>());
        public int PacketCount;
    }
}