using System.Net;
using System.Net.Http;
using Smartstore.IO;

namespace Smartstore.Net.Http
{
    public sealed class DownloadManager : Disposable
    {
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
                using var response = await HttpClient.GetAsync(item.Url, HttpCompletionOption.ResponseHeadersRead, cancelToken);

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
                        var extension = MimeTypes.MapMimeTypeToExtension(contentType).NullEmpty() ?? "jpg";

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
                    item.ErrorMessage = response.ReasonPhrase.HasValue()
                        ? $"{response.StatusCode} ({response.ReasonPhrase})"
                        : response.StatusCode.ToString();
                }
            }
            catch (Exception ex)
            {
                item.Success = false;
                item.ErrorMessage = ex.ToAllMessages();

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
}