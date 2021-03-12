using System.Collections;

namespace Smartstore.Collections
{
    /// <summary>
    /// A collection of objects that has been split into pages.
    /// </summary>
    public interface IPageable : IEnumerable
    {
        /// <summary>
        /// The 0-based current page index
        /// </summary>
        int PageIndex { get; set; }

        /// <summary>
        /// The number of items in each page.
        /// </summary>
        int PageSize { get; set; }

        /// <summary>
        /// The total number of items.
        /// </summary>
        int TotalCount { get; set; }


        /// <summary>
        /// The 1-based current page index
        /// </summary>
        int PageNumber { get; set; }

        /// <summary>
        /// The total number of pages.
        /// </summary>
        int TotalPages { get; }

        /// <summary>
        /// Whether there are pages before the current page.
        /// </summary>
        bool HasPreviousPage { get; }

        /// <summary>
        /// Whether there are pages after the current page.
        /// </summary>
        bool HasNextPage { get; }

        /// <summary>
        /// The 1-based index of the first item in the page.
        /// </summary>
        int FirstItemIndex { get; }

        /// <summary>
        /// The 1-based index of the last item in the page.
        /// </summary>
        int LastItemIndex { get; }

        /// <summary>
        /// Whether the page is the first page
        /// </summary>
        bool IsFirstPage { get; }

        /// <summary>
        /// Whether the page is the last page
        /// </summary>
        bool IsLastPage { get; }
    }
}