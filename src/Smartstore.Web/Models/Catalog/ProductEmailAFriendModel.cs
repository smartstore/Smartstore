using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.Core.Localization;

namespace Smartstore.Web.Models.Catalog
{
    [LocalizedDisplay("Products.EmailAFriend.")]
    public partial class ProductEmailAFriendModel : ModelBase
    {
        public int ProductId { get; set; }

        public LocalizedValue<string> ProductName { get; set; }

        public string ProductSeName { get; set; }

        [DataType(DataType.EmailAddress)]
        [LocalizedDisplay("*FriendEmail")]
        public string FriendEmail { get; set; }

        [DataType(DataType.EmailAddress)]
        [LocalizedDisplay("*YourEmailAddress")]
        public string YourEmailAddress { get; set; }

        [SanitizeHtml]
        [LocalizedDisplay("*PersonalMessage")]
        public string PersonalMessage { get; set; }

        public bool AllowChangedCustomerEmail { get; set; }

        public bool DisplayCaptcha { get; set; }
    }
    public class ProductEmailAFriendValidator : SmartValidator<ProductEmailAFriendModel>
    {
        public ProductEmailAFriendValidator()
        {
            RuleFor(x => x.FriendEmail).NotEmpty().EmailAddress();
            RuleFor(x => x.YourEmailAddress).NotEmpty().EmailAddress();
        }
    }
}
