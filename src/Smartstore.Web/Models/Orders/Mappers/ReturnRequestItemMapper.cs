using System.Dynamic;
using Smartstore.Collections;
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
    public static async Task<List<ReturnRequestItemModel>> MapAsync(this Order order,
        SmartDbContext db)
    {
        Guard.NotNull(order?.OrderItems);
        Guard.NotNull(order?.Customer?.ReturnRequests);

        var customerCurrency = await db.Currencies
            .AsNoTracking()
            .Where(x => x.CurrencyCode == order.CustomerCurrencyCode)
            .FirstOrDefaultAsync() ?? new() { CurrencyCode = order.CustomerCurrencyCode };

        var orderItemIds = order.OrderItems.Select(x => x.Id).ToArray();

        dynamic parameters = new ExpandoObject();
        parameters.Order = order;
        parameters.CustomerCurrency = customerCurrency;
        parameters.ReturnRequests = order.Customer.ReturnRequests
            .Where(x => orderItemIds.Contains(x.OrderItemId))
            .ToMultimap(x => x.OrderItemId, x => x);

        var models = await order.OrderItems
            .SelectAwait(async x => (ReturnRequestItemModel)await MapperFactory.MapAsync<OrderItem, ReturnRequestItemModel>(x, parameters))
            .ToListAsync();

        return models;
    }
}

internal class ReturnRequestItemMapper : IMapper<OrderItem, ReturnRequestItemModel>
{
    private readonly IWorkContext _workContext;
    private readonly IDateTimeHelper _dateTimeHelper;
    private readonly ProductUrlHelper _productUrlHelper;
    private readonly OrderHelper _orderHelper;
    private readonly ICurrencyService _currencyService;
    private readonly ShoppingCartSettings _shoppingCartSettings;
    private readonly MediaSettings _mediaSettings;
    private readonly CatalogSettings _catalogSettings;

    public ReturnRequestItemMapper(
        IWorkContext workContext,
        IDateTimeHelper dateTimeHelper,
        ProductUrlHelper productUrlHelper,
        OrderHelper orderHelper,
        ICurrencyService currencyService,
        ShoppingCartSettings shoppingCartSettings,
        MediaSettings mediaSettings,
        CatalogSettings catalogSettings)
    {
        _workContext = workContext;
        _dateTimeHelper = dateTimeHelper;
        _productUrlHelper = productUrlHelper;
        _orderHelper = orderHelper;
        _currencyService = currencyService;
        _shoppingCartSettings = shoppingCartSettings;
        _mediaSettings = mediaSettings;
        _catalogSettings = catalogSettings;
    }

    public async Task MapAsync(OrderItem from, ReturnRequestItemModel to, dynamic parameters = null)
    {
        Guard.NotNull(from);
        Guard.NotNull(from?.Product);
        Guard.NotNull(to);

        var language = _workContext.WorkingLanguage;
        var order = Guard.NotNull(parameters?.Order as Order);
        var customerCurrency = Guard.NotNull(parameters?.CustomerCurrency as Currency);
        var allReturnRequests = Guard.NotNull(parameters?.ReturnRequests as Multimap<int, ReturnRequest>);
        var returnRequests = allReturnRequests.TryGetValues(from.Id, out var tmp) ? tmp.ToList() : [];

        to.Id = from.Id;
        to.ProductId = from.Product.Id;
        to.ProductName = from.Product.GetLocalized(x => x.Name);
        to.ProductSeName = await from.Product.GetActiveSlugAsync();
        to.ProductUrl = await _productUrlHelper.GetProductUrlAsync(to.ProductSeName, from);
        to.AttributeInfo = HtmlUtility.FormatPlainText(HtmlUtility.ConvertHtmlToPlainText(from.AttributeDescription));
        to.Quantity = from.Quantity;
        to.ReturnQuantity = Math.Max(from.Quantity - returnRequests.Sum(x => x.Quantity), 0);
        to.ReturnRequests = returnRequests
            .Select(x => new CustomerReturnRequestModel
            {
                Id = x.Id,
                Quantity = x.Quantity,
                OrderItemId = x.OrderItemId,
                ReturnRequestStatus = x.ReturnRequestStatus.GetLocalizedEnum(language.Id),
                CreatedOn = _dateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc)
            })
            .ToList();

        switch (order.CustomerTaxDisplayType)
        {
            case TaxDisplayType.ExcludingTax:
                to.UnitPrice = _currencyService.ConvertToExchangeRate(from.UnitPriceExclTax, order.CurrencyRate, customerCurrency, true);
                break;

            case TaxDisplayType.IncludingTax:
                to.UnitPrice = _currencyService.ConvertToExchangeRate(from.UnitPriceInclTax, order.CurrencyRate, customerCurrency, true);
                break;
        }

        if (_shoppingCartSettings.ShowProductImagesOnShoppingCart)
        {
            to.Image = await _orderHelper.PrepareOrderItemImageModelAsync(
                from.Product,
                _mediaSettings.CartThumbPictureSize,
                to.ProductName,
                from.AttributeSelection,
                _catalogSettings);
        }
    }
}
