#nullable enable

using Microsoft.Net.Http.Headers;
using System.Net;
using System.Net.Sockets;
using Smartstore.Core.Common.Services;

namespace Smartstore.Core.Web;

/// <summary>
/// Provides information about a web client, including user agent, client identifier, IP address, referrer URL, and
/// GEO country details.
/// </summary>
public abstract class WebClientInfo
{
    /// <summary>
    /// Gets the user agent string associated with the current request or client context.
    /// </summary>
    public abstract string? UserAgent { get; }

    /// <summary>
    /// A unique client identifier based on the current IP address and user agent string.
    /// </summary>
    /// <remarks>The returned identifier can be used to distinguish clients based on their network address and
    /// browser information. If either the IP address is not set or the user agent is missing, the method returns
    /// null.</remarks>
    /// <returns>A lowercase hash string representing the client identifier if both the IP address and user agent are available;
    /// otherwise, null.</returns>
    public abstract string? ClientIdent { get; }

    /// <summary>
    /// Retrieves the IP address of the remote client making the current HTTP request.
    /// </summary>
    /// <remarks>If the remote address is an IPv6 loopback address, it is mapped to the IPv4 loopback address.
    /// If the HTTP context or request is unavailable, the method returns <see cref="System.Net.IPAddress.None"/>.</remarks>
    /// <returns>An <see cref="System.Net.IPAddress"/> representing the remote client's IP address. Returns <see
    /// cref="System.Net.IPAddress.None"/> if the address cannot be determined.</returns>
    public abstract IPAddress IpAddress { get; }

    /// <summary>
    /// Retrieves the URL of the referring resource for the current HTTP request, if available.
    /// </summary>
    public abstract Uri? UrlReferrer { get; }

    /// <summary>
    /// Retrieves country information associated with the current IP address, if available.
    /// </summary>
    /// <returns>A <see cref="GeoCountryInfo"/> object containing country details for the current IP address; or <see
    /// langword="null"/> if the IP address is not set or no country information is found.</returns>
    public abstract GeoCountryInfo? Country { get; }
}

internal sealed class DefaultWebClientInfo : WebClientInfo
{
    private readonly Lazy<string?> _clientIdentAccessor;
    private readonly Lazy<IPAddress> _ipAddressAccessor;
    private readonly Lazy<Uri?> _urlReferrerAccessor;
    private readonly Lazy<GeoCountryInfo?> _countryAccessor;

    private DefaultWebHelper _helper;
    private bool _isDisposed;

    public DefaultWebClientInfo(DefaultWebHelper helper)
    {
        _helper = helper;

        _clientIdentAccessor = new(GetClientIdent, false);
        _ipAddressAccessor = new(GetIpAddress, false);
        _urlReferrerAccessor = new(GetUrlReferrer, false);
        _countryAccessor = new(GetCountry, false);
    }

    internal void SetDisposed()
    {
        _isDisposed = true;
        _helper = null!;
    }

    public override string? UserAgent
    {
        get => field ??= _helper.HttpContext?.Request?.UserAgent();
    }

    public override string? ClientIdent
    {
        get => field ??= _clientIdentAccessor.Value;
    }

    public override IPAddress IpAddress
    {
        get => field ??= _ipAddressAccessor.Value;
    }

    public override Uri? UrlReferrer
    {
        get => field ??= _urlReferrerAccessor.Value;
    }

    public override GeoCountryInfo? Country
    {
        get => field ??= _countryAccessor.Value;
    }

    private string? GetClientIdent()
    {
        CheckNotDisposed();

        var ipAddress = IpAddress;
        var userAgent = UserAgent.EmptyNull();

        if (ipAddress != IPAddress.None && userAgent.HasValue())
        {
            var headers = _helper.HttpContext?.Request?.Headers;

            // Include Accept-Language: highly user-specific locale setting, virtually
            // always present in real browsers and differs between users even behind NAT.
            var acceptLanguage = headers?[HeaderNames.AcceptLanguage].ToString().EmptyNull() ?? string.Empty;

            // Include Sec-CH-UA-Platform: OS platform hint sent by all Chromium-based
            // browsers. Adds entropy for users sharing IP + browser version.
            var platform = headers?["Sec-CH-UA-Platform"].ToString().EmptyNull() ?? string.Empty;

            return (ipAddress.ToString() + userAgent + acceptLanguage + platform)
                .XxHash64()?.ToLowerInvariant();
        }

        return null;
    }

    private Uri? GetUrlReferrer()
    {
        CheckNotDisposed();

        var referrer = _helper.HttpContext?.Request?.UrlReferrer();
        if (referrer.HasValue() && Uri.TryCreate(referrer, UriKind.RelativeOrAbsolute, out var urlReferrer))
        {
            return urlReferrer;
        }

        return null;
    }

    private IPAddress GetIpAddress()
    {
        CheckNotDisposed();

        var httpContext = _helper.HttpContext;
        if (httpContext?.Request == null)
        {
            return IPAddress.None;
        }

        if (httpContext.Connection?.RemoteIpAddress is IPAddress ip)
        {
            if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (ip == IPAddress.IPv6Loopback)
                {
                    ip = IPAddress.Loopback;
                }
                else if (ip.IsIPv4MappedToIPv6)
                {
                    // Only unmap if it truly is an IPv4-mapped address (::ffff:x.x.x.x),
                    // otherwise MapToIPv4() would silently truncate to the last 4 bytes,
                    // causing unrelated IPv6 clients to collide on the same fake IPv4.
                    ip = ip.MapToIPv4();
                }
                // Native IPv6: keep as-is to preserve full address uniqueness.
            }

            return ip!;
        }

        return IPAddress.None;
    }

    private GeoCountryInfo? GetCountry()
    {
        CheckNotDisposed();

        var ipAddress = IpAddress;
        if (ipAddress != IPAddress.None)
        {
            return _helper.LookupCountry(ipAddress);
        }

        return null;
    }

    private void CheckNotDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(
                nameof(DefaultWebHelper),
                $"{nameof(WebClientInfo)} is no longer available because the owning {nameof(DefaultWebHelper)} has been disposed.");
        }
    }
}