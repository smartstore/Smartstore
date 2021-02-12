using System;
using System.ComponentModel.DataAnnotations;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.Identity
{
    [LocalizedDisplay("Account.Fields.")]
    public partial class RegisterModel : ModelBase
    {
        public bool UserNamesEnabled { get; set; }

        [Required]
        [LocalizedDisplay("Email")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [LocalizedDisplay("*Username")]
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [LocalizedDisplay("*Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [LocalizedDisplay("*ConfirmPassword")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }
}
