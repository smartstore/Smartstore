using System.Linq.Dynamic.Core;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Data;

namespace Smartstore.Core.Checkout.Attributes
{
    public partial class CheckoutAttributeMaterializer : ICheckoutAttributeMaterializer
    {
        private readonly SmartDbContext _db;

        public CheckoutAttributeMaterializer(SmartDbContext db)
        {
            _db = db;
        }

        public async Task<List<CheckoutAttribute>> MaterializeCheckoutAttributesAsync(CheckoutAttributeSelection selection)
        {
            Guard.NotNull(selection, nameof(selection));

            var ids = selection.AttributesMap.Select(x => x.Key).ToArray();

            if (!ids.Any())
            {
                return new List<CheckoutAttribute>();
            }

            return await _db.CheckoutAttributes
                .Include(x => x.CheckoutAttributeValues)
                .Where(x => ids.Contains(x.Id))
                .ToListAsync();
        }

        public async Task<List<CheckoutAttributeValue>> MaterializeCheckoutAttributeValuesAsync(CheckoutAttributeSelection selection)
        {
            Guard.NotNull(selection, nameof(selection));

            var attributeIds = selection.AttributesMap.Select(x => x.Key).ToArray();
            if (!attributeIds.Any())
            {
                return new List<CheckoutAttributeValue>();
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

            if (!numericValues.Any())
            {
                return new List<CheckoutAttributeValue>();
            }

            var values = await _db.CheckoutAttributeValues
                .AsNoTracking()
                .Include(x => x.CheckoutAttribute)
                .Where(x => attributeIds.Contains(x.CheckoutAttributeId) && numericValues.Contains(x.Id))
                .ApplyListTypeFilter()
                .ToListAsync();

            return values;
        }

        public async Task<List<CheckoutAttribute>> GetCheckoutAttributesAsync(ShoppingCart cart, int storeId = 0)
        {
            Guard.NotNull(cart, nameof(cart));

            var checkoutAttributes = await _db.CheckoutAttributes
                .AsNoTracking()
                .ApplyStandardFilter(false, storeId)
                .ToListAsync();

            if (!cart.IncludesMatchingItems(x => x.IsShippingEnabled))
            {
                // Remove attributes which require shippable products.
                checkoutAttributes = checkoutAttributes
                    .Where(x => !x.ShippableProductRequired)
                    .ToList();
            }

            return checkoutAttributes;
        }

        public async Task<CheckoutAttributeSelection> CreateCheckoutAttributeSelectionAsync(ProductVariantQuery query, ShoppingCart cart)
        {
            Guard.NotNull(query, nameof(query));
            Guard.NotNull(cart, nameof(cart));

            var selection = new CheckoutAttributeSelection(string.Empty);

            if (!query.CheckoutAttributes.Any())
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
                        var selectedValue = string.Join(",", selectedItems.Select(x => x.Value));
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
                            selection.AddAttributeValue(attribute.Id, selectedValue.Value);
                        }
                    }
                    break;

                    case AttributeControlType.FileUpload:
                    {
                        var selectedValue = string.Join(",", selectedItems.Select(x => x.Value));
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
    }
}