using FluentValidation;
using FluentValidation.Attributes;
using Smartstore.Core.Localization;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.Catalog
{
    [Validator(typeof(ProductEmailAFriendValidator))]
    public partial class ProductEmailAFriendModel : ModelBase
    {
        public int ProductId { get; set; }

        public LocalizedValue<string> ProductName { get; set; }

        public string ProductSeName { get; set; }

        [LocalizedDisplay("Products.EmailAFriend.FriendEmail")]
        public string FriendEmail { get; set; }

        [LocalizedDisplay("Products.EmailAFriend.YourEmailAddress")]
        public string YourEmailAddress { get; set; }

        [SanitizeHtml]
        [LocalizedDisplay("Products.EmailAFriend.PersonalMessage")]
        public string PersonalMessage { get; set; }

        public bool AllowChangedCustomerEmail { get; set; }

        public bool DisplayCaptcha { get; set; }
    }

    public class ProductEmailAFriendValidator : AbstractValidator<ProductEmailAFriendModel>
    {
        public ProductEmailAFriendValidator()
        {
            RuleFor(x => x.FriendEmail).NotEmpty();
            RuleFor(x => x.FriendEmail).EmailAddress();
            RuleFor(x => x.YourEmailAddress).NotEmpty();
            RuleFor(x => x.YourEmailAddress).EmailAddress();
        }
    }
}
