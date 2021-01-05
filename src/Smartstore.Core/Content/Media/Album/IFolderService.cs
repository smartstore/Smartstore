using System;
using System.Threading.Tasks;
using Smartstore.Collections;
using Smartstore.Threading;

namespace Smartstore.Core.Content.Media
{
    /// <summary>
    /// Reads media folder objects either from cache (as <see cref="TreeNode<MediaFolderNode>"/>).
    /// Methods with <see cref="MediaFolderNode"/> in their signature always work against the cache and enable very fast
    /// data retrieval.
    /// The tree cache is invalidated automatically after any storage action.
    /// </summary>
    public interface IFolderService
    {
        /// <summary>
        /// Gets the root folder node from cache.
        /// </summary>
        Task<TreeNode<MediaFolderNode>> GetRootNodeAsync();

        /// <summary>
        /// Gets a folder node by storage id.
        /// </summary>
        Task<TreeNode<MediaFolderNode>> GetNodeByIdAsync(int id);

        /// <summary>
        /// Gets a folder node by path, e.g. "catalog/subfolder1/subfolder2".
        /// The first token always refers to an album. This method operates very fast 
        /// because all possible pathes are cached.
        /// </summary>
        Task<TreeNode<MediaFolderNode>> GetNodeByPathAsync(string path);

        /// <summary>
        /// Checks whether any given path does already exist and - if true -
        /// outputs a unique leaf folder name that can be used to save a folder
        /// to the database.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns>If <see cref="AsyncOut{TOut}.Success"/> is <c>true</c> <see cref="AsyncOut{TOut}.Value"/> will contain the new unique folder name.</returns>
        Task<AsyncOut<string>> CheckUniqueFolderNameAsync(string path);

        /// <summary>
        /// Clears the cache and reloads data from database.
        /// </summary>
        Task ClearCacheAsync();
    }
}
