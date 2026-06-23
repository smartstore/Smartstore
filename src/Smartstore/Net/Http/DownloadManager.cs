using System.Net;
using System.Net.Http;
using Smartstore.IO;
using Smartstore.Net;

namespace Smartstore.Net.Http;

public sealed class DownloadManager : Disposable
{
    private const int MaxRedirects = 5;

    public DownloadManager(HttpClient httpClient)
    {
        HttpClient = Guard.NotNull(httpClient, nameof(httpClient));
    }

    public HttpClient HttpClient { get; }

    /// <summary>
    /// Downloads a file asynchronously and saves it to disk.
    /// </summary>
    /// <param name="item">Information about the file to download.</param>
    /// <param name="cancelToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    public Task DownloadFileAsync(DownloadManagerItem item, CancellationToken cancelToken = default)
    {
        return ProcessUrl(item, cancelToken);
    }

    /// <summary>
    /// Starts asynchronous download of multiple files and saves them to disk.
    /// </summary>
    /// <param name="items">Information about the files to download.</param>
    /// <param name="cancelToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    public async Task DownloadFilesAsync(IEnumerable<DownloadManagerItem> items, CancellationToken cancelToken = default)
    {
        var downloadTasks = items
            .Select(x => ProcessUrl(x, cancelToken))
            .ToList();

        while (downloadTasks.Count > 0)
        {
            // Identify the first task that completes.
            Task firstFinishedTask = await Task.WhenAny(downloadTasks);

            // Process only once.
            downloadTasks.Remove(firstFinishedTask);

            await firstFinishedTask;
        }
    }

    private async Task ProcessUrl(DownloadManagerItem item, CancellationToken cancelToken)
    {
        try
        {
            if (!Uri.TryCreate(item.Url, UriKind.Absolute, out var uri))
            {
                item.Success = false;
                item.ErrorMessage = "Invalid URL.";
                return;
            }

            // SSRF guard: resolve the host and reject private/loopback/link-local addresses
            // before issuing the request. Redirects are followed manually so each hop is re-validated.
            if (!uri.Scheme.EqualsNoCase("http") && !uri.Scheme.EqualsNoCase("https") ||
                !await IPAddressUtils.IsPublicHostAsync(uri.DnsSafeHost, cancelToken))
            {
                item.Success = false;
                item.ErrorMessage = "The URL was blocked by the security policy.";
                return;
            }

            var redirectCount = 0;
            HttpResponseMessage response = null;

            while (true)
            {
                response?.Dispose();
                response = await HttpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancelToken);

                if (!response.StatusCode.IsRedirect() ||
                    response.Headers.Location is not { } location ||
                    redirectCount++ >= MaxRedirects)
                {
                    break;
                }

                // Re-validate the redirect target before following it.
                var redirectUri = location.IsAbsoluteUri ? location : new Uri(uri, location);
                if (!redirectUri.Scheme.EqualsNoCase("http") && !redirectUri.Scheme.EqualsNoCase("https") ||
                    !await IPAddressUtils.IsPublicHostAsync(redirectUri.DnsSafeHost, cancelToken))
                {
                    response.Dispose();
                    item.Success = false;
                    item.ErrorMessage = "The URL was blocked by the security policy.";
                    return;
                }

                uri = redirectUri;
            }

            using (response)
            {
                item.StatusCode = response.StatusCode;

                if (response.IsSuccessStatusCode)
                {
                    if (item.MimeType.IsEmpty())
                    {
                        item.MimeType = MimeTypes.MapNameToMimeType(item.FileName);
                    }

                    var contentType = response.Content.Headers.ContentType?.MediaType;
                    if (contentType.HasValue() && !contentType.EqualsNoCase(item.MimeType))
                    {
                        // Update mime type and local path.
                        var extension = MimeTypes.MapMimeTypeToExtension(contentType)
                            .OrDefault(Path.GetExtension(item.Url)!)
                            .OrDefault("jpg");

                        item.MimeType = contentType;
                        item.Path = Path.ChangeExtension(item.Path, extension.EnsureStartsWith('.'));
                    }

                    using var source = await response.Content.ReadAsStreamAsync(cancelToken);
                    using var target = File.Open(item.Path, FileMode.Create);

                    await source.CopyToAsync(target, cancelToken);

                    item.Success = true;
                }
                else
                {
                    item.Success = false;
                    // Avoid surfacing status + reason verbatim — it acts as a port-scan oracle for internal services.
                    item.ErrorMessage = $"Remote server returned HTTP {(int)response.StatusCode}.";
                }
            }
        }
        catch (Exception ex)
        {
            item.Success = false;
            item.ErrorMessage = ex.Message;

            if (ex.InnerException is WebException webExc)
            {
                item.ExceptionStatus = webExc.Status;
            }
        }
    }

    protected override void OnDispose(bool disposing)
    {
        if (disposing)
        {
            // Don't dispose HttpClient, let HttpClientFactory do the heavy stuff.
        }
    }
}