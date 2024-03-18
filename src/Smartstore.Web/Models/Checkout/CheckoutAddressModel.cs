using FluentValidation;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Smartstore.Web.Models.Common;

namespace Smartstore.Web.Models.Checkout
{
    public partial class CheckoutAddressModel : CheckoutModelBase
    {
        public bool IsShippingRequired { get; set; }

        [ValidateNever]
        public bool ShippingAddressDiffers { get; set; }

        public bool HasAddresses =>
            !ExistingAddresses.IsNullOrEmpty();

        public List<AddressModel> ExistingAddresses { get; set; } = [];
        public AddressModel NewAddress { get; set; } = new();
    }

    public class CheckoutAddressValidator : SmartValidator<CheckoutAddressModel>
    {
        public CheckoutAddressValidator(IValidator<AddressModel> addressValidator)
        {
            RuleFor(x => x.NewAddress).SetValidator(addressValidator);
        }
    }
}
