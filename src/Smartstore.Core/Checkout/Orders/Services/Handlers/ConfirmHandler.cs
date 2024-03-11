namespace Smartstore.Core.Checkout.Orders.Handlers
{
    [CheckoutStep(int.MaxValue, "Confirm")]
    public class ConfirmHandler : ICheckoutHandler
    {
        // "Success" must be "false" to always open confirm page.
        public Task<CheckoutHandlerResult> ProcessAsync(CheckoutContext context)
            => Task.FromResult(new CheckoutHandlerResult(false));
    }
}
