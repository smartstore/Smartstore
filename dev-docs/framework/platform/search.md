# ✔️ Search

## Overview

[ICatalogSearchService](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Catalog/Search/ICatalogSearchService.cs) provides a product search based on search terms. The interface has two implementations:

<table><thead><tr><th width="289">Implementation</th><th>Description</th></tr></thead><tbody><tr><td><code>CatalogSearchService</code></td><td><p>Searches for products by implementations of <a href="https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Search/Indexing/IIndexProvider.cs">IIndexProvider</a>, <a href="https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Search/Indexing/IIndexStore.cs">IIndexStore</a> and <a href="https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Search/ISearchEngine.cs">ISearchEngine</a>.</p><p>An example of this is the Smartstore <em>MegaSearch</em> module, which connects the search library Lucene.Net to Smartstore.</p></td></tr><tr><td><code>LinqCatalogSearchService</code></td><td>Searches the database directly for hits using LINQ.</td></tr></tbody></table>

The `CatalogSearchingEvent` is published right before a catalog search regardless of the implementation. `CatalogSearchedEvent` is published at the end of a search.

## Search query

`CatalogSearchQueryFactory` reads query string parameters and creates a `CatalogSearchQuery` from it, which contains all the information needed for the search engine to perform the search. Parameters are typically used to filter the search result. Possible parameters:

<table><thead><tr><th width="147" align="right">Query token</th><th width="100">Type</th><th>Description</th></tr></thead><tbody><tr><td align="right"><strong>q</strong></td><td>string</td><td>Specifies the search <strong>query</strong>/term.</td></tr><tr><td align="right"><strong>i</strong></td><td>int</td><td>Specifies the <strong>page index</strong> (starting from 1).</td></tr><tr><td align="right"><strong>s</strong></td><td>int</td><td>Specifies the <strong>page size</strong> of search hits.</td></tr><tr><td align="right"><strong>o</strong></td><td>int</td><td>Specifies how to <strong>order</strong> the search hits. For supported values see <a href="https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Catalog/Products/Domain/ProductEnums.cs">ProductSortingEnum</a>.</td></tr><tr><td align="right"><strong>p</strong></td><td>decimal</td><td>Filters products by a <strong>price range</strong>. Supported formats: <em>from~to</em>, <em>from(~)</em> or <em>~to</em>.</td></tr><tr><td align="right"><strong>c</strong></td><td>int</td><td>Filters products by assigned <strong>categories</strong>. Supports a comma separated list of category identifiers.</td></tr><tr><td align="right"><strong>m</strong></td><td>int</td><td>Filters products by assigned <strong>manufacturers</strong>. Supports a comma separated list of manufacturer identifiers.</td></tr><tr><td align="right"><strong>r</strong></td><td>double</td><td>Filters products by their minimum <strong>rating</strong>. Supports values from 0 to 5.</td></tr><tr><td align="right"><strong>a</strong></td><td>bool</td><td>Filters products by their <strong>stock level</strong>.</td></tr><tr><td align="right"><strong>n</strong></td><td>bool</td><td>Filters for <strong>newly arrived products</strong>.</td></tr><tr><td align="right"><strong>d</strong></td><td>int</td><td>Filters products by assigned <strong>delivery times</strong>. Supports a comma separated list of delivery time identifiers.</td></tr><tr><td align="right"><strong>v</strong></td><td>string</td><td>Specifies the <strong>view mode</strong> for search hits. Supported value are <em>grid</em> or <em>list</em>.</td></tr></tbody></table>

More parameters are available for filtering products by variants and attributes, if the _MegaSearchPlus_ module is installed. They are prefixed by _attr_ (product attribute), _vari_ (product variant) and _opt_ (option value of an attribute or variant).

{% hint style="info" %}
Parameter tokens can be overwritten by the user in the backend via alias fields (see [ISearchAlias](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Search/ISearchAlias.cs)). For example if the manufacturer's name should appear in the URL instead of the token _m_.
{% endhint %}

`CatalogSearchQuery` also contains the search term, the field(s) to be searched, the search mode and many other settings.

{% hint style="info" %}
These settings can have a huge impact on the performance of a search. For instance:

