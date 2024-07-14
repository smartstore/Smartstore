﻿using Smartstore.Collections;

namespace Smartstore.Core.Content.Media
{
    public static class IFolderServiceExtensions
    {
        /// <summary>
        /// Finds the folder node for a given <see cref="MediaFile"/> object.
        /// </summary>
        /// <returns>The found folder node or <c>null</c>.</returns>
        public static TreeNode<MediaFolderNode> FindNode(this IFolderService service, MediaFile mediaFile) =>
            service.GetNodeById(mediaFile?.FolderId ?? 0);

        /// <summary>
        /// Finds the root album node for a given <see cref="MediaFile"/> object.
        /// </summary>
        /// <returns>The found album node or <c>null</c>.</returns>
        public static TreeNode<MediaFolderNode> FindAlbum(this IFolderService service, MediaFile mediaFile) =>
            FindNode(service, mediaFile)?.Closest(x => x.Value.IsAlbum);

        /// <summary>
        /// Finds the root album node for a given folder id.
        /// </summary>
        /// <returns>The found album node or <c>null</c>.</returns>
        public static TreeNode<MediaFolderNode> FindAlbum(this IFolderService service, int folderId) =>
            service.GetNodeById(folderId)?.Closest(x => x.Value.IsAlbum);

        /// <summary>
        /// Checks whether all passed files are contained in the same album.
        /// </summary>
        public static bool AreInSameAlbum(this IFolderService service, params MediaFile[] files) => files.Select(x => FindAlbum(service, x)).Distinct().Count() <= 1;

        /// <summary>
        /// Checks whether all passed folder ids are children of the same album.
        /// </summary>
        public static bool AreInSameAlbum(this IFolderService service, params int[] folderIds) => 
            folderIds.Select(x => FindAlbum(service, x)).Distinct().Count() <= 1;

        public static IEnumerable<MediaFolderNode> GetNodesFlattened(this IFolderService service, string path, bool includeSelf = true)
        {
            var node = service.GetNodeByPath(path);
            if (node == null)
            {
                return Enumerable.Empty<MediaFolderNode>();
            }

            return node.FlattenNodes(includeSelf).Select(x => x.Value);
        }

        public static IEnumerable<MediaFolderNode> GetNodesFlattened(this IFolderService service, int folderId, bool includeSelf = true)
        {
            var node = service.GetNodeById(folderId);
            if (node == null)
            {
                return Enumerable.Empty<MediaFolderNode>();
            }

            return node.FlattenNodes(includeSelf).Select(x => x.Value);
        }
    }
}
