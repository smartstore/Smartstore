using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Smartstore.IO;

namespace Smartstore
{
    public static partial class IFileSystemExtensions
    {
        public static void WriteAllText(this IFileSystem fs, string subpath, string contents, Encoding encoding = null)
        {
            Guard.NotEmpty(subpath, nameof(subpath));
            Guard.NotNull(contents, nameof(contents));

            var file = fs.GetFile(subpath);

            using (var stream = file.OpenWrite())
            using (var streamWriter = new StreamWriter(stream, encoding ?? new UTF8Encoding(false, true)))
            {
                streamWriter.Write(contents);
            }
        }

        public static void WriteAllBytes(this IFileSystem fs, string subpath, byte[] contents)
        {
            Guard.NotEmpty(subpath, nameof(subpath));
            Guard.NotNull(contents, nameof(contents));

            var file = fs.GetFile(subpath);

            using (var stream = file.OpenWrite())
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
        public static void SaveStream(this IFileSystem fs, string subpath, Stream inStream, bool leaveOpen = true)
        {
            Guard.NotEmpty(subpath, nameof(subpath));
            Guard.NotNull(inStream, nameof(inStream));

            var file = fs.GetFile(subpath);

            using (var stream = file.OpenWrite())
            {
                inStream.CopyTo(stream);
            }

            if (!leaveOpen)
            {
                inStream.Dispose();
            }
        }

        public static async Task WriteAllTextAsync(this IFileSystem fs, string subpath, string contents, Encoding encoding = null)
        {
            Guard.NotEmpty(subpath, nameof(subpath));
            Guard.NotNull(contents, nameof(contents));

            var file = await fs.GetFileAsync(subpath);

            using (var stream = file.OpenWrite())
            using (var streamWriter = new StreamWriter(stream, encoding ?? new UTF8Encoding(false, true)))
            {
                await streamWriter.WriteAsync(contents);
            }
        }

        public static async Task WriteAllBytesAsync(this IFileSystem fs, string subpath, byte[] contents)
        {
            Guard.NotEmpty(subpath, nameof(subpath));
            Guard.NotNull(contents, nameof(contents));

            var file = await fs.GetFileAsync(subpath);

            using (var stream = file.OpenWrite())
            {
                await stream.WriteAsync(contents.AsMemory(0, contents.Length));
            }
        }

        /// <summary>
        /// Saves a stream in the storage. If the file already exists, it will be overwritten.
        /// </summary>
        /// <param name="subpath">The relative path to the file to be saved.</param>
        /// <param name="inStream">The stream to be saved.</param>
        /// <exception cref="FileSystemException">If the stream can't be saved due to access permissions.</exception>
        public static async Task SaveStreamAsync(this IFileSystem fs, string subpath, Stream inStream, bool leaveOpen = true)
        {
            Guard.NotEmpty(subpath, nameof(subpath));
            Guard.NotNull(inStream, nameof(inStream));

            var file = await fs.GetFileAsync(subpath);

            using (var stream = file.OpenWrite())
            {
                await inStream.CopyToAsync(stream);
            }

            if (!leaveOpen)
            {
                inStream.Dispose();
            }
        }
    }
}