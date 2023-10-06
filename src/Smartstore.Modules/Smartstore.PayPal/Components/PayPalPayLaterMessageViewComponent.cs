using Microsoft.AspNetCore.Mvc;
using Smartstore.Web.Components;
using Smartstore.Web.Models.Catalog;

namespace Smartstore.PayPal.Components
{
    /// <summary>
    /// Renders the pay later widget on the product detail page.
    /// </summary>
    public class PayPalPayLaterMessageViewComponent : SmartViewComponent
    {
        private readonly PayPalSettings _settings;

        public PayPalPayLaterMessageViewComponent(PayPalSettings settings)
        {
            _settings = settings;
        }

        public IViewComponentResult Invoke(object model)
        {
            // If client id or secret haven't been configured yet, don't render button.
            if (!_settings.ClientId.HasValue() || !_settings.Secret.HasValue())
            {
                return Empty();
            }

            var productDetailsModel = (ProductDetailsModel)model;
            var finalPrice = productDetailsModel.Price.FinalPrice.Amount;

            // PayPal allows pay later only for amounts between 99€ und 5.000€.
            if (productDetailsModel != null && finalPrice >= 99 && finalPrice <= 5000)
            {
                ViewBag.Price = finalPrice.ToStringInvariant("F");
                return View();
            }

            return Empty();
        }
    }
}