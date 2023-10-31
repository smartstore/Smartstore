using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Smartstore.IO;

namespace Smartstore
{
    public static class StreamExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string AsString(this Stream stream)
            => AsStringInternal(stream, Encoding.UTF8, false).Await();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<string> AsStringAsync(this Stream stream)
            => AsStringInternal(stream, Encoding.UTF8, true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string AsString(this Stream stream, Encoding encoding)
            => AsStringInternal(stream, encoding, false).Await();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<string> AsStringAsync(this Stream stream, Encoding encoding)
            => AsStringInternal(stream, encoding, true);

        private static async Task<string> AsStringInternal(Stream stream, Encoding encoding, bool async)
        {
            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            using (var sr = new StreamReader(stream, encoding))
            {
                var result = async ? await sr.ReadToEndAsync() : sr.ReadToEnd();
                return result;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ToByteArray(this Stream stream)
            => ToByteArrayInternal(stream, false).Await();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<byte[]> ToByteArrayAsync(this Stream stream)
            => ToByteArrayInternal(stream, true);

        private static async Task<byte[]> ToByteArrayInternal(Stream stream, bool async)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (stream is MemoryStream mem)
            {
                if (mem.TryGetBuffer(out var buffer))
                {
                    return buffer.Array;
                }

                return mem.ToArray();
            }
            else
            {
                try
                {
                    var len = stream.Length;
                    if (len > int.MaxValue)
                    {
                        return await ToByteArrayCopy(stream, async);
                    }

                    var buffer = new byte[(int)len];

                    if (async)
                    {
                        await stream.ReadAsync(buffer.AsMemory(0, (int)len));
                    }
                    else
                    {
                        stream.Read(buffer, 0, (int)len);
                    }

                    return buffer;
                }
                catch
                {
                    return await ToByteArrayCopy(stream, async);
                }
            }

            static async Task<byte[]> ToByteArrayCopy(Stream stream, bool async)
            {
                using var memStream = new MemoryStream();
                if (async)
                {
                    await stream.CopyToAsync(memStream);
                }
                else
                {
                    stream.CopyTo(memStream);
                }

                return memStream.ToArray();
            }
        }

        public static MemoryStream WriteString(this MemoryStream stream, string value, bool seekToBegin = true)
        {
            Guard.NotNull(stream, nameof(stream));
            Guard.NotNull(value, nameof(value));

            using var writer = new StreamWriter(stream, Encoding.Unicode, 1024, true);

            writer.Write(value);
            writer.Flush();

            if (seekToBegin)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }

            return stream;
        }

        public static Stream MakeSeekable(this Stream stream)
        {
            if (stream.CanSeek)
            {
                Debug.WriteLine("The stream is seekable already.");
            }

            return new SeekableReadOnlyStream(stream, (int)stream.Length);
        }

        public static async Task<bool> CopyToFileAsync(this Stream stream, string destinationPath, bool leaveOpen = true)
        {
            Guard.NotNull(stream);
            Guard.NotEmpty(destinationPath);

            try
            {
                using (var outStream = new FileStream(
                    destinationPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 4096,
                    useAsync: true))
                {
                    await stream.CopyToAsync(outStream);
                }

                return File.Exists(destinationPath);
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.Message);
                return false;
            }
            finally
            {
                if (leaveOpen)
                {
                    if (stream.CanSeek)
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                    }
                }
                else
                {
                    stream.Close();
                    await stream.DisposeAsync();
                }
            }
        }

        public static async Task<bool> CopyToFileAsync(this Stream stream, IFile destinationFile, bool leaveOpen = true)
        {
            Guard.NotNull(stream, nameof(stream));
            Guard.NotNull(destinationFile, nameof(destinationFile));

            try
            {
                using (var outStream = await destinationFile.OpenWriteAsync())
                {
                    await stream.CopyToAsync(outStream);
                }

                // Refresh & check
                destinationFile = await destinationFile.FileSystem.GetFileAsync(destinationFile.SubPath);
                return destinationFile.Exists;
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.Message);
                return false;
            }
            finally
            {
                if (leaveOpen)
                {
                    if (stream.CanSeek)
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                    }
                }
                else
                {
                    stream.Close();
                    await stream.DisposeAsync();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContentsEqual(this Stream src, Stream other, bool? forceLengthCompare = null)
            => ContentsEqualInternal(src, other, forceLengthCompare, false).Await();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<bool> ContentsEqualAsync(this Stream src, Stream other, bool? forceLengthCompare = null)
            => ContentsEqualInternal(src, other, forceLengthCompare, true);

        private static async Task<bool> ContentsEqualInternal(Stream src, Stream other, bool? forceLengthCompare, bool async)
        {
            Guard.NotNull(src, nameof(src));
            Guard.NotNull(other, nameof(other));

            if (src == other)
            {
                // This is not merely an optimization, as incrementing one stream's position
                // should not affect the position of the other.
                return true;
            }

            // This is not 100% correct, as a stream can be non-seekable but still have a known
            // length (but hopefully the opposite can never happen). I don't know how to check
            // if the length is available without throwing an exception if it's not.
            if ((!forceLengthCompare.HasValue && src.CanSeek && other.CanSeek) || (forceLengthCompare == true))
            {
                if (src.Length != other.Length)
                {
                    return false;
                }
            }

            const int intSize = sizeof(long);
            const int bufferSize = 1024 * intSize; // 2048;
            var buffer1 = new byte[bufferSize];
            var buffer2 = new byte[bufferSize];

            while (true)
            {
                int len1 = async ? await src.ReadAsync(buffer1.AsMemory(0, bufferSize)) : src.Read(buffer1, 0, bufferSize);
                int len2 = async ? await other.ReadAsync(buffer2.AsMemory(0, bufferSize)) : other.Read(buffer2, 0, bufferSize);

                if (len1 != len2)
                    return false;

                if (len1 == 0)
                    return true;

                int iterations = (int)Math.Ceiling((double)len1 / sizeof(long));

                for (int i = 0; i < iterations; i++)
                {
                    if (BitConverter.ToInt64(buffer1, i * intSize) != BitConverter.ToInt64(buffer2, i * intSize))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }

}
