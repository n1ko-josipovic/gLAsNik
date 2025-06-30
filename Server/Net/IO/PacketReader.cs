using System.Net.Sockets;
using System.Text;

namespace Server.Net.IO
{
    class PacketReader : BinaryReader
    {
        private NetworkStream _ms;
        public PacketReader(NetworkStream ms) : base(ms)
        {
            _ms = ms;
        }

        public async Task<string> ReadMessageAsync()
        {
            byte[] lengthBytes = new byte[4];
            int read = 0;
            while (read < 4)
            {
                int r = await _ms.ReadAsync(lengthBytes, read, 4 - read);
                if (r == 0) break;
                read += r;
            }
            int length = BitConverter.ToInt32(lengthBytes, 0);

            byte[] msgBuffer = new byte[length];
            read = 0;
            while (read < length)
            {
                int r = await _ms.ReadAsync(msgBuffer, read, length - read);
                if (r == 0) break;
                read += r;
            }

            return Encoding.UTF8.GetString(msgBuffer);
        }

        public async Task<(string fileName, byte[] FileBytes)> ReadFileAsync()
        {
            byte[] nameLengthBuffer = new byte[4];
            await ReadExactAsync(nameLengthBuffer, 4);
            int nameLength = BitConverter.ToInt32(nameLengthBuffer, 0);

            byte[] nameBytes = new byte[nameLength];
            await ReadExactAsync(nameBytes, nameLength);
            string fileName = Encoding.UTF8.GetString(nameBytes);

            byte[] fileLengthBuffer = new byte[4];
            await ReadExactAsync(fileLengthBuffer, 4);
            int fileLength = BitConverter.ToInt32(fileLengthBuffer, 0);

            byte[] fileBytes = new byte[fileLength];
            await ReadExactAsync(fileBytes, fileLength);

            return (fileName, fileBytes);
        }
        private async Task ReadExactAsync(byte[] buffer, int count)
        {
            int offset = 0;
            while (offset < count)
            {
                int bytesRead = await _ms.ReadAsync(buffer, offset, count - offset);
                if (bytesRead == 0)
                    return;
                offset += bytesRead;
            }
        }
    }
}
