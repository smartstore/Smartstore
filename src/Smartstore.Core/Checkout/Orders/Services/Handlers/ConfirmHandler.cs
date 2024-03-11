namespace Smartstore.Core.Checkout.Orders.Handlers
{
    [CheckoutStep(int.MaxValue, "Confirm")]
    public class ConfirmHandler : ICheckoutHandler
    {
        public Task<CheckoutHandlerResult> ProcessAsync(CheckoutContext context)
            => Task.FromResult(new CheckoutHandlerResult(true));
    }
}
