using System;
using System.IO;

namespace Smartstore.IO
{
    /// <summary>
    /// Wraps a stream and exposes only a limited region of its data
    /// </summary>
    public class PartialStream : Stream
    {
        private readonly Stream _source;
        private readonly long _start;
        private readonly long _length;
        private readonly bool _leaveOpen;

        private long _position;

        public PartialStream(Stream source, long start, long length, bool leaveOpen = true)
        {
            if (start < 0)
            {
                throw new ArgumentException("Start index must not be less than 0", nameof(start));
            }

            if (!source.CanSeek)
            {
                if (start != 0)
                {
                    throw new ArgumentException("The only valid start for unseekable streams is 0", nameof(start));
                }
            }

            _source = Guard.NotNull(source, nameof(source));
            _start = start;
            _length = Math.Min(source.Length - start, start + length);
            _leaveOpen = leaveOpen;
        }

        /// <summary>
        /// Stream being wrapped by the partial stream wrapper.
        /// </summary>
        public Stream Sourcetream
        {
            get => _source;
        }

        /// <inheritdoc/>
        public override bool CanRead => _source.CanRead;

        /// <inheritdoc/>
        public override bool CanSeek => _source.CanSeek;

        /// <inheritdoc/>
        public override bool CanWrite => _source.CanWrite;

        /// <inheritdoc/>
        public override long Length => _length;

        /// <inheritdoc/>
        public override long Position 
        { 
            get => _position; 
            set => _position = value; 
        }

        /// <inheritdoc/>
        public override void Flush() => _source.Flush();

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            long remaining = _length - _position;
            int bytesToRead = (int)Math.Min(count, remaining);

            if (_source.CanSeek)
            {
                _source.Position = _position + _start;
            }

            int bytesRead = _source.Read(buffer, offset, bytesToRead);
            _position += bytesRead;

            return bytesRead;
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return origin switch
            {
                SeekOrigin.Begin => Position = offset,
                SeekOrigin.Current => Position += offset,
                SeekOrigin.End => Position = (Length + offset),
                _ => throw new ArgumentException("Invalid seek origin", nameof(origin)),
            };
        }

        /// <inheritdoc/>
        public override void SetLength(long value)
        {
            throw new NotSupportedException("Resizing partial streams is not supported");
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            long remaining = _length - _position;
            if (count > remaining)
            {
                throw new NotSupportedException("Cannot extend the length of the partial stream.");
            }

            if (_source.CanSeek)
            {
                _source.Position = _position + _start;
            }

            _source.Write(buffer, offset, count);

            _position += count;
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing && !_leaveOpen)
            {
                _source.Dispose();
            }
        }
    }
}
