using System.Runtime.CompilerServices;
using System.Text;
using Smartstore.IO;

namespace Smartstore
{
    public static partial class IFileSystemExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ReadAllText(this IFileSystem fs, string subpath, Encoding encoding = null)
        {
            Guard.NotEmpty(subpath, nameof(subpath));
            return ReadAllTextInternal(fs.GetFile(subpath), encoding, false).Await();
        }

        public static string ReadAllText(this IFile file, Encoding encoding = null)
            => ReadAllTextInternal(file, encoding, false).Await();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<string> ReadAllTextAsync(this IFileSystem fs, string subpath, Encoding encoding = null)
        {
            Guard.NotEmpty(subpath, nameof(subpath));
            return await (ReadAllTextInternal(await fs.GetFileAsync(subpath), encoding, true));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<string> ReadAllTextAsync(this IFile file, Encoding encoding = null)
            => ReadAllTextInternal(file, encoding, true);

        private static async Task<string> ReadAllTextInternal(IFile file, Encoding encoding, bool async)
        {
            Guard.NotNull(file, nameof(file));

            if (!file.Exists)
            {
                return null;
            }

            using var stream = async ? await file.OpenReadAsync() : file.OpenRead();
            using var streamReader = new StreamReader(stream, encoding ?? Encoding.UTF8);

            return async ? await streamReader.ReadToEndAsync() : streamReader.ReadToEnd();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ReadAllBytes(this IFileSystem fs, string subpath)
        {
            Guard.NotEmpty(subpath, nameof(subpath));
            return ReadAllBytesInternal(fs.GetFile(subpath), false).Await();
        }

        public static byte[] ReadAllBytes(this IFile file)
            => ReadAllBytesInternal(file, false).Await();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<byte[]> ReadAllBytesAsync(this IFileSystem fs, string subpath)
        {
            Guard.NotEmpty(subpath, nameof(subpath));
            return await (ReadAllBytesInternal(await fs.GetFileAsync(subpath), true));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<byte[]> ReadAllBytesAsync(this IFile file)
            => ReadAllBytesInternal(file, true);

        private static async Task<byte[]> ReadAllBytesInternal(IFile file, bool async)
        {
            Guard.NotNull(file, nameof(file));

            if (!file.Exists)
            {
                return null;
            }

            using var stream = async ? await file.OpenReadAsync() : file.OpenRead();
            return async ? await stream.ToByteArrayAsync() : stream.ToByteArray();
        }
    }
}