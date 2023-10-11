namespace Smartstore.Web.Models.Customers
{
    public partial class MyAccountHeaderModel : ModelBase
    {
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public CustomerAvatarModel Avatar { get; set; } = new();
    }
}
