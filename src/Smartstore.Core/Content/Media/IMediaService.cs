using Newtonsoft.Json;
using Smartstore.Collections;
using Smartstore.Core.Content.Media.Imaging;
using Smartstore.Core.Content.Media.Storage;
using Smartstore.Threading;

namespace Smartstore.Core.Content.Media
{
    #region Enums

    [Flags]
    public enum MediaLoadFlags
    {
        None = 0,
        WithBlob = 1 << 0,
        WithTags = 1 << 1,
        WithTracks = 1 << 2,
        WithFolder = 1 << 3,
        AsNoTracking = 1 << 4,
        Full = WithBlob | WithTags | WithTracks | WithFolder,
        FullNoTracking = Full | AsNoTracking
    }

    public enum SpecialMediaFolder
    {
        AllFiles = -500,
        Trash = -400,
        Orphans = -300,
        TransientFiles = -200,
        UnassignedFiles = -100
    }

    public enum FileHandling
    {
        SoftDelete,
        MoveToRoot,
        Delete
    }

    public enum DuplicateFileHandling
    {
        ThrowError,
        Overwrite,
        Rename
    }

    public enum DuplicateEntryHandling
    {
        ThrowError,
        Overwrite,
        // Folder: Overwrite, File: Rename
        Rename,
        Skip
    }

    public enum MimeValidationType
    {
        NoValidation,
        MimeTypeMustMatch,
        MediaTypeMustMatch
    }

    #endregion

    #region Result & Cargo objects

    public class DuplicateFileInfo
    {
        [JsonProperty("source")]
        public MediaFileInfo SourceFile { get; set; }

        [JsonProperty("dest")]
        public MediaFileInfo DestinationFile { get; set; }

        [JsonProperty("uniquePath")]
        public string UniquePath { get; set; }
    }

    public class FolderOperationResult
    {
        public string Operation { get; set; }
        public MediaFolderInfo Folder { get; set; }
        public DuplicateEntryHandling DuplicateEntryHandling { get; set; }
        public IList<DuplicateFileInfo> DuplicateFiles { get; set; }
    }

    public class FolderDeleteResult
    {
        public HashSet<int> DeletedFolderIds { get; set; } = new HashSet<int>();
        public IList<string> DeletedFileNames { get; set; } = new List<string>();
        public IList<string> TrackedFileNames { get; set; } = new List<string>();
        public IList<string> LockedFileNames { get; set; } = new List<string>();
    }

    public class FileOperationResult
    {
        public string Operation { get; set; }
        public MediaFileInfo SourceFile { get; set; }
        public MediaFileInfo DestinationFile { get; set; }
        public DuplicateFileHandling DuplicateFileHandling { get; set; }
        public bool IsDuplicate { get; set; }
        public string UniquePath { get; set; }
    }

    #endregion

    /// <summary>
    /// Media service interface.
    /// </summary>
    public partial interface IMediaService
    {
        /// <summary>
        /// Gets the underlying storage provider
        /// </summary>
        IMediaStorageProvider StorageProvider { get; }

        /// <summary>
        /// Gets or sets a value indicating whether image post-processing is enabled.
        /// It is recommended to turn this off during long-running processes - like product imports -
        /// as post-processing can heavily decrease processing time.
        /// </summary>
        public bool ImagePostProcessingEnabled { get; set; }

        /// <summary>
        /// Determines the number of files that match the filter criteria in <paramref name="query"/> asynchronously.
        /// </summary>
        /// <param name="query">The query that defines the criteria.</param>
        /// <returns>The number of matching files.</returns>
        Task<int> CountFilesAsync(MediaSearchQuery query);

        /// <summary>
        /// Determines the number of files that match the filter criteria in <paramref name="filter"/> and groups them by folders.
        /// </summary>
        /// <param name="filter">The filter that defines the criteria.</param>
        /// <returns>The grouped file counts (all, trash, unassigned, transient, all folders as dictionary)</returns>
        Task<FileCountResult> CountFilesGroupedAsync(MediaFilesFilter filter);

        /// <summary>
        /// Searches files that match the filter criteria in <paramref name="query"/>.
        /// </summary>
        /// <param name="query">The query that defines the criteria.</param>
        /// <param name="queryModifier">An optional modifier function for the LINQ query that was internally derived from <paramref name="query"/>. Can be null.</param>
        /// <param name="flags">Flags that affect the loading behaviour (eager-loading, tracking etc.)</param>
        /// <returns>The search result.</returns>
        Task<MediaSearchResult> SearchFilesAsync(
            MediaSearchQuery query,
            Func<IQueryable<MediaFile>, IQueryable<MediaFile>> queryModifier,
            MediaLoadFlags flags = MediaLoadFlags.AsNoTracking);

