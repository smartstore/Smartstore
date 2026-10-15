using Smartstore.Collections;
using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Web.Models.Cart;

internal sealed class CartItemSorter
{
    private readonly OrganizedShoppingCartItem[] _source;
    private readonly Dictionary<OrganizedShoppingCartItem, OrganizedShoppingCartItem> _requiredProductParents;

    public CartItemSorter(IEnumerable<OrganizedShoppingCartItem> items)
    {
        _source = Guard.NotNull(items).ToArray();
        _requiredProductParents = new(ReferenceEqualityComparer.Instance);
    }

    public IReadOnlyDictionary<OrganizedShoppingCartItem, OrganizedShoppingCartItem> RequiredProductParents
        => _requiredProductParents;

    public IReadOnlyList<OrganizedShoppingCartItem> Sort()
    {
        var comparer = ReferenceEqualityComparer.Instance;
        var requiredProductsByParent = new Multimap<OrganizedShoppingCartItem, OrganizedShoppingCartItem>(comparer);
        _requiredProductParents.Clear();

        // INFO: There is no persisted association between a cart item and its required product yet (see issue #1529).
        // Assign each matching cart item to the first eligible parent to keep the display order deterministic.
        foreach (var parent in _source.Where(x => x.Item.Product.RequireOtherProducts && x.Item.Product.AutomaticallyAddRequiredProducts))
        {
            foreach (var productId in parent.Item.Product.RequiredProductIdList.Distinct())
            {
                var requiredProduct = _source.FirstOrDefault(x => !ReferenceEquals(x, parent)
                    && !_requiredProductParents.ContainsKey(x)
                    && x.Item.ProductId == productId);

                if (requiredProduct != null)
                {
                    _requiredProductParents[requiredProduct] = parent;
                    requiredProductsByParent.Add(parent, requiredProduct);
                }
            }
        }

        var orderedItems = new List<OrganizedShoppingCartItem>(_source.Length);
        var addedItems = new HashSet<OrganizedShoppingCartItem>(comparer);

        void AddItem(OrganizedShoppingCartItem item)
        {
            if (!addedItems.Add(item))
            {
                return;
            }

            orderedItems.Add(item);

            if (requiredProductsByParent.TryGetValues(item, out var requiredProducts))
            {
                requiredProducts.Each(AddItem);
            }
        }

        foreach (var item in _source.Where(x => !_requiredProductParents.ContainsKey(x)))
        {
            AddItem(item);
        }

        // Add remaining items in their original order. This also provides a safe fallback for circular product configurations.
        _source.Each(AddItem);

        return orderedItems;
    }
}
