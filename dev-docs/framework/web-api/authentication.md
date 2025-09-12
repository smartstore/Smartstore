# ✔️ Authentication

Smartstore Web API uses _Basic Authentication_ over _HTTPS authentication_ method to protect data from unauthorized access. It is recommended by the _OData protocol version 4.0_ for the highest level of interoperability with generic clients.

The client sends the credentials by using the Authorization header. The credentials are formatted as the string `publicKey:secretKey` using UTF-8 and base64 encoding. The credentials are not encrypted, so HTTPS is required.

{% code title="Authentication example" %}
```csharp
var credentials = Convert.ToBase64String(
    Encoding.UTF8.GetBytes($"{publicKey}:{secretKey}"));

using var message = new HttpRequestMessage(
    new HttpMethod("GET"),
    "http://localhost:59318/odata/v1/Customers");

message.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
// Authorization: Basic ZWE2NGQ0YTIyZGI1......ZDY4NGRlMDRmZEFiZGUwMmY3MTg=
```
{% endcode %}

The API will respond with a `401 Unauthorized` status code if the user is not authorized to exchange data via the API. In this case, the response HTTP headers **Smartstore-Api-AuthResultId** (ID of the denied reason) and **Smartstore-Api-AuthResultDesc** (short description of the denied reason) are sent with details of the reason for denial. In addition, the **WWW-Authenticate** header is sent with the value `Basic realm="Smartstore.WebApi", charset="UTF-8"`.

## Reasons for denial

<table><thead><tr><th width="159.33333333333331" align="center">AuthResultId</th><th width="247">AuthResultDesc</th><th>Description</th></tr></thead><tbody><tr><td align="center">0</td><td>ApiDisabled</td><td>The API is disabled.</td></tr><tr><td align="center">1</td><td>SslRequired</td><td>HTTPS is required in any case unless the request takes place in a development environment.</td></tr><tr><td align="center">2</td><td>InvalidAuthorizationHeader</td><td>The HTTP authorization header is missing or invalid. Must include a pair of public and secret keys.</td></tr><tr><td align="center">3</td><td>InvalidCredentials</td><td>The credentials sent by the HTTP authorization header do not match those of the user.</td></tr><tr><td align="center">4</td><td>UserUnknown</td><td>The user is unknown.</td></tr><tr><td align="center">5</td><td>UserDisabled</td><td>The user is known but his access via the API is disabled.</td></tr></tbody></table>
