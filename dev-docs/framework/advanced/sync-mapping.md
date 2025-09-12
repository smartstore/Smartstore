# Sync mapping

Sync mappings tie Smartstore entities to records in external systems. Each record stores the internal entity identifier together with the external key and a context name that identifies the integration (for example `"ERP"` or `"PIM"`). Mappings are unique per `(EntityId, EntityName, ContextName)` and per `(SourceKey, EntityName, ContextName)` so sync operations stay idempotent.

## Schema

`SyncMapping` lives in `DbContext.SyncMappings` and is never cached. It contains the following fields:

- `EntityId` – identifier of the Smartstore entity.
- `EntityName` – entity type, usually `nameof(Product)`, `nameof(Category)` etc.
- `SourceKey` – primary key of the external record.
- `ContextName` – name of the remote system.
- `SourceHash` – optional checksum of the source payload to detect changes.
- `CustomInt`, `CustomString`, `CustomBool` – free-form metadata slots.
- `SyncedOnUtc` – timestamp of the last synchronization.

## Creating or updating a mapping

```csharp
var map = await _db.SyncMappings
    .SingleOrDefaultAsync(x => x.SourceKey == dto.Id
        && x.ContextName == "ERP"
        && x.EntityName == nameof(Product));

if (map == null)
{
    map = new SyncMapping {
        EntityId = product.Id,
        EntityName = nameof(Product),
        ContextName = "ERP",
        SourceKey = dto.Id
    };
    _db.SyncMappings.Add(map);
}

map.SourceHash = dto.Hash;
map.SyncedOnUtc = DateTime.UtcNow;
await _db.SaveChangesAsync();
```

## Resolving an entity by external key

```csharp
var map = await _db.SyncMappings
    .SingleOrDefaultAsync(x => x.SourceKey == dto.Id
        && x.ContextName == "ERP"
        && x.EntityName == nameof(Product));

if (map != null)
{
    var product = await _db.Products.FindAsync(map.EntityId);
}
```

Use `SyncMappingQueryExtensions.ApplyEntityFilter` to filter mappings by entity identifiers, entity name or context when cleaning up mappings or preparing batch jobs.

## Remote access

The Web API exposes `/odata/v1/SyncMappings` so remote applications can query or modify mappings using standard OData operations (`GET`, `POST`, `PATCH`, `PUT`, `DELETE`).