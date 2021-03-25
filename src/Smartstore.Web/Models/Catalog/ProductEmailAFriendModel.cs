using System.ComponentModel.DataAnnotations;
using FluentValidation;
using FluentValidation.Attributes;
using Smartstore.Core.Localization;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.Catalog
{
    public partial class ProductEmailAFriendModel : ModelBase
    {
        public int ProductId { get; set; }

        public LocalizedValue<string> ProductName { get; set; }

        public string ProductSeName { get; set; }

        [Required]
        [DataType(DataType.EmailAddress)]
        [LocalizedDisplay("Products.EmailAFriend.FriendEmail")]
        public string FriendEmail { get; set; }

        [Required]
        [DataType(DataType.EmailAddress)]
        [LocalizedDisplay("Products.EmailAFriend.YourEmailAddress")]
        public string YourEmailAddress { get; set; }

        [SanitizeHtml]
        [LocalizedDisplay("Products.EmailAFriend.PersonalMessage")]
        public string PersonalMessage { get; set; }

        public bool AllowChangedCustomerEmail { get; set; }

        public bool DisplayCaptcha { get; set; }
    }
}
