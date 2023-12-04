using Smartstore.Core.Search;

namespace Smartstore.Core.Catalog.Search
{
    /// <summary>
    /// Filter products by category tree path.
    /// </summary>
    public class CategoryTreePathFilter : SearchFilter
    {
        public CategoryTreePathFilter(string treePath, bool? featuredOnly, bool includeSelf = true)
        {
            Guard.NotEmpty(treePath);

            var names = CatalogSearchQuery.KnownFilters;

            FieldName = featuredOnly.HasValue
                ? featuredOnly.Value ? names.FeaturedCategoryPath : names.NotFeaturedCategoryPath
                : names.CategoryPath;

            Term = treePath;
            FeaturedOnly = featuredOnly;
            IncludeSelf = includeSelf;
            TypeCode = IndexTypeCode.String;
            Mode = SearchMode.StartsWith;
            Occurence = SearchFilterOccurence.Must;
            IsNotAnalyzed = true;
        }

        /// <summary>
        /// A value indicating whether loaded products are marked as "featured" at their category assignment.
        /// <c>true</c> to load featured products only, <c>false</c> to load unfeatured products only, <c>null</c> to load all products.
        /// </summary>
        public bool? FeaturedOnly { get; set; }

        /// <summary>
        /// <c>true</c> = add the parent node to the result list, <c>false</c> = ignore the parent node.
        /// </summary>
        public bool IncludeSelf { get; }

        /// <summary>
        /// Category tree path.
        /// </summary>
        public string TreePath
            => (string)Term;

        /// <summary>
        /// Category identifier from <see cref="TreePath"/>.
        /// </summary>
        public int CategoryId
            => TreePath.Trim('/').Tokenize('/').LastOrDefault()?.ToInt() ?? 0;
    }
}
