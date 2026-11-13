#nullable enable

using System.Net.Http;
using System.Net.Mime;
using Smartstore.Http;
using Smartstore.Net;

namespace Smartstore.Pdf;

/// <summary>Contains a downloaded PDF and its suggested file name.</summary>
public sealed class PdfDownloadResult
{
    /// <summary>Gets the PDF content.</summary>
    public required byte[] Buffer { get; init; }

    /// <summary>Gets the suggested file name.</summary>
    public required string FileName { get; init; }
}

/// <summary>Downloads PDFs using captured request cookies.</summary>
/// <remarks>
/// Does not access the current HTTP context or retain scoped dependencies. Resolve the client in
/// the scope performing the download. Register it using AddPdfHttpClient to disable shared cookie
/// storage and automatic redirects.
/// </remarks>
public sealed class PdfHttpClient
{
    private readonly HttpClient _httpClient;

    /// <summary>Creates a PDF client using the supplied HTTP client.</summary>
    /// <param name="httpClient">The HTTP client configured for PDF downloads.</param>
    public PdfHttpClient(HttpClient httpClient)
    {
        Guard.NotNull(httpClient);

        _httpClient = httpClient;
    }

    /// <summary>Downloads a PDF from the captured request's origin.</summary>
    /// <param name="url">The absolute URL of the PDF document generation endpoint.</param>
    /// <param name="snapshot">The originating request's captured data.</param>
    /// <param name="cancelToken">The cancellation token.</param>
    /// <returns>The PDF content and its suggested file name.</returns>
    /// <remarks>
    /// Forwards the identity and visitor cookies from the snapshot. The target must have the same
    /// scheme, host, and port as the source request. URL generation and localized error reporting
    /// belong to the caller.
    /// </remarks>
    public async Task<PdfDownloadResult> GetPdfAsync(
        string url,
        HttpRequestSnapshot snapshot,
        CancellationToken cancelToken = default)
    {
        Guard.NotEmpty(url);
        Guard.NotNull(snapshot);

        if (!Uri.TryCreate(url, UriKind.Absolute, out var target))
        {
            throw new ArgumentException("An absolute PDF URL is required.", nameof(url));
        }

        var source = new Uri(snapshot.Url);

        // These cookies identify the originating user. Only send them to the application
        // from which they were captured.
        if (target.Scheme != source.Scheme
            || !target.IdnHost.EqualsNoCase(source.IdnHost)
            || target.Port != source.Port)
        {
            throw new ArgumentException(
                "The PDF URL must have the same origin as the captured request.",
                nameof(url));
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, target);
        request.Headers.Accept.ParseAdd(MediaTypeNames.Application.Pdf);

        string? cookieHeader = snapshot.GetCookieHeader(CookieNames.Identity, CookieNames.Visitor);

        if (cookieHeader.HasValue())
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
        }

        // Buffer the response within SendAsync so HttpClient.Timeout covers the body as well.
        using var response = await _httpClient.SendAsync(request, cancelToken);
        response.EnsureSuccessStatusCode();

        string? mimeType = response.Content.Headers.ContentType?.MediaType;

        if (!mimeType.EqualsNoCase(MediaTypeNames.Application.Pdf))
        {
            throw new InvalidOperationException(
                $"Expected a PDF response from '{url}', received '{mimeType}'.");
        }

        byte[] buffer = await response.Content.ReadAsByteArrayAsync(cancelToken);

        if (buffer.Length == 0)
        {
            throw new InvalidOperationException($"The PDF response from '{url}' was empty.");
        }

        var disposition = response.Content.Headers.ContentDisposition;
        string? fileName = (disposition?.FileNameStar.NullEmpty() ?? disposition?.FileName)
            ?.Trim('"')
            .NullEmpty();

        return new PdfDownloadResult
        {
            Buffer = buffer,
            FileName = fileName ?? WebHelper.GetFileNameFromUrl(url) ?? "document.pdf"
        };
    }
}
