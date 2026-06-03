using System.Dynamic;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using Smartstore.Web.Rendering.Choices;

namespace Smartstore.Admin.Models.Orders;

internal static partial class AddProductToOrderMappingExtensions
{
    public static async Task<MapperResult<AddOrderProductModel, AddOrderProductData>> MapAsync(this AddOrderProductModel from,
        ProductVariantQuery query = null)
    {
        dynamic parameters = new ExpandoObject();
        parameters.ProductVariantQuery = query;

        var mapper = MapperFactory.GetMapper<AddOrderProductModel, MapperResult<AddOrderProductModel, AddOrderProductData>>();
        var result = new MapperResult<AddOrderProductModel, AddOrderProductData>(from, new());
        
        await mapper.MapAsync(from, result, parameters);
        return result;
    }
}

internal class AddProductToOrderMapper : IMapper<AddOrderProductModel, MapperResult<AddOrderProductModel, AddOrderProductData>>
{
    private readonly SmartDbContext _db;
    private readonly IWorkContext _workContext;
    private readonly Lazy<ICurrencyService> _currencyService;
    private readonly IPriceCalculationService _priceCalculationService;
    private readonly IProductAttributeMaterializer _productAttributeMaterializer;
    private readonly CatalogSettings _catalogSettings;

    public AddProductToOrderMapper(
        SmartDbContext db,
        IWorkContext workContext,
        Lazy<ICurrencyService> currencyService,
        IPriceCalculationService priceCalculationService,
        IProductAttributeMaterializer productAttributeMaterializer,
        CatalogSettings catalogSettings)
    {
        _db = db;
        _workContext = workContext;
        _currencyService = currencyService;
        _priceCalculationService = priceCalculationService;
        _productAttributeMaterializer = productAttributeMaterializer;
        _catalogSettings = catalogSettings;
    }

    public async Task MapAsync(AddOrderProductModel from, MapperResult<AddOrderProductModel, AddOrderProductData> to, dynamic parameters = null)
    {
        Guard.NotNull(from);

        var orderId = Guard.NotZero(from.OrderId);
        var productId = Guard.NotZero(from.ProductId);
        var model = Guard.NotNull(to.Model);
        var data = Guard.NotNull(to.Data);

        var query = parameters?.ProductVariantQuery as ProductVariantQuery;
        var preselectAttributes = query == null;
        query ??= new();

        var product = await _db.Products
            .AsSplitQuery()
            .Include(x => x.ProductVariantAttributes)
                .ThenInclude(x => x.ProductAttribute)
            .Include(x => x.ProductVariantAttributes)
                .ThenInclude(x => x.ProductVariantAttributeValues)
            .Include(x => x.ProductVariantAttributes)
                .ThenInclude(x => x.RuleSet)
            .FindByIdAsync(productId);

        var order = await _db.Orders
            .Include(x => x.Customer)
            .IncludeOrderItems()
            .FindByIdAsync(orderId);

        if (product == null || order == null)
        {
            return;
        }

        var currency = await _db.Currencies
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CurrencyCode == order.CustomerCurrencyCode) ?? _currencyService.Value.PrimaryCurrency;

        var attributes = product.ProductVariantAttributes
            .OrderBy(x => x.DisplayOrder)
            .ToList();

        var giftCardInfo = product.IsGiftCard ? query.GetGiftCardInfo(product.Id, 0) : null;

        var linkedProducts = new Dictionary<int, Product>();
        var linkedProductIds = attributes
            .SelectMany(x => x.ProductVariantAttributeValues)
            .Where(x => x.ValueType == ProductVariantAttributeValueType.ProductLinkage && x.LinkedProductId != 0)
            .ToDistinctArray(x => x.LinkedProductId);

        if (linkedProductIds.Length > 0)
        {
            linkedProducts = await _db.Products
                .AsNoTracking()
                .Where(x => linkedProductIds.Contains(x.Id) && x.Visibility != ProductVisibility.Hidden)
                .SelectSummary()
                .ToDictionaryAsync(x => x.Id);
        }

