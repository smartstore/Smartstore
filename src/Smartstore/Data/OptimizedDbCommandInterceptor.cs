using System.Collections;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Smartstore.Data
{
    /// <summary>
    /// This interceptor optimizes a <see cref="Microsoft.EntityFrameworkCore.DbContext"/> for
    /// accessing large columns (text, ntext, varchar(max) and nvarchar(max)). It enables the
    /// <see cref="CommandBehavior.SequentialAccess"/> option and uses an optimized method
    /// for converting large text columns into <see cref="string"/> objects.
    /// </summary>
    public class OptimizedDbCommandInterceptor : DbCommandInterceptor
    {
        public async override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, 
            CommandEventData eventData, 
            InterceptionResult<DbDataReader> result, 
            CancellationToken cancellationToken = default)
        {
            var behavior = CommandBehavior.SequentialAccess;
            var reader = await command.ExecuteReaderAsync(behavior, cancellationToken);
            var wrapper = await DbDataReaderOptimizedWrapper.CreateAsync(reader, cancellationToken);

            return InterceptionResult<DbDataReader>.SuppressWithResult(wrapper);
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command, 
            CommandEventData eventData, 
            InterceptionResult<DbDataReader> result)
        {
            var behavior = CommandBehavior.SequentialAccess;
            var reader = command.ExecuteReader(behavior);
            var wrapper = DbDataReaderOptimizedWrapper.Create(reader);

            return InterceptionResult<DbDataReader>.SuppressWithResult(wrapper);
        }

        /// <summary>
        /// This wrapper caches the values of accessed columns of each row, allowing non-sequential access
        /// even when <see cref="CommandBehavior.SequentialAccess"/> is specified. It enables using this option it with EF Core.
        /// In addition, it provides an optimized method for reading text, ntext, varchar(max) and nvarchar(max) columns.
        /// All in all, it speeds up database operations reading from large text columns.
        /// </summary>
        sealed class DbDataReaderOptimizedWrapper : DbDataReader
        {
            const int MaxBufferSize = int.MaxValue / 2;

            private readonly DbDataReader _reader;
            private readonly DbColumn[] _schema;
            private readonly object[] _cache;
            private readonly Func<object>[] _materializers;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private T Get<T>(int ordinal)
            {
                if (_cache[ordinal] != DBNull.Value)
                {
                    return (T)_cache[ordinal];
                }

                // This line will throw an exception if T is not a reference type (class), otherwise it will return null.
                return (T)(object)null;
            }

            private DbDataReaderOptimizedWrapper(DbDataReader reader, IEnumerable<DbColumn> schema)
            {
                _reader = reader;
                _schema = schema.OrderBy(x => x.ColumnOrdinal).ToArray();

                _cache = new object[_schema.Length];

                byte[] stringGetterBuffer = null;

                string stringGetter(int i)
                {
                    var dbColumn = _schema[i];

                    // Using GetBytes instead of GetString is much faster, but only works for text, ntext, varchar(max) and nvarchar(max)
                    if (dbColumn.ColumnSize < int.MaxValue)
                    {
                        return reader.GetString(i);
                    }

                    stringGetterBuffer ??= new byte[32 * 1024];

                    var totalRead = 0;

                    while (true)
                    {
                        var offset = totalRead;

                        totalRead += (int)reader.GetBytes(i, offset, stringGetterBuffer, offset, stringGetterBuffer.Length - offset);

                        if (totalRead < stringGetterBuffer.Length)
                        {
                            break;
                        }

                        if (stringGetterBuffer.Length >= MaxBufferSize)
                        {
                            throw new OutOfMemoryException($"{nameof(DbDataReaderOptimizedWrapper)}.{nameof(GetString)} cannot load column '{GetName(i)}' because it contains a string longer than {MaxBufferSize} bytes.");
                        }

                        Array.Resize(ref stringGetterBuffer, 2 * stringGetterBuffer.Length);
                    }

                    var c = dbColumn.DataTypeName[0];
                    var encoding = (c is 'N' or 'n') ? Encoding.Unicode : Encoding.ASCII;

                    return encoding.GetString(stringGetterBuffer.AsSpan(0, totalRead));
                }

                var dict = new Dictionary<Type, Func<DbColumn, int, Func<object>>>
                {
                    [typeof(bool)] = (column, index) => () => reader.GetBoolean(index),
                    [typeof(byte)] = (column, index) => () => reader.GetByte(index),
                    [typeof(char)] = (column, index) => () => reader.GetChar(index),
                    [typeof(short)] = (column, index) => () => reader.GetInt16(index),
                    [typeof(int)] = (column, index) => () => reader.GetInt32(index),
                    [typeof(long)] = (column, index) => () => reader.GetInt64(index),
                    [typeof(float)] = (column, index) => () => reader.GetFloat(index),
                    [typeof(double)] = (column, index) => () => reader.GetDouble(index),
                    [typeof(decimal)] = (column, index) => () => reader.GetDecimal(index),
                    [typeof(DateTime)] = (column, index) => () => reader.GetDateTime(index),
                    [typeof(Guid)] = (column, index) => () => reader.GetGuid(index),
                    [typeof(string)] = (column, index) => () => stringGetter(index),
                };

                _materializers = schema.Select((column, index) => dict[column.DataType](column, index)).ToArray();
            }

            public static DbDataReaderOptimizedWrapper Create(DbDataReader reader)
                => new(reader, reader.GetColumnSchema());

            public static async ValueTask<DbDataReaderOptimizedWrapper> CreateAsync(DbDataReader reader, CancellationToken cancellationToken)
                => new DbDataReaderOptimizedWrapper(reader, await reader.GetColumnSchemaAsync(cancellationToken));

            protected override void Dispose(bool disposing) 
                => _reader.Dispose();

            public override ValueTask DisposeAsync() 
                => _reader.DisposeAsync();


            public override object this[int ordinal] 
                => Get<object>(ordinal);
            public override object this[string name] 
                => Get<object>(GetOrdinal(name));

            public override int Depth 
                => _reader.Depth;

            public override int FieldCount
                => _reader.FieldCount;

            public override bool HasRows 
                => _reader.HasRows;

            public override bool IsClosed 
                => _reader.IsClosed;

            public override int RecordsAffected 
                => _reader.RecordsAffected;

            public override int VisibleFieldCount 
                => _reader.VisibleFieldCount;


            public override bool GetBoolean(int ordinal) 
                => Get<bool>(ordinal);

            public override byte GetByte(int ordinal) 
                => Get<byte>(ordinal);

            public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length) 
                => throw new NotSupportedException();

            public override char GetChar(int ordinal) 
                => Get<char>(ordinal);

            public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length) 
                => throw new NotSupportedException();

            public override string GetDataTypeName(int ordinal) 
                => _reader.GetDataTypeName(ordinal);

            public override DateTime GetDateTime(int ordinal) 
                => Get<DateTime>(ordinal);

            public override decimal GetDecimal(int ordinal) 
                => Get<decimal>(ordinal);

            public override double GetDouble(int ordinal) 
                => Get<double>(ordinal);

            public override IEnumerator GetEnumerator() 
                => _reader.GetEnumerator();

            public override Type GetFieldType(int ordinal) 
                => _reader.GetFieldType(ordinal);

            public override float GetFloat(int ordinal) 
                => Get<float>(ordinal);

            public override Guid GetGuid(int ordinal) 
                => Get<Guid>(ordinal);

            public override short GetInt16(int ordinal) 
                => Get<short>(ordinal);

            public override int GetInt32(int ordinal) 
                => Get<int>(ordinal);

            public override long GetInt64(int ordinal) 
                => Get<long>(ordinal);

            public override string GetName(int ordinal) 
                => _reader.GetName(ordinal);

            public override int GetOrdinal(string name) 
                => _reader.GetOrdinal(name);

            public override string GetString(int ordinal) 
                => Get<string>(ordinal);

            public override object GetValue(int ordinal) 
                => Get<object>(ordinal);

            public override int GetValues(object[] values)
            {
                var min = Math.Min(_cache.Length, values.Length);

                Array.Copy(_cache, values, min);

                return min;
            }

            public override bool IsDBNull(int ordinal) 
                => Convert.IsDBNull(_cache[ordinal]);

            public override bool NextResult() 
                => _reader.NextResult();

            public override bool Read()
            {
                Array.Clear(_cache, 0, _cache.Length);

                if (_reader.Read())
                {
                    for (int i = 0; i < _cache.Length; ++i)
                    {
                        if ((_schema[i].AllowDBNull ?? true) && _reader.IsDBNull(i))
                        {
                            _cache[i] = DBNull.Value;
                        }
                        else 
                        { 
                            _cache[i] = _materializers[i](); 
                        }
                    }

                    return true;
                }

                return false;
            }

            public override void Close() 
                => _reader.Close();

            public override Task CloseAsync() 
                => _reader.CloseAsync();

            public override DataTable GetSchemaTable() 
                => _reader.GetSchemaTable();

            public override Task<DataTable> GetSchemaTableAsync(CancellationToken cancellationToken = default) 
                => _reader.GetSchemaTableAsync(cancellationToken);

            public override Task<ReadOnlyCollection<DbColumn>> GetColumnSchemaAsync(CancellationToken cancellationToken = default) 
                => _reader.GetColumnSchemaAsync(cancellationToken);

            public override Task<bool> NextResultAsync(CancellationToken cancellationToken) 
                => _reader.NextResultAsync(cancellationToken);

            public async override Task<bool> ReadAsync(CancellationToken cancellationToken)
            {
                Array.Clear(_cache, 0, _cache.Length);

                if (await _reader.ReadAsync(cancellationToken))
                {
                    for (int i = 0; i < _cache.Length; ++i)
                    {
                        if ((_schema[i].AllowDBNull ?? true) && await _reader.IsDBNullAsync(i, cancellationToken))
                        {
                            _cache[i] = DBNull.Value;
                        }
                        else
                        {
                            _cache[i] = _materializers[i]();
                        }
                    }

                    return true;
                }

                return false;
            }
        }
    }
}
