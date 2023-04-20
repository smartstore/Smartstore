using System.Net;
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

            if (connection.RemoteIpAddress != null)
            {
                if (connection.LocalIpAddress != null)
                {
                    // When both local and remote IP are equal, we are local.
                    return connection.RemoteIpAddress.Equals(connection.LocalIpAddress);
                }
                else
                {
                    // If for some reason local IP is null, we are local when remote IP is a loopback.
                    return IPAddress.IsLoopback(connection.RemoteIpAddress);
                }
            }

            // For in memory TestServer or when dealing with default connection info  
            if (connection.RemoteIpAddress == null && connection.LocalIpAddress == null)
            {
                return true;
            }

            return false;
        }
    }
}