using System.Globalization;
using System.Runtime.CompilerServices;
using Smartstore.Collections;

namespace Smartstore.Core.DataExchange.Import
{
    public interface IImportDataSegmenterConsumer
    {
        /// <summary>
        /// Total number of rows.
        /// </summary>
        int TotalRows { get; }

        /// <summary>
        /// Total number of columgs.
        /// </summary>
        int TotalColumns { get; }

        /// <summary>
        /// Number of current segment.
        /// </summary>
        int CurrentSegment { get; }

        /// <summary>
        /// Index of the first row in current segment.
        /// </summary>
        int CurrentSegmentFirstRowIndex { get; }

        /// <summary>
        /// Total number of segments.
        /// </summary>
        int TotalSegments { get; }

        /// <summary>
        /// Gets a value indicating whether the current segment is the last one.
        /// </summary>
        bool IsLastSegment { get; }

        /// <summary>
        /// Determines whether a specific column exists in the underlying data table.
        /// </summary>
        /// <param name="name">The name of the column to find.</param>
        /// <param name="withAnyIndex">
        ///		If <c>true</c> and a column with the passed <paramref name="name"/> does not exist,
        ///		this method tests for the existence of any indexed column with the same name.
        /// </param>
        /// <returns><c>true</c> if the column exists, <c>false</c> otherwise.</returns>
        /// <remarks>This method takes mapped column names into account.</remarks>
        bool HasColumn(string name, bool withAnyIndex = false);

        /// <summary>
        /// Determines whether the column <c>name[index]</c> exists in the underlying data table.
        /// </summary>
        /// <param name="name">The name of the column to find.</param>
        /// <param name="index">The index of the column.</param>
        /// <returns><c>true</c> if the column exists, <c>false</c> otherwise.</returns>
        /// <remarks>This method takes mapped column names into account.</remarks>
        bool HasColumn(string name, string index);

        /// <summary>
        /// Indicates whether to ignore the property that is mapped to <paramref name="columnName"/>.
        /// </summary>
        /// <param name="columnName">The name of the column.</param>
        /// <returns><c>true</c> ignore, <c>false</c> do not ignore.</returns>
        bool IsIgnored(string columnName);

        /// <summary>
        /// Indicates whether to ignore the property that is mapped to <paramref name="columnName"/>.
        /// </summary>
        /// <param name="columnName">The name of the column.</param>
        /// <param name="index">The index of the column.</param>
        /// <returns><c>true</c> ignore, <c>false</c> do not ignore.</returns>
        bool IsIgnored(string columnName, string index);

        /// <summary>
        /// Returns an array of exisiting index names for a column.
        /// </summary>
        /// <param name="name">The name of the columns without index qualification.</param>
        /// <returns>An array of index names.</returns>
        /// <remarks>
        /// If following columns exist in source: Attr[Color], Attr[Size]
        /// This method returns: <code>string[] { "Color", "Size" }.</code> 
        /// </remarks>
        string[] GetColumnIndexes(string name);

        /// <summary>
        /// Gets the current batch of data (all rows of current segment).
        /// </summary>
        /// <returns>Current batch of data.</returns>
        IEnumerable<ImportRow<T>> GetCurrentBatch<T>() where T : BaseEntity;
    }

    public class ImportDataSegmenter : IImportDataSegmenterConsumer
    {
        private const int BATCHSIZE = 100;

        private readonly IDataTable _table;
        private object[] _currentBatch;
        private readonly IPageable _pageable;
        private bool _bof;
        private CultureInfo _culture;
        private ColumnMap _columnMap;

        private readonly Dictionary<string, string[]> _columnIndexes = new(StringComparer.OrdinalIgnoreCase);

        public ImportDataSegmenter(IDataTable table, ColumnMap map)
        {
            Guard.NotNull(table, nameof(table));
            Guard.NotNull(map, nameof(map));

            _table = table;
            _columnMap = map;

            _bof = true;
            _pageable = new PagedDataList(0, BATCHSIZE, table.Rows.Count);
            _culture = CultureInfo.InvariantCulture;
        }

        public CultureInfo Culture
        {
            get => _culture;
            set => _culture = value ?? CultureInfo.InvariantCulture;
        }

        public ColumnMap ColumnMap
        {
            get => _columnMap;
            set => _columnMap = value ?? new ColumnMap();
        }

        /// <inheritdoc/>
        public int TotalRows => _table.Rows.Count;

        /// <inheritdoc/>
        public int TotalColumns => _table.Columns.Count;

        /// <inheritdoc/>
        public int CurrentSegment => _bof ? 0 : _pageable.PageNumber;

        /// <inheritdoc/>
        public int CurrentSegmentFirstRowIndex => _pageable.FirstItemIndex;

        /// <inheritdoc/>
        public int TotalSegments => _pageable.TotalPages;

        /// <inheritdoc/>
        public bool IsLastSegment => CurrentSegment == TotalSegments;

        public static int BatchSize => BATCHSIZE;

        /// <inheritdoc/>
        public bool HasColumn(string name, bool withAnyIndex = false)
        {
            var result = HasColumn(name, null);

            if (!result && withAnyIndex)
            {
                // Column does not exist, but withAnyIndex is true:
                // Test for existence of any indexed column.
                result = GetColumnIndexes(name).Length > 0;
            }

            return result;
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasColumn(string name, string index)
            => _table.HasColumn(_columnMap.GetMapping(name, index).MappedName);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsIgnored(string columnName)
            => IsIgnored(columnName, null);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsIgnored(string columnName, string index)
        {
            var mapping = _columnMap.GetMapping(columnName, index);
            return mapping.IgnoreProperty;
        }

        /// <inheritdoc/>
        public string[] GetColumnIndexes(string name)
        {
            if (!_columnIndexes.TryGetValue(name, out string[] indexes))
            {
                var startsWith = name + "[";

                var columns1 = _columnMap.Mappings
                    .Where(x => x.Key.StartsWith(startsWith, StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.Key);

                var columns2 = _table.Columns
                    .Where(x => x.Name.StartsWith(startsWith, StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.Name);

                indexes = columns1.Concat(columns2)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(x => x[(x.IndexOf("[", StringComparison.OrdinalIgnoreCase) + 1)..].TrimEnd(']'))
                    .ToArray();

                _columnIndexes[name] = indexes;
            }

            return indexes;
        }

        public void Reset()
        {
            if (_pageable.PageIndex != 0 && _currentBatch != null)
            {
                _currentBatch = null;
            }

            _bof = true;
            _pageable.PageIndex = 0;
        }

        public bool ReadNextBatch()
        {
            if (_currentBatch != null)
            {
                _currentBatch = null;
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

        /// <inheritdoc/>
        public IEnumerable<ImportRow<T>> GetCurrentBatch<T>() where T : BaseEntity
        {
            if (_currentBatch == null)
            {
                var start = _pageable.FirstItemIndex - 1;
                var end = _pageable.LastItemIndex - 1;

                _currentBatch = new ImportRow<T>[end - start + 1];

                // Determine values per row.
                var i = 0;
                for (var r = start; r <= end; r++)
                {
                    _currentBatch[i] = new ImportRow<T>(this, _table.Rows[r], r);
                    i++;
                }
            }

            return _currentBatch.Cast<ImportRow<T>>();
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
