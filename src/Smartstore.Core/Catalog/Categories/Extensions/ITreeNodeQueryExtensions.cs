namespace Smartstore.Core.Catalog.Categories
{
    public static class ITreeNodeQueryExtensions
    {
        /// <summary>
        /// Applies a filter that reads all descendant nodes of the given <paramref name="node"/>.
        /// </summary>
        /// <param name="node">The node to retrieve descendants from.</param>
        public static IQueryable<ITreeNode> ApplyDescendantsFilter(
            this IQueryable<ITreeNode> query, 
            ITreeNode node)
        {
            Guard.NotNull(node);
            return query.ApplyDescendantsFilter(node.TreePath, false);
        }

        /// <summary>
        /// Applies a filter that reads all descendant nodes of the node with Id = given <paramref name="parentNodeId"/>.
        /// </summary>
        /// <param name="parentNodeId">The parent's id to get descendants from.</param>
        /// <param name="deep"><c>false</c> = retrieve only direct children, <c>true</c> = retrieve any descendant.</param>
        /// <param name="includeSelf"><c>true</c> = add the parent node to the result list, <c>false</c> = ignore the parent node.</param>
        public static IQueryable<ITreeNode> ApplyDescendantsFilter(
            this IQueryable<ITreeNode> query,
            int parentNodeId,
            bool deep = true,
            bool includeSelf = true)
        {
            if (parentNodeId <= 0)
            {
                return query;
            }

            if (!deep)
            {
                return includeSelf 
                    ? query.Where(x => x.ParentId == parentNodeId || x.Id == parentNodeId) 
                    : query.Where(x => x.ParentId == parentNodeId);
            }

            var subquery = query
                .Where(x => x.ParentId == parentNodeId || (includeSelf && x.Id == parentNodeId))
                .Select(x => x.TreePath);

            return query.Where(x => subquery.Any(tp => x.TreePath.StartsWith(tp)));
        }

        /// <summary>
        /// Applies a filter that reads all descendant nodes of the node with the given <paramref name="treePath"/>.
        /// </summary>
        /// <param name="treePath">The parent's tree path to get descendants from.</param>
        /// <param name="includeSelf"><c>true</c> = add the parent node to the result list, <c>false</c> = ignore the parent node.</param>
        public static IQueryable<ITreeNode> ApplyDescendantsFilter(
            this IQueryable<ITreeNode> query, 
            string treePath,
            bool includeSelf = true)
        {
            Guard.NotNull(query);
            Guard.NotEmpty(treePath);

            if (treePath.Length < 3 || (treePath[0] != '/' && treePath[^1] != '/'))
            {
                throw new ArgumentException("Invalid treePath format.", nameof(treePath));
            }

            query = query.Where(x => x.TreePath.StartsWith(treePath));

            if (!includeSelf)
            {
                query = query.Where(x => x.TreePath.Length > treePath.Length);
            }

            return query;
        }
    }
}
