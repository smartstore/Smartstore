using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Http;
using Smartstore.IO;

namespace Smartstore.Net
{
    // TODO: (mg) (core) review\rework DownloadManager.
    // DownloadFileAsync is misplaced (HttpRequest dependency). HttpClient should be instantiated per DownloadManager instance.
    public sealed class DownloadManager
    {
        private readonly HttpRequest _httpRequest;

        public DownloadManager(HttpRequest httpRequest)
        {
            Guard.NotNull(httpRequest, nameof(httpRequest));

            _httpRequest = httpRequest;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        /// <summary>
        /// Downloads a single file asynchronously
        /// </summary>
        /// <param name="url">The URL to download the file from (either a fully qualified URL or an app relative/absolute path)</param>
        /// <param name="sendAuthCookie">Specifies whether the authentication cookie should be sent along</param>
        /// <param name="timeout">Timeout in milliseconds</param>
        /// <param name="isLocal">Specifies whether the file is located on the local server</param>
        public async Task<DownloadResponse> DownloadFileAsync(string url, bool sendAuthCookie = false, int? timeout = null, bool isLocal = false)
        {
            Guard.NotEmpty(url, nameof(url));

            url = WebHelper.GetAbsoluteUrl(url, _httpRequest);

            HttpWebRequest req;

            if (isLocal)
            {
                req = await WebHelper.CreateHttpRequestForSafeLocalCallAsync(new Uri(url));
            }
            else
            {
                req = WebRequest.CreateHttp(url);
                req.UserAgent = "Smartstore";
            }

            if (timeout.HasValue)
            {
                req.Timeout = timeout.Value;
            }

            if (sendAuthCookie)
            {
                req.SetAuthenticationCookie(_httpRequest);
                req.SetVisitorCookie(_httpRequest);
            }

            using (var resp = (HttpWebResponse)await req.GetResponseAsync())
            using (var stream = resp.GetResponseStream())
            {
                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    var data = stream.ToByteArray();
                    if (data != null && data.Length != 0)
                    {
                        string fileName = null;

                        var contentDisposition = resp.Headers["Content-Disposition"];
                        if (contentDisposition.HasValue())
                        {
                            var cd = new ContentDisposition(contentDisposition);
                            fileName = cd.FileName;
                        }

                        if (fileName.IsEmpty())
                        {
                            try
                            {
                                var uri = new Uri(url);
                                fileName = Path.GetFileName(uri.LocalPath);
                            }
                            catch
                            {
                            }
                        }

                        return new DownloadResponse(data, fileName, resp.ContentType);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Starts asynchronous download of files and saves them to disk.
        /// </summary>
        /// <param name="items">Items to be downloaded.</param>
        /// <param name="downloadTimeout">The timespan to wait before the download request times out.</param>
        /// <param name="cancelToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        public async Task DownloadFilesAsync(IEnumerable<DownloadManagerItem> items, TimeSpan downloadTimeout = default, CancellationToken cancelToken = default)
        {
            try
            {
                using var client = new HttpClient();

                client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
                {
                    NoCache = true
                };

                client.DefaultRequestHeaders.Add("Connection", "Keep-alive");

                // INFO: the default HttpClient.Timeout is 100 seconds, which has proven to be too small in some situations.
                if (downloadTimeout != default && downloadTimeout.TotalMilliseconds > 0)
                    client.Timeout = downloadTimeout;

                IEnumerable<Task> downloadTasksQuery =
                    from item in items
                    select ProcessUrl(client, item, cancelToken);

                // Now execute the bunch.
                List<Task> downloadTasks = downloadTasksQuery.ToList();

                while (downloadTasks.Count > 0)
                {
                    // Identify the first task that completes.
                    Task firstFinishedTask = await Task.WhenAny(downloadTasks);

                    // Process only once.
                    downloadTasks.Remove(firstFinishedTask);

                    await firstFinishedTask;
                }
            }
            catch (Exception ex)
            {
                Logger?.ErrorsAll(ex);
            }
        }

        private async Task ProcessUrl(HttpClient client, DownloadManagerItem item, CancellationToken cancelToken)
        {
            try
            {
                using var response = await client.GetAsync(item.Url, HttpCompletionOption.ResponseHeadersRead, cancelToken);

                if (response.IsSuccessStatusCode)
                {
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

                    item.Success = !cancelToken.IsCancellationRequested;
                }
                else
                {
                    item.Success = false;
                    item.ErrorMessage = response.StatusCode.ToString();
                }
            }
            catch (Exception ex)
            {
                try
                {
                    item.Success = false;
                    item.ErrorMessage = ex.ToAllMessages();

                    if (ex.InnerException is WebException webExc)
                    {
                        item.ExceptionStatus = webExc.Status;
                    }

                    Logger?.Error(ex, item.ToString());
                }
                catch
                {
                }
            }
        }
    }


    public class DownloadResponse
    {
        public DownloadResponse(byte[] data, string fileName, string contentType)
        {
            Guard.NotNull(data, nameof(data));

            Data = data;
            FileName = fileName;
            ContentType = contentType;
        }

        /// <summary>
        /// The downloaded file's byte array
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// The file name
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// The mime type opf the downloaded file
        /// </summary>
        public string ContentType { get; private set; }
    }

    public class DownloadManagerItem
    {
        /// <summary>
        /// Identifier of the item
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// New identifier of the downloaded item
        /// </summary>
        public int NewId { get; set; }

        /// <summary>
        /// Display order of the item
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Download URL
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Absolute path for saving the item
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// File name without file extension
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Mime type of the item
        /// </summary>
        public string MimeType { get; set; }

        /// <summary>
        /// Whether the operation succeeded
        /// </summary>
        public bool? Success { get; set; }

        /// <summary>
        /// Exception status if an exception of type WebException occurred
        /// </summary>
        public WebExceptionStatus ExceptionStatus { get; set; }

        /// <summary>
        /// Error message
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Whether the operation timed out
        /// </summary>
        public bool HasTimedOut
        {
            get
            {
                return ExceptionStatus == WebExceptionStatus.Timeout || ExceptionStatus == WebExceptionStatus.RequestCanceled;
            }
        }

        /// <summary>
        /// Use dictionary for any required extra data
        /// </summary>
        public IDictionary<string, object> CustomProperties { get; set; }

        public override string ToString()
        {
            var str = "Result: {0} {1}{2}, {3}".FormatInvariant(
                Success,
                ExceptionStatus.ToString(),
                ErrorMessage.HasValue() ? " ({0})".FormatInvariant(ErrorMessage) : "",
                Path);

            return str;
        }
    }
}