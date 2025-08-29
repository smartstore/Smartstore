# ✔ Database bulk operations

By default, Smartstore (which uses Entity Framework for data access) tracks changes to entities, and then sends updates to the database when one of the `SaveChanges` methods is called. Changes are only sent for properties and relationships that have actually changed. Also, the tracked entities remain in sync with the changes sent to the database. This mechanism is an efficient and convenient way to send general-purpose inserts, updates, and deletes to the database. These changes are also batched to reduce the number of database round-trips.

However, it is sometimes useful to execute update or delete commands on the database without involving the change tracker. Entity Framework enables this with the `ExecuteUpdate` and `ExecuteDelete` methods. These methods are applied to a LINQ query and will update or delete entities in the database based on the results of that query. Many entities can be updated with a single command and the entities are not loaded into memory, which means this can result in more efficient updates and deletes.

However, keep in mind that:

* The Smartstore [hooking](../../platform/hooks.md) mechanism will be bypassed.
* The specific changes to make must be specified explicitly; they are not automatically detected by EF.
* Any tracked entities will not be kept in sync.
* Additional commands may need to be sent in the correct order so as not to violate database constraints. For example deleting dependents before a principal can be deleted.

All of this means that the `ExecuteUpdate` and `ExecuteDelete` methods complement, rather than replace, the existing `SaveChanges` mechanism.

## Basic examples

```csharp
// Bulk delete all LocaleStringResource entities 
// whose ResourceName property start with "MyModule."
await db.LocaleStringResources
    .Where(x => x.ResourceName.StartsWith("MyModule."))
    .ExecuteDeleteAsync();

// Append " (old)" to the Name property of product entities
// whose year of creation is before 2020.
await _db.Products
    .Where(x => x.CreatedOnUtc.Year < 2020)
    .ExecuteUpdateAsync(
        x => x.SetProperty(p => p.Name, p => p.Name + " (old)"));
```

To learn more about bulk operations, please refer to the [EF Core 7 documentation](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-7.0/whatsnew#executeupdate-and-executedelete-bulk-updates).&#x20;
