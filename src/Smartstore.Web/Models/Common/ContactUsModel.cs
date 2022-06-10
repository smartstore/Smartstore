using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.Core.Identity;

namespace Smartstore.Web.Models.Common
{
    [LocalizedDisplay("ContactUs.")]
    public partial class ContactUsModel : ModelBase
    {
        [LocalizedDisplay("*Email")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [SanitizeHtml]
        [LocalizedDisplay("*Enquiry")]
        public string Enquiry { get; set; }

        [LocalizedDisplay("*FullName")]
        public string FullName { get; set; }
        public bool FullNameRequired { get; set; }

        public bool SuccessfullySent { get; set; }
        public string Result { get; set; }

        public bool DisplayCaptcha { get; set; }

        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string MetaKeywords { get; set; }
    }

    public class ContactUsValidator : SmartValidator<ContactUsModel>
    {
        public ContactUsValidator(PrivacySettings privacySettings)
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Enquiry).NotEmpty();

            if (privacySettings.FullNameOnContactUsRequired)
            {
                RuleFor(x => x.FullName).NotEmpty();
            }
        }
    }
}
