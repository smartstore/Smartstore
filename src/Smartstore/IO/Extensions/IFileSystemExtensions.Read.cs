using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Smartstore.IO;

namespace Smartstore
{
    public static partial class IFileSystemExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ReadAllText(this IFileSystem fs, string subpath, Encoding encoding = null)
        {
            Guard.NotEmpty(subpath, nameof(subpath));
            return fs.GetFile(subpath).ReadAllText(encoding);
        }

        public static string ReadAllText(this IFile file, Encoding encoding = null)
        {
            Guard.NotNull(file, nameof(file));

            if (!file.Exists)
            {
                return null;
            }

            using (var stream = file.OpenRead())
            using (var streamReader = new StreamReader(stream, encoding ?? Encoding.UTF8))
            {
                return streamReader.ReadToEnd();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ReadAllBytes(this IFileSystem fs, string subpath)
        {
            Guard.NotEmpty(subpath, nameof(subpath));
            return fs.GetFile(subpath).ReadAllBytes();
        }

        public static byte[] ReadAllBytes(this IFile file)
        {
            Guard.NotNull(file, nameof(file));

            if (!file.Exists)
            {
                return null;
            }

            using (var stream = file.OpenRead())
            {
                return stream.ToByteArray();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<string> ReadAllTextAsync(this IFileSystem fs, string subpath, Encoding encoding = null)
        {
            Guard.NotEmpty(subpath, nameof(subpath));
            return await (await fs.GetFileAsync(subpath)).ReadAllTextAsync(encoding);
        }

        public static async Task<string> ReadAllTextAsync(this IFile file, Encoding encoding = null)
        {
            Guard.NotNull(file, nameof(file));

            if (!file.Exists)
            {
                return null;
            }

            using (var stream = file.OpenRead())
            using (var streamReader = new StreamReader(stream, encoding ?? Encoding.UTF8))
            {
                return await streamReader.ReadToEndAsync();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<byte[]> ReadAllBytesAsync(this IFileSystem fs, string subpath)
        {
            Guard.NotEmpty(subpath, nameof(subpath));
            return await (await fs.GetFileAsync(subpath)).ReadAllBytesAsync();
        }

        public static async Task<byte[]> ReadAllBytesAsync(this IFile file)
        {
            Guard.NotNull(file, nameof(file));

            if (!file.Exists)
            {
                return null;
            }

            using (var stream = file.OpenRead())
            {
                return await stream.ToByteArrayAsync();
            }
        }
    }
}