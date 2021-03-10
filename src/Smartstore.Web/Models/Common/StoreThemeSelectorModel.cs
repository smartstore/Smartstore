using System.Collections.Generic;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.Common
{
    public partial class StoreThemeSelectorModel : ModelBase
    {
        public List<StoreThemeModel> AvailableStoreThemes { get; set; } = new();

        public StoreThemeModel CurrentStoreTheme { get; set; }
    }
}
