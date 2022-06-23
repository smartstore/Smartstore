using Smartstore.Core.Content.Media;
using Smartstore.Data;
using Smartstore.IO;
using Smartstore.Net.Http;

namespace Smartstore.Core.DataExchange.Import
{
    /// <summary>
    /// Helper to import images (like product and category images) in a performant way.
    /// Images are downloaded if <see cref="DownloadManagerItem.Url"/> is specified and
    /// they will not be imported if they already exist (duplicate check).
    /// </summary>
    public interface IMediaImporter
    {
        /// <summary>
        /// A handler that is called when reportable events such as errors occur.
        /// </summary>
        Action<ImportMessage, DownloadManagerItem> MessageHandler { get; set; }

        /// <summary>
        /// Creates a download manager item.
        /// </summary>
        /// <param name="imageDirectory">
        /// Directory with images to be imported. 
        /// In that case, the images in the import file are referenced by file path (absolute or relative).
        /// </param>
        /// <param name="downloadDirectory">Directory in which the downloaded images will be saved.</param>
        /// <param name="entity">The entity to which the file belongs. E.g. <see cref="Product"/>, <see cref="Category"/> etc.</param>
        /// <param name="urlOrPath">URL or path to download.</param>
        /// <param name="state">Any state to identify the source later after batch save. E.g. <see cref="ImportRow{T}"/> etc.</param>
        /// <param name="displayOrder">Display order of the item.</param>
        /// <param name="fileNameLookup">Lookup for existing file names to avoid name duplicates.</param>
        /// <returns>Download manager item or <c>null</c> if none could be created.</returns>
        DownloadManagerItem CreateDownloadItem(
            IDirectory imageDirectory,
            IDirectory downloadDirectory,
            BaseEntity entity,
            string urlOrPath,
            object state = null,
            int displayOrder = 0,
            HashSet<string> fileNameLookup = null);

        /// <summary>
        /// Imports a batch of product images.
        /// </summary>
        /// <param name="items">Collection of product images to be imported. Images are downloaded if <see cref="DownloadManagerItem.Url"/> is specified.</param>
        /// <param name="duplicateFileHandling">A value indicating how to handle duplicate images.</param>
        /// <returns>Number of state entries written to the database.</returns>
        Task<int> ImportProductImagesAsync(
            DbContextScope scope,
            ICollection<DownloadManagerItem> items,
            DuplicateFileHandling duplicateFileHandling = DuplicateFileHandling.Rename,
            CancellationToken cancelToken = default);

        /// <summary>
        /// Imports a batch of category images.
        /// </summary>
        /// <param name="items">Collection of category images to be imported. Images are downloaded if <see cref="DownloadManagerItem.Url"/> is specified.</param>
        /// <param name="duplicateFileHandling">A value indicating how to handle duplicate images.</param>
        /// <returns>Number of state entries written to the database.</returns>
        Task<int> ImportCategoryImagesAsync(
            DbContextScope scope,
            ICollection<DownloadManagerItem> items,
            DuplicateFileHandling duplicateFileHandling = DuplicateFileHandling.Rename,
            CancellationToken cancelToken = default);

        /// <summary>
        /// Imports a batch of customer avatars.
        /// </summary>
        /// <param name="items">Collection of customer avatars to be imported. Images are downloaded if <see cref="DownloadManagerItem.Url"/> is specified.</param>
        /// <param name="duplicateFileHandling">A value indicating how to handle duplicate images.</param>
        /// <returns>Number of state entries written to the database.</returns>
        Task<int> ImportCustomerAvatarsAsync(
            DbContextScope scope,
            ICollection<DownloadManagerItem> items,
            DuplicateFileHandling duplicateFileHandling = DuplicateFileHandling.Rename,
            CancellationToken cancelToken = default);
    }
}
