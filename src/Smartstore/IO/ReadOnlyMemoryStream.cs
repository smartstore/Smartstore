namespace Smartstore.IO
{
    /// <summary>
    /// Provides a read-only stream implementation that operates on a <see cref="ReadOnlyMemory{T}"/> of bytes.
    /// </summary>
    /// <remarks>This stream allows reading and seeking operations on a fixed, immutable block of memory. 
    /// Writing and length modification operations are not supported and will throw a <see
    /// cref="NotSupportedException"/>.</remarks>
    public class ReadOnlyMemoryStream : Stream
    {
        private readonly ReadOnlyMemory<byte> _memory;
        private long _position;

        public ReadOnlyMemoryStream(ReadOnlyMemory<byte> memory)
        {
            _memory = memory;
            _position = 0;
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => _memory.Length;

        public override long Position
        {
            get => _position;
            set => _position = Math.Clamp(value, 0, _memory.Length);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var remaining = _memory.Length - _position;
            if (remaining <= 0) return 0;

            var bytesToRead = (int)Math.Min(count, remaining);
            _memory.Span.Slice((int)_position, bytesToRead).CopyTo(buffer.AsSpan(offset));

            _position += bytesToRead;
            return bytesToRead;
        }

        public override int Read(Span<byte> buffer)
        {
            var remaining = _memory.Length - _position;
            if (remaining <= 0) return 0;

            var bytesToRead = (int)Math.Min(buffer.Length, remaining);
            _memory.Span.Slice((int)_position, bytesToRead).CopyTo(buffer);

            _position += bytesToRead;
            return bytesToRead;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancelToken)
        {
            return await Task.Run(() => Read(buffer, offset, count), cancelToken);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancelToken = default)
        {
            return new ValueTask<int>(Read(buffer.Span));
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            _position = origin switch
            {
                SeekOrigin.Begin => Math.Clamp(offset, 0, _memory.Length),
                SeekOrigin.Current => Math.Clamp(_position + offset, 0, _memory.Length),
                SeekOrigin.End => Math.Clamp(_memory.Length + offset, 0, _memory.Length),
                _ => throw new ArgumentException("Invalid SeekOrigin")
            };

            return _position;
        }

        public override void Flush() { }

        // Write methods throw exceptions, because stream is readonly.
        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotSupportedException("Stream ist read-only");

        public override void SetLength(long value)
            => throw new NotSupportedException("Stream ist read-only");
    }
}
