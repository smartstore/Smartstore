using System;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using Smartstore.Data;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Data;

[TestFixture]
public class SqlBlobStreamTests
{
    [TestCase(true)]
    [TestCase(false)]
    public void Seek_before_first_read_preserves_the_complete_tail(bool reportRemainingLength)
    {
        using var database = new SequentialBlobDatabase(reportRemainingLength);
        using var stream = database.OpenStream();
        stream.Seek(6, SeekOrigin.Begin);

        using var tail = new MemoryStream();
        int value;
        while ((value = stream.ReadByte()) != -1)
        {
            tail.WriteByte((byte)value);
        }

        Assert.That(tail.ToArray(), Is.EqualTo(new byte[] { 7, 8, 9, 10 }));
        Assert.That(stream.Length, Is.EqualTo(10));
        Assert.That(stream.Position, Is.EqualTo(10));
        Assert.That(database.OpenReaders, Is.Zero);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Seek_to_end_before_first_read_allows_rewind(bool reportRemainingLength)
    {
        using var database = new SequentialBlobDatabase(reportRemainingLength);
        using var stream = database.OpenStream();
        stream.Seek(10, SeekOrigin.Begin);

        Assert.That(stream.ReadByte(), Is.EqualTo(-1));
        Assert.That(database.OpenReaders, Is.Zero);

        stream.Position = 0;

        Assert.That(stream.ReadByte(), Is.EqualTo(1));
        Assert.That(stream.Length, Is.EqualTo(10));
    }

    [Test]
    public async Task Async_read_releases_reader_at_eof_and_preserves_buffer_offset()
    {
        using var database = new SequentialBlobDatabase(true);
        await using var stream = database.OpenStream();
        var buffer = new byte[12];

        await stream.ReadExactlyAsync(buffer, 1, 10);

        Assert.That(buffer, Is.EqualTo(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 0 }));
        Assert.That(database.OpenReaders, Is.Zero);
        Assert.That(stream.Length, Is.EqualTo(10));
        Assert.That(stream.ReadByte(), Is.EqualTo(-1));
        Assert.That(stream.ReadByte(), Is.EqualTo(-1));
        Assert.That(database.OpenReaders, Is.Zero);
    }

    private sealed class SequentialBlobDatabase : IDisposable
    {
        private readonly DbContext _context;
        private readonly bool _reportRemainingLength;
        private readonly byte[] _data = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

        public SequentialBlobDatabase(bool reportRemainingLength)
        {
            _reportRemainingLength = reportRemainingLength;

            var connection = new Mock<DbConnection>();
            ConnectionState state = ConnectionState.Closed;
            connection.SetupGet(x => x.State).Returns(() => state);
            connection.Setup(x => x.Open()).Callback(() => state = ConnectionState.Open);
            connection.Setup(x => x.Close()).Callback(() => state = ConnectionState.Closed);
            connection.Protected().Setup<DbCommand>("CreateDbCommand").Returns(CreateCommand);

            _context = new DbContext(new DbContextOptionsBuilder().UseSqlite(connection.Object).Options);
        }

        public int OpenReaders { get; private set; }

        public SqlBlobStream OpenStream()
            => new(new TestDataProvider(_context.Database), "Blobs", "Data", "Id", 1);

        private DbCommand CreateCommand()
        {
            var command = new Mock<DbCommand>();
            command.Protected().Setup<DbParameter>("CreateDbParameter").Returns(() => Mock.Of<DbParameter>());
            command.Protected().SetupGet<DbParameterCollection>("DbParameterCollection").Returns(Mock.Of<DbParameterCollection>());
            command.Protected()
                .Setup<DbDataReader>("ExecuteDbDataReader", CommandBehavior.SequentialAccess | CommandBehavior.SingleResult)
                .Returns(CreateReader);
            return command.Object;
        }

        private DbDataReader CreateReader()
        {
            var reader = new Mock<DbDataReader>();
            long position = 0;
            bool closed = false;
            OpenReaders++;

            reader.Setup(x => x.Read()).Returns(true);
            reader.SetupGet(x => x.IsClosed).Returns(() => closed);
            reader.Setup(x => x.Close()).Callback(() =>
            {
                closed = true;
                OpenReaders--;
            });
            reader.Setup(x => x.GetBytes(0, It.IsAny<long>(), It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns((int ordinal, long offset, byte[] buffer, int bufferOffset, int count) =>
                {
                    if (buffer == null)
                    {
                        // SqlClient reports remaining bytes for varbinary(n), and zero after consuming varbinary(max).
                        return _reportRemainingLength || position == _data.Length ? _data.Length - position : _data.Length;
                    }

                    if (offset < position)
                    {
                        throw new InvalidOperationException("Sequential access cannot revisit consumed bytes.");
                    }

                    int read = (int)Math.Min(count, Math.Max(0, _data.Length - offset));
                    Array.Copy(_data, offset, buffer, bufferOffset, read);
                    position = offset + read;
                    return read;
                });
            return reader.Object;
        }

        public void Dispose()
            => _context.Dispose();
    }
}
