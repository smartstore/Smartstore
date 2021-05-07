using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Smartstore.Core.DataExchange.Export
{
    public class ExportFileStream : Stream
    {
        private readonly Stream _stream;
        private readonly long _flushBytesNumber;
        private long _bytesCounter;

        /// <param name="stream">Stream instance to write to.</param>
        /// <param name="flushBytesNumber">Number of bytes when to write to the hard disk. Default is each 4 MB.</param>
        public ExportFileStream(Stream stream, long flushBytesNumber = 4194304)
        {
            Guard.NotNull(stream, nameof(stream));

            _stream = stream;
            _flushBytesNumber = flushBytesNumber;
        }

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanSeek;

        public override bool CanWrite => _stream.CanWrite;

        public override long Length => _stream.Length;

        public override long Position
        {
            get => _stream.Position;
            set => _stream.Position = value;
        }

        public override void Flush()
        {
            if (_stream is FileStream fStream)
            {
                fStream.Flush(true);
            }
            else
            {
                _stream.Flush();
            }
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            if (_stream is FileStream fStream)
            {
                fStream.Flush(true);
            }
            else
            {
                await _stream.FlushAsync(cancellationToken);
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _stream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);

            if (_flushBytesNumber > 0)
            {
                _bytesCounter += count;

                if (_bytesCounter > _flushBytesNumber)
                {
                    _bytesCounter = 0;

                    Flush();
                }
            }
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await _stream.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);

            if (_flushBytesNumber > 0)
            {
                _bytesCounter += count;

                if (_bytesCounter > _flushBytesNumber)
                {
                    _bytesCounter = 0;

                    await FlushAsync(cancellationToken);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stream?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
