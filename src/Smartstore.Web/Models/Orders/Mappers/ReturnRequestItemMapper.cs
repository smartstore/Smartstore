using System.Dynamic;
using Microsoft.AspNetCore.Http;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using Smartstore.Utilities.Html;
using Smartstore.Web.Models.Customers;

namespace Smartstore.Web.Models.Orders;

public static partial class ReturnRequestMappingExtensions
{
    public static async Task<ReturnRequestItemsModel> MapAsync(this Order order,
        Dictionary<int, int> selectedQuantities = null)
    {
        dynamic parameters = new ExpandoObject();
        parameters.SelectedQuantities = selectedQuantities;

        var model = new ReturnRequestItemsModel();
        await MapperFactory.MapAsync<Order, ReturnRequestItemsModel>(order, model, parameters);

        return model;
    }
}

internal class ReturnRequestItemsMapper : IMapper<Order, ReturnRequestItemsModel>
{
    private readonly SmartDbContext _db;
    private readonly IWorkContext _workContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDateTimeHelper _dateTimeHelper;
    private readonly ProductUrlHelper _productUrlHelper;
    private readonly OrderHelper _orderHelper;
    private readonly ICurrencyService _currencyService;
    private readonly ShoppingCartSettings _shoppingCartSettings;
    private readonly MediaSettings _mediaSettings;
    private readonly CatalogSettings _catalogSettings;

    public ReturnRequestItemsMapper(
        SmartDbContext db,
        IWorkContext workContext,
        IHttpContextAccessor httpContextAccessor,
        IDateTimeHelper dateTimeHelper,
        ProductUrlHelper productUrlHelper,
        OrderHelper orderHelper,
        ICurrencyService currencyService,
        ShoppingCartSettings shoppingCartSettings,
        MediaSettings mediaSettings,
        CatalogSettings catalogSettings)
    {
        _db = db;
        _workContext = workContext;
        _httpContextAccessor = httpContextAccessor;
        _dateTimeHelper = dateTimeHelper;
        _productUrlHelper = productUrlHelper;
        _orderHelper = orderHelper;
        _currencyService = currencyService;
        _shoppingCartSettings = shoppingCartSettings;
        _mediaSettings = mediaSettings;
        _catalogSettings = catalogSettings;
    }

    public async Task MapAsync(Order from, ReturnRequestItemsModel to, dynamic parameters = null)
    {
        Guard.NotNull(from);
        Guard.NotNull(from.OrderItems);
        Guard.NotNull(from.Customer?.ReturnRequests);
        Guard.NotNull(to);

        var selectedQuantities = parameters?.SelectedQuantities as Dictionary<int, int>;

        var language = _workContext.WorkingLanguage;
        var request = _httpContextAccessor.HttpContext?.Request;
        var form = request != null && request.IsPost() && request.HasFormContentType ? request.Form : null;
        var excludingTax = from.CustomerTaxDisplayType == TaxDisplayType.ExcludingTax;
        var customerCurrency = await _db.Currencies
            .AsNoTracking()
            .Where(x => x.CurrencyCode == from.CustomerCurrencyCode)
            .FirstOrDefaultAsync() ?? new() { CurrencyCode = from.CustomerCurrencyCode };

        var orderItemIds = from.OrderItems.Select(x => x.Id).ToArray();
        var allReturnRequests = from.Customer.ReturnRequests
            .Where(x => orderItemIds.Contains(x.OrderItemId))
            .ToMultimap(x => x.OrderItemId, x => x);

        foreach (var oi in from.OrderItems)
        {
            var selected = false;
            var selectedReturnQuantity = 0;
            var productSeName = await oi.Product.GetActiveSlugAsync();
            var returnRequests = allReturnRequests.TryGetValues(oi.Id, out var tmp) ? tmp.ToList() : [];

            if (selectedQuantities != null)
            {
                if (selectedQuantities.TryGetValue(oi.Id, out selectedReturnQuantity))
                {
                    selected = selectedReturnQuantity > 0;
                }
            }
            else if (form != null)
            {
                selectedReturnQuantity = form.TryGetValue($"orderitem-quantity{oi.Id}", out var qtyVal) ? qtyVal.ToString().ToInt() : 0;
                selected = selectedReturnQuantity > 0 && form.TryGetValue($"orderitem-select{oi.Id}", out var selectedVal) && selectedVal.ToString().ToBool();
            }

            var item = new ReturnRequestItemsModel.ItemModel
            {
                Id = oi.Id,
                ProductId = oi.Product.Id,
                ProductName = oi.Product.GetLocalized(x => x.Name),
                ProductSeName = productSeName,
                ProductUrl = await _productUrlHelper.GetProductUrlAsync(productSeName, oi),
                AttributeInfo = HtmlUtility.FormatPlainText(HtmlUtility.ConvertHtmlToPlainText(oi.AttributeDescription)),
                Quantity = oi.Quantity,
                Selected = selected,
                SelectedReturnQuantity = selectedReturnQuantity,
                MaxReturnQuantity = Math.Max(oi.Quantity - returnRequests.Sum(x => x.Quantity), 0),
                ReturnRequests = returnRequests
                    .Select(x => new CustomerReturnRequestModel
                    {
                        Id = x.Id,
                        Quantity = x.Quantity,
                        OrderItemId = x.OrderItemId,
                        ReturnRequestStatus = x.ReturnRequestStatus.GetLocalizedEnum(language.Id),
                        CreatedOn = _dateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc)
                    })
                    .ToList(),
                UnitPrice = _currencyService.ConvertToExchangeRate(
                    excludingTax ? oi.UnitPriceExclTax : oi.UnitPriceInclTax,
                    from.CurrencyRate,
                    customerCurrency,
                    true)
            };

            if (_shoppingCartSettings.ShowProductImagesOnShoppingCart)
            {
                item.Image = await _orderHelper.PrepareOrderItemImageModelAsync(
                    oi.Product,
                    _mediaSettings.CartThumbPictureSize,
                    item.ProductName,
                    oi.AttributeSelection,
                    _catalogSettings);
            }

            to.Items.Add(item);
        }
    }
}
