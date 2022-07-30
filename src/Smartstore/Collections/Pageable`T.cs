using System.Collections;

using Microsoft.EntityFrameworkCore;

namespace Smartstore.Collections
{
    public class Pageable<T> : IPageable<T>
    {
        private bool _queryIsPagedAlready;
        private int? _totalCount;

        private List<T> _list;

        protected Pageable(IPageable<T> pageable)
        {
            Guard.NotNull(pageable, nameof(pageable));

            Init(pageable.SourceQuery, pageable.PageIndex, pageable.PageSize, pageable.TotalCount);
        }

        /// <param name="pageIndex">The 0-based page index</param>
        public Pageable(IEnumerable<T> source, int pageIndex, int pageSize)
        {
            Guard.NotNull(source, nameof(source));

            Init(source.AsQueryable(), pageIndex, pageSize, null);
        }

        /// <param name="pageIndex">The 0-based page index</param>
        public Pageable(IEnumerable<T> source, int pageIndex, int pageSize, int totalCount)
        {
            Guard.NotNull(source, nameof(source));

            Init(source.AsQueryable(), pageIndex, pageSize, totalCount);
        }

        /// <param name="pageIndex">The 0-based page index</param>
        public Pageable(int pageIndex, int pageSize, int totalCount)
        {
            Init(Enumerable.Empty<T>().AsQueryable(), pageIndex, pageSize, totalCount);
        }

        private void Init(IQueryable<T> source, int pageIndex, int pageSize, int? totalCount)
        {
            Guard.NotNull(source, nameof(source));
            Guard.PagingArgsValid(pageIndex, pageSize, nameof(pageIndex), nameof(pageSize));

            SourceQuery = source;
            PageIndex = pageIndex;
            PageSize = pageSize;

            _totalCount = totalCount;
            _queryIsPagedAlready = totalCount.HasValue;
        }

        protected virtual void EnsureIsLoaded()
        {
            if (_list == null)
            {
                if (_totalCount == null)
                {
                    _totalCount = SourceQuery.Count();
                }

                if (_queryIsPagedAlready)
                {
                    _list = SourceQuery.ToList();
                }
                else
                {
                    _list = ApplyPaging().ToList();
                }
            }
        }

        protected virtual async Task EnsureIsLoadedAsync(CancellationToken cancellationToken = default)
        {
            if (_list == null)
            {
                if (SourceQuery is not IAsyncEnumerable<T>)
                {
                    // Don't call EF's async extension methods if query is not IAsyncEnumerable<T>
                    EnsureIsLoaded();
                    return;
                }

                if (_totalCount == null)
                {
                    _totalCount = await SourceQuery.CountAsync(cancellationToken);
                }

                if (_queryIsPagedAlready)
                {
                    _list = await SourceQuery.ToListAsync(cancellationToken);
                }
                else
                {
                    _list = await ApplyPaging().ToListAsync(cancellationToken);
                }
            }
        }

        protected List<T> List
        {
            get => _list;
            set => _list = value;
        }

        #region IPageable

        public int PageIndex { get; set; }

        public int PageSize { get; set; }

        public int TotalCount
        {
            get
            {
                if (!_totalCount.HasValue)
                {
                    _totalCount = SourceQuery.Count();
                }

                return _totalCount.Value;
            }
            set
            {
                _totalCount = value;
            }
        }

        public int PageNumber
        {
            get => PageIndex + 1;
            set => PageIndex = value - 1;
        }

        public int TotalPages
        {
            get
            {
                var total = TotalCount / PageSize;

                if (TotalCount % PageSize > 0)
                    total++;

                return total;
            }
        }

        public bool HasPreviousPage
        {
            get => PageIndex > 0;
        }

        public bool HasNextPage
        {
            get => (PageIndex < (TotalPages - 1));
        }

        public int FirstItemIndex
        {
            get => (PageIndex * PageSize) + 1;
        }

        public int LastItemIndex
        {
            get => Math.Min(TotalCount, ((PageIndex * PageSize) + PageSize));
        }

        public bool IsFirstPage
        {
            get => (PageIndex <= 0);
        }

        public bool IsLastPage
        {
            get => (PageIndex >= (TotalPages - 1));
        }

        #endregion

        #region IPageable<T>

        public IQueryable<T> SourceQuery { get; protected set; }

        public virtual async Task<int> GetTotalCountAsync()
        {
            if (!_totalCount.HasValue)
            {
                _totalCount = SourceQuery is IAsyncEnumerable<T>
                    ? await SourceQuery.CountAsync()
                    : SourceQuery.Count();
            }

            return _totalCount.Value;
        }

        public IQueryable<T> ApplyPaging()
        {
            return SourceQuery.ApplyPaging(PageIndex, PageSize);
        }

        #endregion

        #region Enumerator

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            EnsureIsLoaded();
            return _list.GetEnumerator();
        }

        #endregion
    }

    public abstract class PagedListBase : IPageable
    {
        protected PagedListBase()
        {
            PageIndex = 0;
            PageSize = 0;
            TotalCount = 1;
        }

        protected PagedListBase(IPageable pageable)
        {
            Init(pageable);
        }

        protected PagedListBase(int pageIndex, int pageSize, int totalItemsCount)
        {
            Guard.PagingArgsValid(pageIndex, pageSize, "pageIndex", "pageSize");

            PageIndex = pageIndex;
            PageSize = pageSize;
            TotalCount = totalItemsCount;
        }

        public void LoadPagedList<T>(IPagedList<T> pagedList)
        {
            Init(pagedList);
        }

        protected void Init(IPageable pageable)
        {
            Guard.NotNull(pageable, "pageable");

            PageIndex = pageable.PageIndex;
            PageSize = pageable.PageSize;
            TotalCount = pageable.TotalCount;
        }

        public int PageIndex
        {
            get;
            set;
        }

        public int PageSize
        {
            get;
            set;
        }

        public int TotalCount
        {
            get;
            set;
        }

        public int PageNumber
        {
            get => PageIndex + 1;
            set => PageIndex = value - 1;
        }

        public int TotalPages
        {
            get
            {
                if (PageSize == 0)
                    return 0;

                var total = TotalCount / PageSize;

                if (TotalCount % PageSize > 0)
                    total++;

                return total;
            }
        }

        public bool HasPreviousPage
        {
            get => PageIndex > 0;
        }

        public bool HasNextPage
        {
            get => (PageIndex < (TotalPages - 1));
        }

        public int FirstItemIndex
        {
            get => (PageIndex * PageSize) + 1;
        }

        public int LastItemIndex
        {
            get => Math.Min(TotalCount, ((PageIndex * PageSize) + PageSize));
        }

        public bool IsFirstPage
        {
            get => (PageIndex <= 0);
        }

        public bool IsLastPage
        {
            get => (PageIndex >= (TotalPages - 1));
        }

        public virtual IEnumerator GetEnumerator()
        {
            return Enumerable.Empty<int>().GetEnumerator();
        }
    }
}
