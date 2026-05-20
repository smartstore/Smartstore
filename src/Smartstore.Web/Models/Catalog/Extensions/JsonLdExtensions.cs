#nullable enable

using Microsoft.AspNetCore.Http;
using Smartstore.Json;

namespace Smartstore.Web.Models.Catalog;

public static class JsonLdExtensions
{
    /// <summary>
    /// Builds a JSON-LD <c>CollectionPage</c> root fragment with a nested <c>ItemList</c>
    /// as <c>mainEntity</c> for a collection of product items.
    /// Only position and canonical URL are emitted per item — full <c>Product</c> markup
    /// belongs on the product detail page.
    /// </summary>
    /// <param name="builder">The <see cref="JsonLdBuilder"/> to populate.</param>
    /// <param name="items">The ordered sequence of items to include in the list.</param>
    /// <param name="listName">The display name of the collection (e.g. category name).</param>
    /// <param name="url">The absolute canonical URL of the collection page.</param>
    public static JsonLdFragment BuildProductItemList(
        this JsonLdBuilder builder,
        HttpRequest httpRequest,
        IEnumerable<ProductSummaryItemModel> items,
        string listName,
        string? url)
    {
        Guard.NotNull(builder);
        Guard.NotNull(httpRequest);
        Guard.NotNull(items);
        Guard.NotEmpty(listName);

        var position = 1;

        var itemList = JsonLdFragment.Create("ItemList")
            .Prop("name", listName)
            .Arr("itemListElement", items.Select(item => JsonLdFragment.Create("ListItem")
                .Prop("position", position++)
                .Prop("url", httpRequest.ToAbsoluteUrl(item.DetailUrl))));

        return builder.CollectionPage
            .Prop("name", listName)
            .Prop("url", httpRequest.ToAbsoluteUrl(url))
            .Obj("mainEntity", itemList);
    }
}
