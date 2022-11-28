## Breaking changes in Smartstore Web API 5

- HMAC authentication is no longer supported. For the highest level of interoperability with generic clients, the Web API now uses Basic authentication over HTTPS 
as recommended by OData protocol version 4.0.

- Querying a related entity via path **GET /EntitySet(&lcub;id&rcub;)/RelatedEntity(&lcub;relatedId&rcub;)** is no longer supported. Use the related path directly.  
 Example: old `/Customers(1)/Addresses(2)`, new `/Addresses(2)`.

- Querying a single, simple property value via path **GET /EntitySet(&lcub;id&rcub;)/PropertyName** is no longer supported. Use the more flexible **$select** instead.  
Example: old `/Categories(14)/Name`, new `/Categories(14)?$select=Name`.

- For PUT and PATCH requests, the HTTP header **Prefer** with the value **return=representation** must be sent to get a 
status code 200 with entity content response. This is the default behavior of ASP.NET Core OData 8.0. Otherwise **204 No Content** is returned.

- `/MediaFiles` returns type **FileItemInfo** which wraps and enriches the MediaFile entity. 
`/MediaFolders` returns type **FolderNodeInfo** which wraps and enriches the MediaFolder entity.
FileItemInfo and FolderNodeInfo are flattened objects and no longer entities.

- Request parameters are always written in camel case, for example for OData actions.    
Example: old `/MediaFiles/GetFileByPath {"Path":"catalog/my-image.jpg"}`, new `/MediaFiles/GetFileByPath {"path":"catalog/my-image.jpg"}`.

- Changed endpoints:
<table>
<tbody>
    <tr>
        <th>Old endpoint</th>
        <th>New endpoint</th>
        <th>Remarks</th>
    </tr>
    <tr>
        <td>GET MediaFiles/Download({Id})</td>
        <td>GET MediaFiles/DownloadFile({id})</td>
        <td></td>
    </tr>
    <tr>
        <td>POST OrderItems({id})/Infos</td>
        <td>GET OrderItems/GetShipmentInfo({id})</td>
        <td></td>
    </tr>
    <tr>
        <td>POST Orders({id})/Infos</td>
        <td>GET Orders/GetShipmentInfo({id})</td>
        <td></td>
    </tr>
    <tr>
        <td>POST Orders({id})/Pdf</td>
        <td>GET Orders/DownloadPdf({id})</td>
        <td></td>
    </tr>
    <tr>
        <td>GET Payments/Methods <sup>1.</sup></td>
        <td>GET PaymentMethods/GetAllPaymentMethods({active},{storeId})</td>
        <td>New method.</td>
    </tr>
    <tr>
        <td>ProductPictures/...</td>
        <td>ProductMediaFiles/...</td>
        <td>The controller name has changed.</td>
    </tr>
    <tr>
        <td>Products/ProductPictures</td>
        <td>Products/ProductMediaFiles</td>
        <td></td>
    </tr>
</tbody>
<tfoot>
    <tr>
        <td colspan="3">
            <small>
                <sup>1.</sup> Route <strong>/api/v1/</strong> no longer exists.
            </small>
        </td>
    </tr>
</tfoot>
</table>

- Changed response header names:
<table>
    <tr>
        <th>Old name</th>
        <th>New name</th>
        <th>Remarks</th>
    </tr>
    <tr>
        <td>SmartStore-Net-Api-...</td>
        <td>Smartstore-Api-...</td>
        <td>Name prefix changed.</td>
    </tr>
    <tr>
        <td>SmartStore-Net-Api-HmacResultId</td>
        <td>Smartstore-Api-AuthResultId</td>
        <td>New values see <a href="https://smartstore.atlassian.net/wiki/spaces/SMNET50/pages/1956121714/Web+API">docu</a>.</td>
    </tr>
    <tr>
        <td>SmartStore-Net-Api-HmacResultDesc</td>
        <td>Smartstore-Api-AuthResultDesc</td>
        <td>New values see <a href="https://smartstore.atlassian.net/wiki/spaces/SMNET50/pages/1956121714/Web+API">docu</a>.</td>
    </tr>
    <tr>
        <td>SmartStore-Net-Api-MissingPermission</td>
        <td>-</td>
        <td>Obsolete, no longer sent.</td>
    </tr>
</table>

- The query string parameter **SmNetFulfill** has been renamed to **SmApiFulfill**.

## General developer notes
### OData
- Accurate OData <a href="https://github.com/dotnet/aspnet-api-versioning/tree/93bd8dc7582ec14c8ec97997c01cfe297b085e17/examples/AspNetCore/OData">examples</a>.
- <a href="https://learn.microsoft.com/en-us/odata/webapi/built-in-routing-conventions">Routing conventions</a> (only partly applicable for AspNetCore.OData v.8).
- <a href="https://learn.microsoft.com/en-us/aspnet/web-api/overview/odata-support-in-aspnet-web-api/odata-v4/entity-relations-in-odata-v4#creating-a-relationship-between-entities">$ref</a> (not supported).

- **IActionResult** is used when multiple return types are possible, otherwise **ActionResult&lt;T&gt;** can be used. 
The type property of ProducesResponseTypeAttribute can be excluded for **ActionResult&lt;T&gt;**.

- OData **functions** can be only HttpGet, OData **actions** only HttpPost.

- By protocol specification **enums** are serialized using the enum member string, not the enum member value.

### <a href="https://github.com/domaindrivendev/Swashbuckle.AspNetCore">Swashbuckle</a>
- Explicit **From** parameter bindings are required otherwise Swashbuckle will describe them as query parameters by default.
Code comments of parameters decorated with **FromForm** do not show up (<a href="https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/2519">#2519</a>).

### Errors
<table>
    <tr>
        <th>Error message</th>
        <th>Possible reason</th>
    </tr>
    <tr>
        <td><strong>ODataException</strong>: Invalid JSON. An unexpected comma was found in scope 'Object'. A comma is only valid between properties of an object or between elements of an array.</td>
        <td>A comma appended to the last property of a JSON formatted request body.</td>
    </tr>
</table>
