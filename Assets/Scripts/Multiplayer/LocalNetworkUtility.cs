using System.Net;
using System.Net.Sockets;

namespace Rocket.Multiplayer
{
    public static class LocalNetworkUtility
    {
        public static string GetLanAddress()
        {
            string hostName = Dns.GetHostName();
            IPAddress[] addresses = Dns.GetHostAddresses(hostName);
            for (int i = 0; i < addresses.Length; i++)
            {
                if (addresses[i].AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(addresses[i]))
                {
                    return addresses[i].ToString();
                }
            }

            return "127.0.0.1";
        }
    }
}
