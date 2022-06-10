using Smartstore.Collections;

namespace Smartstore.Core.Search.Indexing
{
    public class IndexDataSegmenter : IIndexDataSegmenter
    {
        private const int SEGMENT_SIZE = 500;

        private readonly IPageable _pageable;
        private readonly Func<int, int, Task<List<IIndexOperation>>> _segmentFactory;
        private List<IIndexOperation> _currentSegment;
        private bool _bof = true;

        public IndexDataSegmenter(int totalRecords, Func<int, int, Task<List<IIndexOperation>>> segmentFactory)
        {
            Guard.NotNull(segmentFactory, nameof(segmentFactory));

            _pageable = new PagedDataList(0, SEGMENT_SIZE, totalRecords);
            _segmentFactory = segmentFactory;
        }

        public async Task<IEnumerable<IIndexOperation>> GetCurrentSegmentAsync()
        {
            _currentSegment ??= await _segmentFactory(_pageable.FirstItemIndex - 1, SEGMENT_SIZE);

            return _currentSegment;
        }

        public int CurrentSegmentIndex => _pageable.PageIndex;

        public int SegmentSize => SEGMENT_SIZE;

        public int TotalDocuments => _pageable.TotalCount;

        public bool ReadNextSegment()
        {
            if (_currentSegment != null)
            {
                _currentSegment.Clear();
                _currentSegment = null;
            }

            if (_bof)
            {
                _bof = false;
                return _pageable.TotalCount > 0;
            }

            if (_pageable.HasNextPage)
            {
                _pageable.PageIndex++;
                return true;
            }

            Reset();
            return false;
        }

        private void Reset()
        {
            if (_pageable.PageIndex != 0 && _currentSegment != null)
            {
                _currentSegment.Clear();
                _currentSegment = null;
            }

            _bof = true;
            _pageable.PageIndex = 0;
        }

        class PagedDataList : PagedListBase
        {
            public PagedDataList(int pageIndex, int pageSize, int totalItemsCount)
                : base(pageIndex, pageSize, totalItemsCount)
            {
            }
        }
    }
}