        Task<bool> FileExistsAsync(string path);
        Task<MediaFileInfo> GetFileByPathAsync(string path, MediaLoadFlags flags = MediaLoadFlags.None);
        Task<MediaFileInfo> GetFileByIdAsync(int id, MediaLoadFlags flags = MediaLoadFlags.None);
        Task<MediaFileInfo> GetFileByNameAsync(int folderId, string fileName, MediaLoadFlags flags = MediaLoadFlags.None);
        Task<List<MediaFileInfo>> GetFilesByIdsAsync(int[] ids, MediaLoadFlags flags = MediaLoadFlags.AsNoTracking);
        Task<AsyncOut<string>> CheckUniqueFileNameAsync(string path);
        string CombinePaths(params string[] paths);

        /// <summary>
        /// Tries to find an equal file by comparing the source stream to a list of files.
        /// </summary>
        /// <param name="source">The source stream to find a match for.</param>
        /// <param name="files">The sequence of files to seek within for duplicates.</param>
        /// <param name="leaveOpen">Whether to leave the <paramref name="source"/>source stream</param> open.
        /// <param name="equalFile">A file from the <paramref name="files"/> collection whose content is equal to <paramref name="source"/>.</param>
        /// <returns><c>true</c> when a duplicate file was found, <c>false</c> otherwise.</returns>
        bool FindEqualFile(Stream source, IEnumerable<MediaFile> files, bool leaveOpen, out MediaFile equalFile);

        /// <summary>
        /// Tries to find an equal file by comparing the source stream to a list of files.
        /// </summary>
        /// <param name="source">The source stream to find a match for.</param>
        /// <param name="files">The sequence of files to seek within for duplicates.</param>
        /// <param name="leaveOpen">Whether to leave the <paramref name="source"/>source stream</param> open.
        /// <returns>
        /// <c>true</c> when a duplicate file was found, <c>false</c> otherwise.
        /// If true, a file from the <paramref name="files"/> collection whose content is equal to <paramref name="source"/> is the <c>out</c> parameter.
        /// </returns>
        Task<AsyncOut<MediaFile>> FindEqualFileAsync(
            Stream source,
            IEnumerable<MediaFile> files,
            bool leaveOpen);

        Task<MediaFileInfo> SaveFileAsync(
            string path,
            Stream stream,
            bool isTransient = true,
            DuplicateFileHandling dupeFileHandling = DuplicateFileHandling.ThrowError);

        Task DeleteFileAsync(
            MediaFile file,
            bool permanent, bool
            force = false);

        Task<FileOperationResult> CopyFileAsync(
            MediaFileInfo mediaFile,
            string destinationFileName,
            DuplicateFileHandling dupeFileHandling = DuplicateFileHandling.ThrowError);

        Task<MediaFileInfo> MoveFileAsync(
            MediaFile file,
            string destinationFileName,
            DuplicateFileHandling dupeFileHandling = DuplicateFileHandling.ThrowError);

        Task<MediaFileInfo> ReplaceFileAsync(
            MediaFile file,
            Stream inStream,
            string newFileName);

        Task ReprocessImageAsync(MediaFileInfo fileInfo);

        /// <summary>
        /// Resolves metadata for any files that are missing metadata.
        /// </summary>
        /// <param name="folderPath">Optional folder path. If <c>null</c>, all files with empty names will be processed.</param>
        /// <returns>Number of processed files.</returns>
        Task<int> EnsureMetadataResolvedAsync(string folderPath = null);

        /// <summary>
        /// Saves multiple files batched.
        /// </summary>
        /// <param name="sources">The source files to save. The source streams will be disposed after batch completion.</param>
        /// <param name="destinationFolder">The destination folder to save files to.</param>
        Task<IList<FileBatchResult>> BatchSaveFilesAsync(
            FileBatchSource[] sources,
            MediaFolderNode destinationFolder,
            bool isTransient = true,
            DuplicateFileHandling dupeFileHandling = DuplicateFileHandling.ThrowError,
            CancellationToken cancelToken = default);

        bool FolderExists(string path);
        Task<MediaFolderInfo> CreateFolderAsync(string path);
        Task<MediaFolderInfo> MoveFolderAsync(string path, string destinationPath);

        Task<FolderOperationResult> CopyFolderAsync(
            string path,
            string destinationPath,
            DuplicateEntryHandling dupeEntryHandling = DuplicateEntryHandling.Skip,
            CancellationToken cancelToken = default);

        Task<FolderDeleteResult> DeleteFolderAsync(
            string path,
            FileHandling fileHandling = FileHandling.SoftDelete,
            CancellationToken cancelToken = default);

        MediaFileInfo ConvertMediaFile(MediaFile file);
        MediaFolderInfo ConvertMediaFolder(TreeNode<MediaFolderNode> node);

        string GetUrl(
            MediaFileInfo file,
            ProcessImageQuery query,
            string host = null,
            bool doFallback = true);
    }
}
