namespace Smartstore.Core.Search.Indexing
{
    public class SeekingIndexDataSegmenter : IIndexDataSegmenter
    {
        private const int SEGMENT_SIZE = 500;

        private readonly int _maxId;
        private readonly Func<int, int, Task<List<IIndexOperation>>> _segmentFactory;
        private List<IIndexOperation> _currentSegment;
        private bool _bof = true;
        private int _lastId;

        public SeekingIndexDataSegmenter(
            int totalRecords,
            int maxId,
            Func<int, int, Task<List<IIndexOperation>>> segmentFactory)
        {
            Guard.NotNull(segmentFactory, nameof(segmentFactory));

            TotalDocuments = totalRecords;
            _maxId = maxId;
            _segmentFactory = segmentFactory;
        }

        public async Task<IEnumerable<IIndexOperation>> GetCurrentSegmentAsync()
        {
            _currentSegment ??= await _segmentFactory(_lastId, SEGMENT_SIZE);

            return _currentSegment;
        }

        public int SegmentSize => SEGMENT_SIZE;

        public int CurrentSegmentIndex { get; private set; }

        public int TotalDocuments { get; }

        public bool ReadNextSegment()
        {
            if (_currentSegment != null)
            {
                _lastId = _currentSegment[^1].Entity.Id;

                _currentSegment.Clear();
                _currentSegment = null;
            }

            if (_bof)
            {
                _bof = false;
                return TotalDocuments > 0;
            }

            if (_lastId < _maxId)
            {
                CurrentSegmentIndex++;
                return true;
            }

            Reset();
            return false;
        }

        private void Reset()
        {
            if (_currentSegment != null)
            {
                _currentSegment.Clear();
                _currentSegment = null;
            }

            _bof = true;
            CurrentSegmentIndex = 0;
        }
    }
}
