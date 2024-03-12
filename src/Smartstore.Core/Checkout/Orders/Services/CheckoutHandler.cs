#nullable enable

namespace Smartstore.Core.Checkout.Orders
{
    public interface ICheckoutHandler
    {
        /// <summary>
        /// Processes a checkout step.
        /// </summary>
        Task<CheckoutResult> ProcessAsync(CheckoutContext context);
    }

    public sealed class CheckoutHandler(Lazy<ICheckoutHandler, CheckoutHandlerMetadata> lazy)
    {
        private readonly Lazy<ICheckoutHandler, CheckoutHandlerMetadata> _lazy = lazy;

        public ICheckoutHandler Value => _lazy.Value;
        public CheckoutHandlerMetadata Metadata => _lazy.Metadata;

        public bool IsValueCreated => _lazy.IsValueCreated;

        public Lazy<ICheckoutHandler, CheckoutHandlerMetadata> ToLazy() => _lazy;

        public override string ToString()
            => _lazy.Metadata.ToString();
    }
}
