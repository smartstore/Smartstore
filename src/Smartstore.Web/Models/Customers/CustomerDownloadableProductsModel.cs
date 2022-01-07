using Smartstore.Core.Localization;

namespace Smartstore.Web.Models.Customers
{
    public partial class CustomerDownloadableProductsModel : ModelBase
    {
        public List<DownloadableProductsModel> Items { get; set; } = new();

        public partial class DownloadableProductsModel : ModelBase
        {
            public Guid OrderItemGuid { get; set; }
            public int OrderId { get; set; }
            public int ProductId { get; set; }
            public LocalizedValue<string> ProductName { get; set; }
            public string ProductSeName { get; set; }
            public string ProductUrl { get; set; }
            public string ProductAttributes { get; set; }
            public int LicenseId { get; set; }
            public bool IsDownloadAllowed { get; set; }
            public DateTime CreatedOn { get; set; }
            public List<DownloadVersion> DownloadVersions { get; set; } = new();
        }
    }

    public partial class UserAgreementModel : ModelBase
    {
        public Guid OrderItemGuid { get; set; }
        public string UserAgreementText { get; set; }
        public string FileVersion { get; set; }
    }

    public class DownloadVersion
    {
        public int DownloadId { get; set; }
        public string FileName { get; set; }
        public Guid DownloadGuid { get; set; }
        public string FileVersion { get; set; }
        public string Changelog { get; set; }
    }
}
