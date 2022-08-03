using System.Diagnostics;
using System.Text;
using Smartstore.IO;
using Smartstore.Utilities;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Smartstore.Core.Logging
{
    /// <summary>
    /// Provides simple logging into a file.
    /// </summary>
    public partial class TraceLogger : Disposable, ILogger
    {
        private const string NULL_MESSAGE = "[null]";

        protected TraceSource _traceSource;
        protected StreamWriter _streamWriter;

        /// <summary>
        /// Trace logger ctor.
        /// </summary>
        /// <param name="file">File. Created when it does not exist.</param>
        /// <param name="append"><c>true</c> to append log entries to the file; <c>false</c> to overwrite the file.</param>
        public TraceLogger(IFile file, bool? append = null)
        {
            Guard.NotNull(file, nameof(file));

            File = file;

            Init(append ?? file.Exists);
        }

        protected virtual void Init(bool append)
        {
            _traceSource = new TraceSource("Smartstore")
            {
                Switch = new SourceSwitch("LogSwitch", "Error")
            };

            _traceSource.Listeners.Remove("Default");

            if (CommonHelper.IsDevEnvironment)
            {
                _traceSource.Listeners.Add(new DefaultTraceListener
                {
                    Name = "Debugger",
                    Filter = new EventTypeFilter(SourceLevels.All),
                    //TraceOutputOptions = TraceOptions.DateTime
                });
            }

            var textListener = new TextWriterTraceListener(File.PhysicalPath)
            {
                Name = "File",
                Filter = new EventTypeFilter(SourceLevels.All),
                //TraceOutputOptions = TraceOptions.DateTime
            };

            try
            {
                // Force UTF-8 encoding (even if the text just contains ANSI characters).
                _streamWriter = new StreamWriter(File.PhysicalPath, append, Encoding.UTF8);
                textListener.Writer = _streamWriter;

                _traceSource.Listeners.Add(textListener);
            }
            catch (IOException)
            {
                // File is locked by another process.
            }

            // Allow the trace source to send messages to listeners for all event types.
            // Currently only error messages or higher go to the listeners. 
            // Messages must get past the source switch to get to the listeners,
            // regardless of the settings for the listeners.
            _traceSource.Switch.Level = SourceLevels.All;
        }

        protected IFile File { get; }

        IDisposable ILogger.BeginScope<TState>(TState state) => ActionDisposable.Empty;

        public bool IsEnabled(MsLogLevel logLevel)
        {
            return _traceSource.Switch.ShouldTrace(LogLevelToEventType(logLevel));
        }

        public void Log<TState>(MsLogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter?.Invoke(state, exception) ?? string.Empty;

            if (exception != null && !exception.IsFatal())
            {
                if (message.IsEmpty() || message.EqualsNoCase(NULL_MESSAGE))
                {
                    message = exception.Message;
                }

                message = message.Grow(exception.ToAllMessages(true), Environment.NewLine).TrimEnd('\n', '\r');
            }

            if (message.HasValue())
            {
                _traceSource.TraceEvent(LogLevelToEventType(logLevel), eventId.Id, message);
            }
        }

        public void Flush() => _traceSource.Flush();

        protected override void OnDispose(bool disposing)
        {
            _traceSource?.Flush();
            _traceSource?.Close();

            _streamWriter?.Close();
            _streamWriter?.Dispose();
        }

        protected virtual TraceEventType LogLevelToEventType(MsLogLevel level)
        {
            return level switch
            {
                MsLogLevel.Trace or MsLogLevel.Debug => TraceEventType.Verbose,
                MsLogLevel.Warning => TraceEventType.Warning,
                MsLogLevel.Error => TraceEventType.Error,
                MsLogLevel.Critical => TraceEventType.Critical,
                _ => TraceEventType.Information,
            };
        }
    }
}