* `SearchMode.Contains` is significantly slower than `SearchMode.ExactMatch`.
* If you do not need facets to be returned, turn it off by calling `BuildFacetMap(false)`.
* If you do not need spell check, turn it off by calling `CheckSpelling(0)`.
{% endhint %}

The `ICatalogSearchService.PrepareQuery` method lets you build or modify your own catalog search query using LINQ.

`CatalogSearchQueryFactory` has a virtual method `OnConvertedAsync` which can be used to display more facet groups in the frontend:

```csharp
public class MyCatalogSearchQueryFactory : CatalogSearchQueryFactory
{
    public MyCatalogSearchQueryFactory(
        IHttpContextAccessor httpContextAccessor,
        ICommonServices services,
        ICatalogSearchQueryAliasMapper catalogSearchQueryAliasMapper,
        CatalogSettings catalogSettings,
        SearchSettings searchSettings) : base(
            httpContextAccessor,
            services,
            catalogSearchQueryAliasMapper,
            catalogSettings,
            searchSettings)
    {
    }

    protected override Task OnConvertedAsync(CatalogSearchQuery query, string origin)
    {
        if (!query.IsInstantSearch())
        {
            var descriptor = new FacetDescriptor("mycustomid")
            {
                IsMultiSelect = true,
                DisplayOrder = 101,
                OrderBy = FacetSorting.DisplayOrder,
                MinHitCount = _searchSettings.FilterMinHitCount,
                MaxChoicesCount = _searchSettings.FilterMaxChoicesCount
            };

            // Get your facet values (like entity IDs) from query string
            // and call query.WithFilter to apply them.
            // Do not forget to add selected values to the descriptor:
            // descriptor.AddValue(new FacetValue(valueId, IndexTypeCode.Int32)
            // {
            //   IsSelected = true
            // });

            query.WithFacet(descriptor);
        }

        return Task.CompletedTask;
    }
}
```

## Search result

`ICatalogSearchService.SearchAsync` returns `CatalogSearchResult` containing all requested results of a search, including:

* Facets map
* Spell checker suggestions
* IDs of found products

The entities of the found products are loaded later when `CatalogSearchResult.GetHitsAsync` is called. For this purpose, `CatalogSearchQuery` contains a method `UseHitsFactory`, which can be used to replace the default factory if required.

The `SearchResultModel` is used to present the result of a search in the frontend. The `HitGroups` property is of particular importance here. It is used to display additional groups of search hits in the instant search. Depending on installed modules, these may include:

* Links to the manufacturers and categories of the products
* Spell checker suggestions
* Common search terms

You can inject more groups of links here using an `IAsyncResultFilter`:

```csharp
internal class Startup : StarterBase
{
    public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
    {
        services.Configure<MvcOptions>(o =>
        {
            o.Filters.AddConditional<TopLinksFilter>(
                context => context.ControllerIs<SearchController>() && !context.HttpContext.Request.IsAjax(), 200);
        });
    }
}

public class TopLinksFilter : IAsyncResultFilter
{
    private readonly MySearchSettings _settings;

    public TopLinksFilter(MySearchSettings settings)
    {
        _settings = settings;
    }

    public async Task OnResultExecutionAsync(ResultExecutingContext filterContext, ResultExecutionDelegate next)
    {
        if (!_settings.ShowTopLinks
            || _settings.MaxTopLinks <= 0
            || filterContext.Result is not ViewResult viewResult
            || viewResult.Model is not SearchResultModel model)
        {
            await next();
            return;
        }

        var myLinks = await GetMyTopLinks();
        if (myLinks.Count > 0)
        {
            var hitGroup = new SearchResultModelBase.HitGroup(model)
            {
                Name = "MyTopLinks",
                DisplayName = "My top links"
            };

            hitGroup.Hits.AddRange(myLinks.Select(x => new SearchResultModelBase.HitItem
            {
                Label = x.Label,
                Url = x.Url
            }));

            model.HitGroups.Add(hitGroup);
        }

        await next();
    }
    
    private Task<List<MyTopLink>> GetMyTopLinks()
    {
        // TODO: get my top links from somewhere.
        return Task.FromResult(new List<MyTopLink>());
    }    
}

internal class MyTopLink
{
    public string Label { get; set; }
    public string Url { get; set; }
}
```

