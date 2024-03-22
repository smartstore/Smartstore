#nullable enable

using System.Diagnostics.CodeAnalysis;
using Smartstore.Collections;

namespace Smartstore.Core.Content.Media
{
    /// <summary>
    /// Reads media folder objects either from cache (as <see cref="TreeNode{MediaFolderNode}"/>).
    /// Methods with <see cref="MediaFolderNode"/> in their signature always work against the cache and enable very fast
    /// data retrieval.
    /// The tree cache is invalidated automatically after any storage action.
    /// </summary>
    public interface IFolderService
    {
        /// <summary>
        /// Gets the root folder node from cache.
        /// </summary>
        TreeNode<MediaFolderNode> GetRootNode();

        /// <summary>
        /// Gets a folder node by storage id.
        /// </summary>
        TreeNode<MediaFolderNode>? GetNodeById(int id);

        /// <summary>
        /// Gets a folder node by path, e.g. "catalog/subfolder1/subfolder2".
        /// The first token always refers to an album. This method operates very fast 
        /// because all possible pathes are cached.
        /// </summary>
        TreeNode<MediaFolderNode>? GetNodeByPath(string path);

        /// <summary>
        /// Checks whether any given path does already exist and - if true -
        /// outputs a unique leaf folder name that can be used to save a folder
        /// to the database.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <param name="newName">If method return value is <c>true</c>: the new unique folder name, otherwise: <c>null</c>.</param>
        /// <returns><c>true</c> when passed path exists already.</returns>
        bool CheckUniqueFolderName(string path, [NotNullWhen(true)] out string newName);

        /// <summary>
        /// Clears the cache and reloads data from database.
        /// </summary>
        Task ClearCacheAsync();
    }
}
