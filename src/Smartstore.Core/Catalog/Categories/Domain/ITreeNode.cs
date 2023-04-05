#nullable enable

using Smartstore.Core.Data;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Catalog.Categories
{
    /// <summary>
    /// Represents a tree node entity.
    /// </summary>
    public interface ITreeNode
    {
        int Id { get; }

        /// <summary>
        /// Id of the parent entity or <c>null</c> if the node is the root.
        /// </summary>
        int? ParentId { get; }

        /// <summary>
        /// Gets or sets the tree path, like <c>/1/2/3/</c>.
        /// </summary>
        string TreePath { get; set; }

        /// <summary>
        /// Gets the parent node instance or <c>null</c> if this node is a root.
        /// </summary>
        /// <returns></returns>
        ITreeNode? GetParentNode();

        /// <summary>
        /// Enumerates the child nodes.
        /// </summary>
        IEnumerable<ITreeNode> GetChildNodes();

        /// <summary>
        /// Gets the <see cref="IQueryable{ITreeNode}"/> used to access the entity storage.
        /// </summary>
        IQueryable<ITreeNode> GetQuery(SmartDbContext db);
    }

    public static class ITreeNodeExtensions
    {
        const char PathSeparator = '/';
        
        public static string BuildTreePath(this ITreeNode node)
        {
            using var _ = CultureHelper.UseInvariant();

            if (node.ParentId == null || node.GetParentNode() is not ITreeNode parentNode)
            {
                return $"/{node.Id}/";
            }

            var parentPath = parentNode.TreePath.NullEmpty() ?? parentNode.BuildTreePath();
            parentPath = parentPath.EmptyNull().EnsureEndsWith(PathSeparator);

            return $"{parentPath}{node.Id}/";
        }
    }
}
