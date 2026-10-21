using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Smartstore.Domain;
using Smartstore.IconKit;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models.Media;

/// <summary>Combines the selected icon and the configuration of its browser.</summary>
public class IconBrowserModel : ModelBase, IIcon, IDefaultable
{
    /// <summary>Gets or sets the selected icon. Null represents no selection.</summary>
    public IconExpression? Icon { get; set; }

    /// <summary>Gets or sets the resolution area. Null uses the current request area.</summary>
    [JsonIgnore, BindNever]
    public string Area { get; set; }

    /// <summary>Gets or sets the available presentation tools.</summary>
    [JsonIgnore, BindNever]
    public IconOptionFields VisibleOptions { get; set; }

    /// <summary>Gets or sets whether hidden kits are listed.</summary>
    [JsonIgnore, BindNever]
    public bool ShowHiddenKits { get; set; }

    /// <summary>Gets or sets whether provider-dependent concepts display an indicator.</summary>
    [JsonIgnore, BindNever]
    public bool ShowProviderIndicators { get; set; }

    bool IDefaultable.IsDefaultState => Icon.GetValueOrDefault() == default;
}
