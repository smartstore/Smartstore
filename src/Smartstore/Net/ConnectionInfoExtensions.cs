using System.Net;
using Microsoft.AspNetCore.Http;

namespace Smartstore
{
    public static class ConnectionInfoExtensions
    {
        const string NullIPv6 = "::1";

        /// <summary>
        /// Checks whether the current request originates from a local computer.
        /// </summary>
        public static bool IsLocal(this ConnectionInfo connection)
        {
            Guard.NotNull(connection, nameof(connection));

            var remoteAddress = connection.RemoteIpAddress;
            if (remoteAddress == null || remoteAddress.ToString() == NullIPv6)
            {
                return true;
            }

            // We have a remote address set up.
            // Is local the same as remote, then we are local.
            var localAddress = connection.LocalIpAddress;
            if (localAddress != null && localAddress.ToString() != NullIPv6)
            {
                return remoteAddress.Equals(localAddress);
            }

            // Else we are remote if the remote IP address is not a loopback address
            return IPAddress.IsLoopback(remoteAddress);
        }
    }
}