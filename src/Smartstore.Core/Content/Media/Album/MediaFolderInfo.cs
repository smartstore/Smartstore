using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using Smartstore.Collections;
using Smartstore.IO;
using Smartstore.Utilities;

namespace Smartstore.Core.Content.Media
{
    public partial class MediaFolderInfo : IDirectory
    {
        private readonly IMediaService _mediaService;
        private readonly IMediaSearcher _mediaSearcher;
        private readonly IFolderService _folderService;
        private readonly MediaExceptionFactory _exceptionFactory;
        private readonly DateTimeOffset _now;

        public MediaFolderInfo(
            TreeNode<MediaFolderNode> node,
            IMediaService mediaService,
            IMediaSearcher mediaSearcher,
            IFolderService folderService,
            MediaExceptionFactory exceptionFactory)
        {
            Node = node;
            _mediaService = mediaService;
            _mediaSearcher = mediaSearcher;
            _folderService = folderService;
            _exceptionFactory = exceptionFactory;
            _now = DateTimeOffset.UtcNow;
        }

        [IgnoreDataMember]
        public TreeNode<MediaFolderNode> Node { get; }

        [JsonProperty("filesCount")]
        public int FilesCount => Node.Value.FilesCount;

        public static implicit operator TreeNode<MediaFolderNode>(MediaFolderInfo folderInfo)
        {
            return folderInfo.Node;
        }

        [JsonProperty("id")]
        public int Id => Node.Value.Id;

        [JsonProperty("parentId")]
        public int? ParentId => Node.Value.ParentId;

        [JsonProperty("hasChildren")]
        public bool HasChildren => Node.HasChildren;

        [JsonProperty("path", NullValueHandling = NullValueHandling.Ignore)]
        public string Path => Node.Value.Path;

        #region IDirectory

        /// <inheritdoc/>
        [IgnoreDataMember]
        public bool Exists => Node.Value.Id > 0;

        /// <inheritdoc/>
        [IgnoreDataMember]
        bool IFileInfo.IsDirectory => true;

        /// <inheritdoc/>
        [IgnoreDataMember]
        DateTimeOffset IFileEntry.CreatedOn => _now;

        /// <inheritdoc/>
        [IgnoreDataMember]
        DateTimeOffset IFileInfo.LastModified => _now;

        /// <inheritdoc/>
        [IgnoreDataMember]
        long IFileInfo.Length => -1;

        /// <inheritdoc/>
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name => Node.Value.Name;

        /// <inheritdoc/>
        [IgnoreDataMember]
        string IFileInfo.PhysicalPath => Node.Value.Path;

        /// <inheritdoc/>
        Stream IFileInfo.CreateReadStream() => throw new NotSupportedException();


        /// <inheritdoc/>
        [IgnoreDataMember]
        IFileSystem IFileEntry.FileSystem => throw new NotSupportedException();

        /// <inheritdoc/>
        [IgnoreDataMember]
        string IFileEntry.SubPath => Node.Value.Path;

        /// <inheritdoc/>
        bool IFileEntry.IsSymbolicLink(out string finalPhysicalPath)
        {
            finalPhysicalPath = null;
            return false;
        }


        /// <inheritdoc/>
        [IgnoreDataMember]
        bool IDirectory.IsRoot => false;

        /// <inheritdoc/>
        [IgnoreDataMember]
        public IDirectory Parent => Node.Parent == null ? null : _mediaService.ConvertMediaFolder(Node.Parent);

        /// <inheritdoc/>
        void IFileEntry.Delete()
            => ((IFileEntry)this).DeleteAsync().Await();

        /// <inheritdoc/>
        async Task IFileEntry.DeleteAsync(CancellationToken cancelToken)
        {
            await _mediaService.DeleteFolderAsync(Path, FileHandling.Delete, cancelToken);
        }

        /// <inheritdoc/>
        void IDirectory.Create()
            => ((IDirectory)this).CreateAsync().Await();

        /// <inheritdoc/>
        async Task IDirectory.CreateAsync(CancellationToken cancelToken)
        {
            if (!Exists)
            {
                await _mediaService.CreateFolderAsync(Path);
            }
        }

        /// <inheritdoc/>
        IDirectory IDirectory.CreateSubdirectory(string path)
            => ((IDirectory)this).CreateSubdirectoryAsync(path).Await();

