# ✔️ DbContextScope

An instance of `SmartDbContext` has a short lifetime. It is retrieved from the pool when an HTTP request begins and is returned to the pool at the end of the request. To change the configuration of the request scoped context you can...:

1. Create a custom nested scope.
2. Do several actions within the scope.
3. Dispose the nested scope.

The context’s original state, as it was when you created the scope, is automatically restored when the scope is disposed. A custom scope is useful in scenarios where the default configuration of the data context does not fit your needs. For example:

* Disable automatic change detection in long-running operations, which slows down the more entities the change tracker contains.
* Speed up write access by disabling hook invocation, as both hook resolution and hook invocation take some time to process.
* Force the scope to be atomic, which saves all changes when the scope ends or is explicitly committed.

To create a scope where a `DbContext` instance behaves differently, use [DbContextScope](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/Data/DbContextScope.cs). The following code shows an excerpt from [MediaService.Folder.cs](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore.Core/Content/Media/MediaService.Folder.cs).

{% code overflow="wrap" %}
```csharp
using (var scope = new DbContextScope(_db, 
    autoDetectChanges: false,
    deferCommit: true))
{
    destinationPath += "/" + node.Value.Name;
    var dupeFiles = new List<DuplicateFileInfo>();

    // >>>> Do the heavy stuff
    var folder = await InternalCopyFolder(scope, node, destinationPath,
                            dupeEntryHandling, dupeFiles, cancelToken);

    var result = new FolderOperationResult
    {
        Operation = "copy",
	DuplicateEntryHandling = dupeEntryHandling,
	Folder = folder,
	DuplicateFiles = dupeFiles
    };

    return result;
}
```
{% endcode %}

## Parameter reference

When initializing a `DbContextScope` instance, any non-`null` value will change the corresponding option for the duration of the scope. The `db` parameter is the only one required.

<table><thead><tr><th width="239">Parameter</th><th>Description</th></tr></thead><tbody><tr><td><code>db</code></td><td>The data context instance to create scope for.</td></tr><tr><td><code>autoDetectChanges</code></td><td>Toggles EF's automatic entity <em>change detection</em> feature.</td></tr><tr><td><code>lazyLoading</code></td><td>Toggles EF's <em>lazy loading</em> feature.</td></tr><tr><td><code>forceNoTracking</code></td><td>If <code>true</code>, query results are not tracked, unless specified otherwise.</td></tr><tr><td><code>deferCommit</code></td><td>Suppresses the execution of <code>SaveChanges</code> until the scope is disposed or <code>Commit</code> is called explicitly. The default is <code>false</code>.</td></tr><tr><td><code>retainConnection</code></td><td>Opens a connection and retains it until the scope is closed. May increase load/save performance for large scopes. The default is <code>false</code>.</td></tr><tr><td><code>minHookImportance</code></td><td>Specifies the minimum hook importance level. Hooks below this level will not be executed.</td></tr><tr><td><code>cascadeDeleteTiming</code></td><td>Sets the <code>CascadeDeleteTiming</code> option in EF's change tracker.</td></tr><tr><td><code>deleteOrphansTiming</code></td><td>Sets the <code>DeleteOrphansTiming</code> option in EF's change tracker.</td></tr></tbody></table>
