using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
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
        private const string NullMessage = "[null]";

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

        public IFile File { get; private set; }

        public IDisposable BeginScope<TState>(TState state) => default;

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

            var message = formatter(state, exception);
            
            if (exception != null && !exception.IsFatal())
            {
                if (message.IsEmpty() || message.EqualsNoCase(NullMessage))
                {
                    message = exception.Message;
                }

                message = message.Grow(exception.ToAllMessages(true), Environment.NewLine).TrimEnd('\n', '\r');
            }

            if (message.HasValue())
            {
                var type = LogLevelToEventType(logLevel);
                _traceSource.TraceEvent(type, (int)type, message);
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

        protected virtual void Init(bool append)
        {
            _traceSource = new TraceSource("Smartstore")
            {
                Switch = new SourceSwitch("LogSwitch", "Error")
            };

            _traceSource.Listeners.Remove("Default");

            if (CommonHelper.IsDevEnvironment)
            {
                var defaultListener = new DefaultTraceListener
                {
                    Name = "Debugger",
                    Filter = new EventTypeFilter(SourceLevels.All),
                    TraceOutputOptions = TraceOptions.DateTime
                };
                _traceSource.Listeners.Add(defaultListener);
            }

            var textListener = new TextWriterTraceListener(File.PhysicalPath)
            {
                Name = "File",
                Filter = new EventTypeFilter(SourceLevels.All),
                TraceOutputOptions = TraceOptions.DateTime
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
