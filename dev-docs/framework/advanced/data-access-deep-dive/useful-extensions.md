---
description: For (SmartDb)Context and DbSet
---

# ✔ Useful extensions

### DbContext extensions

<details>

<summary>OpenConnection(Async)</summary>

Opens and retains connection until end of scope. Call this method in long running processes to gain slightly faster database interaction.

</details>

<details>

<summary>FindTracked</summary>

Tries to locate an already loaded and tracked entity in the local state manager.

</details>

<details>

<summary>HasChanges</summary>

Checks whether at least one entity in the change tracker is in `Added`, `Deleted` or `Modified` state.

</details>

<details>

<summary>TryUpdate</summary>

Sets the state of an entity to `Modified` if it is detached.

</details>

<details>

<summary>TryChangeState</summary>

Changes the state of an entity object when requested state differs.

</details>

<details>

<summary>TryGetModifiedProperty</summary>

Determines whether an entity property has changed since it was attached.

</details>

<details>

<summary>GetModifiedProperties</summary>

Gets a list of modified properties for a given entity.

</details>

<details>

<summary>ReloadEntity(Async)</summary>

Reloads the entity from the database overwriting any property values with values from the database. The entity will be in the `Unchanged` state after calling this method.

</details>

<details>

<summary>DetachEntity</summary>

Detaches a single entity from the current context if it is attached.

</details>

<details>

<summary>DetachEntities</summary>

Detaches many entities from the current context.

</details>

<details>

<summary>IsCollectionLoaded</summary>

Checks whether a collection type navigation property has already been loaded for a given entity (either eagerly or lazily).

</details>

<details>

<summary>IsReferenceLoaded</summary>

Checks whether a reference type navigation property has already been loaded for a given entity (either eagerly or lazily).

</details>

<details>

<summary>LoadCollectionAsync</summary>

Loads entities referenced by a collection navigation property from database, unless data is already loaded.

</details>

<details>

<summary>LoadReferenceAsync</summary>

Loads an entity referenced by a navigation property from database, unless data is already loaded.

</details>



### DbSet extensions

<details>

<summary>GetDbContext</summary>

Resolves the `DbContext` instance from which a given `DbSet` was obtained.

</details>

<details>

<summary>FindById(Async)</summary>

Finds an entity with a given id. If an entity with the given id is being tracked by the context, it is returned immediately without making a request to the database. Otherwise, a query is made to the database for an entity with the given id and this entity, if found, is returned. If no entity is found, then `null` is returned. If the `tracked` parameter is set to `true`, the entity is also attached to the context, so that subsequent calls can return the tracked entity without a database roundtrip.

</details>

<details>

<summary>GetMany(Async)</summary>

Loads many entities from database sorted by the given id sequence. Sort is applied in-memory.

</details>

<details>

<summary>DeleteAll(Async)</summary>

Truncates the table for a given entity type.

</details>
