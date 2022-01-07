using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Smartstore.Data
{
    public class SqlBlobStream : Stream
    {
        private readonly DatabaseFacade _database;
        private DbCommand _command;
        private DbDataReader _reader;
        private long? _length;
        private long _dataIndex;

        public SqlBlobStream(
            DatabaseFacade database,
            string tableName,
            string blobColumnName,
            string pkColumnName,
            object pkColumnValue)
        {
            Guard.NotNull(database, nameof(database));
            Guard.NotEmpty(tableName, nameof(tableName));
            Guard.NotEmpty(blobColumnName, nameof(blobColumnName));
            Guard.NotEmpty(pkColumnName, nameof(pkColumnName));
            Guard.NotNull(pkColumnValue, nameof(pkColumnValue));

            TableName = tableName;
            BlobColumnName = blobColumnName;
            PkColumnName = pkColumnName;
            PkColumnValue = pkColumnValue;

            _database = database;
        }

        private void EnsureOpen()
        {
            if (_reader != null)
                return;

            var connection = _database.GetDbConnection();

            _command = connection.CreateCommand();

            var parameter = _command.CreateParameter();
            parameter.ParameterName = "@" + PkColumnName;
            parameter.Value = PkColumnValue;

            _command.CommandType = CommandType.Text;
            _command.CommandText = $"SELECT {BlobColumnName} FROM {TableName} WHERE {parameter.ParameterName[1..]} = {parameter.Value}";
            _command.Parameters.Add(parameter);

            if (_database.CurrentTransaction != null)
            {
                _command.Transaction = _database.CurrentTransaction.GetDbTransaction();
            }

            var commandTimeout = _database.GetCommandTimeout();
            if (commandTimeout.HasValue)
            {
                _command.CommandTimeout = commandTimeout.Value;
            }

            // Open connection if closed
            _database.OpenConnection();

            _reader = _command.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult);
            if (!_reader.Read())
            {
                throw new DataException($"Blob [{TableName}].[{BlobColumnName}] with id '{PrimaryKey.Value}' does not exist.");
            }

            if (_reader.IsDBNull(0))
            {
                _length = 0;
            }
        }

        public string ConnectionString { get; private set; }
        public string TableName { get; private set; }
        public string BlobColumnName { get; private set; }
        public string PkColumnName { get; private set; }
        public object PkColumnValue { get; private set; }
        public DbParameter PrimaryKey { get; private set; }

        #region Stream members

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin != SeekOrigin.Begin)
            {
                throw new NotSupportedException($"{nameof(SqlBlobStream)} only supports 'SeekOrigin.Begin'.");
            }

            if (offset == _dataIndex)
            {
                return 0;
            }

            if (offset < _dataIndex)
            {
                // This is a forward-only read. Close and start over again.
                CloseReader();
                return Seek(offset, origin);
            }

            if (offset > _dataIndex)
            {
                EnsureOpen();

                var buffer = new byte[1];
                var num = _reader.GetBytes(0, offset - 1, buffer, 0, 1);
                _dataIndex = offset;
                return num;
            }

            return 0;
        }

        public override bool CanSeek => true;
        public override bool CanRead => true;
        public override bool CanWrite => false;
        public override long Position
        {
            get => _dataIndex;
            set => Seek(value, SeekOrigin.Begin);
        }
        public override long Length
        {
            get
            {
                EnsureOpen();

                if (_length == null)
                {
                    _length = _reader.GetBytes(0, 0, null, 0, 0);
                }

                return _length.Value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            EnsureOpen();

            var read = _reader.GetBytes(0, _dataIndex + offset, buffer, 0, count);
            _dataIndex += read;

            return (int)read;
        }

        public override void Flush()
            => throw new NotSupportedException();

        public override void SetLength(long value)
            => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                CloseReader();
        }

        public override ValueTask DisposeAsync()
        {
            return CloseReaderAsync();
        }

        public void CloseReader()
        {
            if (_reader != null)
            {
                if (!_reader.IsClosed)
                    _reader.Close();

                _reader = null;
            }

            if (_command != null)
            {
                _command.Parameters.Clear();
                _command.Dispose();
                _command = null;
            }

            _database.CloseConnection();

            _dataIndex = 0;
            PrimaryKey = null;
        }

        public async ValueTask CloseReaderAsync()
        {
            if (_reader != null)
            {
                if (!_reader.IsClosed)
                    await _reader.CloseAsync();

                _reader = null;
            }

            if (_command != null)
            {
                _command.Parameters.Clear();
                await _command.DisposeAsync();
                _command = null;
            }

            await _database.CloseConnectionAsync();

            _dataIndex = 0;
            PrimaryKey = null;
        }

        #endregion
    }
}
