namespace Smartstore.Core.Checkout.Orders.Handlers;

[CheckoutStep(10000, CheckoutActionNames.Confirm)]
public class ConfirmHandler : CheckoutHandlerBase
{
    // "Success" must be "false" to always open confirm page.
    public override Task<CheckoutResult> ProcessAsync(CheckoutContext context)
        => Task.FromResult(new CheckoutResult(false));
}