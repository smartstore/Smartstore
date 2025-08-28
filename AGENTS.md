# AGENTS Guidelines for Smartstore

Smartstore is a modular, open-source e-commerce platform built on ASP.NET Core.
This document sets **non-negotiable rules** and **operational checklists** for code-generating agents working on the repository.

## Project Architecture (overview)

- Smartstore
	- Base library with cross-platform components such as caching, collections, IO, routing, and engine functionality.
	- Provides foundational utilities, extension methods, and infrastructure classes used by higher layers.
- Smartstore.Core
	- Domain layer containing business logic for catalog, checkout, content, and platform features.
	- Includes migrations, data models, and the entry point CoreStarter.cs, which registers core services.
- Smartstore.Web
	- ASP.NET Core application that delivers the main web front-end.
	- Hosts controllers, views, areas, static assets (wwwroot), plus Program.cs and configuration files (appsettings.json).
- Smartstore.Web.Common
	- Shared web infrastructure: bootstrapping, MVC components, Razor helpers, bundling, and theming support.
	- Acts as a bridge between Smartstore.Core and the web app, supplying routing helpers, tag helpers, filters, and more.
- Modules
	- Extensible plugin structure for optional features (e.g., payment providers, shipping methods, authentication integrations).
	- Each subfolder houses a standalone module with its own project file and resources.
- Data 
	- Database-provider packages (e.g., Smartstore.Data.MySql, Smartstore.Data.PostgreSql, Smartstore.Data.SqlServer, Smartstore.Data.Sqlite).
	- Encapsulates migrations and provider logic, keeping the core application database-agnostic.
- Tests
	- Collection of test projects (Smartstore.Tests, Smartstore.Core.Tests, Smartstore.Web.Tests, Smartstore.Test.Common).
	- Covers unit and integration scenarios for the kernel, web layer, and modules.
- Build
	- Scripts and utilities for building and containerizing the project (platform-specific build.*.cmd/sh along with Docker and Compose scripts).
	- Includes the src/Smartstore.Build folder with central MSBuild properties/targets used across builds and modules.

## Tech stack

- Backend & Core Frameworks
	- .NET 9 / C# – primary language and runtime.
	- ASP.NET Core 9 – web framework powering the platform.
	- Entity Framework Core 9 – ORM for database access.
	- Domain‑Driven Design – guiding architectural approach.
- Frontend & UI
	- Bootstrap (4/5 hybrid) – responsive layout and theming.
	- Sass – styling with variables and mixins.
	- jQuery & Select2 – supplemental UI interactions.
	- Liquid (DotLiquid) – template engine for emails and content.
	- Vue.js – reactive components and admin UI.
- Data Storage
	- SQL Server, MySQL, PostgreSQL, SQLite – officially supported database providers via modular packages.	
- Build & Deployment
	- Nuke – build automation (scripts under build/).
	- Docker/Docker Compose – containerization for app and database.
	- .NET CLI – restore, build, and test orchestration.

## Coding Conventions

### General
- **Comments:** English only. Keep them minimal and precise.
- **Nullability:** `#nullable enable`. Avoid `!` except at clearly justified boundaries.
- **Async:** Use `async`/`await`. Suffix async methods with `Async`. Avoid `.Result`/`.Wait()`.
- **DI:** Prefer constructor injection. No service locator anti-pattern.

### C# Style
- Guard clauses for argument validation. Throw `ArgumentException` families; domain errors -> domain-specific exceptions.

### Client Code
- Bootstrap utility classes where possible; Sass variables/mixins for theming.
- Keep JS lean; prefer progressive enhancement. No new heavy dependencies without approval.

## Deployment & Build

- Build: `dotnet build <Solution.sln> -c Release`
- Test: `dotnet test <Solution.sln> -c Release --logger trx`
- Publish: `dotnet publish src/Smartstore.Web/Smartstore.Web.csproj -c Release -o ./publish`

## Module Types

### Payment
- Use src/Smartstore.Modules/Smartstore.PayPal as the canonical blueprint when implementing new payment functionality.
- Each payment provider must implement the IPaymentMethod interface. Without this inheritance, the system will not recognize the plugin as a valid payment module.
- Follow the structure, naming conventions, and configuration patterns exhibited in the PayPal example for consistency across payment integrations.

### Export
- Purpose: Convert catalog or order data into formats consumable by external platforms (e.g., product feeds).
- Typical responsibilities: define an export provider, configuration UI, and deployment profile for scheduled feed generation.
- Implementation: Derive the provider from ExportProviderBase (or implement IExportProvider) to plug into Smartstore’s data‑exchange pipeline; package the module with a module.json, Startup.cs, and optional migrations.
- Example module: src/Smartstore.Modules/Smartstore.Google.MerchantCenter – exports products as an XML feed compatible with Google Merchant Center, including admin configuration screens, a feed provider (GmcXmlExportProvider), and profile management for automated uploads.

### Shipping
- Use src/Smartstore.Modules/Smartstore.Shipping as the canonical reference when implementing new shipping providers.
- Every shipping provider must implement the IShippingRateComputationMethod interface; otherwise the system will not treat the plugin as a valid shipping module.
- Follow the structure, configuration patterns, and naming conventions from the example to ensure consistent integration and admin configuration behavior across all shipping implementations.

### Authentication
- Use src/Smartstore.Modules/Smartstore.Facebook.Auth as the canonical blueprint for building authentication providers.
- Each auth provider must implement the IExternalAuthenticationMethod interface; otherwise the system will not recognize the plugin as a valid authentication module.
- Mirror the structure, configuration patterns, and naming conventions from the Facebook example (e.g., module.json, Startup.cs, Module class, configuration UI, view component) to ensure consistent integration and admin setup across all authentication modules.