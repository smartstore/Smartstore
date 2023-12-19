#nullable enable

using Smartstore.Collections;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
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
        Action<ImportMessage, DownloadManagerItem>? MessageHandler { get; set; }

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
        /// <param name="maxFileNameLength">Max length of generated file names. <c>int.MaxValue</c> to not truncate file names.</param>
        /// <returns>Download manager item or <c>null</c> if none could be created.</returns>
        DownloadManagerItem? CreateDownloadItem(
            IDirectory imageDirectory,
            IDirectory downloadDirectory,
            BaseEntity entity,
            string? urlOrPath,
            object? state = null,
            int displayOrder = 0,
            HashSet<string>? fileNameLookup = null,
            int maxFileNameLength = int.MaxValue);

        /// <summary>
        /// Generic method that imports a batch of media files assigned to entities with a 1:1 relationship to media files.
        /// </summary>
        /// <typeparam name="T">The type of entity for which the file are imported.</typeparam>
        /// <param name="items">Collection of files to be imported. Files are downloaded if <see cref="DownloadManagerItem.Url"/> is specified.</param>
        /// <param name="album">Media album to be assigned to the imported files.</param>
        /// <param name="assignMediaFileHandler">Callback function to assign the imported media file to the designated entity.</param>
        /// <param name="checkAssignedMediaFileHandler">Callback function to check if the file to be imported is already assigned to the designated entity.</param>
        /// <param name="checkExistingFile">Defines whether to check for existing files in the same media folder.</param>
        /// <param name="duplicateFileHandling">A value indicating how to handle duplicate images.</param>
        /// <returns>Number of actually imported files</returns>
        Task<int> ImportMediaFilesAsync<T>(
            DbContextScope scope,
            ICollection<DownloadManagerItem>? items,
            MediaFolderNode album,
            Action<DownloadManagerItem, int> assignMediaFileHandler,
            Func<DownloadManagerItem, Stream, Task<bool>> checkAssignedMediaFileHandler,
            bool checkExistingFile,
            DuplicateFileHandling duplicateFileHandling = DuplicateFileHandling.Rename,
            CancellationToken cancelToken = default) where T : BaseEntity;

        /// <summary>
        /// Generic method that imports a batch of media files assigned to entities with a 1:n relationship to media files (e.g. <see cref="Product"/> --&gt; <see cref="ProductMediaFile"/>).
        /// </summary>
        /// <param name="items">Collection of files to be imported. Files are downloaded if <see cref="DownloadManagerItem.Url"/> is specified.</param>
        /// <param name="album">Media album to be assigned to the imported files.</param>
        /// <param name="existingFiles">Map of already assigned media files.</param>
        /// <param name="assignMediaFileHandler">
        ///     Callback function to assign imported files to designated entities which derive from IMediaFile (e.g. <see cref="ProductMediaFile"/>).
        ///     Returns the assigned media file so it can be added to the existing files dictionary (so that it won't be imported again).
        /// </param>
        /// <param name="duplicateFileHandling">A value indicating how to handle duplicate images.</param>
        /// <returns>Number of actually imported files</returns>
        Task<int> ImportMediaFilesManyAsync(
            DbContextScope scope,
            ICollection<DownloadManagerItem>? items,
            MediaFolderNode album,
            Multimap<int, IMediaFile> existingFiles,
            Func<MediaFile, DownloadManagerItem, IMediaFile> assignMediaFileHandler,
            DuplicateFileHandling duplicateFileHandling = DuplicateFileHandling.Rename,
            CancellationToken cancelToken = default);

        /// <summary>
        /// Imports a batch of product images.
        /// </summary>
        /// <param name="items">Collection of product images to be imported. Images are downloaded if <see cref="DownloadManagerItem.Url"/> is specified.</param>
        /// <param name="duplicateFileHandling">A value indicating how to handle duplicate images.</param>
        /// <returns>Number of new images.</returns>
        Task<int> ImportProductImagesAsync(
            DbContextScope scope,
            ICollection<DownloadManagerItem>? items,
            DuplicateFileHandling duplicateFileHandling = DuplicateFileHandling.Rename,
            CancellationToken cancelToken = default);

        /// <summary>
        /// Imports a batch of product images.
        /// </summary>
        /// <param name="product">Product entity.</param>
        /// <param name="items">
        /// Collection of product images to be imported.
        /// Existing files can be overwritten by passing a dictionary{string},{oblect} for <see cref="FileBatchSource.State"/>
        /// using key "MediaFileId" and value <see cref="MediaFile"/> identifier.
        /// </param>
        /// <param name="duplicateFileHandling">A value indicating how to handle duplicate images.</param>
        /// <returns>Number of new images.</returns>
        Task<int> ImportProductImagesAsync(
            Product product,
            ICollection<FileBatchSource>? items,
            DuplicateFileHandling duplicateFileHandling = DuplicateFileHandling.Rename,
            CancellationToken cancelToken = default);

        /// <summary>
        /// Imports a batch of category images.
        /// </summary>
        /// <param name="items">Collection of category images to be imported. Images are downloaded if <see cref="DownloadManagerItem.Url"/> is specified.</param>
        /// <param name="duplicateFileHandling">A value indicating how to handle duplicate images.</param>
        /// <returns>Number of new images.</returns>
        Task<int> ImportCategoryImagesAsync(
            DbContextScope scope,
            ICollection<DownloadManagerItem>? items,
            DuplicateFileHandling duplicateFileHandling = DuplicateFileHandling.Rename,
            CancellationToken cancelToken = default);

        /// <summary>
        /// Imports a batch of customer avatars.
        /// </summary>
        /// <param name="items">Collection of customer avatars to be imported. Images are downloaded if <see cref="DownloadManagerItem.Url"/> is specified.</param>
        /// <param name="duplicateFileHandling">A value indicating how to handle duplicate images.</param>
        /// <returns>Number of new images.</returns>
        Task<int> ImportCustomerAvatarsAsync(
            DbContextScope scope,
            ICollection<DownloadManagerItem>? items,
            DuplicateFileHandling duplicateFileHandling = DuplicateFileHandling.Rename,
            CancellationToken cancelToken = default);
    }
}
