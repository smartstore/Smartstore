using System;
using System.ComponentModel.DataAnnotations;
using Smartstore.Core.Identity;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.Identity
{
    // TODO: (mh) (core) One property of Email, Username or UsernameOrEmail must be required.
    [LocalizedDisplay("Account.Login.Fields.")]
    public partial class LoginModel : ModelBase
    {
        public bool CheckoutAsGuest { get; set; }

        public CustomerLoginType CustomerLoginType { get; set; }

        [DataType(DataType.EmailAddress)]
        [LocalizedDisplay("*Email", Prompt = "*Email")]
        public string Email { get; set; }

        [LocalizedDisplay("*UserName", Prompt = "*UserName")]
        public string Username { get; set; }

        [LocalizedDisplay("*UsernameOrEmail", Prompt = "*UsernameOrEmail")]
        public string UsernameOrEmail { get; set; }

        [DataType(DataType.Password)]
        [Required]
        [LocalizedDisplay("*Password", Prompt = "*Password")]
        public string Password { get; set; }

        [LocalizedDisplay("*RememberMe")]
        public bool RememberMe { get; set; }

        public bool DisplayCaptcha { get; set; }
    }
}
