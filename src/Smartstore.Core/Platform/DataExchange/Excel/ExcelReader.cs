using System.Data;
using System.Data.Common;
using System.Globalization;
using ExcelDataReader;

namespace Smartstore.Core.DataExchange.Excel
{
    /// <summary>
    /// Reader for reading Microsoft Excel files.
    /// </summary>
    public partial class ExcelReader : Disposable, IDataReader
    {
        private IExcelDataReader _reader;
        private DataTable _schemaTable;
        private DataColumn[] _columns;
        private Dictionary<string, int> _columnIndexes;
        private int _totalRows;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="source">The source stream. Will be closed as soon as the <see cref="ExcelReader"/> is disposed.</param>
        public ExcelReader(Stream source)
            : this(source, true, "Column")
        {
        }

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="source">The source stream. Will be closed as soon as the <see cref="ExcelReader"/> is disposed.</param>
        public ExcelReader(Stream source, bool hasHeaders, string defaultColumnName)
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotEmpty(defaultColumnName, nameof(defaultColumnName));

            HasHeaders = hasHeaders;
            DefaultColumnName = defaultColumnName;

            _reader = ExcelReaderFactory.CreateReader(source);
            if (_reader == null)
            {
                throw new InvalidOperationException("Failed to create an Excel reader for given stream.");
            }

            Init();
        }

        protected virtual void Init()
        {
            _schemaTable = CreateSchemaTable();
            _schemaTable.MinimumCapacity = _reader.FieldCount;

            _columns = new DataColumn[_reader.FieldCount];
            _columnIndexes = new Dictionary<string, int>(_reader.FieldCount, StringComparer.OrdinalIgnoreCase);
            _totalRows = _reader.RowCount - (HasHeaders ? 1 : 0);

            // Get columns.
            var readHeaders = HasHeaders && _reader.Read();

            for (var i = 0; i < _reader.FieldCount; ++i)
            {
                var columnName = readHeaders
                    ? Convert.ToString(_reader.GetValue(i))
                    : null;

                if (columnName.IsEmpty())
                {
                    columnName = DefaultColumnName + i;
                }

                _columns[i] = new DataColumn(columnName, typeof(object))
                {
                    Caption = columnName
                };
                _columnIndexes[columnName] = i - 1;
            }

            if (_columnIndexes.Count != _reader.FieldCount)
            {
                _columns = null;
                _columnIndexes = null;

                throw new InvalidOperationException("The first row of the Excel table must not contain duplicate column names.");
            }

            // Add rows to schema table (if any).
            if (_reader.Read())
            {
                for (var i = 0; i < _reader.FieldCount; ++i)
                {
                    var column = _columns[i];
                    column.DataType = _reader.GetValue(i)?.GetType() ?? typeof(string);

                    _schemaTable.Rows.Add(new object[] {
                        true,                   // 00- AllowDBNull
                        column.ColumnName,      // 01- BaseColumnName
                        string.Empty,           // 02- BaseSchemaName
                        string.Empty,           // 03- BaseTableName
                        column.ColumnName,      // 04- ColumnName
                        i,                      // 05- ColumnOrdinal
                        int.MaxValue,           // 06- ColumnSize
                        column.DataType,        // 07- DataType
                        false,                  // 08- IsAliased
                        false,                  // 09- IsExpression
                        false,                  // 10- IsKey
                        false,                  // 11- IsLong
                        false,                  // 12- IsUnique
                        DBNull.Value,           // 13- NumericPrecision
                        DBNull.Value,           // 14- NumericScale
                        (int)DbType.String,     // 15- ProviderType
                        string.Empty,           // 16- BaseCatalogName
                        string.Empty,           // 17- BaseServerName
                        false,                  // 18- IsAutoIncrement
                        false,                  // 19- IsHidden
                        true,                   // 20- IsReadOnly
                        false                   // 21- IsRowVersion
                    });
                }
            }
        }

        public bool HasHeaders { get; private set; }

        public string DefaultColumnName { get; private set; }

        public int TotalRows => _totalRows;

        public IReadOnlyCollection<DataColumn> GetColumnHeaders() => _columns.AsReadOnly();

        #region IDataReader

        public int Depth { get; private set; } = -1;

        public bool IsClosed => _reader == null;

        public int RecordsAffected => -1;

        public int FieldCount => _reader.FieldCount;

        public object this[string name]
        {
            get
            {
                var index = GetOrdinal(name);

                if (index < 0)
                {
                    throw new ArgumentException($"Excel column header '{name}' not found.", nameof(name));
                }

                return this[index];
            }
        }

        public object this[int i] => _reader[i];

        public DataTable GetSchemaTable() => _schemaTable;

        public bool NextResult() => _reader.NextResult();

        public bool Read()
        {
            if (Depth == -1)
            {
                // Do not read. First row has already been read for the schema table.
                Depth = 1;
                return true;
            }
            else if (_reader.Read())
            {
                ++Depth;
                return true;
            }

            return false;
        }

        public bool GetBoolean(int i) => _reader.GetBoolean(i);

        public byte GetByte(int i) => _reader.GetByte(i);

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
            => CopyFieldToArray(i, fieldOffset, buffer, bufferoffset, length);

        public char GetChar(int i) => _reader.GetChar(i);

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
             => CopyFieldToArray(i, fieldoffset, buffer, bufferoffset, length);

        public IDataReader GetData(int i) => _reader.GetData(i);

        public string GetDataTypeName(int i) => _reader.GetDataTypeName(i);

        public DateTime GetDateTime(int i) => _reader.GetDateTime(i);

