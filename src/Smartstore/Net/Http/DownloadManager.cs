using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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