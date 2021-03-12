using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.Common
{
    public partial class AccountDropdownModel : EntityModelBase
    {
        public string Name { get; set; }
        public bool IsAuthenticated { get; set; }
        public bool DisplayAdminLink { get; set; }
        public bool ShoppingCartEnabled { get; set; }
        public int ShoppingCartItems { get; set; }
        public bool WishlistEnabled { get; set; }
        public int WishlistItems { get; set; }

        // TODO: (mh) (core) Remove obsolete properties.
        // INFO: PMs will be prepared in Forum Module.
        //public bool AllowPrivateMessages { get; set; }
        //public string UnreadPrivateMessages { get; set; }
        //public string AlertMessage { get; set; }


        // TODO: (mh) (core) Remove unused properties.
        //[LocalizedDisplay("Account.Login.Fields.Email")]
        //public string Email { get; set; }

        //public bool UsernamesEnabled { get; set; }

        //[LocalizedDisplay("Account.Login.Fields.UserName")]
        //public string Username { get; set; }

        //[DataType(DataType.Password)]
        //[LocalizedDisplay("Account.Login.Fields.Password")]
        //public string Password { get; set; }

        //[LocalizedDisplay("Account.Login.Fields.RememberMe")]
        //public bool RememberMe { get; set; }
    }
}
