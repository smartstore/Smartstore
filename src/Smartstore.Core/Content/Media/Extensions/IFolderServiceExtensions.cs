using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Collections;

namespace Smartstore.Core.Content.Media
{
    public static class IFolderServiceExtensions
    {
        /// <summary>
        /// Finds the folder node for a given <see cref="MediaFile"/> object.
        /// </summary>
        /// <returns>The found folder node or <c>null</c>.</returns>
        public static Task<TreeNode<MediaFolderNode>> FindNodeAsync(this IFolderService service, MediaFile mediaFile)
        {
            return service.GetNodeByIdAsync(mediaFile?.FolderId ?? 0);
        }

        /// <summary>
        /// Finds the root album node for a given <see cref="MediaFile"/> object.
        /// </summary>
        /// <returns>The found album node or <c>null</c>.</returns>
        public static async Task<TreeNode<MediaFolderNode>> FindAlbumAsync(this IFolderService service, MediaFile mediaFile)
        {
            return (await FindNodeAsync(service, mediaFile))?.Closest(x => x.Value.IsAlbum);
        }

        /// <summary>
        /// Finds the root album node for a given folder id.
        /// </summary>
        /// <returns>The found album node or <c>null</c>.</returns>
        public static async Task<TreeNode<MediaFolderNode>> FindAlbumAsync(this IFolderService service, int folderId)
        {
            return (await service.GetNodeByIdAsync(folderId))?.Closest(x => x.Value.IsAlbum);
        }

        /// <summary>
        /// Checks whether all passed files are contained in the same album.
        /// </summary>
        public static bool AreInSameAlbum(this IFolderService service, params MediaFile[] files)
        {
            return files.Select(x => FindAlbumAsync(service, x)).Distinct().Count() <= 1;
        }

        /// <summary>
        /// Checks whether all passed folder ids are children of the same album.
        /// </summary>
        public static bool AreInSameAlbum(this IFolderService service, params int[] folderIds)
        {
            return folderIds.Select(x => FindAlbumAsync(service, x)).Distinct().Count() <= 1;
        }

        public static async Task<IEnumerable<MediaFolderNode>> GetNodesFlattenedAsync(this IFolderService service, string path, bool includeSelf = true)
        {
            var node = await service.GetNodeByPathAsync(path);
            if (node == null)
            {
                return Enumerable.Empty<MediaFolderNode>();
            }

            return node.FlattenNodes(includeSelf).Select(x => x.Value);
        }

        public static async Task<IEnumerable<MediaFolderNode>> GetNodesFlattenedAsync(this IFolderService service, int folderId, bool includeSelf = true)
        {
            var node = await service.GetNodeByIdAsync(folderId);
            if (node == null)
            {
                return Enumerable.Empty<MediaFolderNode>();
            }

            return node.FlattenNodes(includeSelf).Select(x => x.Value);
        }
    }
}
