using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using Smartstore.Core.DataExchange.Csv;
using Smartstore.Core.DataExchange.Excel;
using Smartstore.IO;

namespace Smartstore.Core.DataExchange.Import
{
    public class LightweightDataTable : IDataTable
    {
        private readonly IList<IDataColumn> _columns;
        private readonly IList<IDataRow> _rows;
        private readonly IDictionary<string, int> _columnIndexes;
        private readonly IDictionary<string, int> _alternativeColumnIndexes;

        public LightweightDataTable(IList<IDataColumn> columns, IList<object[]> data)
        {
            Guard.NotNull(columns, nameof(columns));
            Guard.NotNull(data, nameof(data));

            if (columns.Select(x => x.Name.ToLower()).Distinct().ToArray().Length != columns.Count)
            {
                throw Error.Argument("columns", "The columns collection cannot contain duplicate column names.");
            }

            _columns = new ReadOnlyCollection<IDataColumn>(columns);

            TrimData(data);

            var rows = data.Select(x => new LightweightDataRow(this, x)).Cast<IDataRow>().ToList();
            _rows = new ReadOnlyCollection<IDataRow>(rows);

            _columnIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _alternativeColumnIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < columns.Count; i++)
            {
                var name = columns[i].Name;
                var alternativeName = GetAlternativeColumnNameFor(name);

                _columnIndexes[name] = i;

                if (!alternativeName.EqualsNoCase(name))
                {
                    _alternativeColumnIndexes[alternativeName] = i;
                }
            }
        }

        public IList<IDataColumn> Columns => _columns;

        public IList<IDataRow> Rows => _rows;

        public bool HasColumn(string name)
        {
            if (name.HasValue())
            {
                return _columnIndexes.ContainsKey(name) || _alternativeColumnIndexes.ContainsKey(name);
            }

            return false;
        }

        public int GetColumnIndex(string name)
        {
            if (name.HasValue())
            {
                if (_columnIndexes.TryGetValue(name, out int index))
                    return index;

                if (_alternativeColumnIndexes.TryGetValue(name, out index))
                    return index;
            }

            return -1;
        }

        public static async Task<(bool Success, string Error)> IsValidFileAsync(IFile file)
        {
            try
            {
                using var stream = await file.OpenReadAsync();

                var table = FromFile(file.Name, stream, stream.Length, CsvConfiguration.ExcelFriendlyConfiguration, 0, 1);

                return (table != null, null);
            }
            catch (Exception ex)
            {
                return (false, ex.ToAllMessages());
            }
        }

        public static IDataTable FromFile(
            string fileName,
            Stream stream,
            long contentLength,
            CsvConfiguration configuration,
            int skip = 0,
            int take = int.MaxValue)
        {
            Guard.NotEmpty(fileName, nameof(fileName));
            Guard.NotNull(stream, nameof(stream));
            Guard.NotNull(configuration, nameof(configuration));

            if (contentLength == 0)
            {
                throw new ArgumentException($"The posted file '{fileName}' does not contain any data.", nameof(fileName));
            }

            IDataReader dataReader = null;

            try
            {
                var fileExt = Path.GetExtension(fileName).ToLowerInvariant();

                switch (fileExt)
                {
                    case ".xlsx":
                        dataReader = new ExcelReader(stream); // TODO: let the user specify if excel file has headers
                        break;
                    default:
                        dataReader = new CsvDataReader(new StreamReader(stream), configuration);
                        break;
                }

                var table = FromDataReader(dataReader, skip, take);

                if (table.Columns.Count == 0 || table.Rows.Count == 0)
                {
                    throw new InvalidOperationException($"The posted file '{fileName}' does not contain any columns or data rows.");
                }

                return table;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (dataReader != null && !dataReader.IsClosed)
                {
                    dataReader.Dispose();
                }
            }
        }

