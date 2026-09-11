# Web API Clients

Smartstore provides two reference clients for testing and exploring the [Web API](../web-api/):

* `Smartstore.WebApi.Client`: a Windows desktop application for composing and executing individual requests.
* `Smartstore.WebApi.Client.JavaScript`: a small browser-based example that demonstrates API consumption with JavaScript and jQuery.

The source code is available on GitHub for the [.NET client](../../../tools/Smartstore.WebApi.Client) and the [JavaScript client](../../../tools/Smartstore.WebApi.Client.JavaScript).

Both clients use HTTP Basic Authentication and communicate with the versioned OData service.

{% hint style="warning" %}
These applications are development and testing tools, not production-ready SDKs. In particular, never expose a Web API secret key in client-side JavaScript in a production application.
{% endhint %}

## Prepare Web API access

Before using either client:

1. Install and enable the Web API module in the Smartstore backend.
2. Assign Web API access to a registered customer.
3. Generate a public and secret key for that customer.
4. Ensure the customer has the roles and permissions required by the requested resources.
5. Use HTTPS when connecting to anything other than a local development environment.

See the [Web API prerequisites](../web-api/prerequisites.md) and the [Web API module documentation](../../../src/Smartstore.Modules/Smartstore.WebApi/) for module setup. The [authentication reference](../web-api/authentication.md) explains the credentials and API denial reasons in detail.

The clients send the credentials in this form:

```
Authorization: Basic <base64(publicKey:secretKey)>
```

Basic Authentication encodes the credentials but does not encrypt them. HTTPS is therefore required to protect the keys in transit.

The default OData service URL follows this pattern:

```
https://<store-host>/odata/v1/<resource>
```

For example:

```
https://shop.example.com/odata/v1/Customers
```

## Use the Windows client

Build the `Smartstore.WebApi.Client` project from `Smartstore.Tools.sln`, then start `Smartstore.WebApi.Client.exe`.

The application provides separate fields for the service URL, credentials, request method, resource path, query string, JSON body, headers, and file uploads.

### Configure the connection

| Field          | Description                                                              |
| -------------- | ------------------------------------------------------------------------ |
| **Public-Key** | Public API key assigned to the customer.                                 |
| **Secret-Key** | Secret API key assigned to the customer.                                 |
| **Store URL**  | Base URL of the store, such as `https://shop.example.com/`.              |
| **Proxy Port** | Optional external port when the API is accessed through a reverse proxy. |
| **Version**    | API version without the `odata/` prefix, normally `v1`.                  |

The client constructs the request URL from the following values:

```
<Store URL>/odata/<Version><Path>?<Query>
```

For example:

| Field     | Value                       |
| --------- | --------------------------- |
| Store URL | `https://shop.example.com/` |
| Version   | `v1`                        |
| Path      | `/Customers`                |
| Query     | `$top=3&$select=Id,Email`   |

Result:

```http
GET https://shop.example.com/odata/v1/Customers?$top=3&$select=Id,Email
```

Do not include `/odata/v1` in the Store URL. Enter the API version and resource path in their corresponding fields.

### Compose a request

Select one of the supported methods:

* `GET`
* `POST`
* `PUT`
* `PATCH`
* `DELETE`

The available inputs change according to the method:

* GET and DELETE requests do not support a request body.
* POST, PUT, and PATCH requests support a JSON body.
* File uploads are available for POST requests.

Enter OData query options in **Query** without the leading question mark:

```
$filter=Email ne null&$select=Id,Email&$top=10
```

For more information about headers, paging, query options, and request bodies, see [Web API in detail](../web-api/web-api-in-detail.md).

Enter request bodies as JSON:

```json
{
  "Email": "customer@example.com",
  "FirstName": "Jane",
  "LastName": "Doe"
}
```

Additional HTTP headers must be entered as a JSON object. For example, request the updated entity in the response to a PUT or PATCH request with:

```json
{
  "Prefer": "return=representation"
}
```

Without the `Prefer` header, a successful update can return `204 No Content`.