        /// <inheritdoc/>
        async Task<IDirectory> IDirectory.CreateSubdirectoryAsync(string path, CancellationToken cancelToken)
        {
            CheckExists();

            path = PathUtility.Join(Path, path);
            var node = _folderService.GetNodeByPath(path);

            return node != null
                ? _mediaService.ConvertMediaFolder(node)
                : await _mediaService.CreateFolderAsync(path);
        }

        /// <inheritdoc/>
        void IFileEntry.MoveTo(string newPath)
            => ((IDirectory)this).MoveToAsync(newPath).Await();

        /// <inheritdoc/>
        async Task IFileEntry.MoveToAsync(string newPath, CancellationToken cancelToken)
        {
            Guard.NotNull(newPath, nameof(newPath));

            CheckExists();

            await _mediaService.MoveFolderAsync(Path, newPath);
        }

        /// <inheritdoc/>
        public IEnumerable<IFileEntry> EnumerateEntries(string pattern = "*", bool deep = false)
        {
            return EnumerateFiles(pattern, deep)
                .OfType<IFileEntry>()
                .Concat(EnumerateDirectories(pattern, deep));
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<IFileEntry> EnumerateEntriesAsync(
            string pattern = "*",
            bool deep = false,
            [EnumeratorCancellation] CancellationToken cancelToken = default)
        {
            await foreach (var entry in EnumerateFilesAsync(pattern, deep, cancelToken))
            {
                yield return entry;
            }

            await foreach (var entry in EnumerateDirectoriesAsync(pattern, deep, cancelToken))
            {
                yield return entry;
            }
        }

        /// <inheritdoc/>
        public IEnumerable<IDirectory> EnumerateDirectories(string pattern = "*", bool deep = false)
        {
            CheckExists();

            Wildcard wildcard = pattern.IsEmpty() || pattern == "*"
                ? null
                : new Wildcard(pattern);

            var folders = deep ? Node.FlattenNodes(false) : Node.Children;
            var result = folders
                .Where(MatchesPattern)
                .Select(x => _mediaService.ConvertMediaFolder(x))
                .OfType<IDirectory>();

            return result;

            bool MatchesPattern(TreeNode<MediaFolderNode> node)
            {
                return wildcard == null || wildcard.IsMatch(node.Value.Name);
            }
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<IDirectory> EnumerateDirectoriesAsync(
            string pattern = "*",
            bool deep = false,
            CancellationToken cancelToken = default)
            => EnumerateDirectories(pattern, deep).ToAsyncEnumerable();

        /// <inheritdoc/>
        public IEnumerable<IFile> EnumerateFiles(string pattern = "*", bool deep = false)
        {
            CheckExists();

            var files = _mediaSearcher.SearchFiles(CreateSearchQuery(pattern, deep))
                .Load()
                .AsEnumerable()
                .Select(_mediaService.ConvertMediaFile)
                .OfType<IFile>();

            return files;
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<IFile> EnumerateFilesAsync(
            string pattern = "*",
            bool deep = false,
            [EnumeratorCancellation] CancellationToken cancelToken = default)
        {
            CheckExists();

            var result = await _mediaService.SearchFilesAsync(CreateSearchQuery(pattern, deep));

            foreach (var file in result.OfType<IFile>())
            {
                yield return file;
            }
        }

        /// <inheritdoc/>
        public long CountFiles(string pattern = "*", bool deep = true)
            => CountFilesAsync(pattern, deep).Await();

        /// <inheritdoc/>
        public async Task<long> CountFilesAsync(string pattern = "*", bool deep = true, CancellationToken cancelToken = default)
        {
            CheckExists();
            return await _mediaService.CountFilesAsync(CreateSearchQuery(pattern, deep));
        }

        /// <inheritdoc/>
        public long GetDirectorySize(string pattern = "*", bool deep = true)
            => GetDirectorySizeAsync(pattern, deep).Await();

        /// <inheritdoc/>
        public async Task<long> GetDirectorySizeAsync(string pattern = "*", bool deep = true, CancellationToken cancelToken = default)
        {
            CheckExists();

            return await _mediaSearcher.SearchFiles(CreateSearchQuery(pattern, deep))
                .SourceQuery
                .SumAsync(x => x.Size, cancelToken);
        }

        private MediaSearchQuery CreateSearchQuery(string pattern, bool deep)
        {
            return new MediaSearchQuery
            {
                FolderId = Node.Value.Id,
                Term = pattern,
                DeepSearch = deep,
            };
        }

        private void CheckExists()
        {
            if (!Exists)
            {
                throw _exceptionFactory.FolderNotFound(Path);
            }
        }

        #endregion
    }
}