        public static IDataTable FromDataReader(
            IDataReader reader,
            int skip = 0,
            int take = int.MaxValue)
        {
            Guard.NotNull(reader, nameof(reader));

            if (reader.IsClosed)
                throw new ArgumentException("This operation is invalid when the reader is closed.", nameof(reader));

            var columns = new List<IDataColumn>(reader.FieldCount);
            var data = new List<object[]>();
            var schema = reader.GetSchemaTable();
            var nameCol = schema.Columns[SchemaTableColumn.ColumnName];
            var typeCol = schema.Columns[SchemaTableColumn.DataType];

            foreach (DataRow schemaRow in schema.Rows)
            {
                var column = new LightweightDataColumn((string)schemaRow[nameCol], (Type)schemaRow[typeCol]);
                columns.Add(column);
            }

            var fieldCount = reader.FieldCount;
            var i = -1;

            take = Math.Min(take, int.MaxValue - skip);

            while (reader.Read())
            {
                i++;

                if (skip > i)
                    continue;

                if (i >= skip + take)
                    break;

                var values = new object[fieldCount];
                reader.GetValues(values);
                data.Add(values);
            }

            var table = new LightweightDataTable(columns, data);
            return table;
        }

        public static string GetAlternativeColumnNameFor(string name)
        {
            if (name.IsEmpty())
                return name;

            return name
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("_", "");
        }

        private static void TrimData(IList<object[]> data)
        {
            // When a user deletes content instead of whole rows from an excel sheet,
            // our data table contains completely empty rows at the end.
            // Here we get rid of them as they are absolutely useless.
            for (int i = data.Count - 1; i >= 0; i--)
            {
                var allColumnsEmpty = data[i].All(x => x == null || x == DBNull.Value);
                if (allColumnsEmpty)
                {
                    data.RemoveAt(i);
                    //i--;
                }
                else
                {
                    // Get out here on the first occurence of a NON-empty row.
                    break;
                }
            }
        }
    }

    internal class LightweightDataRow : DynamicObject, IDataRow
    {
        private readonly IDataTable _table;
        private readonly object[] _values;

        public LightweightDataRow(IDataTable table, object[] values)
        {
            Guard.NotNull(values, nameof(values));

            if (table.Columns.Count != values.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(values), $"The number of row values must match the number of columns. Expected: {table.Columns.Count}, actual: {values.Length}");
            }

            _table = table;
            _values = values;
        }

        public IDataTable Table => _table;

        public object[] Values => _values;

        public object this[string name]
        {
            get
            {
                var index = _table.GetColumnIndex(name);
                if (index < 0)
                    throw new KeyNotFoundException();

                return _values[index];
            }
            set
            {
                var index = _table.GetColumnIndex(name);
                if (index < 0)
                    throw new KeyNotFoundException();

                _values[index] = value;
            }
        }

        public object this[int index]
        {
            get
            {
                ValidateColumnIndex(index);
                return _values[index];
            }
            set
            {
                ValidateColumnIndex(index);
                _values[index] = value;
            }
        }

        private void ValidateColumnIndex(int index)
        {
            if (index < 0 || index >= _table.Columns.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"Column index must be included within [0, {_table.Columns.Count}], but specified column index was: '{index}'.");
            }
        }

        public override IEnumerable<string> GetDynamicMemberNames()
            => _table.Columns.Select(x => x.Name);

        public override bool TryGetMember(GetMemberBinder binder, out object result)
            => this.TryGetValue(binder.Name, out result);

        public override bool TrySetMember(SetMemberBinder binder, object value)
            => this.TrySetValue(binder.Name, value);

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            result = null;

            try
            {
                result = _values[(int)indexes[0]];
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            try
            {
                _values[(int)indexes[0]] = value;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    internal class LightweightDataColumn : IDataColumn
    {
        public LightweightDataColumn(string name, Type type)
        {
            Guard.NotEmpty(name, nameof(name));
            Guard.NotNull(type, nameof(type));

            Name = name;
            Type = type;
        }

        public string Name { get; private set; }
        public Type Type { get; private set; }
    }
}
