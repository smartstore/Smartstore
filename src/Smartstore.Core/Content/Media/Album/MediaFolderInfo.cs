using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.EntityFrameworkCore;
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
        }

        [JsonIgnore]
        public TreeNode<MediaFolderNode> Node { get; }

        [JsonProperty("filesCount")]
        public int FilesCount => Node.Value.FilesCount;

        public static implicit operator TreeNode<MediaFolderNode>(MediaFolderInfo folderInfo)
        {
            return folderInfo.Node;
        }

        [JsonProperty("id")]
        public int Id => Node.Value.Id;

        [JsonProperty("path", NullValueHandling = NullValueHandling.Ignore)]
        public string Path => Node.Value.Path;

        #region IDirectory

        /// <inheritdoc/>
        [JsonIgnore]
        public bool Exists => Node.Value.Id > 0;

        /// <inheritdoc/>
        [JsonIgnore]
        bool IFileInfo.IsDirectory => true;

        /// <inheritdoc/>
        [JsonIgnore]
        DateTimeOffset IFileInfo.LastModified => DateTime.UtcNow;

        /// <inheritdoc/>
        [JsonIgnore]
        long IFileInfo.Length => -1;

        /// <inheritdoc/>
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name => Node.Value.Name;

        /// <inheritdoc/>
        [JsonIgnore]
        string IFileInfo.PhysicalPath => Node.Value.Path;

        /// <inheritdoc/>
        Stream IFileInfo.CreateReadStream() => throw new NotSupportedException();


        /// <inheritdoc/>
        [JsonIgnore]
        IFileSystem IFileEntry.FileSystem => throw new NotSupportedException();

        /// <inheritdoc/>
        [JsonIgnore]
        string IFileEntry.SubPath => Node.Value.Path;

        /// <inheritdoc/>
        bool IFileEntry.IsSymbolicLink(out string finalPhysicalPath)
        {
            finalPhysicalPath = null;
            return false;
        }


        /// <inheritdoc/>
        [JsonIgnore]
        bool IDirectory.IsRoot => false;

        /// <inheritdoc/>
        [JsonIgnore]
        public IDirectory Parent => Node.Parent == null ? null : _mediaService.ConvertMediaFolder(Node.Parent);

        /// <inheritdoc/>
        void IFileEntry.Delete()
            => ((IFileEntry)this).DeleteAsync().Await();

        /// <inheritdoc/>
        async Task IFileEntry.DeleteAsync()
        {
            await _mediaService.DeleteFolderAsync(Path, FileHandling.Delete);
        }

        /// <inheritdoc/>
        void IDirectory.Create()
            => ((IDirectory)this).CreateAsync().Await();

        /// <inheritdoc/>
        async Task IDirectory.CreateAsync()
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
        async Task<IDirectory> IDirectory.CreateSubdirectoryAsync(string path)
        {
            CheckExists();

            path = PathUtility.Combine(Path, PathUtility.NormalizeRelativePath(path));
            var node = _folderService.GetNodeByPath(path);

            return node != null 
                ? _mediaService.ConvertMediaFolder(node) 
                : await _mediaService.CreateFolderAsync(path);
        }

        /// <inheritdoc/>
        void IFileEntry.MoveTo(string newPath)
            => ((IDirectory)this).MoveToAsync(newPath).Await();

        /// <inheritdoc/>
        async Task IFileEntry.MoveToAsync(string newPath)
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
        public async IAsyncEnumerable<IFileEntry> EnumerateEntriesAsync(string pattern = "*", bool deep = false)
        {
            await foreach (var entry in EnumerateFilesAsync(pattern, deep))
            {
                yield return entry;
            }

            await foreach (var entry in EnumerateDirectoriesAsync(pattern, deep))
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
                return wildcard == null ? true : wildcard.IsMatch(node.Value.Name);
            }
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<IDirectory> EnumerateDirectoriesAsync(string pattern = "*", bool deep = false)
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
        public async IAsyncEnumerable<IFile> EnumerateFilesAsync(string pattern = "*", bool deep = false)
        {
            CheckExists();

            var result = await _mediaService.SearchFilesAsync(CreateSearchQuery(pattern, deep));

            await foreach (var file in result.OfType<IFile>())
            {
                yield return file;
            }
        }

        /// <inheritdoc/>
        public long CountFiles(string pattern = "*", bool deep = true)
            => CountFilesAsync(pattern, deep).Await();

        /// <inheritdoc/>
        public async Task<long> CountFilesAsync(string pattern = "*", bool deep = true)
        {
            CheckExists();
            return await _mediaService.CountFilesAsync(CreateSearchQuery(pattern, deep));
        }

        /// <inheritdoc/>
        public long GetDirectorySize(string pattern = "*", bool deep = true)
            => GetDirectorySizeAsync(pattern, deep).Await();

        /// <inheritdoc/>
        public async Task<long> GetDirectorySizeAsync(string pattern = "*", bool deep = true)
        {
            CheckExists();
            
            return await _mediaSearcher.SearchFiles(CreateSearchQuery(pattern, deep))
                .SourceQuery
                .SumAsync(x => x.Size);
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