## Filter

Filters are used to limit search results, e.g. to only display products of a certain category. They are determined by the `CatalogSearchQueryFactory` using the query string and passed on to the search using the `CatalogSearchQuery`. If you want to search programmatically, you can create a `CatalogSearchQuery` instance and define it yourself using fluent notation:

```csharp
var searchQuery = new CatalogSearchQuery()
    .VisibleOnly()
    .WithVisibility(ProductVisibility.Full)
    .HasStoreId(Services.StoreContext.CurrentStoreIdIfMultiStoreMode)
    .WithCategoryIds(null, categoryIds.ToArray())
    .BuildFacetMap(false)
    .BuildHits(false);
    
var searchResult = await _catalogSearchService.SearchAsync(searchQuery);
```

There are several types of filters, which all inherit from `ISearchFilter`:

<table><thead><tr><th width="293">Filter</th><th>Description</th></tr></thead><tbody><tr><td><code>IAttributeSearchFilter</code></td><td>Base type to filter a field by a value.</td></tr><tr><td><code>ICombinedSearchFilter</code></td><td>Filter by a list of IDs, e.g. category IDs (combined using logic OR).</td></tr><tr><td><code>IRangeSearchFilter</code></td><td>Filter by lower and\or upper value, e.g. a price range.</td></tr></tbody></table>

A LINQ search like `LinqCatalogSearchService` translates the filters in `CatalogSearchQuery` into an `IQueryable<Product>` that can be used to load product entities directly from the database. The `ISearchEngine` implementation of _MegaSearch_ translates them into a query filter that is compatible with Lucene.Net.

## Facets

Facets are used to refine search results, allowing users to narrow a large set of products down to only those that match specific criteria. This includes reducing the number of filters to those that can further refine the search results. This process is often called **drilldown navigation**. A search library like Lucene.Net is required to obtain facets.

Facets are obtained using `ISearchEngine.GetFacetMapAsync` and `ISearchProvider.GetFacetMap`, but in practice several more steps are needed to make faceting work. [IFacetMetadataStorage](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Search/Facets/IFacetMetadataStorage.cs) is used to load facet metadata from cache, the search index or both. For drilldown navigation by brand for instance, you need all brand / manufacturer names. For performance reasons you shouldn’t load them from the database, but store them in the search index and retrieve them using `IFacetMetadataStorage`.

The first step is to iterate through `ISearchQuery.FacetDescriptors` to get the actual requested facets. Next, for a particular descriptor, its metadata is loaded including all values to be faceted (e.g. brand names). Usually, all values are applied iteratively to the bitset of the current search result using a bitwise AND-operation to get the number of set bits (which is the number of search hits after applying a certain filter). This process depends on the search library.

{% hint style="info" %}
Faceting can take a while despite all the performance optimisation. Depending on the amount of data, a lot of bit operations have to be performed.

If no facets are needed for a search, `SearchQuery.BuildFacetMap(false)` should be called so that none are obtained.
{% endhint %}

### Facet presentation

The presentation of facets in the frontend can be changed via [IFacetTemplateSelector](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Search/Facets/IFacetTemplateSelector.cs). The interface requests a template widget for a `FacetGroup`.

In the following example, a view component is used to create a custom representation for the template types `FacetTemplateHint.NumericRange` and `FacetTemplateHint.Custom` (e.g. color or picture boxes).

```csharp
public class MyCustomFacetGroupViewComponent : SmartViewComponent
{
    public IViewComponentResult Invoke(FacetGroup facetGroup, string templateName)
    {
        Guard.NotNull(facetGroup);
        Guard.NotEmpty(templateName);

        // TODO: add views Box.cshtml and NumericRange.cshtml with custom rendering.
        return View(templateName, facetGroup);
    }
}

public class MyFacetTemplateSelector : IFacetTemplateSelector
{
    public int Ordinal => -200;

    public Widget GetTemplateWidget(FacetGroup facetGroup)
    {
        if (facetGroup.Kind == FacetGroupKind.Attribute || facetGroup.Kind == FacetGroupKind.Variant)
        {
            string templateName;
            switch (facetGroup.TemplateHint)
            {
                case FacetTemplateHint.Custom:
                case FacetTemplateHint.NumericRange:
                    templateName = facetGroup.TemplateHint == FacetTemplateHint.Custom ? "Box" : "NumericRange";

                    return new ComponentWidget("MyCustomFacetGroup", Module.SystemName, new { facetGroup, templateName })
                    {
                        Order = facetGroup.DisplayOrder
                    };

                default:
                    templateName = facetGroup.IsMultiSelect ? "MultiSelect" : "SingleSelect";

                    return new ComponentWidget("FacetGroup", new { facetGroup, templateName })
                    {
                        Order = facetGroup.DisplayOrder
                    };
            }
        }

        return null;
    }
}
```

