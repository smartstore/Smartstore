# ✔ Help & Tools

## The OData metadata document <a href="#helpandtools-theodatametadatadocument" id="helpandtools-theodatametadatadocument"></a>

The metadata document describes the _Entity Data Model_ (EDM) of the OData service, using an XML language called the _Conceptual Schema Definition Language_ (CSDL). The metadata document shows the structure of the data in the OData service and can be used to generate client code. This is the recommended overview for the consumer to indicate the location of a particular resource or API endpoint. To retrieve the metadata document, send the following request:

```http
GET http://localhost:59318/odata/v1/$metadata
```

## Swagger Web API help <a href="#helpandtools-swaggerwebapihelp" id="helpandtools-swaggerwebapihelp"></a>

Swagger is a machine-readable representation of a RESTful API that enables support for interactive documentation and discoverability. It is capable of sending test requests to the API via a **Try It Out** button. To open the Swagger help pages, send the following request:

```
GET http://localhost:59318/docs/api
```

## Client test tool <a href="#helpandtools-clienttesttool" id="helpandtools-clienttesttool"></a>

The Smartstore source code includes a Windows Forms application for testing the API. It is included in the source code package that can be downloaded from the [Smartstore Releases page](https://github.com/smartstore/Smartstore/releases). The client source code is open source. It can be found at [GitHub](https://github.com/smartstore/Smartstore) under **tools/Smartstore.WebApi.Client**. A simple JavaScript client is also available under **tools/Smartstore.WebApi.ClientJavaScript**.

Alternatively, you can use [Postman](https://www.postman.com/) to submit requests to the API.

{% hint style="info" %}
You won't find help for every API resource in this documentation. Instead, use the tools listed above (or others) to explore endpoints, fields, field types, etc. While this documentation provides some examples of common data exchange scenarios, it is not a substitute for detailed OData documentation.
{% endhint %}
