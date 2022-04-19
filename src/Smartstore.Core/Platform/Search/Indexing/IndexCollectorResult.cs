namespace Smartstore.Core.Search.Indexing
{
    public class IndexCollectorResult
    {
        public IndexCollectorResult(
            IIndexDataSegmenter segmenter,
            bool forceRebuild,
            int documentOffset = 0)
        {
            Guard.NotNull(segmenter, nameof(segmenter));

            Segmenter = segmenter;
            ForceRebuild = forceRebuild;
            DocumentOffset = documentOffset;
        }

        public IIndexDataSegmenter Segmenter { get; }
        public List<IndexDataUnit> ExtraIndexUnits { get; } = new();

        public bool ForceRebuild { get; }
        public int DocumentOffset { get; }
    }


    public class IndexDataUnit
    {
        public IndexDataUnit(IIndexDataSegmenter segmenter, SearchDocumentType type)
        {
            Guard.NotNull(segmenter, nameof(segmenter));

            Segmenter = segmenter;
            DocumentType = type;
        }

        public IIndexDataSegmenter Segmenter { get; }
        public SearchDocumentType DocumentType { get; }

        public string Id { get; set; }
        public bool Rebuild { get; set; } = true;
    }
}
