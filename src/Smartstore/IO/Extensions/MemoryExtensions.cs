#nullable enable

using System.Runtime.InteropServices;

namespace Smartstore.IO
{
    public static class MemoryExtensions
    {
        /// <summary>
        /// Converts a <see cref="ReadOnlyMemory{T}"/> of bytes to a <see cref="Stream"/>.
        /// </summary>
        /// <remarks>This method attempts to create the stream efficiently by using the underlying array
        /// of the memory, if available. If the memory is not backed by an array (e.g., it originates from unmanaged
        /// memory), a fallback implementation is used (see <see cref="ReadOnlyMemoryStream"/>). 
        /// The returned stream does not allow writing and is not publicly visible.</remarks>
        /// <param name="memory">The <see cref="ReadOnlyMemory{T}"/> of bytes to convert. Must not be empty.</param>
        /// <returns>A <see cref="Stream"/> that provides read-only access to the data in the specified memory.</returns>
        public static Stream AsStream(this ReadOnlyMemory<byte> memory)
        {
            Guard.NotNull(memory);

            if (MemoryMarshal.TryGetArray(memory, out var segment))
            {
                // Efficient - uses the array in memory directly
                return new MemoryStream(
                    segment.Array!,
                    segment.Offset,
                    segment.Count,
                    writable: false,
                    publiclyVisible: false);
            }
            else
            {
                // Fallback: For non-array-based memory (e.g., from unmanaged memory), we use our ReadOnlyMemoryStream
                return new ReadOnlyMemoryStream(memory);
            }
        }
    }
}
