---
description: Getting started to access the application database
---

# 🐣 Data access

## Overview

Every store is represented by a single database, even when using the multi-store option. Each table is represented by a special entity type derived from the `BaseEntity` type. Smartstore currently supports **Microsoft SQL Server**, **MySQL**, **PostgreSQL** and **SQLite** database systems and uses the [Entity Framework Core (EF)](https://learn.microsoft.com/en-us/ef/core/) Object-relational-Mapper (O/R-Mapper) to access the store database.

* It enables .NET developers to work with a database using .NET objects.
* Most of the data access code that would normally be written can be omitted.
* Write provider-agnostic data access code, regardless of the underlying database provider.

The main gateway to the application database is the `SmartDbContext`, representing the **Unit of Work**. The context keeps track of everything you do during a request / transaction that affects the database. It figures out everything that needs to be done to alter the database as a result of your work. Properties and extension methods that expose **repositories** to interact with specific database tables are also defined by it.

## SmartDbContext

The main EF `DbContext` implementation for the application database is the `SmarDbContext`. It is registered as a scoped dependency in the DI container, and as such its type can easily be passed around as a dependency to get resolved.

```csharp
public class MyService : IMyService
{
    private readonly SmartDbContext _db;

    public MyService(SmartDbContext db)
    {
        _db = db;
    }

    public Task<List<MyEntity>> GetMyEntities()
    {
        return _db.Set<MyEntity>().ToListAsync();
    }
}
```

{% hint style="info" %}
It is good practice to name the data context parameter `db`, or `_db` if used in a field. Keep it simple and short!
{% endhint %}

## Pooled factory

A `SmartDbContext` instance is not directly resolved from DI container, but from the pooled `IDbContextFactory<SmartDbContext>` which is configured and created on app start-up. The factory, registered as a singleton, is responsible for creating and pooling the `SmartDbContext` instances.

An instance created by the factory is returned to the pool upon disposal and is reset to its initial state. Every time a `SmartDbContext` instance is requested, the pool will return an already existing unleased instance instead of creating a new one - or will create a new instance if the pool is depleted. Therefore, the `SmartDbContext` DI registration is actually a delegate that leases an instance from this pool, not from the DI container.

Pooling is very beneficial for performance in high-load scenarios, because the factory prevents too many objects from being instantiated.

{% hint style="info" %}
By default, the pool size is set to **1024** instances and can be altered via `appsettings.json` using the `DbContextPoolSize` setting.
{% endhint %}

In some situations, it may be necessary to manually lease a context instance from the pool. Two scenarios in which this occurs are:

* You cannot pass the `SmartDbContext` as a dependency in singleton objects, because of its [scope](dependency-injection.md).
* You need granular control of your unit of work.

In these cases, you might want to do one of the following:

```csharp
public class MySingletonService : IMySingletonService 
{
    private IDbContextFactory<SmartDbContext> _dbFactory;

    public MySingletonService(IDbContextFactory<SmartDbContext> dbFactory)
    {
        // IDbContextFactory<T> is singleton and can safely 
        // be passed to other singleton dependencies.
        _dbFactory = dbFactory;
    }

    public Task<List<MyEntity>> GetMyEntities()
    {
        // Lease context instance from pool
        using (var db = _dbFactory.CreateDbContext())
        {
            // do something in this "unit of work"
        } // --> Dispose: return context instance to pool
    }
}
```

## Paging

### PagedList

The traditional approach to apply paging to a LINQ query would be by calling the `Skip()` and `Take()` methods. Smartstore provides a more convenient way with `PagedList`. It allows you to take an `IQueryable` (or even an `IEnumerable`), slice it up into _pages_, and grab a particular _page_ by an _index_.

You can simply call `ToPagedList` (instead of `ToList`) from your `IQueryable` / `IEnumerable`, passing the page _size_ and the page _index_ you want to load. The result is still an `IList<T>`, but contains only a subset of the total data. The `ToPagedList` call returns an instance of [IPagedList\<T>](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/Collections/IPagedList%60T.cs), which also implements the `IList<T>` and [IPageable\<T>](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/Collections/IPageable%60T.cs) interfaces.

If the source `IQueryable<T>` is a `DbSet<T>`, paging is performed on the database side, otherwise in memory.

{% hint style="info" %}
A `PagedList` loads data in a deferred manner when the iteration starts or when the list gets accessed for the first time. To load the data explicitly beforehand, you must call `Load` or `LoadAsync`.
{% endhint %}

#### Example

```csharp
// _db is SmartDbContext
var db = _db;

// Simple paging: load page 3 with 100 items
var productSlice = await db.Products.ToPagedList(3, 100).LoadAsync();

// More complex paging
var pageIndex = -1;

while (true)
{
    var products = db.Products.ToPagedList(++pageIndex, 500);
    
    // Iterate through all items in the page
    await foreach (var product in products.AsAsyncEnumerable())
    {
        // Do something...
    }

    if (!products.HasNextPage)
    {
        // Exit loop if there are no more pages
        break;
    }
}
```

### FastPager

The `FastPager` is not as convenient as a `PagedList`, but very efficient. It provides stable and consistent paging performance over **very large** datasets.

Unlike LINQ’s `Skip(x).Take(y)` approach, the entity set is sorted by descending id and a specified number of records are returned. The `FastPager` remembers the last (lowest) id returned and uses it for the WHERE clause of next batch. This way you can completely avoid `Skip()`, which is known to perform poorly on large tables when the skip count is very high.

#### Example

```csharp
// _db is SmartDbContext
var db = _db;

// Build the query beforehand
var query = db.Products.Where(x => x.Price > 100);

// Create a FastPager instance with given query and page size of 500
var pager = new FastPager<Product>(query, 500);

// Page synchronously
while (pager.ReadNextPage<Product>(out var products))
{
    // Do something meaningful...
}

// ...or page asynchronously
while ((await pager.ReadNextPageAsync<Product>()).Out(out var products))
{
    // Do something meaningful...
}
```

## Second-Level cache

The _second-level cache_ is where queried entities are cached in memory. This allows subsequent queries on the same entities to retrieve the result directly from the cache, bypassing the database altogether. Not having to access the database means that queries are executed much faster. This is further increased because there is no record to class materialization involved, as the materialized entities are already cached.

A cache entry always contains the result of a query, so it contains either a single entity or a list of entities. The key of the entry is the unique hash of its query. Even the slightest variation in the query results in a different hash and thus a new cache entry.

Whenever an entity that came from the cache is updated or deleted, all cache entries that contain that entity will be automatically invalidated. This means that the next time the query is executed, the database will be accessed again.

However, not all entity types are cacheable. Only types annotated with the `CacheableEntity` attribute are cached. This attribute also defines the caching policy, such as how long to cache the entry (default is 3 hours) and a max rows limit (causes query results with more items than the specified number not to be cached).

Only those entity types that do not change frequently, and those that are not likely to produce large database tables, are marked as cacheable. For example: `Country`, `StateProvince`, `Currency`, `Language`, `Store`, `TaxCategory`, `DeliveryTime`, `QuantityUnit`, `EmailAccount`, etc.

To activate caching on a per query basis, two approaches are used. They depend on whether the queried entity type is annotated with the `CacheableEntity` attribute. If it is annotated with the `CacheableEntity` attribute, call `AsNoTracking()` on the query, because only untracked entities are cached.

{% code title="CategoryTemplate.cs" %}
```csharp
[CacheableEntity]
public partial class CategoryTemplate : EntityWithAttributes, IDisplayOrder
{
    // The template...
}
```
{% endcode %}

{% code title="CategoryImporter.cs" %}
```csharp
var categoryTemplates = await _db.CategoryTemplates
    .AsNoTracking()
    .OrderBy(x => x.DisplayOrder)
    .ToListAsync(context.CancelToken);
```
{% endcode %}

If the queried entity type is **not** annotated with the `CacheableEntity` attribute, call `AsNoTracking()` and `AsCaching()` for the query.

```csharp
// Retrieve all customers with email addresses that end in ".com".
var customerQuery = _db.Customers
    .AsNoTracking()
    .AsCaching()
    .Where(x => x.Email.EndsWith(".com"))
```

To explicitly deactivate caching on a per query basis, call `AsNoCaching()` for the query.

{% hint style="warning" %}
**Tracked entities never get cached**, not even when `AsCaching()` is called!
{% endhint %}

## DataProvider

A [DataProvider](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/Data/Providers/DataProvider.cs) abstracts and unifies the internals of a database system supported by Smartstore. It acts as an adapter for low-level database operations and provides a unified interface to the different database systems.

The current DataProvider instance can be accessed by calling the `DataProvider` property of the `SmartDbContext` instance. It provides the following members, among others:

<table><thead><tr><th width="276">Member</th><th>Description</th></tr></thead><tbody><tr><td><code>BackupDatabase()</code></td><td>Creates a database backup. Currently only supported by <em>SQLServer</em> and <em>SQLite</em>.</td></tr><tr><td><code>Can*</code></td><td>Checks whether the database supports a specific feature. e.g.: <code>CanStreamBlob</code> returns <code>true</code> if the database can efficiently stream BLOB fields.</td></tr><tr><td><code>CreateParameter()</code></td><td>Returns a <code>DbParameter</code> instance that is compatible with the database.</td></tr><tr><td><code>EncloseIdentifier()</code></td><td>Encloses the given identifier in provider specific quotes, e.g. <em>[Name]</em> for MSSQL, `<em>Name</em>` for MySQL.</td></tr><tr><td><code>ExecuteSqlScript()</code></td><td>Executes a (multiline) SQL script in an atomic transaction.</td></tr><tr><td><code>GetDatabaseSize()</code></td><td>Gets the total size of the database in bytes.</td></tr><tr><td><code>GetTableIdent&#x3C;T>()</code></td><td>Gets the current ident value of the given table.</td></tr><tr><td><code>HasTable()</code></td><td>Checks whether the database contains the given table.</td></tr><tr><td><code>InsertInto()</code></td><td>Executes the given INSERT INTO SQL command and returns the ident of the inserted row.</td></tr><tr><td><code>IsTransientException()</code></td><td>Checks whether the given exception represents a transient failure that can be compensated by a retry.</td></tr><tr><td><code>OpenBlobStream()</code></td><td>Opens a BLOB stream for the given property accessor.</td></tr><tr><td><code>ReIndexTables()</code></td><td>Re-indexes all tables in the database.</td></tr><tr><td><code>RestoreDatabase()</code></td><td>Restores a previously created backup. Currently only supported by <em>SQLServer</em> and only if the database and web server are located on the same machine.</td></tr><tr><td><code>SetTableIdent&#x3C;T>()</code></td><td>Sets the table ident value.</td></tr><tr><td><code>ShrinkDatabase()</code></td><td>Shrinks / compresses / vacuums the database.</td></tr><tr><td><code>Sql()</code></td><td>Normalizes a given SQL command text by replacing quoted identifiers in MSSQL dialect to provider-specific quotes. E.g.: <code>SELECT [Id] FROM [Customers]</code> --> <code>SELECT</code> `<code>Id</code>` <code>FROM</code> `<code>Customers</code>` (MySQL dialect).</td></tr><tr><td><code>TruncateTable&#x3C;T>()</code></td><td>Truncates/clears a table. ALL rows will be deleted irreversibly!</td></tr></tbody></table>

## Conventions & best practices

### Eager load related data

Eager loading is a feature in EF Core that allows you to load related data along with the main entity data in a single query. This can improve performance compared to lazy loading, where related data is loaded on demand, because you avoid making multiple round-trips to the database for each related entity.

Here's an example of how you can use eager loading in EF Core 7:

```csharp
// Retrieve all customers whose orders have earned them
// points in the last month.
var orderQuery = _db.Orders
    .ApplyAuditDateFilter(DateTime.UtcNow.AddMonths(-1))
    .Include(x => x.Customer)
    .Where(x => x.RewardPointsWereAdded)
```

### Encapsulate queries and predicates

Encapsulating LINQ queries and `Where` predicates in extension methods provides several advantages:

1. Reusability: You can reuse the same code in multiple parts of your application, making it easier to maintain and reducing the likelihood of errors.
2. Improved readability: You can make your code more readable and easier to understand. This is especially true if you give descriptive names to the extension methods that encapsulate the predicates.
3. Increased maintainability: You can make changes to the predicates in one place, rather than having to change the code in multiple places throughout your application. This can make it easier to maintain your code and reduce the likelihood of bugs being introduced.
4. Better separation of concerns: Helps to separate the query logic from the rest of your code, making it easier to understand and maintain.

Here are some of the most commonly used built-in query extension methods in Smartstore:

* [GridCommandQueryExtension](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Web.Common/Models/DataGrid/GridCommandQueryExtensions.cs) -> `ToPagedList`
* [IStoreRestrictedQueryExtension](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Stores/Extensions/IStoreRestrictedQueryExtensions.cs) -> `ApplyStoreFilter`
* [CustomerQueryExtension](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Platform/Identity/Extensions/CustomerQueryExtensions.cs) -> `IncludeCustomerRoles`
* [ManufacturerQueryExtension](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Catalog/Brands/Extensions/ManufacturerQueryExtensions.cs) -> `ApplyStandardFilter`
* [OrderQueryExtension](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Checkout/Orders/Extensions/OrderQueryExtensions.cs) -> `ApplyStandardFilter`

Here is what a custom query extension class might look like:

```csharp
public static class MyEntityQueryExtensions
{
    /* Sample method call:
	await _db.MyEntities()
	    .AsNoTracking()
	    .ApplyCustomerFilter(model.CustomerId, true)
	    .AnyAsync();
    */
    public static IQueryable<MyEntity> ApplyCustomerFilter(
    	this IQueryable<MyEntity> query,
        int? customerId = null, 
        bool? approved = null)
    {
        if (customerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == customerId.Value);
        }

        if (approved.HasValue)
        {
            query = query.Where(x => x.IsApproved == approved.Value);
        }

        return query;
    }
}
```

## Deep Dive

To learn more about data access in Smartstore, read [data-access-deep-dive](../framework/advanced/data-access-deep-dive/ "mention").