        public decimal GetDecimal(int i) => _reader.GetDecimal(i);

        public double GetDouble(int i) => _reader.GetDouble(i);

        public Type GetFieldType(int i) => _reader.GetFieldType(i);

        public float GetFloat(int i) => _reader.GetFloat(i);

        public Guid GetGuid(int i) => _reader.GetGuid(i);

        public short GetInt16(int i) => _reader.GetInt16(i);

        public int GetInt32(int i) => _reader.GetInt32(i);

        public long GetInt64(int i) => _reader.GetInt64(i);

        public string GetName(int i)
        {
            ValidateColumnIndex(i);

            return _columns[i].ColumnName;
        }

        public int GetOrdinal(string name)
        {
            int index = -1;
            _columnIndexes?.TryGetValue(name, out index);

            return index;
        }

        public string GetString(int i) => _reader.GetString(i);

        public object GetValue(int i) => _reader.GetValue(i);

        public int GetValues(object[] values)
        {
            var record = (IDataRecord)this;

            for (var i = 0; i < _reader.FieldCount; ++i)
            {
                values[i] = record.GetValue(i);
            }

            return _reader.FieldCount;
        }

        public bool IsDBNull(int i) => _reader.IsDBNull(i);

        public void Close() => DisposeInternal();

        #endregion

        #region Disposable

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                DisposeInternal();
            }
        }

        protected virtual void DisposeInternal()
        {
            try
            {
                _reader?.Close();
                _reader?.Dispose();
                _reader = null;

                _schemaTable = null;
                _columns = null;
                _columnIndexes = null;
                _totalRows = 0;
            }
            catch
            {
            }
        }

        #endregion

        #region Utilities

        private static DataTable CreateSchemaTable()
        {
            var schema = new DataTable("SchemaTable")
            {
                Locale = CultureInfo.InvariantCulture
            };

            schema.Columns.Add(SchemaTableColumn.AllowDBNull, typeof(bool)).ReadOnly = true;
            schema.Columns.Add(SchemaTableColumn.BaseColumnName, typeof(string)).ReadOnly = true;
            schema.Columns.Add(SchemaTableColumn.BaseSchemaName, typeof(string)).ReadOnly = true;
            schema.Columns.Add(SchemaTableColumn.BaseTableName, typeof(string)).ReadOnly = true;
            schema.Columns.Add(SchemaTableColumn.ColumnName, typeof(string)).ReadOnly = true;
            schema.Columns.Add(SchemaTableColumn.ColumnOrdinal, typeof(int)).ReadOnly = true;
            schema.Columns.Add(SchemaTableColumn.ColumnSize, typeof(int)).ReadOnly = true;
            schema.Columns.Add(SchemaTableColumn.DataType, typeof(object)).ReadOnly = true;
            schema.Columns.Add(SchemaTableColumn.IsAliased, typeof(bool)).ReadOnly = true;
            schema.Columns.Add(SchemaTableColumn.IsExpression, typeof(bool)).ReadOnly = true;
            schema.Columns.Add(SchemaTableColumn.IsKey, typeof(bool)).ReadOnly = true;
            schema.Columns.Add(SchemaTableColumn.IsLong, typeof(bool)).ReadOnly = true;
            schema.Columns.Add(SchemaTableColumn.IsUnique, typeof(bool)).ReadOnly = true;
            schema.Columns.Add(SchemaTableColumn.NumericPrecision, typeof(short)).ReadOnly = true;
            schema.Columns.Add(SchemaTableColumn.NumericScale, typeof(short)).ReadOnly = true;
            schema.Columns.Add(SchemaTableColumn.ProviderType, typeof(int)).ReadOnly = true;

            schema.Columns.Add(SchemaTableOptionalColumn.BaseCatalogName, typeof(string)).ReadOnly = true;
            schema.Columns.Add(SchemaTableOptionalColumn.BaseServerName, typeof(string)).ReadOnly = true;
            schema.Columns.Add(SchemaTableOptionalColumn.IsAutoIncrement, typeof(bool)).ReadOnly = true;
            schema.Columns.Add(SchemaTableOptionalColumn.IsHidden, typeof(bool)).ReadOnly = true;
            schema.Columns.Add(SchemaTableOptionalColumn.IsReadOnly, typeof(bool)).ReadOnly = true;
            schema.Columns.Add(SchemaTableOptionalColumn.IsRowVersion, typeof(bool)).ReadOnly = true;

            return schema;
        }

        private void ValidateColumnIndex(int index)
        {
            if (index < 0 || index >= _columns.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"Excel column index must be included within [0, {_columns.Length}], but specified column index was '{index}'.");
            }
        }

        private long CopyFieldToArray(int column, long columnOffset, Array destinationArray, int destinationOffset, int length)
        {
            ValidateColumnIndex(column);

            if (columnOffset < 0 || columnOffset >= int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(columnOffset));
            }

            if (length == 0)
            {
                return 0;
            }

            var value = this[column].ToString() ?? string.Empty;

            if (destinationArray.GetType() == typeof(char[]))
            {
                Array.Copy(value.ToCharArray((int)columnOffset, length), 0, destinationArray, destinationOffset, length);
            }
            else
            {
                char[] chars = value.ToCharArray((int)columnOffset, length);
                byte[] source = new byte[chars.Length];

                for (var i = 0; i < chars.Length; i++)
                {
                    source[i] = Convert.ToByte(chars[i]);
                }

                Array.Copy(source, 0, destinationArray, destinationOffset, length);
            }

            return length;
        }

        #endregion
    }
}
