using Smartstore.Collections;

namespace Smartstore.Core.Catalog.Categories
{
    /// <summary>
    /// Category service interface.
    /// </summary>
    public partial interface ICategoryService
    {
        /// <summary>
        /// Deletes a category.
        /// </summary>
        /// <param name="category">Category entity.</param>
        /// <param name="deleteSubCategories">A value indicating whether to delete child categories or to set them to no parent.</param>
        Task DeleteCategoryAsync(Category category, bool deleteSubCategories = false);

        /// <summary>
        /// Assigns ACL restrictions to sub-categories and products.
        /// </summary>
        /// <param name="categoryId">Category identifier.</param>
        /// <param name="touchProductsWithMultipleCategories">Reserved for future use. 
        /// A value indicating whether to assign ACL restrictions to products which are contained in multiple categories.</param>
        /// <param name="touchExistingAcls">Reserved for future use. 
        /// A value indicating whether to delete existing ACL restrictions.</param>
        /// <param name="categoriesOnly">Reserved for future use. 
        /// A value indicating whether to assign ACL restrictions only to categories.</param>
        /// <returns>Number of affected categories and products.</returns>
        Task<(int AffectedCategories, int AffectedProducts)> InheritAclIntoChildrenAsync(
            int categoryId,
            bool touchProductsWithMultipleCategories = false,
            bool touchExistingAcls = false,
            bool categoriesOnly = false);

        /// <summary>
        /// Assigns store restrictions to sub-categories and products.
        /// </summary>
        /// <param name="categoryId">Category identifier.</param>
        /// <param name="touchProductsWithMultipleCategories">Reserved for future use.
        /// A value indicating whether to assign store restrictions to products which are contained in multiple categories.</param>
        /// <param name="touchExistingAcls">Reserved for future use.
        /// A value indicating whether to delete existing store restrictions.</param>
        /// <param name="categoriesOnly">Reserved for future use.
        /// A value indicating whether to assign store restrictions only to categories.</param>
        /// <returns>Number of affected categories and products.</returns>
        Task<(int AffectedCategories, int AffectedProducts)> InheritStoresIntoChildrenAsync(
            int categoryId,
            bool touchProductsWithMultipleCategories = false,
            bool touchExistingAcls = false,
            bool categoriesOnly = false);

        /// <summary>
        /// Get categories by parent category identifier.
        /// </summary>
        /// <param name="parentCategoryId">Parent category identifier.</param>
        /// <param name="includeHidden">A value indicating whether to include hidden categories.</param>
        /// <returns>Categories.</returns>
        Task<IList<Category>> GetCategoriesByParentCategoryIdAsync(int parentCategoryId, bool includeHidden = false);

        /// <summary>
        /// Gets product category mappings.
        /// </summary>
        /// <param name="productIds">Product identifiers.</param>
        /// <param name="includeHidden">A value indicating whether to include hidden categories.</param>
        /// <returns>Product categories.</returns>
        Task<IList<ProductCategory>> GetProductCategoriesByProductIdsAsync(int[] productIds, bool includeHidden = false);

        /// <summary>
        /// Builds a category breadcrumb (path) for a particular category node.
        /// </summary>
        /// <param name="categoryNode">The category node.</param>
        /// <param name="languageId">The language identifier. Pass <c>null</c> to skip localization.</param>
        /// <param name="aliasPattern">How the category alias - if specified - should be appended to the category name (e.g. <c>({0})</c>).</param>
        /// <param name="separator">The separator string.</param>
        /// <returns>Category breadcrumb path.</returns>
        Task<string> GetCategoryPathAsync(
            ICategoryNode categoryNode,
            int? languageId = null,
            string aliasPattern = null,
            string separator = " » ");

        /// <summary>
        /// Builds a category breadcrumb (path) for a particular category node.
        /// </summary>
        /// <param name="treeNode">The category node.</param>
        /// <param name="languageId">The language identifier. Pass <c>null</c> to skip localization.</param>
        /// <param name="aliasPattern">How the category alias - if specified - should be appended to the category name (e.g. <c>({0})</c>).</param>
        /// <param name="separator">The separator string.</param>
        /// <returns>Category breadcrumb path.</returns>
        string GetCategoryPath(
            TreeNode<ICategoryNode> treeNode,
            int? languageId = null,
            string aliasPattern = null,
            string separator = " » ");

        /// <summary>
        /// Gets the tree representation of categories.
        /// </summary>
        /// <param name="rootCategoryId">Specifies which node to return as root.</param>
        /// <param name="includeHidden"><c>false</c> excludes unpublished and ACL-inaccessible categories.</param>
        /// <param name="storeId">&gt; 0 = apply store mapping, 0 to bypass store mapping.</param>
        /// <returns>The category tree representation.</returns>
        /// <remarks>
        /// This method puts the tree result into application cache, so subsequent calls are very fast.
        /// Localization is up to the caller because the nodes only contain unlocalized data.
        /// Subscribe to the <c>CategoryTreeChanged</c> event if you need to evict cache entries which depend
        /// on this method's result.
        /// </remarks>
        Task<TreeNode<ICategoryNode>> GetCategoryTreeAsync(
            int rootCategoryId = 0,
            bool includeHidden = false,
            int storeId = 0);
    }
}
