using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Models
{
    [LocalizedDisplay("Admin.Catalog.Products.List.")]
    public class SettingListModel : ModelBase
    {
        [LocalizedDisplay("*SearchSettingName")]
        public string SearchSettingName { get; set; }

        [LocalizedDisplay("*SearchSettingValue")]
        public string SearchSettingValue { get; set; }

        [UIHint("Stores")]
        [LocalizedDisplay("Admin.Common.Store.SearchFor")]
        public int SearchStoreId { get; set; }
        public bool IsSingleStoreMode { get; set; }
        public List<SelectListItem> AvailableStores { get; set; } = new();
    }
}
