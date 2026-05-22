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

namespace Smartstore.Web.Models.Orders;

public static partial class ReturnCaseMappingExtensions
{
    public static async Task<ReturnCaseItemsModel> MapAsync(this Order order,
        bool isEditable = true,
        bool returnAllItems = true)
    {
        dynamic parameters = new ExpandoObject();
        parameters.IsEditable = isEditable;
        parameters.ReturnAllItems = returnAllItems;

        var model = new ReturnCaseItemsModel();
        await MapperFactory.MapAsync<Order, ReturnCaseItemsModel>(order, model, parameters);

        return model;
    }

    public static async Task<ReturnCaseModel> MapAsync(this ReturnCase returnCase,
        OrderItem orderItem = null)
    {
        dynamic parameters = new ExpandoObject();
        parameters.OrderItem = orderItem;

        var model = new ReturnCaseModel();
        await MapperFactory.MapAsync<ReturnCase, ReturnCaseModel>(returnCase, model, parameters);
        return model;
    }
}

internal class ReturnCaseItemsMapper : IMapper<Order, ReturnCaseItemsModel>
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

    public ReturnCaseItemsMapper(
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

    public async Task MapAsync(Order from, ReturnCaseItemsModel to, dynamic parameters = null)
    {
        Guard.NotNull(from);
        Guard.NotNull(from.OrderItems);
        Guard.NotNull(from.Customer?.ReturnCases);
        Guard.NotNull(to);

        to.IsEditable = parameters?.IsEditable == true;
        to.ReturnAllItems = parameters?.ReturnAllItems == true;

        var language = _workContext.WorkingLanguage;
        var request = _httpContextAccessor.HttpContext?.Request;
        var form = request != null && request.IsPost() && request.HasFormContentType ? request.Form : null;
        var excludingTax = from.CustomerTaxDisplayType == TaxDisplayType.ExcludingTax;
        var customerCurrency = await _db.Currencies
            .AsNoTracking()
            .Where(x => x.CurrencyCode == from.CustomerCurrencyCode)
            .FirstOrDefaultAsync() ?? new() { CurrencyCode = from.CustomerCurrencyCode };

        var orderItemIds = from.OrderItems.Select(x => x.Id).ToArray();
        var allReturnCases = from.Customer.ReturnCases
            .Where(x => orderItemIds.Contains(x.OrderItemId))
            .ToMultimap(x => x.OrderItemId, x => x);

        foreach (var oi in from.OrderItems)
        {
            var productSeName = await oi.Product.GetActiveSlugAsync();
            var returnCases = allReturnCases.TryGetValues(oi.Id, out var tmp) ? tmp.ToList() : [];
            var item = new ReturnCaseItemsModel.ReturnCaseItemModel
            {
                Id = oi.Id,
                ProductId = oi.Product.Id,
                ProductName = oi.Product.GetLocalized(x => x.Name),
                ProductSeName = productSeName,
                ProductUrl = await _productUrlHelper.GetProductUrlAsync(productSeName, oi),
                AttributeInfo = HtmlUtility.FormatPlainText(HtmlUtility.ConvertHtmlToPlainText(oi.AttributeDescription)),
                Quantity = oi.Quantity,
                MaxReturnQuantity = Math.Max(oi.Quantity - returnCases.Sum(x => x.Quantity), 0),
                ReturnCases = await returnCases
                    .SelectAwait(async x => await x.MapAsync(oi))
                    .ToListAsync(),
                UnitPrice = _currencyService.ConvertToExchangeRate(
                    excludingTax ? oi.UnitPriceExclTax : oi.UnitPriceInclTax,
                    from.CurrencyRate,
                    customerCurrency,
                    true)
            };

            if (form != null)
            {
                item.SelectedReturnQuantity = form.TryGetValue($"orderitem-quantity{oi.Id}", out var qtyVal) ? qtyVal.ToString().ToInt() : 0;
                item.Selected = item.SelectedReturnQuantity > 0 && form.TryGetValue($"orderitem-select{oi.Id}", out var selectedVal) && selectedVal.ToString().ToBool();
            }

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

        to.HasSingleItemToReturn = from.OrderItems.Count == 1
            && to.Items.Count == 1
            && to.Items[0].MaxReturnQuantity == 1;
        if (to.HasSingleItemToReturn)
        {
            // Force "ReturnAllItems" if there's only a single item to return.
            to.ReturnAllItems = true;
        }
    }
}