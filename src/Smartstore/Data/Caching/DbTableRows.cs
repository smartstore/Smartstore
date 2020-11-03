using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace Smartstore.Data.Caching
{
    public class DbTableRows
    {
        private Dictionary<int, DbTableColumnInfo> _columnInfos;
        private Dictionary<string, int> _columnNameOrdinalMap;

        public DbTableRows()
        {
            _columnInfos = new Dictionary<int, DbTableColumnInfo>();
            _columnNameOrdinalMap = new Dictionary<string, int>();
        }

        public DbTableRows(DbDataReader reader)
        {
            _columnInfos = new Dictionary<int, DbTableColumnInfo>(reader.FieldCount);

            for (int i = 0; i < reader.FieldCount; i++)
            {
                _columnInfos.Add(i, new DbTableColumnInfo
                {
                    Ordinal = i,
                    Name = reader.GetName(i),
                    DbTypeName = reader.GetDataTypeName(i),
                    TypeName = reader.GetFieldType(i).ToString()
                });
            }

            RefreshNameOrdinalMap(_columnInfos);
        }

        private void RefreshNameOrdinalMap(Dictionary<int, DbTableColumnInfo> columnInfos)
        {
            _columnNameOrdinalMap = new Dictionary<string, int>(columnInfos.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var info in columnInfos.Values)
            {
                _columnNameOrdinalMap[info.Name] = info.Ordinal;
            }
        }

        /// <summary>
        /// Rows of the table
        /// </summary>
        public List<DbTableRow> Rows { set; get; } = new List<DbTableRow>();

        /// <summary>
        /// TableColumn's Info
        /// </summary>
        public Dictionary<int, DbTableColumnInfo> ColumnInfos 
        {
            get => _columnInfos;
            set
            {
                _columnInfos = value ?? new Dictionary<int, DbTableColumnInfo>();
                RefreshNameOrdinalMap(_columnInfos);
            }
        }

        /// <summary>
        /// Gets the number of columns in the current row.
        /// </summary>
        public int FieldCount { get; set; }

        /// <summary>
        /// EfTableRows's unique ID
        /// </summary>
        public string TableName { get; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets a value that indicates whether the SqlDataReader contains one or more rows.
        /// </summary>
        public bool HasRows => Rows?.Count > 0;

        /// <summary>
        /// Gets the number of rows changed, inserted, or deleted by execution of the Transact-SQL statement.
        /// </summary>
        public int RecordsAffected => -1;

        /// <summary>
        /// Gets the number of fields in the SqlDataReader that are not hidden.
        /// </summary>
        public int VisibleFieldCount { get; set; }

        /// <summary>
        /// Number of Db rows.
        /// </summary>
        public int RowCount => Rows?.Count ?? 0;

        /// <summary>
        /// Gets or sets the Get(index)
        /// </summary>
        public DbTableRow this[int index]
        {
            get => Get(index);
            set => Rows[index] = value;
        }

        /// <summary>
        /// Adds an item to the EFTableRows
        /// </summary>
        public void Add(DbTableRow item)
        {
            if (item != null)
            {
                Rows.Add(item);
            }
        }

        /// <summary>
        /// returns the value of the given index.
        /// </summary>
        public DbTableRow Get(int index) => Rows[index];

        /// <summary>
        /// Gets the column ordinal, given the name of the column.
        /// </summary>
        public int GetOrdinal(string name)
        {
            if (!_columnNameOrdinalMap.TryGetValue(name, out var ordinal))
            {
                throw new IndexOutOfRangeException(name);
            }

            return ordinal;
        }

        /// <summary>
        /// Gets the name of the specified column.
        /// </summary>
        public string GetName(int ordinal) => GetColumnInfo(ordinal).Name;

        /// <summary>
        /// Gets a string representing the data type of the specified column.
        /// </summary>
        public string GetDataTypeName(int ordinal) => GetColumnInfo(ordinal).DbTypeName;

        /// <summary>
        /// Gets the Type that is the data type of the object.
        /// </summary>
        public Type GetFieldType(int ordinal) => Type.GetType(GetColumnInfo(ordinal).TypeName);

        /// <summary>
        /// Gets the Type that is the data type of the object.
        /// </summary>
        public string GetFieldTypeName(int ordinal) => GetColumnInfo(ordinal).TypeName;

        private DbTableColumnInfo GetColumnInfo(int ordinal)
        {
            var dbColumnInfo = ColumnInfos[ordinal];
            if (dbColumnInfo != null)
            {
                return dbColumnInfo;
            }

            throw new IndexOutOfRangeException($"Index[{ordinal}] was outside of array's bounds.");
        }
    }
}
