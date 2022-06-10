using Smartstore.Collections;

namespace Smartstore.Core.Content.Menus
{
    /// <summary>
    /// Main service for menus.
    /// </summary>
    public partial interface IMenuService
    {
        /// <summary>
        /// Gets a menu instance
        /// </summary>
        /// <param name="name">Name of a code-based or a persisted user menu.</param>
        /// <returns>Menu instance</returns>
        Task<IMenu> GetMenuAsync(string name);

        /// <summary>
        /// Gets the root node of a menu
        /// </summary>
        /// <param name="menuName">Name of a code-based or a persisted user menu.</param>
        /// <returns>The root menu item node.</returns>
        Task<TreeNode<MenuItem>> GetRootNodeAsync(string menuName);

        /// <summary>
        /// Resolves all element counts for a tree subset, e.g. resolves the number of products in categories.
        /// </summary>
        /// <param name="menuName">Name of a code-based or a persisted user menu.</param>
        /// <param name="curNode">The node to begin resolution.</param>
        /// <param name="deep"><c>true</c>: process ALL children of <paramref name="curNode"/>, <c>false:</c> process only direct children of <paramref name="curNode"/>.</param>
        Task ResolveElementCountsAsync(string menuName, TreeNode<MenuItem> curNode, bool deep = false);

        /// <summary>
        /// Removes all cached menu variations for <paramref name="menuName"/>
        /// </summary>
        /// <param name="menuName">Name of a code-based or a persisted user menu.</param>
        Task ClearCacheAsync(string menuName);
    }
}
