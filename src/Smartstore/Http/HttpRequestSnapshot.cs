#nullable enable

using System.Net;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Net.Http.Headers;

namespace Smartstore.Http;

/// <summary>
/// A JSON-serializable, point-in-time copy of an HTTP request.
/// </summary>
/// <remarks>
/// Contains values only, without retaining a request or its services. Create a new snapshot after
/// changing request data or the current user, and treat a captured snapshot as read-only when
/// sharing it with background operations. Derived properties
/// are excluded from JSON and reconstructed from the serialized data.
/// </remarks>
public sealed class HttpRequestSnapshot
{
    private Dictionary<string, string[]> _headers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets or sets the incoming HTTP method, such as GET or POST.</summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>Gets or sets the request scheme, such as http or https.</summary>
    public string Scheme { get; set; } = string.Empty;

    /// <summary>Gets or sets the request host, including its port if specified.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>Gets or sets the application's base path, or an empty string for the root.</summary>
    public string PathBase { get; set; } = string.Empty;

    /// <summary>Gets or sets the request path relative to <see cref="PathBase"/>.</summary>
    /// <remarks>Uses the representation of <see cref="HttpRequest.Path"/>, not an encoded URL string.</remarks>
    public string Path { get; set; } = string.Empty;

    /// <summary>Gets or sets the original query string, including the leading question mark.</summary>
    public string QueryString { get; set; } = string.Empty;

    /// <summary>Gets or sets the incoming HTTP protocol, such as HTTP/1.1 or HTTP/2.</summary>
    public string Protocol { get; set; } = string.Empty;

    /// <summary>Gets or sets the original raw request target, preserving its encoding and query string.</summary>
    /// <remarks>This is the raw path and query received by the server (PathBase + Path + QueryString), rather than an absolute URL.</remarks>
    public string RawUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the captured action, controller, and route values.</summary>
    /// <remarks>
    /// The route value dictionary is copied when capturing the request. This property is
    /// <see langword="null"/> and omitted from JSON when no action is available, such as before
    /// routing or for a non-MVC endpoint.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RouteInfo? RouteInfo { get; set; }

    /// <summary>Gets or sets the captured request headers, preserving multiple values per header.</summary>
    /// <remarks>
    /// Header names remain case-insensitive after JSON deserialization. Capturing a request copies
    /// the value arrays as well. Cookies and authorization headers may contain sensitive data;
    /// forward only the headers needed by the destination and do not log the complete snapshot.
    /// </remarks>
    public Dictionary<string, string[]> Headers
    {
        get => _headers;
        set => _headers = value is null
            ? new(StringComparer.OrdinalIgnoreCase)
            : new(value, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Gets or sets the originating request's trace identifier.</summary>
    public string TraceIdentifier { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC time at which the snapshot was captured.</summary>
    public DateTime CapturedOnUtc { get; set; }

    /// <summary>Gets or sets the incoming request user's name, or <see langword="null"/> if unavailable.</summary>
    /// <remarks>The name is descriptive only; it does not represent authentication or authorization.</remarks>
    public string? UserName { get; set; }

    /// <summary>Gets the absolute, encoded request URL, including scheme, host, base path, path, and query.</summary>
    /// <remarks>Derived from the parsed URL components; <see cref="RawUrl"/> preserves the original request target.</remarks>
    [JsonIgnore]
    public string Url => UriHelper.BuildAbsolute(
        Scheme,
        new HostString(Host),
        new PathString(PathBase),
        new PathString(Path),
        new QueryString(QueryString));

    /// <summary>Gets the captured User-Agent header, or <see langword="null"/> if absent.</summary>
    [JsonIgnore]
    public string? UserAgent => GetHeader(HeaderNames.UserAgent);

    /// <summary>Gets the captured Cookie header, joining multiple values with semicolons.</summary>
    [JsonIgnore]
    public string? Cookie => GetHeader(HeaderNames.Cookie, "; ");

    /// <summary>Gets the captured Referer header, or <see langword="null"/> if absent.</summary>
    [JsonIgnore]
    public string? Referrer => GetHeader(HeaderNames.Referer);

    /// <summary>Reads a captured header by its case-insensitive name.</summary>
    /// <param name="name">The header name.</param>
    /// <param name="separator">The separator used to join multiple values.</param>
    /// <returns>The combined header value, or <see langword="null"/> if absent.</returns>
    public string? GetHeader(string name, string separator = ",")
    {
        Guard.NotEmpty(name);
        Guard.NotNull(separator);

        return _headers.TryGetValue(name, out var values) && values.Length > 0
            ? string.Join(separator, values)
            : null;
    }

    /// <summary>Reads and URL-decodes an individual request cookie.</summary>
    /// <param name="name">The case-sensitive cookie name.</param>
    /// <returns>The decoded value, or <see langword="null"/> if the cookie is absent.</returns>
    public string? GetCookieValue(string name)
    {
        Guard.NotEmpty(name);

        if (_headers.TryGetValue(HeaderNames.Cookie, out var values))
        {
            foreach (var cookie in CookieHeaderValue.ParseList(values))
            {
                if (string.Equals(cookie.Name.Value, name, StringComparison.Ordinal))
                {
                    return Uri.UnescapeDataString(HeaderUtilities.RemoveQuotes(cookie.Value).ToString());
                }
            }
        }

        return null;
    }

    /// <summary>Builds a Cookie header for forwarding all or selected captured cookies.</summary>
    /// <param name="cookieNames">The cookie names to forward, or no names to forward the original header.</param>
    /// <returns>A formatted header, or <see langword="null"/> if no matching cookies exist.</returns>
    /// <remarks>
    /// Mirrors the request cookie propagation handler: selected cookies are decoded and placed in
    /// a private container using the captured host and base path. The container is never shared
    /// between requests. The returned header belongs to the source application and must only be
    /// forwarded to an intended destination; this method does not authorize a target URL.
    /// </remarks>
    public string? GetCookieHeader(params string[] cookieNames)
    {
        if (cookieNames is null || cookieNames.Length == 0)
        {
            return Cookie;
        }

        var container = new CookieContainer();

        foreach (string name in cookieNames)
        {
            string? value = GetCookieValue(name);
            if (value is not null)
            {
                container.Add(new System.Net.Cookie(name, value, PathBase.NullEmpty(), new HostString(Host).Host));
            }
        }

        return container.Count > 0 ? container.GetCookieHeader(new Uri(Url)) : null;
    }
}
