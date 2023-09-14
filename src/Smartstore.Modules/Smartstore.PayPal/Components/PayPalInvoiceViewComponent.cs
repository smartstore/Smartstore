using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Web.Components;

namespace Smartstore.PayPal.Components
{
    public class PayPalInvoiceViewComponent : SmartViewComponent
    {
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        
        public PayPalInvoiceViewComponent(ICheckoutStateAccessor checkoutStateAccessor)
        {
            _checkoutStateAccessor = checkoutStateAccessor;
        }

        public IViewComponentResult Invoke()
        {
            var customer = Services.WorkContext.CurrentCustomer;
            var model = new PublicInvoiceModel();

            if ((customer.BillingAddress != null && customer.BillingAddress.Country != null) || !customer.BillingAddress.LastName.HasValue())
            {
                var diallingCode = customer.BillingAddress.Country.DiallingCode;
                model.DiallingCode = diallingCode.Value.ToString();
            }
            else
            {
                // If there's no BillingAddress or no country, we can't offer invoice.
                return Empty();
            }

            // Set postback values.
            var paymentData = _checkoutStateAccessor.CheckoutState.PaymentData;
            var phoneNumber = (string)paymentData.Get("PayPalInvoicePhoneNumber");

            if (phoneNumber.HasValue())
            {
                model.PhoneNumber = phoneNumber;
            }

            if (((string)paymentData.Get("PayPalInvoiceBirthdate")).HasValue())
            {
                try
                {
                    var birthDate = DateTime.Parse((string)paymentData.Get("PayPalInvoiceBirthdate"));
                    model.DateOfBirth = birthDate;
                }
                catch 
                { 
                }
            }
            else if (customer.BirthDate != null)
            {
                // Prepare model if customer is logged in and has already entered his data of birth.
                model.DateOfBirth = (DateTime)customer.BirthDate;
            }

            return View(model);
        }
    }
}