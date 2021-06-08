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
using Smartstore.Http;
using Smartstore.IO;

namespace Smartstore.Net
{
    public sealed class DownloadManager : Disposable
    {
        private readonly TimeSpan _downloadTimeout;
        private readonly TimeSpan _maxCachingAge;
        private HttpClient _httpClient;

        /// <summary>
        /// DownloadManager ctor.
        /// </summary>
        /// <param name="downloadTimeout">
        /// Specifies the timespan to wait before the download request times out.
        /// The default value is 100 seconds (which has proven to be too short in some situations, e.g. during imports).
        /// </param>
        /// <param name="maxCachingAge">
        /// Specifies the maximum age that the HTTP client is willing to accept a response.
        /// By default the client accepts no cached response at all.
        /// </param>
        public DownloadManager(TimeSpan downloadTimeout = default, TimeSpan maxCachingAge = default)
        {
            _downloadTimeout = downloadTimeout;
            _maxCachingAge = maxCachingAge;
        }

        private HttpClient HttpClient
        {
            get
            {
                if (_httpClient == null)
                {
                    // See also "socket exhausting": https://www.aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/
                    _httpClient = new HttpClient();

                    if (_downloadTimeout != default && _downloadTimeout.TotalMilliseconds > 0)
                    {
                        _httpClient.Timeout = _downloadTimeout;
                    }

                    var cache = new CacheControlHeaderValue();

                    if (_maxCachingAge != default)
                    {
                        cache.Public = true;
                        cache.MaxAge = _maxCachingAge;
                    }
                    else
                    {
                        cache.NoCache = true;
                    }

                    _httpClient.DefaultRequestHeaders.CacheControl = cache;
                    _httpClient.DefaultRequestHeaders.Add("Connection", "Keep-alive");
                }

                return _httpClient;
            }
        }

        /// <summary>
        /// Downloads a single file asynchronously.
        /// </summary>
        /// <param name="process">Function to process the download response.</param>
        /// <param name="url">The URL to download the file from (either a fully qualified URL or an app relative/absolute path).</param>
        /// <param name="httpRequest">HTTP request.</param>
        /// <param name="timeout">Timeout in milliseconds.</param>
        /// <param name="sendAuthCookie">Specifies whether the authentication cookie should be sent along.</param>
        /// <param name="isLocal">A value indicating whether the file is located on the local server.</param>
        public static async Task<TResult> DownloadFileAsync<TResult>(
            Func<DownloadResponse, Task<TResult>> process,
            string url,
            HttpRequest httpRequest,
            int? timeout = null,
            bool sendAuthCookie = false,
            bool isLocal = false)
        {
            // TODO: (mg) (core) I don't like the DownloadFileAsync method's signature at all! TBD with MC.
            // RE: should be removed (disturbs here). Was once intended as a synchronous method. Callers must be refactored in this regard anyway.
            // They should use DownloadFilesAsync or setup own download request (in case of CreatePdfInvoiceAttachmentAsync).
            Guard.NotEmpty(url, nameof(url));

            url = WebHelper.GetAbsoluteUrl(url, httpRequest);
            HttpWebRequest request;
            TResult result = default;

            if (isLocal)
            {
                request = await WebHelper.CreateHttpRequestForSafeLocalCallAsync(new Uri(url));
            }
            else
            {
                request = WebRequest.CreateHttp(url);
                request.UserAgent = "Smartstore";
            }

            if (timeout.HasValue)
            {
                request.Timeout = timeout.Value;
            }

            if (sendAuthCookie)
            {
                request.SetAuthenticationCookie(httpRequest);
                request.SetVisitorCookie(httpRequest);
            }

            using (var response = (HttpWebResponse)await request.GetResponseAsync())
            using (var stream = response.GetResponseStream())
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string fileName = null;
                    var contentDisposition = response.Headers["Content-Disposition"];

                    if (contentDisposition.HasValue())
                    {
                        fileName = new ContentDisposition(contentDisposition).FileName;
                    }

                    if (fileName.IsEmpty())
                    {
                        fileName = WebHelper.GetFileNameFromUrl(url);
                    }

                    var arg = new DownloadResponse
                    {
                        Stream = stream,
                        FileName = fileName,
                        ContentType = response.ContentType,
                        ContentLength = response.ContentLength
                    };

                    result = await process(arg);
                }
            }

