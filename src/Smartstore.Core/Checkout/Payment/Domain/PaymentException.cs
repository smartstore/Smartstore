#nullable enable

namespace Smartstore.Core.Checkout.Payment
{
    /// <summary>
    /// Represents an error that occurs during payment processing.
    /// </summary>
    public class PaymentException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public PaymentException(string? message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="provider">Payment provider that caused the exception.</param>
        public PaymentException(string? message, string? provider)
            : base(message)
        {
            Provider = provider;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        /// <param name="provider">Payment provider that caused the exception.</param>
        public PaymentException(string? message, Exception? innerException, string? provider)
            : base(message, innerException)
        {
            Provider = provider;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="response">The failed HTTP response.</param>
        /// <param name="provider">Payment provider that caused the exception.</param>
        public PaymentException(string? message, PaymentResponse response, string? provider)
            : this(message, response, null, provider)
        {
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="response">The failed HTTP response.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        /// <param name="provider">Payment provider that caused the exception.</param>
        public PaymentException(string? message, PaymentResponse response, Exception? innerException, string? provider)
            : base(message, innerException)
        {
            Guard.NotNull(response, nameof(response));

            Provider = provider;
            Response = response;
        }

        /// <summary>
        /// Payment provider that caused the exception.
        /// </summary>
        public string? Provider { get; init; }

        /// <summary>
        /// HTTP payment response (optional).
        /// </summary>
        public virtual PaymentResponse? Response { get; init; }
    }
}
