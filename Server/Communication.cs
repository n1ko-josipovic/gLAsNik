using System.Net.Sockets;
using System.Net;
using System.Text;
using Server.Net.IO;

namespace Server;

public partial class Program
{    
    static void StartUdpListener()
    {
        _udpListener = new UdpClient(7750);
        WriteToConsole("Poslužitelj je uspješno pokrenut!");

        while (_isRunning)
        {
            try
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] requestData = _udpListener.Receive(ref remoteEP);
                string message = Encoding.UTF8.GetString(requestData);

                if (message == "gLAsNik")
                {
                    byte[] responseData = Encoding.UTF8.GetBytes("IP:" + GetLocalIPAddress());
                    _udpListener.Send(responseData, responseData.Length, remoteEP);
                }
            }
            catch (Exception){}
        }
    }
    static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        throw new Exception("Nema mrežnih adaptera s IPv4 adresom u sustavu!");
    }

    static void StartTcpListener()
    {
        _tcpListener = new TcpListener(IPAddress.Any, 7751);
        _tcpListener.Start();

        while (_isRunning)
        {
            try
            {
                var client = new Client(_tcpListener.AcceptTcpClient());
            }
            catch (Exception ex)
            {
                WriteToConsole($"Greška pri prihvaćanju novog klijenta.");
            }
        }
    }
    

    static void BroadcastConnection()
    {
        if (_users == null) return;

        foreach (var user in _users)
        {
            foreach (var usr in _users)
            {
                var broadcastPacket = new PacketBuilder();
                broadcastPacket.WriteOpCode(0);
                broadcastPacket.WriteMessage(usr.Username);
                broadcastPacket.WriteMessage(usr.UID.ToString());
                user.ClientSocket.Client.Send(broadcastPacket.GetPacketBytes());
            }
        }
    }
    public static void BroadcastMessage(string message)
    {
        if (_users == null) return;

        foreach (var user in _users)
        {
            var broadcastPacket = new PacketBuilder();
            broadcastPacket.WriteOpCode(5);
            broadcastPacket.WriteMessage(message);
            user.ClientSocket.Client.Send(broadcastPacket.GetPacketBytes());
        }
    }

    public static void BroadcastFile(string fileInfo)
    {
        if (_users == null) return;

        foreach (var user in _users)
        {
            var broadcastPacket = new PacketBuilder();
            broadcastPacket.WriteOpCode(6);
            broadcastPacket.WriteMessage(fileInfo);
            user.ClientSocket.Client.Send(broadcastPacket.GetPacketBytes());
        }
    }

    public static void BroadcastDisconnect(string uid)
    {
        if (_users == null) return;

        var disconnectedUser = _users.FirstOrDefault(x => x.UID.ToString() == uid);
        if (disconnectedUser != null)
        {
            _users.Remove(disconnectedUser);

            foreach (var user in _users)
            {
                var broadcastPacket = new PacketBuilder();
                broadcastPacket.WriteOpCode(10);
                broadcastPacket.WriteMessage(uid);
                user.ClientSocket.Client.Send(broadcastPacket.GetPacketBytes());
            }

            BroadcastMessage($"{disconnectedUser.Username} se isključio!");
        }
    }

    public static void BroadcastEnd()
    {
        if (_users == null) return;

        BroadcastMessage($"Poslužitelj je prestao s radom.");

        foreach (var user in _users)
        {
            var broadcastPacket = new PacketBuilder();
            broadcastPacket.WriteOpCode(15);
            broadcastPacket.WriteMessage("");
            user.ClientSocket.Client.Send(broadcastPacket.GetPacketBytes());
        }
    }
}
