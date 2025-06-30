using Hub.Client.Net.IO;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Hub.Client.MVVM.ViewModel.ClientViewModel;

namespace Hub.Client.Net
{
    class Server
    {
        TcpClient _client;
        public PacketReader PacketReader;
        public event Action connectedEvent;
        public event Action messageReceivedEvent;
        public event Action fileReceivedEvent;
        public event Action fileDownloadEvent;
        public event Action userDisconnectEvent;
        public event Action disconnectedEvent;
        private bool _isRunning = false;

        public Server() => _client = new TcpClient();

        public bool ServerExists = false;

        public async Task<string> FindServer()
        {
            using (UdpClient udpClient = new UdpClient())
            {
                udpClient.EnableBroadcast = true;
                IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Broadcast, 7750);
                byte[] requestData = Encoding.UTF8.GetBytes("gLAsNik");
                await udpClient.SendAsync(requestData, requestData.Length, serverEndPoint);
                UdpReceiveResult result = await udpClient.ReceiveAsync();
                string response = Encoding.UTF8.GetString(result.Buffer);
                if (response.StartsWith("IP:"))
                {
                    return response.Replace("IP:", "");
                }
                return null;
            }
        }

        public async void ConnectToServer(string username)
        {
            if (!_client.Connected)
            {
                string serverIP = await FindServer();
                if (serverIP == null)
                {
                    ServerExists = false;
                    return;
                }
                else
                {
                    ServerExists = true;
                }
                try
                {
                    _client.Connect(serverIP, 7751);
                    PacketReader = new PacketReader(_client.GetStream());
                    if (!string.IsNullOrEmpty(username))
                    {
                        var connectPacket = new PacketBuilder();
                        connectPacket.WriteOpCode(0);
                        connectPacket.WriteMessage(username);
                        _client.Client.Send(connectPacket.GetPacketBytes());
                    }
                    _isRunning = true;
                    ReadPackets();
                }
                catch (Exception ex)
                {
                    disconnectedEvent?.Invoke();
                }
            }
        }

        public void DisconnectFromServer()
        {
            if (_client != null && _client.Connected)
            {
                try
                {
                    var disconnectPacket = new PacketBuilder();
                    disconnectPacket.WriteOpCode(10);
                    _client.Client.Send(disconnectPacket.GetPacketBytes());

                    _isRunning = false;

                    _client.GetStream().Close();
                    _client.Close();

                    disconnectedEvent?.Invoke();
                }
                catch (Exception ex) { }
                finally
                {
                    _client = new TcpClient();
                }
            }
        }

        public void ReadPackets()
        {
            Task.Run(() =>
            {
                while (_isRunning)
                {
                    try
                    {
                        if (!_client.Connected)
                        {
                            _isRunning = false;
                            disconnectedEvent?.Invoke();
                            break;
                        }

                        var opCode = PacketReader.ReadByte();
                        switch (opCode)
                        {
                            case 0:
                                connectedEvent?.Invoke();
                                break;
                            case 5:
                                messageReceivedEvent?.Invoke();
                                break;
                            case 6:
                                fileReceivedEvent?.Invoke();
                                break;
                            case 7:
                                fileDownloadEvent?.Invoke();
                                break;
                            case 10:
                                userDisconnectEvent?.Invoke();
                                break;
                            case 15:
                                DisconnectFromServer();
                                break;
                            default:
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _isRunning = false;
                        disconnectedEvent?.Invoke();
                        break;
                    }
                }
            });
        }

        public async Task SendMessageToServerAsync(string message)
        {
            if (_client != null && _client.Connected)
            {
                var messagePacket = new PacketBuilder();
                messagePacket.WriteOpCode(5);
                messagePacket.WriteMessage(message);

                byte[] packet = messagePacket.GetPacketBytes();

                NetworkStream stream = _client.GetStream();
                await stream.WriteAsync(packet, 0, packet.Length);
                await stream.FlushAsync();
            }
        }

        public async Task SendFileToServerAsync(string fileName, byte[] fileBytes)
        {
            if (_client != null && _client.Connected)
            {
                var filePacket = new PacketBuilder();
                filePacket.WriteOpCode(6);
                filePacket.WriteFile(fileName, fileBytes);

                byte[] packet = filePacket.GetPacketBytes();

                NetworkStream stream = _client.GetStream();
                await stream.WriteAsync(packet, 0, packet.Length);
                await stream.FlushAsync();
            }
        }

        public async Task RequestFileAsync(string fileName)
        {
            if (_client != null && _client.Connected)
            {
                var fileRequestPacket = new PacketBuilder();
                fileRequestPacket.WriteOpCode(7);
                fileRequestPacket.WriteMessage(fileName);

                byte[] packet = fileRequestPacket.GetPacketBytes();

                NetworkStream stream = _client.GetStream();
                await stream.WriteAsync(packet, 0, packet.Length);
                await stream.FlushAsync();
            }
        }
    }
}