using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Smartstore.Json;

public static partial class IndentedSerializationExtensions
{
    extension (JsonSerializerOptions options)
    {
        /// <summary>
        /// Serializes the specified value as indented JSON and writes the result to the provided stream using UTF-8
        /// encoding.
        /// </summary>
        /// <remarks>The method writes formatted (indented) JSON to improve readability. The caller is
        /// responsible for managing the lifetime of the provided stream.</remarks>
        public void SerializeIndented<T>(Stream utf8Destination, T value)
        {
            Guard.NotNull(utf8Destination);

            using var writer = new Utf8JsonWriter(utf8Destination, CreateIndentedWriterOptions(options));
            SerializeCore(writer, value, options);
        }

        /// <summary>
        /// Serializes the specified value as indented JSON and writes the UTF-8 encoded output to the provided buffer
        /// writer.
        /// </summary>
        /// <remarks>The output JSON will be formatted with indentation for improved readability. This
        /// method does not write a Byte Order Mark (BOM) to the output.</remarks>
        public void SerializeIndented<T>(IBufferWriter<byte> utf8Destination, T value)
        {
            Guard.NotNull(options);
            Guard.NotNull(utf8Destination);

            using var writer = new Utf8JsonWriter(utf8Destination, CreateIndentedWriterOptions(options));
            SerializeCore(writer, value, options);
        }

        /// <summary>
        /// Serializes the specified value to a UTF-8 encoded JSON byte array using indented formatting.
        /// </summary>
        /// <returns>A byte array containing the indented UTF-8 encoded JSON representation of the value.</returns>
        public byte[] SerializeIndentedToUtf8Bytes<T>(T value)
        {
            Guard.NotNull(options);

            var buffer = new ArrayBufferWriter<byte>(initialCapacity: 1024);
            options.SerializeIndented(buffer, value);
            return buffer.WrittenSpan.ToArray();
        }

        /// <summary>
        /// Serializes the specified value to a formatted JSON string using indented formatting.
        /// </summary>
        /// <returns>A JSON string representation of the value, formatted with indentation for readability.</returns>
        public string SerializeIndented<T>(T value)
            => Encoding.UTF8.GetString(options.SerializeIndentedToUtf8Bytes(value));

        // ----------------------------
        // Async (real async for Stream/PipeWriter)
        // ----------------------------

        /// <summary>
        /// Asynchronously serializes the specified value as indented JSON to the provided UTF-8 stream.
        /// </summary>
        /// <remarks>The method writes indented (pretty-printed) JSON to the specified stream using UTF-8
        /// encoding. The stream remains open after the operation completes. If the operation is canceled or an error
        /// occurs during serialization, the stream may be left in an incomplete state.</remarks>
        /// <param name="utf8Destination">The stream to which the indented JSON will be written. The stream must be writable and is not closed after
        /// serialization.</param>
        public async ValueTask SerializeIndentedAsync<T>(Stream utf8Destination, T value, CancellationToken cancelToken = default)
        {
            Guard.NotNull(options);
            Guard.NotNull(utf8Destination);

            var pipeWriter = PipeWriter.Create(
                utf8Destination,
                new StreamPipeWriterOptions(leaveOpen: true));

            try
            {
                await options.SerializeIndentedAsync(pipeWriter, value, cancelToken).ConfigureAwait(false);
                await pipeWriter.CompleteAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await pipeWriter.CompleteAsync(ex).ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>
        /// Asynchronously serializes the specified value as indented JSON to the provided UTF-8 destination.
        /// </summary>
        /// <remarks>The output JSON will be formatted with indentation for readability. The method
        /// flushes the writer and the underlying PipeWriter after serialization completes.</remarks>
        public async ValueTask SerializeIndentedAsync<T>(PipeWriter utf8Destination, T value, CancellationToken cancelToken = default)
        {
            Guard.NotNull(options);
            Guard.NotNull(utf8Destination);

            using var writer = new Utf8JsonWriter(utf8Destination, CreateIndentedWriterOptions(options));
            JsonSerializer.Serialize(writer, value, options);
            writer.Flush();

            await utf8Destination.FlushAsync(cancelToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously serializes the specified value as indented JSON to the provided UTF-8 buffer.
        /// </summary>
        public ValueTask SerializeIndentedAsync<T>(IBufferWriter<byte> utf8Destination, T value, CancellationToken cancelToken = default)
        {
            cancelToken.ThrowIfCancellationRequested();
            options.SerializeIndented(utf8Destination, value);
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Asynchronously serializes the specified value to a UTF-8 encoded JSON byte array using indented formatting.
        /// </summary>
        /// <returns>A value task representing the asynchronous operation. The result contains a byte array with the indented
        /// UTF-8 encoded JSON representation of the value.</returns>
        public ValueTask<byte[]> SerializeIndentedToUtf8BytesAsync<T>(T value, CancellationToken cancelToken = default)
        {
            cancelToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(options.SerializeIndentedToUtf8Bytes(value));
        }

        /// <summary>
        /// Asynchronously serializes the specified value to a formatted JSON string using indented formatting.
        /// </summary>
        /// <param name="value">The value to serialize to JSON. Can be null for reference types.</param>
        public ValueTask<string> SerializeIndentedAsync<T>(T value, CancellationToken cancelToken = default)
        {
            cancelToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(options.SerializeIndented(value));
        }
    }

    private static JsonWriterOptions CreateIndentedWriterOptions(JsonSerializerOptions options)
    {
        return new JsonWriterOptions
        {
            Indented = true,
            Encoder = options.Encoder ?? JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            SkipValidation = false
        };
    }

    private static void SerializeCore<T>(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
        writer.Flush();
    }
}