Enable **IEEE754Compatible** when large numeric values should be serialized as strings to avoid precision loss in JavaScript-compatible consumers.

Select **Execute** to send the request.

### Review the result

The lower part of the application displays:

* The final HTTP method and URL.
* The generated request headers and body.
* The response status.
* The response headers.
* The formatted JSON response.

The client recognizes customer collection responses and additionally displays selected customer properties as a simple deserialization example.

JSON responses are automatically formatted for readability.

{% hint style="warning" %}
The request display contains the Basic Authentication header. Treat copied output and screenshots as sensitive information.
{% endhint %}

### Send a GET request

To retrieve three customers:

| Field     | Value        |
| --------- | ------------ |
| Method    | `GET`        |
| Path      | `/Customers` |
| Query     | `$top=3`     |
| JSON Body | Empty        |

The resulting request is:

```http
GET https://shop.example.com/odata/v1/Customers?$top=3
```

### Send a PATCH request

To update the email address of customer `42`:

| Field     | Value                                 |
| --------- | ------------------------------------- |
| Method    | `PATCH`                               |
| Path      | `/Customers(42)`                      |
| Headers   | `{"Prefer":"return=representation"}`  |
| JSON Body | `{"Email":"new-address@example.com"}` |

The resulting request is:

```http
PATCH https://shop.example.com/odata/v1/Customers(42)
Content-Type: application/json; charset=UTF-8
Prefer: return=representation

{
  "Email": "new-address@example.com"
}
```

Additional request patterns are available in the [Web API examples](../web-api/examples/).

### Upload files

File uploads are sent as `multipart/form-data` and are available for POST requests.

Select **Open file** to add one or more files. The client represents the upload configuration as JSON:

```json
{
  "Files": [
    {
      "Id": 0,
      "LocalPath": "C:\\Data\\product-image.jpg",
      "Path": "catalog/product-image.jpg",
      "IsTransient": true,
      "DuplicateFileHandling": 0
    }
  ],
  "CustomProperties": {}
}
```

Each file supports:

| Property                | Description                                                                  |
| ----------------------- | ---------------------------------------------------------------------------- |
| `Id`                    | Existing media-file identifier when updating a file. Use `0` for a new file. |
| `LocalPath`             | Absolute path of the local file to upload.                                   |
| `Path`                  | Optional target path in the Smartstore media system.                         |
| `IsTransient`           | Whether Smartstore should initially mark the uploaded file as transient.     |
| `DuplicateFileHandling` | `0` throws an error, `1` overwrites, and `2` creates a unique name.          |

`CustomProperties` adds ordinary form-data fields required by a particular endpoint:

```json
{
  "Files": [
    {
      "LocalPath": "C:\\Data\\products.xlsx"
    }
  ],
  "CustomProperties": {
    "deleteFiles": true,
    "startImport": true
  }
}
```

If an upload configuration is present for a POST request, the request is sent as multipart content instead of using the JSON Body field.

### Download files

When a successful response contains an image, video, PDF, or ZIP file, the client asks for a destination directory. It saves the response using the filename supplied by the server and then opens the downloaded file.

### Reuse request values

The desktop client remembers:

* API URL and version
* Public and secret keys
* Proxy port
* Previously used paths
* Queries
* Request bodies
* Headers
* File-upload configurations

The values are restored the next time the application starts. The history buttons marked with `x` remove the current value.

Because the credentials are stored in the current user's application settings, use dedicated development credentials and do not use the tool from a shared Windows account.

## Use the JavaScript client

`Smartstore.WebApi.Client.JavaScript` is a static example consisting of:

| File                 | Purpose                                      |
| -------------------- | -------------------------------------------- |
| `index.html`         | Example page and connection configuration.   |
| `smwapi-consumer.js` | Reusable request wrapper around jQuery AJAX. |
| `smwapi-client.js`   | Demo requests and user-interface handling.   |
| `styles.css`         | Minimal example styling.                     |

The example loads jQuery from a public CDN, so an internet connection is required unless the dependency is replaced with a local copy.

