using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smartstore.Admin.Models.Themes;

[LocalizedDisplay("Admin.Configuration.Themes.Option.")]
public class ThemeListModel : TabbableModel
{
    public List<SelectListItem> AvailableBundleOptimizationValues { get; set; } = [];

    public List<SelectListItem> AvailableAssetCachingValues { get; set; } = [];

    [LocalizedDisplay("*BundleOptimizationEnabled")]
    public int BundleOptimizationEnabled { get; set; }

    [LocalizedDisplay("*AssetCachingEnabled")]
    public int AssetCachingEnabled { get; set; }

    [LocalizedDisplay("*DefaultDesktopTheme")]
    public string DefaultTheme { get; set; }
    public List<ThemeDescriptorModel> Themes { get; set; } = [];

    [LocalizedDisplay("*AllowCustomerToSelectTheme")]
    public bool AllowCustomerToSelectTheme { get; set; }

    [LocalizedDisplay("*SaveThemeChoiceInCookie")]
    public bool SaveThemeChoiceInCookie { get; set; }

    [LocalizedDisplay("Admin.Common.Store")]
    public int StoreId { get; set; }

    public IList<SelectListItem> AvailableStores { get; set; }
}