        foreach (var attribute in attributes)
        {
            var attributeModel = new AddOrderProductModel.ProductVariantAttributeModel
            {
                Id = attribute.Id,
                ProductId = attribute.ProductId,
                BundleItemId = 0,
                ProductAttributeId = attribute.ProductAttributeId,
                Alias = attribute.ProductAttribute.Alias,
                Name = attribute.ProductAttribute.GetLocalized(x => x.Name),
                Description = attribute.ProductAttribute.GetLocalized(x => x.Description),
                TextPrompt = attribute.TextPrompt,
                CustomData = attribute.CustomData,
                IsRequired = attribute.IsRequired,
                AttributeControlType = attribute.AttributeControlType,
                AllowedFileExtensions = _catalogSettings.FileUploadAllowedExtensions
            };

            if (attribute.IsListTypeAttribute())
            {
                var valueModels = await attribute.ProductVariantAttributeValues
                    .SelectAwait(async x =>
                    {
                        var m = new AddOrderProductModel.ProductVariantAttributeValueModel
                        {
                            Id = x.Id,
                            PriceAdjustment = string.Empty,
                            Name = x.GetLocalized(x => x.Name),
                            Alias = x.Alias,
                            Color = x.Color,
                            IsPreSelected = x.IsPreSelected,
                            DisplayOrder = x.DisplayOrder
                        };

                        if (x.ValueType == ProductVariantAttributeValueType.ProductLinkage &&
                            linkedProducts.TryGetValue(x.LinkedProductId, out var linkedProduct))
                        {
                            m.SeName = await linkedProduct.GetActiveSlugAsync();
                        }

                        return m;
                    })
                    .ToListAsync();

                attributeModel.Values = [.. valueModels
                    .Select(x => (ChoiceItemModel)x)
                    .OrderBy(x => x.DisplayOrder)
                    .ThenNaturalBy(_catalogSettings.SortAttributesNaturally ? x => x.Name : null)];

                if (preselectAttributes)
                {
                    // Get preselected values.
                    foreach (var value in valueModels.Where(x => x.IsPreSelected))
                    {
                        query.AddVariant(new()
                        {
                            Value = value.Id.ToString(),
                            ProductId = product.Id,
                            AttributeId = attribute.ProductAttributeId,
                            VariantAttributeId = attribute.Id,
                            Alias = attribute.ProductAttribute.Alias,
                            ValueAlias = value.Alias
                        });
                    }
                }
            }

            model.ProductVariantAttributes.Add(attributeModel);
        }

        model.Name = product.GetLocalized(x => x.Name);
        model.ProductType = product.ProductType;
        model.ShowUpdateTotals = order.OrderStatusId <= (int)OrderStatus.Pending;
        model.GiftCard.IsGiftCard = product.IsGiftCard;
        model.GiftCard.GiftCardType = product.GiftCardType;
        model.Quantity = from.Quantity;
        model.UpdateTotals = from.ShowUpdateTotals;
        model.AdjustInventory = from.AdjustInventory;

        // Price calculation.
        var (selection, _) = await _productAttributeMaterializer.CreateAttributeSelectionAsync(query, attributes, product.Id, 0);
        selection.AddGiftCardInfo(giftCardInfo);

        var selectedCombination = await _productAttributeMaterializer.FindAttributeCombinationAsync(product.Id, selection);
        product.MergeWithCombination(selectedCombination);

        var calculationOptions = _priceCalculationService.CreateDefaultOptions(false, _workContext.CurrentCustomer, currency);
        calculationOptions.IgnoreDiscounts = true;

        var calculationContext = new PriceCalculationContext(product, from.Quantity, calculationOptions);

        CalculatedPrice unitPrice, subtotal;
        if (from.Quantity > 1)
        {
            (unitPrice, subtotal) = await _priceCalculationService.CalculateSubtotalAsync(calculationContext);
        }
        else
        {
            unitPrice = subtotal = await _priceCalculationService.CalculatePriceAsync(calculationContext);
        }

        var taxUnit = unitPrice.Tax.Value;
        var taxSubtotal = subtotal.Tax.Value;

        model.UnitPriceInclTax = taxUnit.PriceGross;
        model.UnitPriceExclTax = taxUnit.PriceNet;
        model.PriceInclTax = taxSubtotal.PriceGross;
        model.PriceExclTax = taxSubtotal.PriceNet;
        model.TaxRate = taxUnit.Rate.Rate;

        data.Order = order;
        data.Product = product;
        data.Selection = selection;
        data.GiftCardInfo = giftCardInfo;
    }
}
