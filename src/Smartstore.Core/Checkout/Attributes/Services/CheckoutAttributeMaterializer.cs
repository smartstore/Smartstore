using System.Linq.Dynamic.Core;
using System.Text;
using Smartstore.Caching;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Rules;
using Smartstore.Core.Data;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Checkout.Attributes;

public partial class CheckoutAttributeMaterializer : ICheckoutAttributeMaterializer
{
    // {0} = StoreId, {1} = MatchRules, {2} = ShippingRequired
    internal readonly static CompositeFormat CheckoutAttributesKey = CompositeFormat.Parse("checkoutattributes:all-{0}-{1}-{2}");

    private readonly SmartDbContext _db;
    private readonly Lazy<ICartRuleProvider> _cartRuleProvider;
    private readonly IRequestCache _requestCache;
    private readonly IStoreContext _storeContext;

    public CheckoutAttributeMaterializer(
        SmartDbContext db,
        Lazy<ICartRuleProvider> cartRuleProvider,
        IRequestCache requestCache,
        IStoreContext storeContext)
    {
        _db = db;
        _cartRuleProvider = cartRuleProvider;
        _requestCache = requestCache;
        _storeContext = storeContext;
    }

    public async Task<MaterializedCheckoutAttributes> MaterializeCheckoutAttributesAsync(ShoppingCart cart, bool matchRules = true)
    {
        Guard.NotNull(cart);

        var selection = cart.Customer.GenericAttributes.CheckoutAttributes;
        var attributes = await GetCheckoutAttributesAsync(cart, cart.StoreId, matchRules);
        var selectedValues = await GetValuesAsync(selection, attributes.Select(x => x.Id).ToArray());

        return new(cart.Customer, selection, attributes, selectedValues);
    }

    public async Task<List<CheckoutAttribute>> GetCheckoutAttributesAsync(ShoppingCart cart, int storeId = 0, bool matchRules = true)
    {
        Guard.NotNull(cart);

        var cacheKey = CheckoutAttributesKey.FormatInvariant(storeId, matchRules, cart.IsShippingRequired);
        var checkoutAttributes = await _requestCache.GetAsync(cacheKey, async () =>
        {
            // INFO: Always load tracked to prevent stale RuleSets after mapping changes.
            // Tracked queries bypass the second-level entity cache, hence the request cache above.
            var query = _db.CheckoutAttributes.AsQueryable();

            if (matchRules)
            {
                query = query.Include(x => x.RuleSets);
            }

            var attributes = await query
                .ApplyStandardFilter(false, storeId)
                .ToListAsync();

            if (!cart.IsShippingRequired)
            {
                // Remove attributes which require shippable products.
                attributes = attributes
                    .Where(x => !x.ShippableProductRequired)
                    .ToList();
            }

            return attributes;
        });

        if (matchRules)
        {
            void contextAction(CartRuleContext context)
            {
                context.Customer = cart.Customer;
                context.ShoppingCart = cart;

                if (storeId > 0 && storeId != context.Store.Id)
                {
                    context.Store = _storeContext.GetStoreById(storeId);
                }
            }

            return await _cartRuleProvider.Value.FilterByCartRulesAsync(checkoutAttributes, contextAction);
        }

        return checkoutAttributes;
    }

    public async Task<CheckoutAttributeSelection> CreateCheckoutAttributeSelectionAsync(ProductVariantQuery query, ShoppingCart cart)
    {
        Guard.NotNull(query);
        Guard.NotNull(cart);

        var selection = new CheckoutAttributeSelection(string.Empty);

        if (query.CheckoutAttributes.Count == 0)
        {
            return selection;
        }

        var checkoutAttributes = await GetCheckoutAttributesAsync(cart, cart.StoreId);

        foreach (var attribute in checkoutAttributes)
        {
            var selectedItems = query.CheckoutAttributes.Where(x => x.AttributeId == attribute.Id);

            switch (attribute.AttributeControlType)
            {
                case AttributeControlType.DropdownList:
                case AttributeControlType.RadioList:
                case AttributeControlType.Boxes:
                {
                    var selectedValue = selectedItems.FirstOrDefault()?.Value;
                    if (selectedValue.HasValue())
                    {
                        var selectedAttributeValueId = selectedValue.SplitSafe(',').FirstOrDefault()?.ToInt();
                        if (selectedAttributeValueId.GetValueOrDefault() > 0)
                        {
                            selection.AddAttributeValue(attribute.Id, selectedAttributeValueId.Value);
                        }
                    }
                }
                break;

                case AttributeControlType.Checkboxes:
                {
                    foreach (var item in selectedItems)
                    {
                        var selectedValue = item.Value.SplitSafe(',').FirstOrDefault()?.ToInt();
                        if (selectedValue.GetValueOrDefault() > 0)
                        {
                            selection.AddAttributeValue(attribute.Id, selectedValue);
                        }
                    }
                }
                break;

                case AttributeControlType.TextBox:
                case AttributeControlType.MultilineTextbox:
                {
                    var selectedValue = string.Join(',', selectedItems.Select(x => x.Value));
                    if (selectedValue.HasValue())
                    {
                        selection.AddAttributeValue(attribute.Id, selectedValue);
                    }
                }
                break;

                case AttributeControlType.Datepicker:
                {
                    var selectedValue = selectedItems.FirstOrDefault()?.Date;
                    if (selectedValue.HasValue)
                    {
                        selection.AddAttributeValue(attribute.Id, selectedValue.Value.ToStringInvariant());
                    }
                }
                break;

                case AttributeControlType.FileUpload:
                {
                    var selectedValue = string.Join(',', selectedItems.Select(x => x.Value));
                    if (selectedValue.HasValue())
                    {
                        selection.AddAttributeValue(attribute.Id, selectedValue);
                    }
                }
                break;
            }
        }

        return selection;
    }

    protected virtual async Task<List<CheckoutAttributeValue>> GetValuesAsync(CheckoutAttributeSelection selection, int[] attributeIds)
    {
        if (attributeIds.Length == 0)
        {
            return [];
        }

        // AttributesMap can also contain numeric values of text fields that are not CheckoutAttributeValue IDs!
        var numericValues = selection.AttributesMap
            .SelectMany(x => x.Value)
            .Select(x => x.ToString())
            .Where(x => x.HasValue())
            .Select(x => x.ToInt())
            .Where(x => x != 0)
            .Distinct()
            .ToArray();

        if (numericValues.Length == 0)
        {
            return [];
        }

        var values = await _db.CheckoutAttributeValues
            .AsNoTracking()
            .Include(x => x.CheckoutAttribute)
            .Where(x => attributeIds.Contains(x.CheckoutAttributeId) && numericValues.Contains(x.Id) && x.CheckoutAttribute.IsActive)
            .ApplyListTypeFilter()
            .ToListAsync();

        return values;
    }
}