using System.Runtime.CompilerServices;
using System.Text;
using Smartstore.IO;

namespace Smartstore
{
    public static partial class IFileSystemExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAllText(this IFileSystem fs, string subpath, string contents, Encoding encoding = null)
            => WriteAllTextInternal(fs, subpath, contents, encoding, false).Await();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteAllTextAsync(this IFileSystem fs, string subpath, string contents, Encoding encoding = null)
            => WriteAllTextInternal(fs, subpath, contents, encoding, true);

        private static async Task WriteAllTextInternal(IFileSystem fs, string subpath, string contents, Encoding encoding, bool async)
        {
            Guard.NotEmpty(subpath, nameof(subpath));
            Guard.NotNull(contents, nameof(contents));

            var file = async ? await fs.GetFileAsync(subpath) : fs.GetFile(subpath);

            using var stream = async ? await file.OpenWriteAsync() : file.OpenWrite();
            using var streamWriter = new StreamWriter(stream, encoding ?? new UTF8Encoding(false, true));
            if (async)
            {
                await streamWriter.WriteAsync(contents);
            }
            else
            {
                streamWriter.Write(contents);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAllBytes(this IFileSystem fs, string subpath, byte[] contents)
            => WriteAllBytesInternal(fs, subpath, contents, false).Await();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteAllBytesAsync(this IFileSystem fs, string subpath, byte[] contents)
            => WriteAllBytesInternal(fs, subpath, contents, true);

        private static async Task WriteAllBytesInternal(IFileSystem fs, string subpath, byte[] contents, bool async)
        {
            Guard.NotEmpty(subpath, nameof(subpath));
            Guard.NotNull(contents, nameof(contents));

            var file = async ? await fs.GetFileAsync(subpath) : fs.GetFile(subpath);

            using var stream = async ? await file.OpenWriteAsync() : file.OpenWrite();

            if (async)
            {
                await stream.WriteAsync(contents.AsMemory(0, contents.Length));
            }
            else
            {
                stream.Write(contents, 0, contents.Length);
            }
        }


        /// <summary>
        /// Saves a stream in the storage. If the file already exists, it will be overwritten.
        /// </summary>
        /// <param name="subpath">The relative path to the file to be saved.</param>
        /// <param name="inStream">The stream to be saved.</param>
        /// <exception cref="FileSystemException">If the stream can't be saved due to access permissions.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SaveStream(this IFileSystem fs, string subpath, Stream inStream, bool leaveOpen = true)
            => SaveStreamInternal(fs, subpath, inStream, leaveOpen, false).Await();

        /// <summary>
        /// Saves a stream in the storage. If the file already exists, it will be overwritten.
        /// </summary>
        /// <param name="subpath">The relative path to the file to be saved.</param>
        /// <param name="inStream">The stream to be saved.</param>
        /// <exception cref="FileSystemException">If the stream can't be saved due to access permissions.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task SaveStreamAsync(this IFileSystem fs, string subpath, Stream inStream, bool leaveOpen = true)
            => SaveStreamInternal(fs, subpath, inStream, leaveOpen, true);

        public static async Task SaveStreamInternal(IFileSystem fs, string subpath, Stream inStream, bool leaveOpen, bool async)
        {
            Guard.NotEmpty(subpath, nameof(subpath));
            Guard.NotNull(inStream, nameof(inStream));

            var file = async ? await fs.GetFileAsync(subpath) : fs.GetFile(subpath);

            using (var stream = (async ? await file.OpenWriteAsync() : file.OpenWrite()))
            {
                if (async)
                {
                    await inStream.CopyToAsync(stream);
                }
                else
                {
                    inStream.CopyTo(stream);
                }
            }

            if (!leaveOpen)
            {
                if (async)
                {
                    await inStream.DisposeAsync();
                }
                else
                {
                    inStream.Dispose();
                }
            }
        }
    }
}