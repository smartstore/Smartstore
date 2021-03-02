using System;
using System.Collections;
using System.Linq;

namespace Smartstore.Collections
{
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
