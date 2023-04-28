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
            private DbColumn[] _columns;
            private TypeCase[] _columnTypeCases;
            private int[] _ordinalToIndexMap;
            private int[] _nullableOrdinalToIndexMap;

            private bool[] _nulls;
            private bool[] _bools;
            private byte[] _bytes;
            private char[] _chars;
            private DateTime[] _dateTimes;
            private decimal[] _decimals;
            private double[] _doubles;
            private float[] _floats;
            private Guid[] _guids;
            private short[] _shorts;
            private int[] _ints;
            private long[] _longs;
            private uint[] _uints;
            private ushort[] _ushorts;
            private ulong[] _ulongs;
            private string[] _strings;
            private object[] _objects;

            public SequentialDataReader(DbDataReader reader, IReadOnlyList<DbColumn> columns)
            {
                _reader = reader;
                _columns = columns.OrderBy(x => x.ColumnOrdinal).ToArray();

                ReadMetadata();
            }

            private void ReadMetadata()
            {
                int nullCount = 0;
                int boolCount = 0;
                int byteCount = 0;
                int charCount = 0;
                int dateTimeCount = 0;
                int decimalCount = 0;
                int doubleCount = 0;
                int floatCount = 0;
                int guidCount = 0;
                int shortCount = 0;
                int intCount = 0;
                int longCount = 0;
                int uintCount = 0;
                int ushortCount = 0;
                int ulongCount = 0;
                int stringCount = 0;
                int objectCount = 0;

                int fieldCount = _columns.Length;

                _columnTypeCases = Enumerable.Repeat(TypeCase.Empty, fieldCount).ToArray();
                _ordinalToIndexMap = Enumerable.Repeat(-1, fieldCount).ToArray();
                _nullableOrdinalToIndexMap = Enumerable.Repeat(-1, fieldCount).ToArray();

                for (var i = 0; i < fieldCount; i++)
                {
                    var column = _columns[i];

                    if (column.AllowDBNull == true)
                    {
                        _nullableOrdinalToIndexMap[i] = nullCount;
                        nullCount++;
                    }

                    var type = column.DataType;
                    if (type == typeof(bool))
                    {
                        _columnTypeCases[i] = TypeCase.Bool;
                        _ordinalToIndexMap[i] = boolCount;
                        boolCount++;
                    }
                    else if (type == typeof(byte))
                    {
                        _columnTypeCases[i] = TypeCase.Byte;
                        _ordinalToIndexMap[i] = byteCount;
                        byteCount++;
                    }
                    else if (type == typeof(char))
                    {
                        _columnTypeCases[i] = TypeCase.Char;
                        _ordinalToIndexMap[i] = charCount;
                        charCount++;
                    }
                    else if (type == typeof(DateTime))
                    {
                        _columnTypeCases[i] = TypeCase.DateTime;
                        _ordinalToIndexMap[i] = dateTimeCount;
                        dateTimeCount++;
                    }
                    else if (type == typeof(decimal))
                    {
                        _columnTypeCases[i] = TypeCase.Decimal;
                        _ordinalToIndexMap[i] = decimalCount;
                        decimalCount++;
                    }
                    else if (type == typeof(double))
                    {
                        _columnTypeCases[i] = TypeCase.Double;
                        _ordinalToIndexMap[i] = doubleCount;
                        doubleCount++;
                    }
                    else if (type == typeof(float))
                    {
                        _columnTypeCases[i] = TypeCase.Float;
                        _ordinalToIndexMap[i] = floatCount;
                        floatCount++;
                    }
                    else if (type == typeof(Guid))
                    {
                        _columnTypeCases[i] = TypeCase.Guid;
                        _ordinalToIndexMap[i] = guidCount;
                        guidCount++;
                    }
                    else if (type == typeof(short))
                    {
                        _columnTypeCases[i] = TypeCase.Short;
                        _ordinalToIndexMap[i] = shortCount;
                        shortCount++;
                    }
                    else if (type == typeof(int))
                    {
                        _columnTypeCases[i] = TypeCase.Int;
                        _ordinalToIndexMap[i] = intCount;
                        intCount++;
                    }
                    else if (type == typeof(long))
                    {
                        _columnTypeCases[i] = TypeCase.Long;
                        _ordinalToIndexMap[i] = longCount;
                        longCount++;
                    }
                    else if (type == typeof(ushort))
                    {
                        _columnTypeCases[i] = TypeCase.UShort;
                        _ordinalToIndexMap[i] = ushortCount;
                        ushortCount++;
                    }
                    else if (type == typeof(uint))
                    {
                        _columnTypeCases[i] = TypeCase.UInt;
                        _ordinalToIndexMap[i] = uintCount;
                        uintCount++;
                    }
                    else if (type == typeof(ulong))
                    {
                        _columnTypeCases[i] = TypeCase.ULong;
                        _ordinalToIndexMap[i] = ulongCount;
                        ulongCount++;
                    }
                    else if (type == typeof(string))
                    {
                        _columnTypeCases[i] = column.ColumnSize < int.MaxValue ? TypeCase.Text : TypeCase.MaxText;
                        _ordinalToIndexMap[i] = stringCount;
                        stringCount++;
                    }
                    else
                    {
                        _columnTypeCases[i] = TypeCase.Object;
                        _ordinalToIndexMap[i] = objectCount;
                        objectCount++;
                    }
                }

                _nulls = new bool[nullCount];
                _bools = new bool[boolCount];
                _bytes = new byte[byteCount];
                _chars = new char[charCount];
                _dateTimes = new DateTime[dateTimeCount];
                _decimals = new decimal[decimalCount];
                _doubles = new double[doubleCount];
                _floats = new float[floatCount];
                _guids = new Guid[guidCount];
                _shorts = new short[shortCount];
                _ints = new int[intCount];
                _longs = new long[longCount];
                _ushorts = new ushort[ushortCount];
                _uints = new uint[uintCount];
                _ulongs = new ulong[ulongCount];
                _strings = new string[stringCount];
                _objects = new object[objectCount];
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
                ClearBuffer();

                _columns = null;
                _columnTypeCases = null;
                _ordinalToIndexMap = null;
                _nullableOrdinalToIndexMap = null;
            }

            public override object this[int ordinal] 
                => GetFieldValue<object>(ordinal);
            public override object this[string name] 
                => GetFieldValue<object>(GetOrdinal(name));

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

            public override IEnumerator GetEnumerator()
                => _reader.GetEnumerator();

            public override Type GetFieldType(int ordinal)
                => _reader.GetFieldType(ordinal);

            public override string GetDataTypeName(int ordinal)
                => _reader.GetDataTypeName(ordinal);

            public override string GetName(int ordinal)
                => _reader.GetName(ordinal);

            public override int GetOrdinal(string name)
                => _reader.GetOrdinal(name);

            public override bool GetBoolean(int ordinal)
                => _columnTypeCases[ordinal] == TypeCase.Bool
                    ? _bools[_ordinalToIndexMap[ordinal]]
                    : GetFieldValue<bool>(ordinal);

            public override byte GetByte(int ordinal)
                => _columnTypeCases[ordinal] == TypeCase.Byte
                    ? _bytes[_ordinalToIndexMap[ordinal]]
                    : GetFieldValue<byte>(ordinal);

            public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length) 
                => throw new NotSupportedException("The SequentialDataReader does not support reading binary data.");

            public override char GetChar(int ordinal)
                => _columnTypeCases[ordinal] == TypeCase.Char
                    ? _chars[_ordinalToIndexMap[ordinal]]
                    : GetFieldValue<char>(ordinal);

            public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
                => throw new NotSupportedException("The SequentialDataReader does not support reading character data.");

            public override DateTime GetDateTime(int ordinal)
                => _columnTypeCases[ordinal] == TypeCase.DateTime
                    ? _dateTimes[_ordinalToIndexMap[ordinal]]
                    : GetFieldValue<DateTime>(ordinal);

            public override decimal GetDecimal(int ordinal)
                => _columnTypeCases[ordinal] == TypeCase.Decimal
                    ? _decimals[_ordinalToIndexMap[ordinal]]
                    : GetFieldValue<decimal>(ordinal);

            public override double GetDouble(int ordinal)
                => _columnTypeCases[ordinal] == TypeCase.Double
                    ? _doubles[_ordinalToIndexMap[ordinal]]
                    : GetFieldValue<double>(ordinal);

            public override float GetFloat(int ordinal)
                => _columnTypeCases[ordinal] == TypeCase.Float
                    ? _floats[_ordinalToIndexMap[ordinal]]
                    : GetFieldValue<float>(ordinal);

            public override Guid GetGuid(int ordinal)
                => _columnTypeCases[ordinal] == TypeCase.Guid
                    ? _guids[_ordinalToIndexMap[ordinal]]
                    : GetFieldValue<Guid>(ordinal);

            public override short GetInt16(int ordinal)
                => _columnTypeCases[ordinal] == TypeCase.Short
                    ? _shorts[_ordinalToIndexMap[ordinal]]
                    : GetFieldValue<short>(ordinal);

            public ushort GetUInt16(int ordinal)
                => _columnTypeCases[ordinal] == TypeCase.UShort
                    ? _ushorts[_ordinalToIndexMap[ordinal]]
                    : GetFieldValue<ushort>(ordinal);

            public override int GetInt32(int ordinal)
                => _columnTypeCases[ordinal] == TypeCase.Int
                    ? _ints[_ordinalToIndexMap[ordinal]]
                    : GetFieldValue<int>(ordinal);

            public uint GetUInt32(int ordinal)
                => _columnTypeCases[ordinal] == TypeCase.UInt
                    ? _uints[_ordinalToIndexMap[ordinal]]
                    : GetFieldValue<uint>(ordinal);

            public override long GetInt64(int ordinal)
                => _columnTypeCases[ordinal] == TypeCase.Long
                    ? _longs[_ordinalToIndexMap[ordinal]]
                    : GetFieldValue<long>(ordinal);

            public ulong GetUInt64(int ordinal)
                => _columnTypeCases[ordinal] == TypeCase.ULong
                    ? _ulongs[_ordinalToIndexMap[ordinal]]
                    : GetFieldValue<ulong>(ordinal);

            public override string GetString(int ordinal)
                => _columnTypeCases[ordinal] is (TypeCase.Text or TypeCase.MaxText)
                    ? _strings[_ordinalToIndexMap[ordinal]]
                    : GetFieldValue<string>(ordinal);

            public override object GetValue(int ordinal)
                => GetFieldValue<object>(ordinal);

            public override int GetValues(object[] values)
            {
                var min = Math.Min(_columns.Length, values.Length);

                for (var i = 0; i < min; i++)
                {
                    var nullIndex = _nullableOrdinalToIndexMap[i];
                    if (IsDBNull(i))
                    {
                        values[i] = null;
                    }
                    else
                    {
                        values[i] = GetFieldValue<object>(i);
                    }
                }

                return min;
            }

            public override T GetFieldValue<T>(int ordinal)
                => (_columnTypeCases[ordinal]) switch
                {
                    TypeCase.Bool => (T)(object)GetBoolean(ordinal),
                    TypeCase.Text => (T)(object)GetString(ordinal),
                    TypeCase.MaxText => (T)(object)GetString(ordinal),
                    TypeCase.Byte => (T)(object)GetByte(ordinal),
                    TypeCase.Char => (T)(object)GetChar(ordinal),
                    TypeCase.DateTime => (T)(object)GetDateTime(ordinal),
                    TypeCase.Decimal => (T)(object)GetDecimal(ordinal),
                    TypeCase.Double => (T)(object)GetDouble(ordinal),
                    TypeCase.Float => (T)(object)GetFloat(ordinal),
                    TypeCase.Guid => (T)(object)GetGuid(ordinal),
                    TypeCase.Short => (T)(object)GetInt16(ordinal),
                    TypeCase.Int => (T)(object)GetInt32(ordinal),
                    TypeCase.Long => (T)(object)GetInt64(ordinal),
                    TypeCase.UShort => (T)(object)GetUInt16(ordinal),
                    TypeCase.UInt => (T)(object)GetUInt32(ordinal),
                    TypeCase.ULong => (T)(object)GetUInt64(ordinal),
                    _ => (T)_objects[_ordinalToIndexMap[ordinal]]
                };

            public override bool IsDBNull(int ordinal)
            {
                var nullIndex = _nullableOrdinalToIndexMap[ordinal];
                return nullIndex != -1 && _nulls[nullIndex];
            }

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

            private void ClearBuffer()
            {
                Array.Clear(_nulls, 0, _nulls.Length);
                Array.Clear(_bools, 0, _bools.Length);
                Array.Clear(_bytes, 0, _bytes.Length);
                Array.Clear(_chars, 0, _chars.Length);
                Array.Clear(_dateTimes, 0, _dateTimes.Length);
                Array.Clear(_decimals, 0, _decimals.Length);
                Array.Clear(_doubles, 0, _doubles.Length);
                Array.Clear(_floats, 0, _floats.Length);
                Array.Clear(_guids, 0, _guids.Length);
                Array.Clear(_shorts, 0, _shorts.Length);
                Array.Clear(_ints, 0, _ints.Length);
                Array.Clear(_longs, 0, _longs.Length);
                Array.Clear(_uints, 0, _uints.Length);
                Array.Clear(_ushorts, 0, _ushorts.Length);
                Array.Clear(_ulongs, 0, _ulongs.Length);
                Array.Clear(_strings, 0, _strings.Length);
                Array.Clear(_objects, 0, _objects.Length);
            }

            private void ReadRow()
            {
                ClearBuffer();

                for (int i = 0; i < _columns.Length; ++i)
                {
                    var column = _columns[i];
                    var nullIndex = _nullableOrdinalToIndexMap[i];

                    if (nullIndex != -1 && _reader.IsDBNull(i))
                    {
                        _nulls[nullIndex] = true;
                    }
                    else
                    {
                        var index = _ordinalToIndexMap[i];

                        switch (_columnTypeCases[i])
                        {
                            case TypeCase.Bool:
                                _bools[index] = _reader.GetBoolean(i);
                                break;
                            case TypeCase.Int:
                                _ints[index] = _reader.GetInt32(i);
                                break;
                            case TypeCase.Text:
                                _strings[index] = _reader.GetString(i);
                                break;
                            case TypeCase.MaxText:
                                _strings[index] = ReadLargeString(i, column);
                                break;
                            case TypeCase.Decimal:
                                _decimals[index] = _reader.GetDecimal(i);
                                break;
                            case TypeCase.DateTime:
                                _dateTimes[index] = _reader.GetDateTime(i);
                                break;
                            case TypeCase.Byte:
                                _bytes[index] = _reader.GetByte(i);
                                break;
                            case TypeCase.Short:
                                _shorts[index] = _reader.GetInt16(i);
                                break;
                            case TypeCase.Long:
                                _longs[index] = _reader.GetInt64(i);
                                break;
                            case TypeCase.Double:
                                _doubles[index] = _reader.GetDouble(i);
                                break;
                            case TypeCase.Guid:
                                _guids[index] = _reader.GetGuid(i);
                                break;
                            case TypeCase.Char:
                                _chars[index] = _reader.GetChar(i);
                                break;
                            case TypeCase.UShort:
                                _ushorts[index] = (ushort)_reader.GetInt16(i);
                                break;
                            case TypeCase.UInt:
                                _uints[index] = (uint)_reader.GetInt32(i);
                                break;
                            case TypeCase.ULong:
                                _ulongs[index] = (ulong)_reader.GetInt64(i);
                                break;
                            case TypeCase.Empty:
                                break;
                            default:
                                _objects[index] = _reader.GetValue(i);
                                break;
                        }
                    }
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

        private enum TypeCase
        {
            Empty = 0,
            Object,
            Bool,
            Byte,
            Char,
            DateTime,
            Decimal,
            Double,
            Float,
            Guid,
            Short,
            Int,
            Long,
            UInt,
            ULong,
            UShort,
            Text,
            MaxText
        }
    }
}
