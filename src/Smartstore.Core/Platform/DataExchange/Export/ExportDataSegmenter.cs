namespace Smartstore.Core.DataExchange.Export
{
    public interface IExportDataSegmenterConsumer
    {
        /// <summary>
        /// Total number of records.
        /// </summary>
        int TotalRecords { get; }

        /// <summary>
        /// Gets current data segment.
        /// </summary>
        Task<IReadOnlyCollection<dynamic>> GetCurrentSegmentAsync();

        /// <summary>
        /// Reads the next segment.
        /// </summary>
        /// <returns><c>true</c> succeeded, <c>false</c> failed.</returns>
        Task<bool> ReadNextSegmentAsync();
    }

    internal interface IExportDataSegmenterProvider : IExportDataSegmenterConsumer, IDisposable
    {
        /// <summary>
        /// A value indicating whether there is data available.
        /// </summary>
        bool HasData { get; }

        /// <summary>
        /// Gets or sets the record per segment counter.
        /// </summary>
        int RecordPerSegmentCount { get; set; }
    }


    public class ExportDataSegmenter<T> : Disposable, IExportDataSegmenterProvider where T : BaseEntity
    {
        private readonly Func<Task<IEnumerable<T>>> _dataLoader;
        private readonly Func<ICollection<T>, Task> _dataLoaded;
        private readonly Func<T, Task<IEnumerable<dynamic>>> _dataConverter;

        private readonly int _offset;
        private readonly int _take;
        private readonly int _limit;
        private readonly int _recordsPerSegment;
        private readonly int _totalRecords;

        private Queue<T> _data;
        private bool _endOfData;

        public ExportDataSegmenter(
            Func<Task<IEnumerable<T>>> dataLoader,
            Func<ICollection<T>, Task> dataLoaded,
            Func<T, Task<IEnumerable<dynamic>>> dataConverter,
            int offset,
            int take,
            int limit,
            int recordsPerSegment,
            int totalRecords)
        {
            _dataLoader = dataLoader;
            _dataLoaded = dataLoaded;
            _dataConverter = dataConverter;
            _offset = offset;
            _take = take;
            _limit = limit;
            _recordsPerSegment = recordsPerSegment;
            _totalRecords = totalRecords;
        }

        /// <inheritdoc/>
        public int TotalRecords
        {
            get
            {
                var total = Math.Max(_totalRecords - _offset, 0);

                if (_limit > 0 && _limit < total)
                {
                    return _limit;
                }

                return total;
            }
        }

        /// <summary>
        /// Number of processed records.
        /// </summary>
        public int RecordCount { get; private set; }

        /// <inheritdoc/>
        public int RecordPerSegmentCount { get; set; }

        /// <inheritdoc/>
        public bool HasData
        {
            get
            {
                if (_limit > 0 && RecordCount >= _limit)
                {
                    return false;
                }

                if (_data != null && _data.Count > 0)
                {
                    return true;
                }

                if (_endOfData)
                {
                    return false;
                }

                return RecordCount < TotalRecords;
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyCollection<dynamic>> GetCurrentSegmentAsync()
        {
            T entity;
            var records = new List<dynamic>();

            while (_data.Count > 0 && (entity = _data.Dequeue()) != null)
            {
                var convertedData = await _dataConverter(entity);
                convertedData.Each(x => records.Add(x));

                if (++RecordCount >= _limit && _limit > 0)
                {
                    return records;
                }

                if (++RecordPerSegmentCount >= _recordsPerSegment && _recordsPerSegment > 0)
                {
                    return records;
                }
            }

            return records;
        }

        /// <inheritdoc/>
        public async Task<bool> ReadNextSegmentAsync()
        {
            if (_limit > 0 && RecordCount >= _limit)
            {
                return false;
            }

            if (_recordsPerSegment > 0 && RecordPerSegmentCount >= _recordsPerSegment)
            {
                return false;
            }

            // Do not make the queue longer than necessary.
            if (_recordsPerSegment > 0 && _data != null && _data.Count >= _recordsPerSegment)
            {
                return true;
            }

            var newData = await _dataLoader();

            if (_data != null && _data.Count > 0)
            {
                var data = new List<T>(_data);
                if (newData != null)
                {
                    data.AddRange(newData);
                }

                _data = new Queue<T>(data);
            }
            else
            {
                if (newData == null)
                {
                    // End of data reached.
                    _endOfData = true;
                    return false;
                }

                _data = new Queue<T>(newData);
            }

            // Give provider the opportunity to do something based on loaded entities.
            if (_dataLoaded != null)
            {
                await _dataLoaded(_data.AsReadOnly());
            }

            return _data.Count > 0;
        }

        /// <summary>
        /// Dispose and reset segmenter instance.
        /// </summary>
        protected override void OnDispose(bool disposing)
        {
            RecordCount = 0;
            RecordPerSegmentCount = 0;

            _endOfData = false;
            _data?.Clear();
        }
    }
}
