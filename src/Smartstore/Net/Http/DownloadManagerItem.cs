using System.Net;
using Smartstore.Domain;

namespace Smartstore.Net.Http
{
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
        /// File name.
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
            => ExceptionStatus == WebExceptionStatus.Timeout || ExceptionStatus == WebExceptionStatus.RequestCanceled;

        /// <summary>
        /// The entity to which the file belongs. E.g. <see cref="Product"/>, <see cref="Category"/> etc.
        /// </summary>
        public BaseEntity Entity { get; set; }

        /// <summary>
        /// Any state to identify the source later after batch save. E.g. <see cref="ImportRow{T}"/> etc.
        /// </summary>
        public object State { get; set; }

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
                Entity = Entity,
                State = State
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
            return
                @"Success:  {0}
                Web Status: {1}
                Path:       {2}
                Error:      {3}.".FormatInvariant(Success, ExceptionStatus.ToString(), ErrorMessage, Path);
        }
    }
}
