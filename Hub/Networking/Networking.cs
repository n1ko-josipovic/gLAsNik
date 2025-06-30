using System.Net.Sockets;
using System.Net;
using System.Text;

namespace Hub.Networking
{
    public static class NetworkUtils
    {
        public static bool IsServerActive(int port)
        {
            using (UdpClient udpClient = new UdpClient())
            {
                try
                {
                    udpClient.EnableBroadcast = true;
                    udpClient.Client.ReceiveTimeout = 10;

                    IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Broadcast, port);
                    byte[] requestData = Encoding.UTF8.GetBytes("gLAsNik");

                    udpClient.Send(requestData, requestData.Length, serverEndPoint);

                    IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, port);
                    byte[] response = udpClient.Receive(ref remoteEndPoint);

                    string responseString = Encoding.UTF8.GetString(response);

                    return responseString.StartsWith("IP:");
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
                {
                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}
