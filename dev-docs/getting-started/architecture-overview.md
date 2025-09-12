---
description: Domain, DB, Service, Model, Validation, UI, wwwroot
---

# Architecture overview

Smartstore uses a layered architecture built on ASP.NET Core and Entity Framework Core. Responsibilities are split across distinct layers and modules to support clean separation of concerns.

## Domain layer

Defines entity classes and value objects that model the business. Entities live in `Smartstore.Core` and are mapped with EF Core to the database. Domain logic is kept close to the entities.

## Data layer

Data access is performed through `SmartDbContext`, repositories, and provider packages under `src/Smartstore.Data.*`. Each provider supplies migrations and SQL‑dialect specific services, keeping the core application database agnostic.

## Service layer

Application services orchestrate domain operations and encapsulate business rules. Services are registered via dependency injection (Microsoft DI with Autofac) and expose asynchronous APIs that are consumed by controllers or other services.

## Models and validation

View models and DTOs[^1] are tailored for the UI and admin areas. Input is validated using data annotations and FluentValidation, ensuring that only valid data reaches the service layer.

## UI layer

`Smartstore.Web` hosts the ASP.NET Core MVC application. Controllers handle HTTP requests, interact with services, and render Razor views. Tag helpers, filters, and theming support come from `Smartstore.Web.Common`.

## Static web assets

Static files such as JavaScript, CSS, and images reside under `wwwroot`. During the build process assets are bundled and minified. Modules can ship their own `wwwroot` folder, and assets are copied to the application at runtime.

## Extensibility

Optional features are packaged as modules (`src/Smartstore.Modules`). A module may contribute domain entities, services, controllers, and UI assets, enabling isolated development and deployment.

{% hint style="info" %}
Database provider modules (`src/Smartstore.Data.*`) supply EF Core migrations and provider-specific services, letting the core remain provider independent.
{% endhint %}

[^1]: Data transfer object: an object that carries data between processes.
