namespace Smartstore.Core.Search.Facets
{
    /// <summary>
    /// Metadata for a facet including facet value.
    /// </summary>
    [Serializable]
    public class FacetMetadata
    {
        public int EntityId { get; set; }
        public long HitCount { get; set; }
        public FacetTemplateHint TemplateHint { get; set; }
        public FacetValue Value { get; set; }
        public FacetMetadata Parent { get; set; }
    }
}
