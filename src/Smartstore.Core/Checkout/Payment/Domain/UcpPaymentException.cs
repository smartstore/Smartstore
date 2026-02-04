#nullable enable

namespace Smartstore.Core.Checkout.Payment;

/// <summary>
/// Represents an error that occurs during UCP (Universal Commerce Protocol) payment processing.
/// </summary>
public class UcpPaymentException : PaymentException
{
    public UcpPaymentErrorCode ErrorCode { get; }

    public UcpPaymentException(UcpPaymentErrorCode errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}

public enum UcpPaymentErrorCode
{
    GeneralError = 0,

    /// <summary>
    /// Provider does not implement <see cref="IUcpPaymentHandler"/>.
    /// </summary>
    HandlerNotSupported = 10,

    /// <summary>
    /// Token expired or malformed.
    /// </summary>
    InvalidToken = 20,

    /// <summary>
    /// Insufficient funds or card blocked.
    /// </summary>
    PaymentDeclined = 30,

    /// <summary>
    /// Strong Customer Authentication required (3DS).
    /// </summary>
    ScaRequired = 40,

    /// <summary>
    /// Agent requested currency not supported by provider.
    /// </summary>
    CurrencyMismatch = 50
}