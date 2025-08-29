# ✔ Web API

The Smartstore Web API enables direct access to online store data. It is based on the latest technologies that Microsoft offers for web-based data using the ASP.NET Core Web API and the OData provider. This documentation provides an overview of how to create an API client, also known as an API consumer. It uses C# code, but you can build your consumer in any common programming language. Because OData is a standardized protocol, there are many frameworks and toolkits available for different platforms.

The **ASP.NET Core Web API** is a framework for building Web APIs on top of the ASP.NET Core framework. It uses the HTTP protocol to communicate between the shop and clients to have data access.

The **Open Data Protocol** ([OData](https://en.wikipedia.org/wiki/Open\_Data\_Protocol)) is a standardized Web protocol that provides a consistent way to expose, structure, query, and manipulate data using REST practices. OData also provides a consistent way to represent metadata about the data, allowing clients to learn more about the type system, relationships, and structure of the data.

The Web API is primarily used to exchange raw data. In addition, it contains service functions and methods similar to those in the store backend.

{% hint style="info" %}
If different data is exchanged on a large scale or requires special processing, it may be more flexible to develop a custom module. The module acts as middleware between Smartstore and your application and can access all service functions of the Smartstore core, where the API can only ever provide a small subset.
{% endhint %}
