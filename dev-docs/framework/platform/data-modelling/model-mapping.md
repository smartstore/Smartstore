---
description: Object mapping made easy
---

# ✔ Model mapping

## Overview

Automatic object mapping is useful whenever you need to convert from type A to type B. Instead of manually assigning each member one at a time, an object mapper does this in a very generic way using reflection. For example, you can convert an entity type (such as `Product`) to a view model type (such as `ProductModel`) or vice-versa. Smartstore comes with object mapping utilities that are ultra-lightweight and fast.

<table><thead><tr><th width="166.33333333333331">Utility</th><th width="194">Description</th><th>Call</th></tr></thead><tbody><tr><td><a href="https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/ComponentModel/MiniMapper.cs">MiniMapper</a></td><td>Very simple object mapper</td><td><code>MiniMapper.Map(From, To)</code></td></tr><tr><td><a href="https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/ComponentModel/MapperFactory.cs">MapperFactory</a></td><td>Advanced, customizable and flexible mapper</td><td><code>MapperFactory.MapAsync(From, To)</code></td></tr></tbody></table>

## MiniMapper

The `MiniMapper` is a lightweight and simple object mapping utility that attempts to map properties **of the same name** between two objects. It uses reflection under the hood to query and access properties. If matched properties have different types, the mapper tries to convert them using the [type conversion system](../../advanced/type-conversion.md). If this fails, the property is simply skipped and no exception is thrown.&#x20;

{% hint style="success" %}
Use `MiniMapper` when:

* source and target type look more or less the same
* you don't need full control over mapping
{% endhint %}

{% hint style="warning" %}
`MiniMapper` **cannot** deep-copy collection types and **does not** clone reference types.
{% endhint %}

### Example

```csharp
BlogSettings settings = /* get instance somehow */;

// Maps BlogSettings --> BlogSettingsModel (must have a parameterless constructor)
// and returns the destination instance
var model = MiniMapper.Map<BlogSettings, BlogSettingsModel>(settings);

// Maps BlogSettings --> BlogSettingsModel by populating the given target instance.
var model = new BlogSettingsModel();
MiniMapper.Map(settings, model);
```

## MapperFactory

The `MapperFactory` is a static factory that can create custom type mapper instances ([`IMapper<TFrom, TTo>`](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/ComponentModel/IMapper.cs)).

{% hint style="success" %}
Use `MapperFactory` when:

* source and target type are very different
* you need more control over mapping
{% endhint %}

Upon initialization, `MapperFactory` automatically scans for all concrete `IMapper<TFrom, TTo>` classes in all loaded assemblies. Because the mapper is DI-enabled it can depend on any registered service.

You can register multiple mappers for the same type pair without any conflicts. When a mapping operation is requested, the `MapperFactory` will combine all the registered mappers into a composite mapper. This composite mapper will internally delegate the object mapping to each individual mapper, ensuring that all the registered mappers contribute to the final mapping result.

<table><thead><tr><th width="420">Call</th><th>Description</th></tr></thead><tbody><tr><td><code>MapperFactory.GetMapper&#x3C;TFrom, TTo>()</code></td><td>Resolve a mapper instance (either single, composite or generic)</td></tr><tr><td><code>GetRegisteredMapper&#x3C;TFrom, TTo>()</code></td><td>Resolve a mapper instance or return null if no mapper is registered for the given type pair.</td></tr><tr><td><code>Map*()</code></td><td>Map object instances (dynamic parameters are allowed)</td></tr></tbody></table>

{% hint style="info" %}
If no mapper is found for a specific mapping operation, a generic mapper is used, which internally delegates object mapping to `MiniMapper`.
{% endhint %}

### Implementing a mapper

To add a mapper using `MapperFactory`, create a class that implements `IMapper<TFrom, TTo>`. There is nothing wrong with having more than one interface implemented in a single class (e.g. `IMapper<News, NewsModel>` and `IMapper<NewsModel, News>`). There is no need for the mapper to be registered in DI.

{% hint style="info" %}
It is good practice to keep the model and mapper classes in the same file.
{% endhint %}

<pre class="language-csharp"><code class="lang-csharp">public class NewsMapper :
    IMapper&#x3C;News, NewsModel>,
    IMapper&#x3C;NewsModel, News>
{
    public NewsItemMapper()
    {
         // Pass dependencies if required
    }

    public Task MapAsync(News from, NewsModel to, dynamic parameters = null)
    {
        // ... map News --> NewsModel ...

<strong>        return Task.CompletedTask;
</strong>    }

    public Task MapAsync(NewsModel from, News to, dynamic parameters = null)
    {
        // ... map NewsModel --> News ...

        return Task.CompletedTask;
    }
}
</code></pre>

The following code shows different ways to map an entity to a model.

```csharp
News entity = /* get entity instance somehow */;
NewsModel model = new NewsModel();

// Get the mapper = an instance of "NewsMapper" above,
// because "NewsMapper" implements "IMapper<News, NewsModel>"
var mapper = MapperFactory.GetMapper<News, NewsModel>();

// Map News --> NewsModel
await mapper.MapAsync(entity, model);
// - OR, short way without resolving the mapper first -
await MapperFactory.MapAsync(entity, model);

// Map News --> NewsModel with parameters
await mapper.MapAsync(entity, model, new { Option1 = true, Option2 = "stuff" });
```
