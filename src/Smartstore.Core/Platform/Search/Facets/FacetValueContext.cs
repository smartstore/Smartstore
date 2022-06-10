namespace Smartstore.Core.Search.Facets
{
    public class FacetValueContext
    {
        public FacetDescriptor Descriptor { get; set; }

        /// <summary>
        /// The name of the field to be faceted.
        /// </summary>
        /// <remarks>
        /// In rare cases (e.g. for prices), the field to be faceted is different from the actual index field.
        /// </remarks>
        public string FieldName { get; set; }

        public Func<IAttributeSearchFilter, bool> ApplyFilters { get; set; }
        public IDictionary<string, object> CustomData { get; private set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }
}
