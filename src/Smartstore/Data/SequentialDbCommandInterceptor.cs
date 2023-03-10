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
    public class SequentialDbCommandInterceptor : DbCommandInterceptor
    {
        public async override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, 
            CommandEventData eventData, 
            InterceptionResult<DbDataReader> result, 
            CancellationToken cancellationToken = default)
        {
            if (!IsValidCommandSource(eventData))
            {
                return result;
            }
            
            var behavior = CommandBehavior.SequentialAccess;
            var reader = await command.ExecuteReaderAsync(behavior, cancellationToken);
            var columns = await reader.GetColumnSchemaAsync(cancellationToken);
            var sequentialReader = new SequentialDataReader(reader, columns);

            return InterceptionResult<DbDataReader>.SuppressWithResult(sequentialReader);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsValidCommandSource(CommandEventData eventData)
        {
            if (eventData.CommandSource != CommandSource.LinqQuery)
            {
                return false;
            }

            var canReadSequential = eventData.Context is HookingDbContext db && db.DataProvider.CanReadSequential;
            return canReadSequential;
        }

        /// <summary>
        /// This wrapper caches the values of accessed columns of each row, allowing non-sequential access
        /// even when <see cref="CommandBehavior.SequentialAccess"/> is specified. It enables using this option with EF Core.
        /// In addition, it provides an optimized method for reading text, ntext, varchar(max) and nvarchar(max) columns.
        /// All in all, it speeds up database operations reading from large text columns.
        /// </summary>
        sealed class SequentialDataReader : DbDataReader
        {
            const int MaxBufferSize = int.MaxValue / 2;

            private DbDataReader _reader;
            private readonly DbColumn[] _columns;
            private readonly object[] _cache;

            public SequentialDataReader(DbDataReader reader, IReadOnlyList<DbColumn> columns)
            {
                _reader = reader;
                _columns = columns.OrderBy(x => x.ColumnOrdinal).ToArray();
                _cache = new object[_columns.Length];
            }

            protected override void Dispose(bool disposing)
            {
                ClearCache();
                _reader?.Dispose();
                
            }

            public override async ValueTask DisposeAsync()
            {
                ClearCache();
                if (_reader != null)
                {
                    await _reader.DisposeAsync();
                    _reader = null;
                }
            }

            private void ClearCache()
            {
                Array.Clear(_cache, 0, _cache.Length);
                Array.Clear(_columns, 0, _columns.Length);
            }


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
                => throw new NotSupportedException("The SequentialDataReader does not support reading binary data.");

            public override char GetChar(int ordinal) 
                => Get<char>(ordinal);

            public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
                => throw new NotSupportedException("The SequentialDataReader does not support reading character data.");

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
                if (_reader.Read())
                {
                    ReadRow();
                    return true;
                }

                return false;
            }

            public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
            {
                if (await _reader.ReadAsync(cancellationToken))
                {
                    ReadRow();
                    return true;
                }

                return false;
            }

            private void ReadRow()
            {
                Array.Clear(_cache, 0, _cache.Length);

                for (int i = 0; i < _cache.Length; ++i)
                {
                    var column = _columns[i];
                    var isDBNull = _reader.IsDBNull(i);
                    object value;

                    if (column.AllowDBNull == true && isDBNull)
                    {
                        value = DBNull.Value;
                    }
                    else
                    {
                        var type = column.DataType;
                        if (type == typeof(bool))
                        {
                            value = _reader.GetBoolean(i);
                        }
                        else if (type == typeof(int))
                        {
                            value = _reader.GetInt32(i);
                        }
                        else if (type == typeof(string))
                        {
                            // Using GetBytes instead of GetString is much faster, but only works for text, ntext, varchar(max) and nvarchar(max)
                            if (column.ColumnSize < int.MaxValue)
                            {
                                value = _reader.GetString(i);
                            }
                            else
                            {
                                value = ReadLargeString(i, column);
                            }
                        }
                        else if (type == typeof(decimal))
                        {
                            value = _reader.GetDecimal(i);
                        }
                        else if (type == typeof(DateTime))
                        {
                            value = _reader.GetDateTime(i);
                        }
                        else if (type == typeof(byte))
                        {
                            value = _reader.GetByte(i);
                        }
                        else if (type == typeof(short))
                        {
                            value = _reader.GetInt16(i);
                        }
                        else if (type == typeof(long))
                        {
                            value = _reader.GetInt64(i);
                        }
                        else if (type == typeof(float))
                        {
                            value = _reader.GetFloat(i);
                        }
                        else if (type == typeof(double))
                        {
                            value = _reader.GetDouble(i);
                        }
                        else if (type == typeof(Guid))
                        {
                            value = _reader.GetGuid(i);
                        }
                        else if (type == typeof(char))
                        {
                            value = _reader.GetChar(i);
                        }
                        else if (type == typeof(ushort))
                        {
                            value = (ushort)_reader.GetInt16(i);
                        }
                        else if (type == typeof(uint))
                        {
                            value = (uint)_reader.GetInt32(i);
                        }
                        else if (type == typeof(ulong))
                        {
                            value = (ulong)_reader.GetInt64(i);
                        }
                        else
                        {
                            value = _reader.GetValue(i);
                        }
                    }

                    _cache[i] = value;
                }
            }

            private string ReadLargeString(int ordinal, DbColumn column)
            {
                var buffer = new byte[32 * 1024];
                var totalRead = 0;

                while (true)
                {
                    var offset = totalRead;

                    totalRead += (int)_reader.GetBytes(ordinal, offset, buffer, offset, buffer.Length - offset);

                    if (totalRead < buffer.Length)
                    {
                        break;
                    }

                    if (buffer.Length >= MaxBufferSize)
                    {
                        throw new OutOfMemoryException($"{nameof(SequentialDataReader)}.{nameof(GetString)} cannot load column '{GetName(ordinal)}' because it contains a string longer than {MaxBufferSize} bytes.");
                    }

                    Array.Resize(ref buffer, 2 * buffer.Length);
                }

                var typeName = column.DataTypeName;
                var encoding = (typeName != null && typeName[0] is 'N' or 'n') ? Encoding.Unicode : Encoding.ASCII;
                return encoding.GetString(buffer.AsSpan(0, totalRead));
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
        }
    }
}
