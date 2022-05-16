using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace Smartstore.Web.Models.Cart
{
    [LocalizedDisplay("Wishlist.EmailAFriend.")]
    public partial class WishlistEmailAFriendModel : ModelBase
    {
        [DataType(DataType.EmailAddress)]
        [LocalizedDisplay("*FriendEmail")]
        public string FriendEmail { get; set; }

        [DataType(DataType.EmailAddress)]
        [LocalizedDisplay("*YourEmailAddress")]
        public string YourEmailAddress { get; set; }

        [SanitizeHtml]
        [LocalizedDisplay("*PersonalMessage")]
        public string PersonalMessage { get; set; }

        public bool SuccessfullySent { get; set; }
        public bool DisplayCaptcha { get; set; }
        public string Result { get; set; }
    }

    public class WishlistEmailAFriendValidator : SmartValidator<WishlistEmailAFriendModel>
    {
        public WishlistEmailAFriendValidator()
        {
            RuleFor(x => x.FriendEmail).NotEmpty().EmailAddress();
            RuleFor(x => x.YourEmailAddress).NotEmpty().EmailAddress();
        }
    }
}