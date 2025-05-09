#nullable enable

using System.Buffers;
using System.Buffers.Text;
using System.IO.Pipelines;
using System.Text;
using Smartstore;

namespace Smartstore.IO
{
    /// <summary>
    /// Provides fast and allocation free methods for writing Server-Sent Events (SSE) responses.
    /// </summary>
    public class SseWriter(PipeWriter writer)
    {
        private static readonly byte[] IdPrefix = Encoding.UTF8.GetBytes("id: ");
        private static readonly byte[] DataPrefix = Encoding.UTF8.GetBytes("data: ");
        private static readonly byte[] EventPrefix = Encoding.UTF8.GetBytes("event: ");
        private static readonly byte[] NewLine = [(byte)'\n'];

        private readonly PipeWriter _pipeWriter = Guard.NotNull(writer);

        /// <summary>
        /// Writes a Server-Sent Event (SSE) response with the specified data and optional ID.
        /// </summary>
        /// <param name="data">The data to send in the SSE response.</param>
        /// <param name="id">The optional ID of the SSE event. Defaults to 0.</param>
        public ValueTask<FlushResult> WriteResponseAsync(
            string data,
            int id = 0,
            CancellationToken cancelToken = default) =>
            WriteInternalAsync(data, id, eventType: null, cancelToken);

        /// <summary>
        /// Writes a Server-Sent Event (SSE) response with the specified event type, data, and optional ID.
        /// </summary>
        /// <param name="response">The HTTP response to write to.</param>
        /// <param name="eventType">The type of the SSE event.</param>
        /// <param name="data">The data to send in the SSE response.</param>
        /// <param name="id">The optional ID of the SSE event. Defaults to 0.</param>
        public ValueTask<FlushResult> WriteEventResponseAsync(
            string eventType,
            string data,
            int id = 0,
            CancellationToken cancelToken = default) =>
            WriteInternalAsync(data, id, eventType, cancelToken);

        private ValueTask<FlushResult> WriteInternalAsync(
            string data,
            int id,
            string? eventType,
            CancellationToken cancelToken)
        {
            if (string.IsNullOrEmpty(data))
            {
                return ValueTask.FromResult(default(FlushResult));
            }

            var payload = data.AsSpan();

            // id: <n>\n
            _pipeWriter.Write(IdPrefix);
            WriteUtf8Int(id);
            _pipeWriter.Write(NewLine);

            // event: <type>\n
            if (eventType is not null)
            {
                _pipeWriter.Write(EventPrefix);
                WriteUtf8(eventType.AsSpan());
                _pipeWriter.Write(NewLine);
            }

            // data: <line>\n   for each line
            var start = 0;
            while (true)
            {
                var index = payload[start..].IndexOf('\n');
                var line = index >= 0 ? payload.Slice(start, index) : payload[start..];

                _pipeWriter.Write(DataPrefix);
                WriteUtf8(line);
                _pipeWriter.Write(NewLine);

                if (index < 0) break;
                start += index + 1;
            }

            // NewLine = end of event
            _pipeWriter.Write(NewLine);

            return _pipeWriter.FlushAsync(cancelToken);
        }

        private void WriteUtf8(ReadOnlySpan<char> text)
        {
            var destination = _pipeWriter.GetSpan(Encoding.UTF8.GetMaxByteCount(text.Length));
            var length = Encoding.UTF8.GetBytes(text, destination);
            _pipeWriter.Advance(length);
        }

        private void WriteUtf8Int(int value)
        {
            var destination = _pipeWriter.GetSpan(11);
            Utf8Formatter.TryFormat(value, destination, out int length);
            _pipeWriter.Advance(length);
        }
    }
}