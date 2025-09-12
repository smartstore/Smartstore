# Async state

Async state stores progress and cancellation information for long running jobs. Entries live in the cache so Async state provides a lightweight shared store for long running jobs. It lets multiple servers exchange progress and cancellation information. For example, when an admin starts a product import, the import service could write its progress and a `CancellationToken` to the distributed async state cache. Any request, even on another server, can read the entry to display a progress bar or cancel the job. An easy example can be found at the bottom of this page.

## Create a state entry

Inject `IAsyncState` and create an initial item. Attach a `CancellationTokenSource` to allow external cancellation.

```csharp
public record ImportState(int Progress);

public class ImportService
{
    private readonly IAsyncState _state;

    public ImportService(IAsyncState state) => _state = state;

    public async Task RunAsync(CancellationToken token)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        await _state.CreateAsync(new ImportState(0), "import", cancelTokenSource: cts);
    }
}
```

## Update and read progress

Use `UpdateAsync` to mutate the stored object. Read it with `GetAsync`. The key combines the type and an optional name.

```csharp
await _state.UpdateAsync<ImportState>(s => s.Progress = 50, "import");

var state = await _state.GetAsync<ImportState>("import");
```

## Cancel a process

Call `Cancel` to request a stop. The stored token is cancelled.

```csharp
_state.Cancel<ImportState>("import");
```

## Expire and remove entries

Items expire after fifteen minutes of inactivity. Pass `neverExpires: true` to keep them longer and always remove them when done.

```csharp
await _state.RemoveAsync<ImportState>("import");
```

## Expose progress to the client

This example shows how a product import exposes its progress to the client.

### Server

```csharp
[HttpPost]
public async Task<JsonResult> ProductImportProgress(int id)
{
    var progress = await _asyncState.GetAsync<ProductImportState>(id.ToStringInvariant());
    return Json(progress);
}
```

### Client

```javascript
$(function () {
    $("#btn-start-product-import").on('click', function (e) {
        // Start throbber and display notifications.
        $.throbber.show({
            message: `
                <div id="import-message">@T("Plugins.Smartstore.ProductImport.Wait").Value</div>
                <div id="import-progress" style="font-size: 16px; font-weight: 400; margin: 10px 0 30px 0"></div>`
            });

        window.setInterval(checkProductCreationProgress, 1500);
        return false;
    });

    function checkProductCreationProgress() {
        $.ajax({
            cache: false,
            type: 'POST',
            url: '@Url.Action("ProductImportProgress", "Import", new { area = "Admin" })',
            dataType: 'json',
            data: $('#frm-product-import').serialize(),
            success: function (data) {
                if (data) {
                    $("#import-progress").html(data.ProgressMessage);
                }
            },
            error: function (xhr, ajaxOptions, thrownError) { },
            complete: function () { }
        });
    }
});
```