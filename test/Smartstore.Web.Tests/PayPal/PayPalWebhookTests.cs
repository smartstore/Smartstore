using NUnit.Framework;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.PayPal.Services;

namespace Smartstore.Web.Tests.PayPal;

[TestFixture]
public class PayPalWebhookTests
{
    [Test]
    public void Late_pending_event_does_not_reset_paid_order()
    {
        Assert.That(PayPalHelper.CanSetOrderPending(OrderStatus.Processing, PaymentStatus.Paid), Is.False);
        Assert.That(PayPalHelper.CanSetOrderPending(OrderStatus.Complete, PaymentStatus.Paid), Is.False);
        Assert.That(PayPalHelper.CanSetOrderPending(OrderStatus.Pending, PaymentStatus.Paid), Is.False);
        Assert.That(PayPalHelper.CanSetOrderPending(OrderStatus.Pending, PaymentStatus.PartiallyRefunded), Is.False);
        Assert.That(PayPalHelper.CanSetOrderPending(OrderStatus.Pending, PaymentStatus.Refunded), Is.False);
    }

    [Test]
    public void Denied_event_does_not_reset_cancelled_order()
    {
        Assert.That(PayPalHelper.CanSetOrderPending(OrderStatus.Cancelled, PaymentStatus.Pending), Is.False);
        Assert.That(PayPalHelper.CanSetOrderPending(OrderStatus.Cancelled, PaymentStatus.Voided), Is.False);
    }

    [Test]
    public void Pending_event_sets_new_order_to_pending()
    {
        Assert.That(PayPalHelper.CanSetOrderPending(OrderStatus.Pending, PaymentStatus.Pending), Is.True);
        Assert.That(PayPalHelper.CanSetOrderPending(OrderStatus.Pending, PaymentStatus.Authorized), Is.True);
    }
}
