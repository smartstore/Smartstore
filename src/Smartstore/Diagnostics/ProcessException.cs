#nullable enable

using System.Diagnostics;

namespace Smartstore.Diagnostics
{
    public class ProcessException : Exception
    {
        public ProcessException(string? message, ICollection<string>? data)
            : base(BuildMessage(message, null, data))
        {
        }

        public ProcessException(string? message, Exception? innerException, ICollection<string>? data)
            : base(BuildMessage(message, null, data), innerException)
        {
        }

        public ProcessException(Process? process, ICollection<string> data)
            : base(BuildMessage(null, process, data))
        {
        }

        private static string BuildMessage(string? message, Process? process, ICollection<string>? data)
        {
            var freezeMessage = false;

            if (message.IsEmpty())
            {
                if (data != null && data.Count == 1)
                {
                    message = data.First();
                    freezeMessage = true;
                }
                else
                {
                    var processName = process?.StartInfo?.FileName.NaIfEmpty();
                    message = data != null && data.Count > 1
                        ? $"One or more errors occured while running process '{processName}':"
                        : $"An unknown error occured while running process '{processName}'.";
                }
            }

            if (data != null && data.Count > 0 && !freezeMessage)
            {
                message += Environment.NewLine + string.Join(Environment.NewLine, data);
            }

            return message ?? string.Empty;
        }
    }
}
