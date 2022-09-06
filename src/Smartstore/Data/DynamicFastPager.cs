using System.Linq.Dynamic.Core;
using Smartstore.Threading;

namespace Smartstore.Data
{
    /// <summary>
    /// Dynamic variant of <see cref="FastPager{T}"/>.
    /// </summary>
    public sealed class DynamicFastPager
    {
        private readonly IQueryable _query;
        private readonly int _pageSize;

        private int? _maxId;
        private int? _currentPage;

        public DynamicFastPager(IQueryable query, int pageSize = 1000)
        {
            Guard.NotNull(query, nameof(query));
            Guard.IsPositive(pageSize, nameof(pageSize));

            _query = query;
            _pageSize = pageSize;
        }

        public void Reset()
        {
            _maxId = null;
            _currentPage = null;
        }

        public int? MaxId => _maxId;

        public int? CurrentPage => _currentPage;

        public bool ReadNextPage(out IList<dynamic> page)
        {
            return ReadNextPage(null, out page);
        }

        public bool ReadNextPage(string selector, out IList<dynamic> page)
        {
            page = null;

            if (_maxId == null)
            {
                _maxId = int.MaxValue;
                _currentPage = 0;
            }
            if (_maxId.Value <= 1)
            {
                return false;
            }

            var query = _query
                .Where("Id < @0", _maxId.Value)
                .OrderBy("Id desc")
                .Take(_pageSize);

            if (selector.HasValue())
            {
                query = query.Select(selector);
            }

            page = query.ToDynamicList();

            if (page.Count == 0)
            {
                _maxId = -1;
                page = null;
                return false;
            }

            _currentPage++;
            _maxId = page.Last().Id;
            return true;
        }

        public Task<AsyncOut<IList<dynamic>>> ReadNextPageAsync()
        {
            return ReadNextPageAsync(null);
        }

        public async Task<AsyncOut<IList<dynamic>>> ReadNextPageAsync(string selector)
        {
            if (_maxId == null)
            {
                _maxId = int.MaxValue;
                _currentPage = 0;
            }
            if (_maxId.Value <= 1)
            {
                return AsyncOut<IList<dynamic>>.Empty;
            }

            var query = _query
                .Where("Id < @0", _maxId.Value)
                .OrderBy("Id desc")
                .Take(_pageSize);

            if (selector.HasValue())
            {
                query = query.Select(selector);
            }

            var page = await query.ToDynamicListAsync();

            if (page.Count == 0)
            {
                _maxId = -1;
                return AsyncOut<IList<dynamic>>.Empty;
            }

            _currentPage++;
            _maxId = page.Last().Id;
            return new AsyncOut<IList<dynamic>>(true, page);
        }
    }
}
