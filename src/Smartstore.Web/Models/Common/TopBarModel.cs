namespace Smartstore.Web.Models.Common
{
    public partial class TopBarModel : ModelBase
    {
        public bool RecentlyAddedProductsEnabled { get; set; }
        public bool IsAuthenticated { get; set; }
        public bool DisplayLoginLink { get; set; }
        public bool DisplayAdminLink { get; set; }
        public bool IsCustomerImpersonated { get; set; }
        public string CustomerEmailUsername { get; set; }
        public bool HasContactUsPage { get; set; }
    }
}
