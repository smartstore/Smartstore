namespace Smartstore.Core.Search.Facets
{
    /// <summary>
    /// Contains utilities that are required for facet processing.
    /// </summary>
    public static partial class FacetUtility
    {
        /// <summary>
        /// Gets the string resource key for a facet group kind.
        /// </summary>
        /// <param name="kind">Facet group kind.</param>
        /// <returns>Resource key.</returns>
        public static string GetLabelResourceKey(FacetGroupKind kind)
        {
            return kind switch
            {
                FacetGroupKind.Category => "Search.Facet.Category",
                FacetGroupKind.Brand => "Search.Facet.Manufacturer",
                FacetGroupKind.Price => "Search.Facet.Price",
                FacetGroupKind.Rating => "Search.Facet.Rating",
                FacetGroupKind.DeliveryTime => "Search.Facet.DeliveryTime",
                FacetGroupKind.Availability => "Search.Facet.Availability",
                FacetGroupKind.NewArrivals => "Search.Facet.NewArrivals",
                FacetGroupKind.Forum => "Search.Facet.Forum",
                FacetGroupKind.Customer => "Search.Facet.Customer",
                FacetGroupKind.Date => "Search.Facet.Date",
                _ => null,
            };
        }

        public static string GetFacetAliasSettingKey(FacetGroupKind kind, int languageId, string scope = null)
        {
            if (scope.HasValue())
            {
                return $"FacetGroupKind-{kind}-Alias-{languageId}-{scope}";
            }

            return $"FacetGroupKind-{kind}-Alias-{languageId}";
        }
    }
}
