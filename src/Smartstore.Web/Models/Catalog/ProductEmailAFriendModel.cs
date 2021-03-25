using System.ComponentModel.DataAnnotations;
using Smartstore.Core.Localization;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.Catalog
{
    [LocalizedDisplay("Products.EmailAFriend.")]
    public partial class ProductEmailAFriendModel : ModelBase
    {
        public int ProductId { get; set; }

        public LocalizedValue<string> ProductName { get; set; }

        public string ProductSeName { get; set; }

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

        public bool AllowChangedCustomerEmail { get; set; }

        public bool DisplayCaptcha { get; set; }
    }
}
