#nullable enable

using System.Text;
using Smartstore.Collections;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Catalog.Categories
{
    public static partial class CategoryNodeExtensions
    {
        /// <summary>
        /// Sort categories for tree representation.
        /// </summary>
        /// <param name="source">Source categories.</param>
        /// <param name="parentId">Parent category identifier.</param>
        /// <param name="ignoreDetachedCategories">A value indicating whether categories without existing parent category in provided category list (source) should be ignored.</param>
        /// <returns>Sorted categories</returns>
        public static IEnumerable<T> SortCategoryNodesForTree<T>(
            this IEnumerable<T> source, 
            int parentId = 0, bool 
            ignoreDetachedCategories = false)
            where T : ICategoryNode
        {
            Guard.NotNull(source);

            var sourceCount = source.Count();
            var result = new List<T>(sourceCount);
            var lookup = source.ToLookup(x => x.ParentId.GetValueOrDefault());

            result.AddRange(SortCategoryNodesForTreeInternal(parentId, lookup));

            if (!ignoreDetachedCategories && result.Count != sourceCount)
            {
                // Find categories without parent in provided category source and insert them into result
                var resultLookup = result.ToDictionarySafe(x => x.Id);
                foreach (var cat in source)
                {
                    if (!resultLookup.ContainsKey(cat.Id))
                    {
                        result.Add(cat);
                    }
                }
            }

            return result;
        }

        private static IEnumerable<T> SortCategoryNodesForTreeInternal<T>(int parentId, ILookup<int, T> lookup)
            where T : ICategoryNode
        {
            if (!lookup.Contains(parentId))
            {
                return Enumerable.Empty<T>();
            }

            var childNodes = lookup[parentId];
            var result = new List<T>();

            foreach (var node in childNodes)
            {
                result.Add(node);
                result.AddRange(SortCategoryNodesForTreeInternal(node.Id, lookup));
            }

            return result;
        }

        class CategoryNodeSorter<T>
            where T : ICategoryNode
        {
            private readonly IEnumerable<T> _source;
            private readonly int _parentId;
            private readonly bool _ignoreDetachedCategories;

            public CategoryNodeSorter(IEnumerable<T> source, int parentId = 0, bool ignoreDetachedCategories = false)
            {
                _source = source;
                _parentId = parentId;
                _ignoreDetachedCategories = ignoreDetachedCategories;
            }

            public IEnumerable<T> Sort()
            {
                var sourceCount = _source.Count();
                var result = new List<T>(sourceCount);
                var lookup = _source.ToLookup(x => x.ParentId.GetValueOrDefault());

                result.AddRange(SortInternal(_parentId, lookup));

                if (!_ignoreDetachedCategories && result.Count != sourceCount)
                {
                    // Find categories without parent in provided category source and insert them into result.
                    var resultLookup = result.ToDictionarySafe(x => x.Id);
                    foreach (var cat in _source)
                    {
                        if (!resultLookup.ContainsKey(cat.Id))
                        {
                            result.Add(cat);
                        }
                    }
                }

                return result;
            }

            private IEnumerable<T> SortInternal(int parentId, ILookup<int, T> lookup)
            {
                if (!lookup.Contains(parentId))
                {
                    return Enumerable.Empty<T>();
                }

                var childNodes = lookup[parentId];
                var result = new List<T>();
                foreach (var node in childNodes)
                {
                    result.Add(node);
                    result.AddRange(SortInternal(node.Id, lookup));
                }

                return result;
            }
        }


        /// <summary>
        /// Gets the indented name of a category.
        /// </summary>
        /// <param name="treeNode">Tree node.</param>
        /// <param name="indentWith">Indent string.</param>
        /// <param name="languageId">Language identifier.</param>
        /// <param name="withAlias">A value indicating whether to append the category alias.</param>
        /// <returns>Indented category name.</returns>
        public static string GetCategoryNameIndented(
            this TreeNode<ICategoryNode> treeNode,
            string indentWith = "--",
            int? languageId = null,
            bool withAlias = true)
        {
            Guard.NotNull(treeNode);

            var sb = new StringBuilder(100);
            var indentSize = treeNode.Depth - 1;

            for (int i = 0; i < indentSize; i++)
            {
                sb.Append(indentWith);
            }

            var cat = treeNode.Value;

            var name = languageId.HasValue
                ? cat.GetLocalized(n => n.Name, languageId.Value)
                : cat.Name;

            sb.Append(name);

            if (withAlias && cat.Alias.HasValue())
            {
                sb.Append(" (");
                sb.Append(cat.Alias);
                sb.Append(')');
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets the tree path of a category.
        /// </summary>
        /// <param name="treeNode">Tree node to get tree path for.</param>
        /// <returns>The tree path, or <c>null</c> if the given <paramref name="treeNode"/> is a root node.</returns>
        public static string? GetTreePath(this TreeNode<ICategoryNode> treeNode)
        {
            Guard.NotNull(treeNode);

            using var _ = CultureHelper.UseInvariant();

            if (treeNode.Depth == 0)
            {
                return null;
            }
            else if (treeNode.Depth == 1)
            {
                // Very fast path
                return $"/{treeNode.Value.Id}/";
            }
            else if (treeNode.Depth == 2)
            {
                // Fast path
                return $"/{treeNode.Parent!.Value.Id}/{treeNode.Value.Id}/";
            }
            else
            {
                // Slow path
                var sb = new StringBuilder("/", 32);
                var trail = treeNode.Trail;

                foreach (var node in trail)
                {
                    if (node.IsRoot)
                    {
                        continue;
                    }

                    sb.Append(node.Value.Id);
                    sb.Append('/');
                }

                return sb.ToString();
            }
        }
    }
}