#nullable enable

namespace Smartstore.Core.Checkout.Payment;

/// <summary>
/// Represents an UCP (Universal Commerce Protocol) capable handler for <see cref="IPaymentMethod"/> providers
/// that supports "Agentic Commerce" (autonomous shopping via AI).
/// </summary>
public partial interface IUcpPaymentHandler
{
    /// <summary>
    /// Validates if the provider can process a headless payment request for a specific UCP method.
    /// </summary>
    /// <param name="ucpMethod">The UCP method string (e.g., "google_pay").</param>
    /// <returns><c>true</c> if the provider can process the token without UI.</returns>
    bool CanHandleUcpMethod(string ucpMethod);

    /// <summary>
    /// Maps a UCP token to a <see cref="ProcessPaymentRequest"/>.
    /// </summary>
    /// <param name="ucpToken">The token provided by the AI agent.</param>
    /// <param name="ucpMethod">The method type (e.g. "card", "token").</param>
    /// <returns>A request ready to be consumed by <see cref="IPaymentService.ProcessPaymentAsync(ProcessPaymentRequest)"/>.</returns>
    Task<ProcessPaymentRequest> CreateUcpPaymentRequestAsync(string ucpToken, string ucpMethod, decimal amount);
}