## Indexing

Search libraries like Lucene.Net determine hits through the file system instead of accessing databases directly. These files are called indexes or search indexes. Smartstore provides a whole range of interfaces for creating and managing search indexes and the _MegaSearch_ module which performs the actual indexing. Essentially, the process is as follows.

An [IIndexingService](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Search/Indexing/IIndexingService.cs) implementation creates or updates the search index, always triggered by its own task. The index scope (information about what kind of index it is), is obtained via the [IIndexScopeManager](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Search/Indexing/IIndexScopeManager.cs). The indexing service uses a data collector (see [IIndexCollector](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Search/Indexing/IIndexCollector.cs)) to collect all the data to be transferred to the index. The index service does not access the index directly, but uses the [IIndexStore](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Search/Indexing/IIndexStore.cs) for that. It ensures that the index can be accessed for writing (during indexing) and reading (during searching).

A [hook](hooks.md) is used to determine changes to products that are relevant for updating the index. In this case, [IndexBacklogItems](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Search/Indexing/Backlog/IndexBacklogItem.cs) are stored in the database using [IIndexBacklogService](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Search/Indexing/Backlog/IIndexBacklogService.cs). In case of an index update, [IIndexCollector](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Search/Indexing/IIndexCollector.cs) retrieves these backlogs via [IIndexBacklogService](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Search/Indexing/Backlog/IIndexBacklogService.cs) and updates the corresponding index entries of the products. This procedure speeds up indexing by updating only the part of the index that is relevant to the product changes, rather than rebuilding the entire index.

The [IIndexCollector](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Search/Indexing/IIndexCollector.cs) publishes an [IndexSegmentProcessedEvent](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Search/Indexing/Events/IndexSegmentProcessedEvent.cs) each time it's processed a segment of entities but before the collected data is written to the index. It can be used to decorate the search index with additional data.

Finally an [IndexingCompletedEvent](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Search/Indexing/Events/IndexingCompletedEvent.cs) is published by the indexing service when the indexing is complete.

## Implementing a custom search

[IIndexScope](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Search/Indexing/IIndexScope.cs) and [ISearchProvider](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Search/ISearchProvider.cs) are used to distinguish between different search scopes, like catalog search (built-in search for products) and forum search (search for forum posts offered by the forum module). To implement another search for a different entity, use the basic structure of the catalog search and modify it accordingly. This way, any number of additional search indexes can be built using _MegaSearch_.

{% hint style="info" %}
This is a generalized description of building a custom search index, and is only intended to show that the possibility exists. A complete description would go beyond the scope of this documentation.
{% endhint %}

### Index scope

First implement [IIndexScope](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Search/Indexing/IIndexScope.cs) and the related interfaces [IIndexCollector](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Search/Indexing/IIndexCollector.cs), [ISearchProvider](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Search/ISearchProvider.cs) and [IIndexAnalyzer](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Search/Indexing/IIndexAnalyzer.cs). Typically the scope and collector have named registrations:

