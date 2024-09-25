namespace Smartstore.Core.OutputCache
{
    public static class OutputCacheDefaults
    {
        public static string HomeRoute { get; } = "Home/Index";
        public static string CategoryRoute { get; } = "Catalog/Category";
        public static string ManufacturerRoute { get; } = "Catalog/Manufacturer";
        public static string ManufacturerAllRoute { get; } = "Catalog/ManufacturerAll";
        public static string ProductsByTagRoute { get; } = "Catalog/ProductsByTag";
        public static string ProductTagsAllRoute { get; } = "Catalog/ProductTagsAll";
        public static string RecentlyAddedProductsRoute { get; } = "Catalog/RecentlyAddedProducts";
        public static string RecentlyAddedProductsRssRoute { get; } = "Catalog/RecentlyAddedProductsRss";
        public static string ProductDetailsRoute { get; } = "Product/ProductDetails";
        public static string SearchRoute { get; } = "Search/Search";
        public static string TopicDetailsRoute { get; } = "Topic/TopicDetails";

        public static string[] AllProductListsRoutes { get; } =
        [
            CategoryRoute, 
            ManufacturerRoute, 
            ProductsByTagRoute, 
            RecentlyAddedProductsRoute
        ];

        public static string[] AllProductListsWithRssRoutes { get; } =
        [
            CategoryRoute,
            ManufacturerRoute,
            ProductsByTagRoute,
            RecentlyAddedProductsRoute,
            RecentlyAddedProductsRssRoute
        ];

        public static string[] AllProductListsWithSearchFiltersRoutes { get; } =
        [
            CategoryRoute,
            ManufacturerRoute,
            ProductsByTagRoute,
            SearchRoute
        ];
    }
}
