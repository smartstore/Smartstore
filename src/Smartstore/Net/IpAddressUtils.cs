using System.Net;
using System.Net.Sockets;

namespace Smartstore.Net;

public static class IpAddressUtils
{
    /// <summary>
    /// Returns <c>true</c> when <paramref name="address"/> is publicly routable,
    /// i.e. is NOT loopback, link-local, RFC-1918 private, or IPv6 ULA.
    /// </summary>
    public static bool IsPublic(this IPAddress address)
    {
        Guard.NotNull(address);

        // Normalise IPv4-mapped IPv6 (e.g. ::ffff:192.168.1.1) to plain IPv4.
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address))
        {
            return false;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var b = address.GetAddressBytes();
            // fc00::/7  — ULA (unique local, private equivalent)
            if ((b[0] & 0xFE) == 0xFC) return false;
            // fe80::/10 — link-local
            if (b[0] == 0xFE && (b[1] & 0xC0) == 0x80) return false;
            return true;
        }

        // IPv4 private / reserved ranges.
        var ip = address.GetAddressBytes();
        return !(
            ip[0] == 10 ||                                      // 10.0.0.0/8
            (ip[0] == 172 && ip[1] is >= 16 and <= 31) ||       // 172.16.0.0/12
            (ip[0] == 192 && ip[1] == 168) ||                   // 192.168.0.0/16
            (ip[0] == 169 && ip[1] == 254)                      // 169.254.0.0/16 — link-local / cloud metadata
        );
    }

    /// <summary>
    /// Resolves <paramref name="host"/> via DNS and returns <c>true</c> only when every
    /// resolved address passes <see cref="IsPublic"/>. Returns <c>false</c> when the host
    /// cannot be resolved or yields no addresses.
    /// </summary>
    public static async Task<bool> IsPublicHostAsync(string host, CancellationToken cancelToken = default)
    {
        Guard.NotEmpty(host);

        try
        {
            var addresses = IPAddress.TryParse(host, out var literal)
                ? [literal]
                : await Dns.GetHostAddressesAsync(host, cancelToken);

            // Every resolved address must be public; one private address is enough to block.
            return addresses.Length > 0 && addresses.All(a => a.IsPublic());
        }
        catch
        {
            return false;
        }
    }
}
