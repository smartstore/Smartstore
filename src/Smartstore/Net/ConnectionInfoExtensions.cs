using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Http;

namespace Smartstore
{
    public static class ConnectionInfoExtensions
    {
        /// <summary>
        /// Checks whether the current request originates from a local computer.
        /// </summary>
        public static bool IsLocal(this ConnectionInfo connection)
        {
            Guard.NotNull(connection);

            var remoteAddress = connection.RemoteIpAddress;
            if (remoteAddress == null)
            {
                return false;
            }

            // If the RemoteAddress is an IPv6 address, the method converts
            // it to an IPv4-mapped address using the MapToIPv4 method and
            // checks if that address is a loopback address as well.
            var isLocalAddress = IPAddress.IsLoopback(remoteAddress);
            if (!isLocalAddress && remoteAddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                //return remoteAddress.Equals(localAddress);
                isLocalAddress = IPAddress.IsLoopback(remoteAddress.MapToIPv4());
            }

            return isLocalAddress;
        }
    }
}