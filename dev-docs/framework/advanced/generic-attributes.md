# Generic attributes

Generic attributes are Smartstore's mechanism for attaching arbitrary metadata to existing entities. Instead of creating a new domain class and database table for every extra piece of information, plugins and customizations can persist key–value pairs that travel with the entity. This keeps the data model lean while still allowing unlimited extensibility.

Each attribute is stored in the `GenericAttribute` table with the columns `EntityId`, `KeyGroup`, `Key`, `Value` and `StoreId`.  
* **EntityId** – identifier of the record the attribute belongs to.  
* **KeyGroup** – usually the entity's CLR type name, grouping all attributes of that type.  
* **Key** – the attribute name.  
* **Value** – string representation of the value.  
* **StoreId** – optional store scope (0 = all stores).

Values are stored as strings but can be converted to and from any .NET type, so you can work with dates, numbers or even custom enums without manual serialization.

## How it works

Most core entities such as `Customer`, `Order` or `Product` derive from `EntityWithAttributes`. The base class exposes a lazily loaded `GenericAttributes` property that wraps all attribute operations. Attributes are loaded on demand and saved automatically when the entity is persisted. If the entity gets deleted, associated attributes are removed as well.

Entities that do **not** inherit from `EntityWithAttributes` can still participate. The `IGenericAttributeService` returns a `GenericAttributeCollection` for any entity name and id:

```csharp
var attributes = _genericAttributeService.GetAttributesForEntity(nameof(Customer), customerId);
```

Collections are cached for the current request. When an entity has not been saved yet (`id = 0`), the collection is read‑only because attributes must reference a persisted record.

## Reading and writing values

`GenericAttributeCollection` makes attribute access feel like working with a dictionary. Use `Get<T>()` to read values and `Set()` to create, update or delete them. Internally Smartstore uses its type‑conversion utilities so that you do not need to parse strings yourself:

```csharp
var attrs = customer.GenericAttributes;                // shortcut for EntityWithAttributes
var lastLogin = attrs.Get<DateTime>("LastLogin");     // default(DateTime) if missing
attrs.Set("LastLogin", DateTime.UtcNow);              // inserts or updates
await attrs.SaveChangesAsync();                        // persists changes
```

Passing `null` or an empty string to `Set` removes the attribute.

You can batch multiple `Set` calls and then persist them in one go via `SaveChanges()` or `SaveChangesAsync()`. Until then, changes live only in memory, keeping database traffic low.

### Store‑specific values

In multi‑store scenarios an entity can hold different values per store by supplying a `storeId` argument:

```csharp
attrs.Set("WelcomeMessage", "Hi there", storeId: 1);
var message = attrs.Get<string>("WelcomeMessage", storeId: 1);
```

Without a store id the value is considered store‑neutral and applies to all stores. 

## Bulk loading

Fetching attributes one entity at a time can generate many queries when dealing with lists. The service therefore supports prefetching entire sets:

```csharp
await _genericAttributeService.PrefetchAttributesAsync(nameof(Customer), customerIds);
// subsequent GetAttributesForEntity calls for these ids are served from memory
```

Prefetching is scoped to the current request and dramatically reduces database round‑trips in batch operations or grid views.

## Example: plugin metadata

Imagine a shipping plugin that needs to remember the last pickup date for each order. Instead of adding a new column or table, the plugin can store the value as a generic attribute:

```csharp
var attrs = _genericAttributeService.GetAttributesForEntity(nameof(Order), order.Id);
var lastPickup = attrs.Get<DateTime?>("LastPickup");

if (pickupCompleted)
{
    attrs.Set("LastPickup", DateTime.UtcNow);
    await attrs.SaveChangesAsync();
}
```

Developers can introduce new keys at any time without running migrations, which makes generic attributes perfect for optional, module‑specific data, user preferences or external identifiers.

## When to create your own entity

Generic attributes are designed for small bits of data. If you need to query by value, store large text blobs or model complex relationships, a dedicated domain entity with its own table is usually the better choice. Attributes are stored as plain strings and are not indexed, so they should not replace proper domain modeling where structure and performance matter.

## Summary

* Attach arbitrary data to any entity without altering the database.
* Use `Get<T>()` and `Set()` for strongly typed access; call `SaveChangesAsync()` to persist.
* Optional store scope lets values differ between stores.
* `PrefetchAttributesAsync` batches lookups for many entities.
* Ideal for lightweight metadata; use real entities for complex or heavy data.