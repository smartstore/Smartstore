namespace Smartstore.Web.Models.Customers
{
    public partial class MyAccountHeaderModel : ModelBase
    {
        public string CustomerName { get; set; }
        public string CustomerSince { get; set; }
        public int RewardPoints { get; set; }

        /// <summary>
        /// Not used at this moment but we leave it here for easy customization.
        /// </summary>
        public string CustomerEmail { get; set; }
        public CustomerAvatarModel Avatar { get; set; } = new();
    }
}
