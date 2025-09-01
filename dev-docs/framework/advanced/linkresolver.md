# LinkResolver

Smartstore stores navigation targets as compact link expressions like `product:123`, `topic:about-us|_blank` or `https://example.com`. `ILinkResolver` translates these expressions to concrete URLs and labels, ensuring store and ACL security and reusing results from a memory cache.

## Expression format

A link expression consists of `schema:target|linkTarget?query`:

| Part | Description |
| --- | --- |
| `schema` | Identifier selecting a provider (`product`, `category`, `topic`, etc.). If omitted, the string is treated as a raw URL. |
| `target` | Entity id, system name or URL path. |
| `linkTarget` | Optional window target such as `_blank`. |
| `query` | Optional query string appended to the generated link. |

Built‑in schemas are:

| Schema | Resolves to |
| --- | --- |
| `product` | Product detail page |
| `category` | Category listing |
| `manufacturer` | Manufacturer page |
| `topic` | Topic by id or system name |
| `url` | Absolute or application-relative URL (`~/path`) |
| `file` | Static file path |

Providers implement `ILinkProvider` and can add further schemas.

## Resolving links

Inject `ILinkResolver` and call `ResolveAsync` with an expression:

```csharp
public class LinkDemo
{
    private readonly ILinkResolver _links;

    public LinkDemo(ILinkResolver links) => _links = links;

    public async Task<string> GetProductLinkAsync(int id)
    {
        var result = await _links.ResolveAsync($"product:{id}");
        return result.Status == LinkStatus.Ok ? result.Link : string.Empty;
    }
}
```

`ResolveAsync` returns a `LinkResolutionResult` containing the final URL, a localized label and the entity identifiers when available. Access control is evaluated by store mapping and ACL before a link is considered valid.

## Custom providers

Custom schemas are added by implementing `ILinkProvider` and registering it via DI:

```csharp
public class BlogPostLinkProvider : ILinkProvider
{
    public int Order => 100;
    public IEnumerable<LinkBuilderMetadata> GetBuilderMetadata() =>
        new[] { new LinkBuilderMetadata { Schema = "blogpost", Icon = "fa fa-blog", ResKey = "Common.Entity.BlogPost" } };

    public Task<LinkTranslationResult> TranslateAsync(LinkExpression expr, int storeId, int languageId)
    {
        // look up blog post and return URL/label
    }
}
```

The metadata controls the link picker UI in the admin panel.

## Cache invalidation

Resolved links are cached under a key that includes schema, store, language and roles. When the underlying entity changes, invalidate the cache with:

```csharp
_linkResolver.InvalidateLink("product", productId);
```

A built-in `DefaultLinkInvalidator` listens to update events for core entities and clears entries automatically.