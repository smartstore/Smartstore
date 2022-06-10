using System.ComponentModel.DataAnnotations;

namespace Smartstore.Admin.Models.Menus
{
    public class MenuEntityListModel : EntityModelBase
    {
        [UIHint("Stores")]
        [LocalizedDisplay("Admin.Common.Store.SearchFor")]
        public int StoreId { get; set; }

        [LocalizedDisplay("Admin.ContentManagement.Menus.SystemName")]
        public string SystemName { get; set; }
    }
}
