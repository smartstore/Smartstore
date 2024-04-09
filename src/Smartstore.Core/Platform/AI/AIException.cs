#nullable enable

namespace Smartstore.Core.Platform.AI
{
    /// <summary>
    /// Represents an error that occurs during AI processing.
    /// </summary>
    /// <remarks>
    /// It is recommended to output a user-friendly message and to put all technical details 
    /// such as text or image creation data into an inner exception for logging.
    /// </remarks>
    public class AIException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AIException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public AIException(string? message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AIException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="provider">Payment provider that caused the exception.</param>
        public AIException(string? message, string? provider)
            : base(message)
        {
            Provider = provider;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AIException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        /// <param name="provider">Payment provider that caused the exception.</param>
        public AIException(string? message, Exception? innerException, string? provider)
            : base(message, innerException)
        {
            Provider = provider;
        }

        /// <summary>
        /// AI provider that caused the exception.
        /// </summary>
        public string? Provider { get; init; }
    }
}
