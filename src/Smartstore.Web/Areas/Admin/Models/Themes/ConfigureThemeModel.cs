using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Models.Themes
{
    public class ConfigureThemeModel : ModelBase
    {
        public string ThemeName { get; set; }

        [LocalizedDisplay("Admin.Common.Store")]
        public int StoreId { get; set; }

        public IList<SelectListItem> AvailableStores { get; set; }
    }
}
