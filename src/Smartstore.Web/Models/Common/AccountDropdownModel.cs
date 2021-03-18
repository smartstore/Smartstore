using System.Collections.Generic;
using Smartstore.Core.Content.Menus;
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

        public List<MenuItem> MenuItems { get; } = new();
    }
}
