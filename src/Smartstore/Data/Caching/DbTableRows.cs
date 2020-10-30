using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace Smartstore.Data.Caching
{
    public class DbTableRows
    {
        public DbTableRows()
        {
            ColumnInfos = new Dictionary<int, DbTableColumnInfo>();
        }

        public DbTableRows(DbDataReader reader)
        {
            ColumnInfos = new Dictionary<int, DbTableColumnInfo>(reader.FieldCount);
            for (int i = 0; i < reader.FieldCount; i++)
            {
                ColumnInfos.Add(i, new DbTableColumnInfo
                {
                    Ordinal = i,
                    Name = reader.GetName(i),
                    DbTypeName = reader.GetDataTypeName(i),
                    TypeName = reader.GetFieldType(i).ToString()
                });
            }
        }

        /// <summary>
        /// Rows of the table
        /// </summary>
        public List<DbTableRow> Rows { set; get; } = new List<DbTableRow>();

        /// <summary>
        /// TableColumn's Info
        /// </summary>
        public Dictionary<int, DbTableColumnInfo> ColumnInfos { set; get; }

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
        public int RowsCount => Rows?.Count ?? 0;

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
            var keyValuePair = ColumnInfos.FirstOrDefault(pair => pair.Value.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (keyValuePair.Value != null)
            {
                return keyValuePair.Value.Ordinal;
            }
            throw new IndexOutOfRangeException(name);
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
