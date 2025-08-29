# ✔ Appendix

## Errors

| Error message                                                                                                                                                             | Possible reason                                                         |
| ------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------- |
| **ODataException**: Invalid JSON. An unexpected comma was found in scope 'Object'. A comma is only valid between properties of an object or between elements of an array. | A comma appended to the last property of a JSON formatted request body. |

## General developer notes <a href="#general-developer-notes" id="general-developer-notes"></a>

### OData

* `IActionResult` is used when multiple return types are possible, otherwise `ActionResult<T>` can be used. The type property of `ProducesResponseTypeAttribute` can be excluded for `ActionResult<T>`.
* OData **functions** can only be `HttpGet`, OData **actions** can only be `HttpPost`.
* By protocol specification `enums` are serialized using the enum member's _string_, not its _value_.
* [Routing conventions](https://learn.microsoft.com/en-us/odata/webapi/built-in-routing-conventions) (only partly applicable for AspNetCore.OData v.8).
* [$ref](https://learn.microsoft.com/en-us/aspnet/web-api/overview/odata-support-in-aspnet-web-api/odata-v4/entity-relations-in-odata-v4#creating-a-relationship-between-entities) is not supported.
* Reasonably accurate OData [examples](https://github.com/dotnet/aspnet-api-versioning/tree/93bd8dc7582ec14c8ec97997c01cfe297b085e17/examples/AspNetCore/OData).

### [Swashbuckle](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)

* Explicit `From` parameter bindings are required, otherwise _Swashbuckle_ will describe them as query parameters by default. Code comments of parameters decorated with `FromForm` do not show up ([#2519](https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/2519)).
