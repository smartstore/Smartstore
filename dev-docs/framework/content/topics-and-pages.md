# Topics & Pages

## Overview

Topics provide lightweight CMS pages for static content such as "About us" or legal notices. Each topic defines a unique system name, HTML body and optional metadata. When you assign an SEO slug the topic becomes reachable as its own page. Topics support per-language content, store and role restrictions, sitemap inclusion and optional password protection.

## Editing

Administrators manage topics under **Content > Topics**. Key fields include:

* **System name** – stable identifier used in code and link expressions.
* **Body / Intro** – HTML content edited through the WYSIWYG editor.
* **Meta data** – `SeName`, `MetaTitle`, `MetaDescription` and keywords.
* **Render as widget** – also display the topic in specified zones.
* **ACL / Stores** – limit visibility to particular customer roles or stores.

## Rendering a topic

Embed a topic in any view with the `TopicBlock` view component:

```csharp
@await Component.InvokeAsync("TopicBlock", new { systemName = "AboutUs" })
```

A topic with a saved slug renders as a standalone page handled by `TopicController.TopicDetails`. Menus and other components can link to it via the [link resolver](../advanced/linkresolver.md):

```csharp
builder.Item("About us", i => i.Url("topic:AboutUs"));
```

```
var aboutUrl = await Url.TopicAsync("AboutUs");
```

## Widget mode

When _Render as widget_ is enabled, Smartstore turns the topic into a `TopicWidget` and injects it into each listed [widget zone](widgets.md#zones). Wrapper options (`WidgetWrapContent`, `WidgetShowTitle`, `WidgetBordered`) control presentation, while `Priority` defines sort order within the zone.

## Programmatic access

Topics reside in `SmartDbContext.Topics`. Use `TopicQueryExtensions.ApplyStandardFilter()` to respect publication, store and ACL settings when querying:

```csharp
var topics = await _db.Topics
    .ApplyStandardFilter()
    .OrderBy(t => t.SystemName)
    .ToListAsync();
```
