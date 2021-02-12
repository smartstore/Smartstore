using Smartstore.Collections;
using Smartstore.Domain;

namespace Smartstore.Web.TagHelpers
{
    /// <summary>
    /// Enables (plugins) developers to inject menu items to menus.
    /// </summary>
    public interface IMenuProvider : IOrdered
    {
        void BuildMenu(TreeNode<MenuItem> rootNode);

        /// <summary>
        /// Gets the menu name to inject the menu items into (e.g. admin, catalog etc.)
        /// </summary>
        string MenuName { get; }
    }
}