### Configure the example

Open `index.html` and replace the sample settings passed to `smApiConsumer.init`:

```javascript
smApiConsumer.init({
    publicKey: 'your-public-key',
    secretKey: 'your-secret-key',
    url: 'https://shop.example.com'
});
```

The base URL must not end with `/odata/v1`; the demo adds the service path separately.

Open or serve `index.html` in a browser after configuring it.

{% hint style="danger" %}
The credentials are stored in plain text and are visible to anyone who can inspect the page or browser requests. Use this example only with disposable development credentials. Never publish the configured page.
{% endhint %}

### Use the demo page

The page contains a service path and resource path:

```
Service:  /odata/v1
Resource: /Customers?$top=3
```

Select **Get** to execute the combined resource URL.

The two additional buttons demonstrate write operations:

* **POST order note** retrieves the first order and creates a note for it.
* **PATCH order note** retrieves the latest order note and modifies its text.

These buttons change store data. Use them only in a development or test store.

Requests and responses are shown beneath the controls, including HTTP headers and formatted JSON.

### Send requests from JavaScript

Initialize the consumer once:

```javascript
smApiConsumer.init({
    publicKey: 'your-public-key',
    secretKey: 'your-secret-key',
    url: 'https://shop.example.com'
});
```

Then call `startRequest`:

```javascript
smApiConsumer.startRequest({
    method: 'GET',
    resource: '/odata/v1/Customers?$top=3',
    done: function (data, textStatus, jqXHR) {
        console.log(data);
    },
    fail: function (jqXHR, textStatus, errorThrown) {
        console.error(jqXHR.status, jqXHR.responseText);
    }
});
```

The `method` option defaults to `GET`.

To send JSON, pass an object through `content`:

```javascript
smApiConsumer.startRequest({
    method: 'PATCH',
    resource: '/odata/v1/Customers(42)',
    content: {
        Email: 'new-address@example.com'
    },
    done: function (data) {
        console.log(data);
    },
    fail: function (jqXHR) {
        console.error(jqXHR.responseText);
    }
});
```

The consumer serializes object content to JSON and sends:

```
Content-Type: application/json
Accept: application/json
Authorization: Basic <credentials>
```

Supported callbacks are:

| Callback     | Arguments                           | Purpose                                          |
| ------------ | ----------------------------------- | ------------------------------------------------ |
| `beforeSend` | `jqXHR`, AJAX settings              | Inspect or modify the request before it is sent. |
| `done`       | Response data, text status, `jqXHR` | Handle a successful response.                    |
| `fail`       | `jqXHR`, text status, error         | Handle a failed response.                        |

## Resolve common errors

### 401 Unauthorized

Inspect these response headers:

* `Smartstore-Api-AuthResultId`
* `Smartstore-Api-AuthResultDesc`

They distinguish invalid credentials from a disabled API, unknown customer, or disabled API user. See [Authentication](../web-api/authentication.md) for all denial reasons.

### 421 Misdirected Request

The request used HTTP where HTTPS is required. Switch the Store URL to HTTPS. HTTP is allowed only when Smartstore is running in a development environment.

### 403 Forbidden

The authenticated customer does not have permission to access the requested resource or operation. Review the customer's roles and permissions in the backend.

### 404 Not Found

Check that:

* The URL contains `/odata/v1`.
* The resource name uses the correct pluralization.
* The requested entity or operation exists.
* The installed Web API version exposes the endpoint.

Use `/odata/v1/$metadata` or `/docs/api` to inspect the endpoints supported by the target store. See [Help & Tools](../web-api/help-and-tools.md) for details.

### Browser request blocked

Confirm that the request is sent to the correct Smartstore host and that the Web API module is enabled. The module supplies a CORS policy for GET, POST, PUT, PATCH, and DELETE requests.

### Large collections are incomplete

Collection endpoints are paged. Use `$top` and `$skip`, and inspect the `Smartstore-Api-MaxTop` response header for the maximum permitted page size.
