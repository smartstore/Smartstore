using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core.Checkout.Payment;

namespace Smartstore.Admin.Models
{
    public class PaymentSettingsModel : ModelBase
    {
        [LocalizedDisplay("Admin.Configuration.Settings.Payment.CapturePaymentReason")]
        public CapturePaymentReason? CapturePaymentReason { get; set; }
        public IList<SelectListItem> AvailableCapturePaymentReasons { get; set; }
    }
}
