namespace Smartstore.Core.Search.Facets
{
    /// <summary>
    /// Contains utilities that are required for facet processing.
    /// </summary>
    public static partial class FacetUtility
    {
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