            return result;
        }

        /// <summary>
        /// Starts asynchronous download of multiple files and saves them to disk.
        /// </summary>
        /// <param name="items">Items to be downloaded.</param>
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
                _httpClient?.Dispose();
                _httpClient = null;
            }
        }
    }


    public class DownloadResponse
    {
        /// <summary>
        /// The stream that is used to read the body of the response from the server.
        /// </summary>
        /// <remarks>
        /// <see cref="Stream.Length"/> not supported (throws NotSupportedException). Use <see cref="ContentLength"/> instead.
        /// </remarks>
        public Stream Stream { get; init; }

        /// <summary>
        /// The file name.
        /// </summary>
        public string FileName { get; init; }

        /// <summary>
        /// The content type of the response.
        /// </summary>
        public string ContentType { get; init; }

        /// <summary>
        /// The number of bytes returned by the request.
        /// </summary>
        public long ContentLength { get; init; }

        public override string ToString()
        {
            return $"{FileName.NaIfEmpty()} ({ContentType}, {ContentLength} bytes)";
        }
    }

    public class DownloadManagerItem : ICloneable<DownloadManagerItem>, IEquatable<DownloadManagerItem>
    {
        /// <summary>
        /// Identifier of the item.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// New identifier of the downloaded item.
        /// </summary>
        public int NewId { get; set; }

        /// <summary>
        /// Display order of the item.
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Download URL.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Absolute path for saving the item.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// File name without file extension.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Mime type of the item.
        /// </summary>
        public string MimeType { get; set; }

        /// <summary>
        /// A value indicating whether the operation succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The response status code.
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// Exception status if an exception of type WebException occurred.
        /// </summary>
        public WebExceptionStatus ExceptionStatus { get; set; }

        /// <summary>
        /// Error message.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// A value indicating whether the operation timed out.
        /// </summary>
        public bool HasTimedOut
        {
            get => ExceptionStatus == WebExceptionStatus.Timeout || ExceptionStatus == WebExceptionStatus.RequestCanceled;
        }

        /// <summary>
        /// Dictionary to be used for any custom data.
        /// </summary>
        public IDictionary<string, object> CustomProperties { get; set; }

        object ICloneable.Clone() => Clone();
        public DownloadManagerItem Clone()
        {
            return new DownloadManagerItem
            {
                Id = Id,
                NewId = NewId,
                DisplayOrder = DisplayOrder,
                Url = Url,
                Path = Path,
                FileName = FileName,
                MimeType = MimeType,
                Success = Success,
                StatusCode = StatusCode,
                ExceptionStatus = ExceptionStatus,
                ErrorMessage = ErrorMessage,
                CustomProperties = CustomProperties != null
                    ? new Dictionary<string, object>(CustomProperties)
                    : null
            };
        }

        public override bool Equals(object other)
        {
            return ((IEquatable<DownloadManagerItem>)this).Equals(other as DownloadManagerItem);
        }

        bool IEquatable<DownloadManagerItem>.Equals(DownloadManagerItem other)
        {
            if (other == null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return Url.EqualsNoCase(other.Url);
        }

        public override int GetHashCode()
            => Url?.GetHashCode() ?? 0;

        public override string ToString()
        {
            return "Result: {0} {1}{2}, {3}".FormatInvariant(
                Success,
                ExceptionStatus.ToString(),
                ErrorMessage.HasValue() ? $" ({ErrorMessage})" : "",
                Path);
        }
    }
}