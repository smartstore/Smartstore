namespace Smartstore
{
    public static class ITreeNodeQueryExtensions
    {
        /// <summary>
        /// Applies a filter that reads all descendant nodes of given <paramref name="node"/>.
        /// </summary>
        /// <param name="node">The node to read descendants of.</param>
        public static IQueryable<ITreeNode> ApplyDescendantsFilter(
            this IQueryable<ITreeNode> query, 
            ITreeNode node)
        {
            Guard.NotNull(node);

            // TODO: Validate node.TreePath

            return query.ApplyDescendantsFilter(node.TreePath, false);
        }

        //public static IQueryable<ITreeNode> ApplyDescendantsFilter(
        //    this IQueryable<ITreeNode> query,
        //    int parentNodeId, 
        //    bool deep = true, 
        //    bool includeSelf = false)
        //{
        //    return query.ApplyDescendantsFilter(node.TreePath, false);
        //}

        /// <summary>
        /// Applies a filter that reads all descendant nodes of the node with the given <paramref name="treePath"/>.
        /// </summary>
        /// <param name="treePath">The tree path of the current/parent node.</param>
        public static IQueryable<ITreeNode> ApplyDescendantsFilter(
            this IQueryable<ITreeNode> query, 
            string treePath,
            bool includeSelf = false)
        {
            Guard.NotNull(query);
            Guard.NotEmpty(treePath);

            // TODO: Validate treePath

            query = query.Where(x => x.TreePath.StartsWith(treePath));

            if (!includeSelf)
            {
                query = query.Where(x => x.TreePath.Length > treePath.Length);
            }

            return query;
        }
    }
}
