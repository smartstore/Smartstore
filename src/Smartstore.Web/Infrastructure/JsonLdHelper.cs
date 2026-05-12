using Smartstore.Web.Models.Catalog;

// TODO: (jsonld) Find a better place/method for this. I havn't thought about architecture a sigle second yet. 
// I just needed a central place to put this helper method for now. 

namespace Smartstore.Json;

/// <summary>
/// Provides helper methods for building schema.org JSON-LD fragments.
/// </summary>
public static class JsonLdHelper
{
    /// <summary>
    /// Builds a JSON-LD <c>ItemList</c> fragment for a collection of product items.
    /// </summary>
    /// <param name="builder">The <see cref="JsonLdBuilder"/> to populate.</param>
    /// <param name="items">The ordered sequence of items to include in the list.</param>
    /// <param name="url">The canonical URL for the list.</param>
    /// <param name="listName">The name of the list.</param>
    public static void BuildProductItemList(
        JsonLdBuilder builder,
        List<ProductSummaryItemModel> items,
        string listName,
        string listId,
        string url)
    {
        Guard.NotNull(builder);
        Guard.NotNull(items);
        Guard.NotEmpty(url);
        Guard.NotEmpty(listName);

        var position = 1;

        var uri = new Uri(url);

        builder.ItemList
            .Prop("@id", listId)
            .Prop("name", listName)
            .Arr("itemListElement", items.Select(item => JsonLdFragment.Create("ListItem")
                .Prop("position", position++)
                .Obj("item", JsonLdFragment.Create("Product")
                    .Prop("name", item.Name)
                    .Prop("description", item.ShortDescription)
                    .Prop("url", new Uri(uri, item.DetailUrl))
                    .Prop("sku", item.Sku)
                    .Prop("image", new Uri(uri, item.Image?.Url))
                    .Obj("brand", JsonLdFragment.Create("Brand")
                        .Prop("name", item.Brand?.Name))
                    .Obj("offers", JsonLdFragment.Create("Offer")
                        .Prop("priceCurrency", item.Price?.FinalPrice.Currency.CurrencyCode)
                        .Prop("price", item.Price?.FinalPrice.Amount.ToStringInvariant())
            ))));
    }
}