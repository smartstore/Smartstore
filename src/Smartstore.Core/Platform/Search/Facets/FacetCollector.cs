namespace Smartstore.Core.Search.Facets
{
    /// <summary>
    /// Helper to speed up collecting facets.
    /// Allows to break the iteration of all facet values if no better facets can be achieved with regard to the hit counts.
    /// </summary>
    public class FacetCollector
    {
        private readonly int _maxChoicesCount;
        private int _uncalculatedSelectedCount;
        private readonly HashSet<FacetValue> _selectedValues;
        private readonly List<Facet> _selectedFacets = new();
        private readonly List<Facet> _nonSelectedFacets = new();

        public FacetCollector(IEnumerable<FacetValue> selectedValues, int maxChoicesCount)
        {
            _selectedValues = new HashSet<FacetValue>(selectedValues);
            _uncalculatedSelectedCount = _selectedValues.Count;
            _maxChoicesCount = maxChoicesCount;
        }

        public long MinCountForNonSelected { get; protected set; } = 0;

        public bool HaveEnoughResults
            => _uncalculatedSelectedCount == 0 && _maxChoicesCount > 0 && _nonSelectedFacets.Count >= _maxChoicesCount;

        public bool IsSelected(FacetValue value)
            => _uncalculatedSelectedCount > 0 && _selectedValues.Contains(value);

        public IEnumerable<Facet> GetResult()
            => _selectedFacets.Union(_nonSelectedFacets);

        public void Add(Facet facet, bool isSelected)
        {
            if (isSelected)
            {
                _selectedFacets.Add(facet);
                _uncalculatedSelectedCount--;
                return;
            }

            // Not selected.
            if (_maxChoicesCount > 0 && _nonSelectedFacets.Count >= _maxChoicesCount)
            {
                if (facet.HitCount < MinCountForNonSelected)
                {
                    return;
                }

                if (facet.HitCount > MinCountForNonSelected)
                {
                    // Remove tail if possible.
                    while (true)
                    {
                        var allWithMinCount = _nonSelectedFacets.Where(x => x.HitCount == MinCountForNonSelected).ToList();
                        if (allWithMinCount.Count == 0)
                        {
                            break;
                        }

                        var countWhenAddingThisAndRemovingMin = _nonSelectedFacets.Count - allWithMinCount.Count + 1;
                        if (countWhenAddingThisAndRemovingMin >= _maxChoicesCount)
                        {
                            allWithMinCount.ForEach(x => _nonSelectedFacets.Remove(x));

                            MinCountForNonSelected = _nonSelectedFacets.Min(x => x.HitCount);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            MinCountForNonSelected = MinCountForNonSelected == 0
                ? facet.HitCount
                : Math.Min(MinCountForNonSelected, facet.HitCount);

            _nonSelectedFacets.Add(facet);
        }

        public List<Facet> GetSelectedValues(IDictionary<object, FacetMetadata> metadata)
        {
            var result = new List<Facet>();

            foreach (var value in _selectedValues)
            {
                // Try to get label from index metadata.
                var newFacet = value?.Value != null && metadata.TryGetValue(value.Value, out var item) && item?.Value != null
                    ? new Facet(item.Value)
                    : new Facet(value);

                newFacet.Value.IsSelected = true;

                result.Add(newFacet);
            }

            return result;
        }
    }
}