```csharp
internal class Startup : StarterBase
{
    const string Scope = "MySearchIndex";
    
    public override void ConfigureContainer(ContainerBuilder builder,
        IApplicationContext appContext)
    {
        builder.RegisterType<MySearchIndexScope>()
            .As<IIndexScope>()
            .Named<IIndexScope>(Scope)
            .WithMetadata<IndexScopeMetadata>(m => m.For(em => em.Name, Scope))
            .InstancePerLifetimeScope();

        builder.RegisterType<MyIndexCollector>()
            .Named<IIndexCollector>(Scope)
            .AsSelf()
            .InstancePerLifetimeScope();
    }
}

public class MySearchIndexScope : IIndexScope
{
    private readonly Lazy<MyIndexCollector> _collector;
    private readonly MySearchSettings _settings;

    public MySearchIndexScope(Lazy<MyIndexCollector> collector,
        MySearchSettings settings)
    {
        _collector = collector;
        _settings = settings;
    }

    public string Scope => "MySearchIndex";

    public Widget GetConfigurationWidget() => null;

    public IndexInfo GetIndexInfo() => new IndexInfo(Scope)
    {
        IndexingTaskType = typeof(MyIndexingTask),
        ScopeKey = "Modules.MyCompany.MyModule.ScopeName",
        DocumentType = "zz", // see SearchDocumentTypes for existing types
        DocumentTypeKey = "Modules.MyCompany.MyModule.DocumentName"
    };

    public virtual IIndexCollector GetCollector()
        => _collector.Value;

    public virtual ISearchProvider GetSearchProvider()
        => new MySearchProvider(_settings);

    public virtual IIndexAnalyzer GetAnalyzer()
        => new MyIndexAnalyzer();
}
```

### Text analyzer

The `IIndexAnalyzer` defines the text analyzer used on your index fields. An analyzer specifies how the content of an index field is processed during indexing and searching. An implementation of the `IIndexAnalyzer` could look like this (excerpt from the forum modules analyzer):

```csharp
public class MyIndexAnalyzer : IIndexAnalyzer
{
    public IndexAnalyzerType? GetDefaultAnalyzerType(
        IndexAnalysisReason reason, 
        IIndexStore indexStore)
    {
        return reason == IndexAnalysisReason.Highlight
            ? IndexAnalyzerType.Whitespace
            : null;
    }

    public IList<IndexAnalyzerInfo> GetAnalyzerInfos(
        IndexAnalysisReason reason, 
        IList<Language> languages, 
        IIndexStore indexStore)
    {
        var result = new List<IndexAnalyzerInfo>();
        var defaultCulture = languages.FirstOrDefault()?.LanguageCulture ?? "en-US";

        if (reason == IndexAnalysisReason.Search || reason == IndexAnalysisReason.CheckSpelling)
        {
            result.Add(new IndexAnalyzerInfo(defaultCulture, null, "subject", "text"));
            result.Add(new IndexAnalyzerInfo(defaultCulture, IndexAnalyzerType.Whitespace, "username"));
        }
        else if (reason == IndexAnalysisReason.Highlight)
        {
            result.Add(new IndexAnalyzerInfo(defaultCulture, IndexAnalyzerType.Whitespace, "subject", "text"));
        }

        return result;
    }
}
```

### Modelling

The next step is implementing search modelling. In the catalog search, these are `CatalogSearchQueryFactory`, `CatalogSearchQueryModelBinder` and `CatalogSearchQueryAliasMapper`. The alias mapper is only needed if the query string can contain alias names that have to be mapped to actual values (such as entity IDs). Then you need a search query object like `CatalogSearchQuery` that inherits from `SearchQuery`.

Don't forget to decorate it with `ValidateNeverAttribute` and `ModelBinderAttribute`. This means action methods can contain parameters of your search query objects type (aka `CatalogSearchQuery`), which are automatically instantiated via model binding. See the `SearchController` in the Smartstore.Web project.

### Facets

When your search supports facets, you need a facet URL helper that inherits from `FacetUrlHelperBase`, like the `CatalogFacetUrlHelper`. This helper lets you modify facet URLs easily. If the link of a facet is clicked and the associated filter is applied, a query string value must be appended to the URLs for this filter. Similarly, a query string value must be removed when the related facet is deactivated. See `CatalogFacetUrlHelper` for details on the implementation.

### Search service

The last part is the search service, the start-up point for your search. In the catalog search, these are `CatalogSearchService`, `LinqCatalogSearchService` and `CatalogSearchResult`. Your implementation of these classes will probably look very similar to them, so there is no need to go into detail here.

{% hint style="info" %}
It's a good idea to add two events to the search, one immediately before and one after execution (see `CatalogSearchingEvent` and `CatalogSearchedEvent`).
{% endhint %}

