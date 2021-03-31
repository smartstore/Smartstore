using System.ComponentModel.DataAnnotations;
using Smartstore.Web.Modelling;

namespace SmartStore.Web.Models.ShoppingCart
{
    [LocalizedDisplay("Wishlist.EmailAFriend.")]
    public partial class WishlistEmailAFriendModel : ModelBase
    {
        [Required]
        [DataType(DataType.EmailAddress)]
        [LocalizedDisplay("*FriendEmail")]
        public string FriendEmail { get; set; }

        [Required]
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
}