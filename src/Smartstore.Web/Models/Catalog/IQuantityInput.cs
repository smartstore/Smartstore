using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Localization;

namespace Smartstore.Web.Models.Catalog
{
    public interface IQuantityInput
    {
        decimal EnteredQuantity { get; }
        decimal MinOrderAmount { get; }
        decimal MaxOrderAmount { get; }
        decimal QuantityStep { get; }
        LocalizedValue<string> QuantityUnitName { get; }
        List<SelectListItem> AllowedQuantities { get; }
        QuantityControlType QuantityControlType { get; }
    }
}
