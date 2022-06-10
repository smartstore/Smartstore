using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smartstore.Admin.Models
{
    [LocalizedDisplay("Admin.Configuration.Settings.AllSettings.Fields.")]
    public class SettingListModel : ModelBase
    {
        [LocalizedDisplay("*Name")]
        public string SearchSettingName { get; set; }

        [LocalizedDisplay("*Value")]
        public string SearchSettingValue { get; set; }

        [UIHint("Stores")]
        [LocalizedDisplay("Admin.Common.Store.SearchFor")]
        public int SearchStoreId { get; set; }
        public bool IsSingleStoreMode { get; set; }
        public List<SelectListItem> AvailableStores { get; set; } = new();
    }
}
