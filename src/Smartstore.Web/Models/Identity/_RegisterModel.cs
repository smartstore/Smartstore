using System;
using System.ComponentModel.DataAnnotations;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.Identity
{
    [LocalizedDisplay("Account.Fields.")]
    public partial class RegisterModel : ModelBase
    {
        [LocalizedDisplay("Email")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        public bool UserNamesEnabled { get; set; }
        [LocalizedDisplay("*Username")]
        public string UserName { get; set; }

        [DataType(DataType.Password)]
        [LocalizedDisplay("*Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [LocalizedDisplay("*ConfirmPassword")]
        public string ConfirmPassword { get; set; }
    }
}
