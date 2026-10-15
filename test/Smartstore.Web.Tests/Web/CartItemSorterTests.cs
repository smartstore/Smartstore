using NUnit.Framework;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Web.Models.Cart;

namespace Smartstore.Web.Tests.Web;

[TestFixture]
public class CartItemSorterTests
{
    [Test]
    public void Can_place_required_product_after_parent()
    {
        var deposit = CreateItem(1);
        var unrelated = CreateItem(2);
        var lemonade = CreateItem(3, true, 1);

        var sorter = new CartItemSorter([deposit, unrelated, lemonade]);
        var result = sorter.Sort();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo([unrelated, lemonade, deposit]));
            Assert.That(sorter.RequiredProductParents[deposit], Is.SameAs(lemonade));
        }
    }

    [Test]
    public void Can_order_required_products_by_sequence()
    {
        var firstRequiredProduct = CreateItem(1);
        var secondRequiredProduct = CreateItem(2);
        var parent = CreateItem(3, true, 2, 1);

        var result = new CartItemSorter([firstRequiredProduct, secondRequiredProduct, parent]).Sort();

        Assert.That(result, Is.EqualTo([parent, secondRequiredProduct, firstRequiredProduct]));
    }

    [Test]
    public void Can_assign_shared_required_product_to_first_parent()
    {
        var requiredProduct = CreateItem(1);
        var firstParent = CreateItem(2, true, 1);
        var secondParent = CreateItem(3, true, 1);

        var sorter = new CartItemSorter([requiredProduct, firstParent, secondParent]);
        var result = sorter.Sort();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo([firstParent, requiredProduct, secondParent]));
            Assert.That(sorter.RequiredProductParents[requiredProduct], Is.SameAs(firstParent));
        }
    }

    [Test]
    public void Can_preserve_order_for_non_automatic_required_product()
    {
        var requiredProduct = CreateItem(1);
        var parent = CreateItem(2, false, 1);

        var sorter = new CartItemSorter([requiredProduct, parent]);
        var result = sorter.Sort();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo([requiredProduct, parent]));
            Assert.That(sorter.RequiredProductParents, Is.Empty);
        }
    }

    [Test]
    public void Can_keep_nested_required_products_together()
    {
        var nestedRequiredProduct = CreateItem(1);
        var requiredProduct = CreateItem(2, true, 1);
        var parent = CreateItem(3, true, 2);

        var result = new CartItemSorter([nestedRequiredProduct, requiredProduct, parent]).Sort();

        Assert.That(result, Is.EqualTo([parent, requiredProduct, nestedRequiredProduct]));
    }

    private static OrganizedShoppingCartItem CreateItem(
        int productId, 
        bool automaticallyAddRequiredProducts = false, 
        params int[] requiredProductIds)
    {
        var product = new Product
        {
            Id = productId,
            RequireOtherProducts = requiredProductIds.Length > 0,
            AutomaticallyAddRequiredProducts = automaticallyAddRequiredProducts,
            RequiredProductIdList = requiredProductIds
        };

        return new(new ShoppingCartItem
        {
            Id = productId,
            ProductId = productId,
            Product = product,
            RawAttributes = string.Empty
        });
    }
}