Typically all these services have scoped registration, so your start-up might look like this if you need them all and want to implement them:

```csharp
internal class Startup : StarterBase
{
    public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
    {
        services.AddScoped<IMyCustomSearchQueryAliasMapper, MyCustomSearchQueryAliasMapper>();
        services.AddScoped<IMyCustomSearchQueryFactory, MyCustomSearchQueryFactory>();
        services.AddScoped<IMyCustomSearchService, MyCustomSearchService>();
        services.AddScoped<MyLinqCustomSearchService>();
        services.AddScoped<IMyCustomUrlHelper, MyCustomFacetUrlHelper>();
    }
}
```

### Search settings

Custom search settings can be integrated into the existing settings of the backend using a new tab. To do this you need a setting class and a model. Please note the `CustomModelPartAttribute` attribute that's required for proper model binding:

```csharp
public class MyCustomSearchSettings : ISettings
{
    //...
}

[CustomModelPart]
public class MyCustomSearchSettingsModel : ModelBase
{
    //...
}
```

Add two event handlers to create the tab and save your settings:

```csharp
public class Events : IConsumer
{
    const string Key = "MyCustomSearchSettings";
    
    // Add new tab to search settings page in backend.
    public async Task HandleEventAsync(TabStripCreated message)
    {
        if (message.TabStripName.EqualsNoCase("searchsettings-edit"))
        {
            await message.TabFactory.AppendAsync(builder => builder
                .Text(T("Modules.MyCompany.MyModule.ScopeName"))
                .Name("tab-mycustom-search-settings")
                .LinkHtmlAttributes(new { data_tab_name = Key })
                .Action("MyCustomSearchSettings", "MyController", new { area = "Admin" })
                .Ajax());
        }
    }

    // Save MyCustomSearchSettings.
    public async Task HandleEventAsync(ModelBoundEvent message,
        ICommonServices services,
        MultiStoreSettingHelper settingHelper,
        Lazy<IForumSearchQueryAliasMapper> forumSearchQueryAliasMapper)
    {
        var cp = message.BoundModel.CustomProperties;
        if (!cp.ContainsKey(Key) || cp[Key] is not MyCustomSearchSettingsModel model)
        {
            return;
        }

        var storeId = services.WorkContext.CurrentCustomer.GenericAttributes.AdminAreaStoreScopeConfiguration;
        var storeScope = services.StoreContext.GetStoreById(storeId)?.Id ?? 0;
        var settings = await services.SettingFactory.LoadSettingsAsync<MyCustomSearchSettings>(storeScope);

        settingHelper.Contextualize(storeScope);
        
        settings = ((ISettings)settings).Clone() as MyCustomSearchSettings;
        MiniMapper.Map(model, settings);
        await services.DbContext.SaveChangesAsync();                    
        // TODO: clear cached query aliases of facets (if any).
    }
}
```

The action method that provides the partial view with your settings may look like this:

```csharp
public async Task<IActionResult> MyCustomSearchSettings()
{
    // This is important for proper model binding. Set HtmlFieldPrefix early 
    // because MultiStoreSettingHelper use it to create override key names.
    ViewData.TemplateInfo.HtmlFieldPrefix = "CustomProperties[MyCustomSearchSettings]";

    var storeScope = GetActiveStoreScopeConfiguration();
    var settings = await Services.SettingFactory.LoadSettingsAsync<MyCustomSearchSettings>(storeScope);
    var model = MiniMapper.Map<MyCustomSearchSettings, MyCustomSearchSettingsModel>(settings);
    //...

    return PartialView(model);
}
```

And the partial view:

```cshtml
@model MyCustomSearchSettingsModel

@{
    Layout = "";
}

@* VERY IMPORTANT for proper model binding *@
<input type="hidden" name="CustomProperties[MyCustomSearchSettings].__Type__" value="@Model.GetType().AssemblyQualifiedName" />

<div class="adminContent" id="mycustom-search-settings">
    @* TODO: HTML of your settings. *@
</div>

@* In this case omit data-origin attribute. *@
<script>
    $(function() {
        // Init common controls like select2 or tooltips.
        applyCommonPlugins($('#mycustom-search-settings'));
        //...
    });
</script>
```
