using System.ComponentModel.DataAnnotations;

namespace Smartstore.Admin.Models.Catalog
{
    [LocalizedDisplay("Admin.Catalog.Manufacturers.List.")]
    public class ManufacturerListModel : ModelBase
    {
        [LocalizedDisplay("*SearchManufacturerName")]
        public string SearchManufacturerName { get; set; }

        [UIHint("Stores")]
        [LocalizedDisplay("Admin.Common.Store.SearchFor")]
        public int SearchStoreId { get; set; }
    }
}
