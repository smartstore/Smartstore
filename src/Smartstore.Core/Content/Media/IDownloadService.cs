using Smartstore.Core.Checkout.Orders;

namespace Smartstore.Core.Content.Media
{
    /// <summary>
    /// Contract for downloadable files service.
    /// </summary>
    public interface IDownloadService
    {
        /// <summary>
        /// Inserts a new download entity and assigns <paramref name="stream"/> as downloadable file.
        /// </summary>
        /// <param name="download">The new entity to insert.</param>
        /// <param name="stream">The source file stream</param>
        /// <param name="fileName">Name of file.</param>
        /// <returns>The inserted <see cref="MediaFileInfo"/> object instance.</returns>
        Task<MediaFileInfo> InsertDownloadAsync(Download download, Stream stream, string fileName);

        /// <summary>
        /// Gets a value indicating whether download is allowed for given <paramref name="orderItem"/>.
        /// </summary>
        /// <param name="orderItem">Order item to check</param>
        /// <returns>True if download is allowed; otherwise, false.</returns>
        bool IsDownloadAllowed(OrderItem orderItem);

        /// <summary>
        /// Opens the download file's stream for reading.
        /// </summary>
        Stream OpenDownloadStream(Download download);

        /// <summary>
        /// Opens the download file's stream for reading.
        /// </summary>
        Task<Stream> OpenDownloadStreamAsync(Download download);
    }

    public static class IDownloadServiceExtensions
    {
        public static bool IsLicenseDownloadAllowed(this IDownloadService service, OrderItem orderItem)
        {
            return service.IsDownloadAllowed(orderItem) && orderItem?.LicenseDownloadId > 0;
        }
    }
}