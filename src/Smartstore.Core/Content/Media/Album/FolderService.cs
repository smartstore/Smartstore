﻿using Smartstore.Caching;
using Smartstore.Collections;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Content.Media
{
    [Important(HookImportance.Essential)]
    public partial class FolderService(IAlbumRegistry albumRegistry, SmartDbContext db, ICacheManager cache) : AsyncDbSaveHook<MediaFolder>, IFolderService
    {
        internal static TimeSpan FolderTreeCacheDuration = TimeSpan.FromHours(3);

        internal const string FolderTreeKey = "mediafolder:tree";

        private readonly IAlbumRegistry _albumRegistry = albumRegistry;
        private readonly SmartDbContext _db = db;
        private readonly ICacheManager _cache = cache;

        #region Invalidation Hook

        public override Task<HookResult> OnBeforeSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            return Task.FromResult(HookResult.Ok);
        }

        public override Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            return ClearCacheAsync();
        }

        #endregion

        public TreeNode<MediaFolderNode> GetRootNode()
        {
            var root = _cache.Get(FolderTreeKey, o =>
            {
                o.ExpiresIn(FolderTreeCacheDuration);

                var query = from x in _db.MediaFolders
                            orderby x.ParentId, x.Name
                            select x;

                var unsortedNodes = query.ToList().Select(x =>
                {
                    var item = new MediaFolderNode
                    {
                        Id = x.Id,
                        Name = x.Name,
                        ParentId = x.ParentId,
                        CanDetectTracks = x.CanDetectTracks,
                        FilesCount = x.FilesCount,
                        Slug = x.Slug
                    };

                    if (x is MediaAlbum album)
                    {
                        item.IsAlbum = true;
                        item.AlbumName = album.Name;
                        item.Path = album.Name;
                        item.ResKey = album.ResKey;
                        item.IncludePath = album.IncludePath;
                        item.Order = album.Order ?? 0;

                        var albumInfo = _albumRegistry.GetAlbumByName(album.Name);
                        if (albumInfo != null)
                        {
                            var displayHint = albumInfo.DisplayHint;
                            item.Color = displayHint.Color;
                            item.OverlayColor = displayHint.OverlayColor;
                            item.OverlayIcon = displayHint.OverlayIcon;
                        }
                    }

                    return item;
                });

                var nodeMap = unsortedNodes.ToMultimap(x => x.ParentId ?? 0, x => x);
                var rootNode = new TreeNode<MediaFolderNode>(new MediaFolderNode { Name = "Root", Id = 0 });

                AddChildTreeNodes(rootNode, 0, nodeMap);

                return rootNode;
            });

            return root;

            static void AddChildTreeNodes(TreeNode<MediaFolderNode> parentNode, int parentId, Multimap<int, MediaFolderNode> nodeMap)
            {
                var parent = parentNode?.Value;
                if (parent == null)
                {
                    return;
                }

                var nodes = Enumerable.Empty<MediaFolderNode>();

                if (nodeMap.ContainsKey(parentId))
                {
                    nodes = parentId == 0
                        ? nodeMap[parentId].OrderBy(x => x.Order)
                        : nodeMap[parentId].OrderBy(x => x.Name);
                }

                foreach (var node in nodes)
                {
                    var newNode = new TreeNode<MediaFolderNode>(node);

                    // Inherit some props from parent node
                    if (!node.IsAlbum)
                    {
                        node.AlbumName = parent.AlbumName;
                        node.CanDetectTracks = parent.CanDetectTracks;
                        node.IncludePath = parent.IncludePath;
                        node.Path = (parent.Path + "/" + (node.Slug.NullEmpty() ?? node.Name)).Trim('/').ToLower();
                    }

                    // We gonna query nodes by path also, therefore we need 2 keys per node (FolderId and computed path)
                    newNode.Id = new object[] { node.Id, node.Path };

                    parentNode.Append(newNode);

                    AddChildTreeNodes(newNode, node.Id, nodeMap);
                }
            }
        }

        public TreeNode<MediaFolderNode> GetNodeById(int id)
        {
            if (id <= 0)
                return null;

            return GetRootNode().SelectNodeById(id);
        }

        public TreeNode<MediaFolderNode> GetNodeByPath(string path)
        {
            Guard.NotEmpty(path);
            return GetRootNode().SelectNodeById(NormalizePath(path));
        }

        public bool CheckUniqueFolderName(string path, out string newName)
        {
            Guard.NotEmpty(path);

            // TODO: (mm) (mc) throw when path is not a folder path

            newName = null;

            var node = GetNodeByPath(path);
            if (node == null)
            {
                return false;
            }

            var sourceName = node.Value.Name;
            var names = new HashSet<string>(node.Parent.Children.Select(x => x.Value.Name), StringComparer.OrdinalIgnoreCase);

            int i = 1;
            while (true)
            {
                var test = sourceName + "-" + i;
                if (!names.Contains(test))
                {
                    // Found our gap
                    newName = test;
                    return true;
                }

                i++;
            }
        }

        public Task ClearCacheAsync()
        {
            return _cache.RemoveAsync(FolderTreeKey);
        }

        protected internal static string NormalizePath(string path, bool forQuery = true)
        {
            if (path.IndexOf('\\') > -1)
            {
                path = path.Replace('\\', '/');
            }

            var trim = path[0] == '/' || (path.Length > 1 && path[^1] == '/');
            if (trim)
            {
                path = path.Trim('/');
            }

            return forQuery ? path.ToLower() : path;
        }
    }
}