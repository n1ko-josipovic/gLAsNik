using Server.Net.IO;
using System.ComponentModel.DataAnnotations;
using System.Net.Sockets;

namespace Server
{
    class Client
    {
        private static int _lastId = 0;
        private static readonly object _idLock = new object();

        public static Action<Client> OnClientConnected;
        public string Username { get; private set; }
        public string UID { get; private set; }
        public TcpClient ClientSocket { get; private set; }

        private PacketReader _packetReader;

        public Client(TcpClient client)
        {
            ClientSocket = client;
            UID = GenerateShortUID();
            _packetReader = new PacketReader(ClientSocket.GetStream());

            Task.Run(HandleClientAsync);
        }

        private string GenerateShortUID()
        {
            lock (_idLock)
            {
                _lastId++;
                return _lastId.ToString("X4");
            }
        }

        private async Task HandleClientAsync()
        {
            try
            {
                var opCode = _packetReader.ReadByte();

                Username = await _packetReader.ReadMessageAsync();

                OnClientConnected?.Invoke(this);
                Program.WriteToConsole($"[{DateTime.Now}]: {Username} - {UID} se povezao!");

                Task.Run(() => Process());
            }
            catch (Exception ex)
            {
                Program.WriteToConsole($"Greška pri povezivanju");
                Disconnect();
            }
        }

        private async Task Process()
        {
            while (true)
            {
                try
                {
                    var opCode = _packetReader.ReadByte();
                    switch (opCode)
                    {
                        case 5:
                            var message = (await _packetReader.ReadMessageAsync()).TrimEnd();
                            Program.WriteToConsole($"[{DateTime.Now}] [{Username}]:\n{message}");
                            Program.BroadcastMessage($"{Username}\n{message}");
                            break;

                        case 6:
                            var (fileName, fileBytes) = await _packetReader.ReadFileAsync();
                            Program.WriteToConsole($"[{DateTime.Now}] {Username} je poslao datoteku: {fileName}");

                            fileName = Path.GetFileNameWithoutExtension(fileName) + "-" + DateTime.Now.ToString("HHmmss") + Path.GetExtension(fileName);


                            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "gLAsNik");
                            if (!Directory.Exists(dir)) {
                                Directory.CreateDirectory(dir);
                            }
                            
                            string savePath = Path.Combine(dir, fileName);
                            await File.WriteAllBytesAsync(savePath, fileBytes);

                            FileStore.FileRegistry[fileName] = savePath;

                            Program.BroadcastFile($"{Username}\n{fileName}");
                            break;

                        case 7:
                            string requestedfileName = await _packetReader.ReadMessageAsync();
                            if (FileStore.FileRegistry.TryGetValue(requestedfileName, out string filePath))
                            {
                                if (File.Exists(filePath)) { 
                                    byte[] requestedFileBytes = await File.ReadAllBytesAsync(filePath);
                                    var packet = new PacketBuilder();
                                    packet.WriteOpCode(7);
                                    packet.WriteFile(requestedfileName, requestedFileBytes);
                                    await ClientSocket.GetStream().WriteAsync(packet.GetPacketBytes());

                                    Program.WriteToConsole($"Datoteka '{requestedfileName}' poslana klijentu {Username}");
                                }
                                else
                                {
                                    Program.WriteToConsole($"Datoteka '{requestedfileName}' postoji u registru, ali ne i na disku.");
                                }
                            }
                            else
                            {
                                Program.WriteToConsole($"Datoteka '{requestedfileName}' nije pronađena u registru za klijenta {Username}");
                            }
                            break;

                        default:
                            break;
                    }
                }
                catch (Exception)
                {
                    Disconnect();
                    break;
                }
            }
        }

        private void Disconnect()
        {
            Program.WriteToConsole($"[{DateTime.Now}]: {Username} - {UID} se isključio!");
            Program.BroadcastDisconnect(UID);
            ClientSocket?.Close();
        }
    }
}
