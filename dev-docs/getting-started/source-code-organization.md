---
description: Structure of the Smartstore repository and solution
---

# Source code organization

Smartstore's repository keeps concerns separated and supports modular development.

## Solutions

* `Smartstore.sln` – main solution containing application projects, modules, and tests.
* `Smartstore.Tools.sln` – separate solution for tooling and utilities.

## Top-level directories

| Path     | Purpose                                                                                           |
| -------- | ------------------------------------------------------------------------------------------------- |
| `src/`   | Application source code: core libraries, domain layer, web host, database providers, and modules. |
| `test/`  | Unit and integration tests mirroring the structure of `src/`.                                     |
| `build/` | Build automation scripts and configuration.                                                       |
| `tools/` | Additional tools used during development and build.                                               |

## `src/` breakdown

* **Smartstore** – cross-cutting infrastructure (caching, I/O, routing, engine runtime).
* **Smartstore.Core** – domain models, business logic, migrations, and service abstractions.
* **Smartstore.Web** – ASP.NET Core host with controllers, views, and configuration.
* **Smartstore.Web.Common** – shared web helpers, MVC components, theming, and bootstrapping.
* **Smartstore.Modules** – optional features like payment or shipping providers. Each module is a separate project with its own resources and optional migrations.
* **Smartstore.Data.**\* – database provider packages (SQL Server, MySQL, PostgreSQL, SQLite) keeping data access provider-specific.

During a build, modules from `src/Smartstore.Modules` are compiled and copied to `src/Smartstore.Web/Modules` for runtime loading.

## Tests

Test projects reside in the `test/` directory and follow the layout of `src/`. Typical projects include `Smartstore.Tests`, `Smartstore.Core.Tests`, and module-specific tests. All tests run via:

```bash
dotnet test Smartstore.sln
```
