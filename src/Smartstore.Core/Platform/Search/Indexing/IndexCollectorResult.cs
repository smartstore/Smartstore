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
        public IndexDataUnit(IIndexDataSegmenter segmenter, string documentType)
        {
            Guard.NotNull(segmenter, nameof(segmenter));

            Segmenter = segmenter;
            DocumentType = documentType;
        }

        public IIndexDataSegmenter Segmenter { get; }
        public string DocumentType { get; }
        public virtual string LocalizedDocumentType { get; set; }

        public virtual string Id { get; set; }
        public virtual bool Rebuild { get; set; } = true;
    }
}
