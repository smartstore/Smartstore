## Breaking changes in Smartstore Web API 5

- For the highest level of interoperability with generic clients, the Web API now uses Basic authentication over HTTPS 
as recommended by OData protocol version 4.0.

- For PUT and PATCH requests, the HTTP header **Prefer** with the value **return=representation** must be sent to get a 
status code 200 with entity content response. This is the default behavior of ASP.NET Core OData 8.0.

- The prefix of response headers changed from **SmartStore-Net-Api-** to **Smartstore-Api-**. More changes:
<table>
    <tr>
        <th>Old name</th>
        <th>New name</th>
        <th>Remarks</th>
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

