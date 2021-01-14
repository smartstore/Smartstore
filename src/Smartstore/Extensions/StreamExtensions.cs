using System;
using System.Text;
using System.IO;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Smartstore.IO;
using System.Threading.Tasks;

namespace Smartstore
{
	public static class StreamExtensions
	{
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StreamReader ToStreamReader(this Stream stream, bool leaveOpen)
		{
			return new StreamReader(stream, Encoding.UTF8, true, 0x400, leaveOpen);
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StreamReader ToStreamReader(this Stream stream, Encoding encoding, bool detectEncoding, int bufferSize, bool leaveOpen)
		{
			return new StreamReader(stream, encoding, detectEncoding, bufferSize, leaveOpen);
		}

        public static Stream MakeSeekable(this Stream stream)
        {
            if (stream.CanSeek)
            {
                Debug.WriteLine("The stream is seekable already.");
            }

            return new SeekableReadOnlyStream(stream, (int)stream.Length);
        }

        public static async Task<bool> ToFileAsync(this Stream srcStream, string path) 
        {
			if (srcStream == null)
				return false;

			const int BuffSize = 32768;
			var result = true;
			Stream dstStream = null;
			var buffer = new byte[BuffSize];

			try 
            {
                await using (dstStream = File.Open(path, FileMode.Create))
                {
					int len;
                    while ((len = await srcStream.ReadAsync(buffer.AsMemory(0, BuffSize))) > 0)
                    {
                        await dstStream.WriteAsync(buffer.AsMemory(0, len));
                    }
				}
            }
			catch 
            {
				result = false;
			}
			finally
			{
				if (dstStream != null)
				{
					dstStream.Close();
					await dstStream.DisposeAsync();
				}
			}

			return (result && File.Exists(path));
		}

        public static bool ContentsEqual(this Stream src, Stream other, bool? forceLengthCompare = null)
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
                int len1 = src.Read(buffer1, 0, bufferSize);
                int len2 = other.Read(buffer2, 0, bufferSize);

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

        public static async Task<bool> ContentsEqualAsync(this Stream src, Stream other, bool? forceLengthCompare = null) 
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
                int len1 = await src.ReadAsync(buffer1.AsMemory(0, bufferSize));
                int len2 = await other.ReadAsync(buffer2.AsMemory(0, bufferSize));

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
