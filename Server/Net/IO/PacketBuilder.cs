using System.Text;

namespace Server.Net.IO
{
    class PacketBuilder
    {
        MemoryStream _ms;
        public PacketBuilder()
        {
            _ms = new MemoryStream();
        }

        public void WriteOpCode(byte opCode)
        {
            _ms.WriteByte(opCode);
        }

        public void WriteMessage(string msg)
        {
            var msgBytes = Encoding.UTF8.GetBytes(msg);
            var msgLength = msgBytes.Length;

            _ms.Write(BitConverter.GetBytes(msgLength));
            _ms.Write(msgBytes);
        }

        public void WriteFile(string fileName, byte[] fileBytes)
        {
            var nameBytes = Encoding.UTF8.GetBytes(fileName);
            var nameLength = nameBytes.Length;
            _ms.Write(BitConverter.GetBytes(nameLength), 0, 4);
            _ms.Write(nameBytes, 0, nameLength);

            var fileLength = fileBytes.Length;
            _ms.Write(BitConverter.GetBytes(fileLength), 0, 4);
            _ms.Write(fileBytes, 0, fileLength);
        }

        public byte[] GetPacketBytes()
        {
            return _ms.ToArray();
        }
    }
}
