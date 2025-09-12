# Catalog

### Overview

The catalog domain covers products, categories, brands and the search infrastructure.

### Products and types

Products represent items or services and are stored in `Product`. `ProductType` defines variations like `SimpleProduct`, `GroupedProduct`, or `Bundle`. Products may include media (`ProductMediaFile`), tags (`ProductTag`), and relationships such as `CrossSellProduct` or `RelatedProduct`.

### Categories and brands

Categories form a tree via `Category` and `ProductCategory` and can be loaded with `ICategoryService`. Manufacturers are modeled as `Manufacturer` entities and assigned to products through `ProductManufacturer`.

### Attributes

Product attributes allow customers to configure variants. Use global attribute definitions and `ProductAttributeValue` links to define options. Specification attributes describe filterable characteristics displayed on category pages.

### Accessing catalog data

#### Products

Retrieve products directly via `SmartDbContext.Products` using standard Entity Framework Core queries:

```csharp
var product = await _db.Products
    .AsNoTracking()
    .FirstOrDefaultAsync(x => x.Sku == "SKU-1");
```

`ProductQueryExtensions` adds helper methods to shape the query. Use `ApplyStandardFilter` to exclude unpublished or system products and stack other filters as needed:

```csharp
var products = await _db.Products
    .ApplyStandardFilter()
    .ApplySkuFilter("SKU-1")
    .ToListAsync();
```

Other useful filters include:

* `ApplyGtinFilter` / `ApplyMpnFilter` – search by GTIN or MPN.
* `ApplyProductCodeFilter` – look up a product by SKU, GTIN, or MPN in one call.
* `ApplyAssociatedProductsFilter` – retrieve child products of grouped items.
* `ApplyDescendantsFilter` – traverse a category tree path.
* `ApplySystemNameFilter` – match a product's system name.

#### Categories&#x20;

Categories benefit from `ICategoryService`, which caches the entire tree and provides helper methods.

`GetCategoryTreeAsync` returns a `TreeNode<ICategoryNode>` rooted at a virtual root. Each node exposes its children and `Depth`, making it easy to traverse or render hierarchical menus:

```csharp
var tree = await _categoryService.GetCategoryTreeAsync();

// dump the tree with indentation
foreach (var node in tree.FlattenNodes(false))
{
    Console.WriteLine(new string(' ', node.Depth * 2) + node.Value.Name);
}
```

To build a breadcrumb for a specific category, load the entity and call `GetCategoryPathAsync`. The method returns a string containing all ancestors, separated by " » ":

```csharp
var category = await _db.Categories.FindAsync(categoryId);
var path = await _categoryService.GetCategoryPathAsync(category);
// path => "Root » Subcategory » Current"
```

This combination of tree and path helpers enables quick menu rendering and breadcrumb generation without repeated database queries.

### Searching

`ICatalogSearchService` builds search queries and executes them against either the database or an index provider. Category and manufacturer pages reuse this service to fetch product lists with paging, filtering and sorting applied:

```csharp
var query = new CatalogSearchQuery();
query.DefaultTerm = "shirt";
query.Slice(0, 10); // page size 10
var result = await _catalogSearchService.SearchAsync(query);
```

`CatalogSearchQuery` inherits from `SearchQuery<T>` and exposes a wide range of properties to shape the request:

* `DefaultTerm` – search phrase.
* `ParseSearchTerm` – parse the term for advanced filter syntax.
* `LanguageId` / `LanguageCulture` / `CurrencyCode` – context for localized fields and prices.
* `StoreId` – restrict results to a specific store.
* `Filters` – collection of additional `SearchFilter` instances.
* `FacetDescriptors` and `HasSelectedFacets` – describe and inspect facet selections.
* `Skip` / `Take` and the computed `PageIndex` – control paging.
* `Sorting` – sort expressions such as price or creation date.
* `IsFuzzySearch` – enable fuzzy matching when supported.
* `SpellCheckerMaxSuggestions`, `SpellCheckerMinQueryLength`, `SpellCheckerMaxHitCount` – configure suggestion generation.
* `ResultFlags` – request hits, facets and/or suggestions.
* `Origin` – identifies the source route (e.g. category page).
* `CustomData` – extension bag for additional values.
* `IsSubPage` – `true` when paging or any facet selection is active.

