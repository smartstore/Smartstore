# AGENTS.md — Smartstore

Smartstore is a modular, open-source e-commerce platform built on ASP.NET Core.
This file states the rules that apply to **every** change in this repository.

`dev-docs/` is the authoritative knowledge base. Consult the relevant page before
implementing anything non-trivial — do not reverse-engineer a subsystem that is
already documented.

## Non-negotiable rules

- **Never edit anything under `src/Smartstore.Web/Modules/`.** That directory is
  gitignored build output — compiled module assemblies plus *copies* of each module's
  `Views` and `wwwroot`. Source files live in `src/Smartstore.Modules/<Name>/`. A search
  for a `.js`, `.scss` or `.cshtml` file will match both; always take the
  `src/Smartstore.Modules/` hit. Edits to the copy are silently overwritten on the next
  build.
- **English only** in code, comments and XML docs. Never write German comments.
- **Do not rewrite existing XML documentation comments** (including `<inheritdoc/>`)
  while changing code. Preserve them verbatim; touch them only when the documentation
  itself is wrong. New public members do get docs.
- **Database changes use FluentMigrator, not EF Core migrations.**
- **Sass is compiled with libsass.** Use `@import`. Never `@use` or `@forward`, and no
  other Dart-Sass-only feature.
- **No new heavy client-side dependency** without asking first.
- **Do not add eager O(n) work to constructors.** Where work was previously deferred,
  keep it deferred; justify any new up-front cost.

## Architecture

| Project | Responsibility |
|---|---|
| `src/Smartstore` | Base library: caching, collections, IO, routing, engine, utilities |
| `src/Smartstore.Core` | Domain layer: catalog, checkout, content, platform. Entry point `CoreStarter.cs` |
| `src/Smartstore.Web` | ASP.NET Core front-end app: controllers, views, areas, `wwwroot`, `Program.cs` |
| `src/Smartstore.Web.Common` | Shared web infrastructure: bootstrapping, MVC components, Razor helpers, bundling, theming |
| `src/Smartstore.Data` | Database provider packages (SQL Server, MySQL, PostgreSQL, SQLite) |
| `src/Smartstore.Modules` | Optional feature modules (payment, shipping, auth, export, …) |
| `src/Smartstore.Build` | Central MSBuild properties and targets used by all projects |
| `test` | `Smartstore.Tests`, `Smartstore.Core.Tests`, `Smartstore.Web.Tests`, `Smartstore.Test.Common` |

Architecture follows Domain-Driven Design. See
`dev-docs/getting-started/architecture-overview.md` and
`dev-docs/getting-started/source-code-organization.md`.

## Tech stack

- **.NET 10 / C#**, **ASP.NET Core 10**, **EF Core 10** (`net10.0`, set centrally in
  `src/Smartstore.Build/Smartstore.Common.props`)
- **Nuke** for build automation (`build.cmd` / `build.ps1` / `build.sh`)
- Bootstrap (4/5 hybrid), Sass via libsass, jQuery, Select2, Vue.js
- DotLiquid (Liquid) for email and content templates
- Docker / Docker Compose for app and database containers

## Where to look things up

| Topic | Read first |
|---|---|
| Dependency injection | `dev-docs/getting-started/dependency-injection.md`, `dev-docs/advanced/di-best-practices.md` |
| Data access & domain | `dev-docs/getting-started/data-access.md`, `dev-docs/getting-started/domain.md` |
| Database migrations | `dev-docs/framework/platform/database-migrations.md` |
| Modules & providers | `dev-docs/framework/platform/modularity-and-providers.md` |
| Building a module | `dev-docs/compose/modules/getting-started-with-modules.md` |
| Localization | `dev-docs/framework/content/localization.md` |
| Hooks & events | `dev-docs/framework/platform/hooks.md`, `dev-docs/framework/platform/events.md` |
| Caching & output cache | `dev-docs/framework/platform/caching.md`, `dev-docs/framework/platform/output-cache.md` |
| Security & permissions | `dev-docs/framework/platform/security.md` |
| Import / Export | `dev-docs/framework/platform/import.md`, `dev-docs/framework/platform/export.md` |
| Widgets & Page Builder | `dev-docs/framework/content/widgets.md`, `dev-docs/framework/content/page-builder-and-blocks.md` |
| Theming & bundling | `dev-docs/compose/theming/`, `dev-docs/compose/theming/asset-bundling.md` |
| Performance | `dev-docs/advanced/performance-guide.md` |
| Deployment & build | `dev-docs/getting-started/deployment-and-build.md`, `build/README.md` |

## Coding conventions

`.editorconfig` is authoritative for formatting and is enforced by the IDE. The rules
that matter most:

- 4 spaces, CRLF, Allman braces (`csharp_new_line_before_open_brace = all`).
- `var` only when the type is apparent from the right-hand side; not for built-in types.
- PascalCase for types and non-field members, interfaces prefixed with `I`.
- Acronyms of two or more letters are fully capitalized, following BCL precedent
  (`IPAddress`, `HTTP`, `XML`) — not `IpAddress`.

Beyond formatting:

- **Nullability:** enable `#nullable enable` at file level in new or modified files,
  primarily in interfaces and contracts for IntelliSense — but only where it earns its
  keep. Avoid `!` except at clearly justified boundaries.
- **Async:** use `async`/`await`, suffix async methods with `Async`, never `.Result`
  or `.Wait()`.
- **DI:** constructor injection. No service locator.
- **Validation:** guard clauses for arguments; `ArgumentException` family for argument
  errors, domain-specific exceptions for domain errors.
- **Reuse existing utilities** before writing your own — e.g.
  `Smartstore.Utilities.HashCodeCombiner` instead of an ad-hoc `GetHashCode`.

## Data and localization

- Schema changes are **FluentMigrator** migrations. EF Core migrations are not used.
- New or changed locale resources go into
  `src/Smartstore.Core/Migrations/SmartDbContextDataSeeder.cs`, using the `AddOrUpdate`
  overload `(key, value, deValue, hint, deHint)` — `value`/`deValue` are the short
  visible labels, `hint`/`deHint` the help tooltips. That overload appends `.Hint` to
  the key itself, so pass the bare key and never write the suffix yourself.
- Every new admin model property must carry `[LocalizedDisplay]` with a resource key.
  Set the shared prefix once on the class and use `*` on each property, so the keys
  cannot drift apart:

  ```csharp
  [LocalizedDisplay("Plugins.Sms.Clickatell.Fields.")]
  public class ConfigurationModel : ModelBase
  {
      [LocalizedDisplay("*Enabled")]
      public bool Enabled { get; set; }
  }
  ```

  Each visible property needs both a `Fields.<Prop>` label and a `Fields.<Prop>.Hint`
  tooltip, in English *and* German.

## Client code

- Prefer Bootstrap utility classes; use Sass variables and mixins for theming.
- Keep JavaScript lean and prefer progressive enhancement.
- The asset pipeline is dynamic. Do not assume a static stylesheet `<link>` — CSS is
  generated at runtime. Reference generated asset manifests, pipeline helpers, or
  runtime-injected links instead.

## Modules

A module lives in `src/Smartstore.Modules/<Name>` and contains at minimum a
`module.json`, a `Module.cs`, a `Startup.cs`, and its `Providers`, `Controllers`,
`Models`, `Views`, `Localization` and `wwwroot` folders. Copy the structure and naming
of the blueprint rather than inventing a new layout.

| Module type | Must implement | Blueprint |
|---|---|---|
| Payment | `IPaymentMethod` | `src/Smartstore.Modules/Smartstore.PayPal` |
| Shipping | `IShippingRateComputationMethod` | `src/Smartstore.Modules/Smartstore.Shipping` |
| Authentication | `IExternalAuthenticationMethod` | `src/Smartstore.Modules/Smartstore.Facebook.Auth` |
| Export | `ExportProviderBase` / `IExportProvider` | `src/Smartstore.Modules/Smartstore.Google.MerchantCenter` |

Without the required interface the host will not recognize the assembly as a valid
module of that type.

## Build and test

```bash
dotnet build Smartstore.sln -c Release
dotnet test  Smartstore.sln -c Release --logger trx
dotnet publish src/Smartstore.Web/Smartstore.Web.csproj -c Release -o ./publish
```

Building the solution detects every module in `src/Smartstore.Modules/`, compiles it,
and copies the result into `src/Smartstore.Web/Modules/` — the directory the runtime
loads modules from. It is build output: irrelevant during development, safe to delete at
any time, and **never** a place to edit files.

### Nuke

`build.cmd` (Windows) and `build.sh` (Linux/macOS) at the repository root run the Nuke
build, defined in `src/Smartstore.Build/Smartstore.Build/Build.cs`. Its default solution
is `Smartstore.sln` (`.nuke/parameters.json`).

Targets: `Clean`, `Restore`, `Compile`, `Test`, `Deploy`, `Zip`, `GenerateSbom`.
Configurations: `Debug`, `DebugNoRazorCompile`, `Release`.

```bash
build --target Compile --configuration DebugNoRazorCompile
build --target Deploy  --configuration Release --runtime win-x64
```

`DebugNoRazorCompile` skips Razor compilation and is noticeably faster for iterating on
non-view code. `Deploy` publishes a **self-contained** build of `Smartstore.Web` to
`build/artifacts/Community.{Version}.{Runtime}/` and triggers `Zip` and `GenerateSbom`.

The ready-made wrappers in `build/` are one-liners around the same command:

| Script | Purpose |
|---|---|
| `build/build.{win-x64,win-x86,linux-x64,osx-x64}.cmd` | Self-contained release build for that runtime |
| `build/dockerize.{linux,windows}[.nobuild].sh` | Build a Docker image; `.nobuild` reuses an existing artifact |
| `build/compose.{mysql,sqlserver,postgres}.sh` | Compose app plus database container |
| `build/create-bom-cyclonedx.bat` | CycloneDX SBOM |

Details in `build/README.md`. On CI, set `MSBUILDTERMINALLOGGER=off` to avoid
`InternalLoggerException` on .NET CI images.

> Working inside the private `Smartstore.Full` workspace? Build
> `Smartstore.Full-sym.sln` instead, and follow `../AGENTS.md` for the rules that
> apply there.

## Known design decisions

Do not "fix" these; they are deliberate:

- `Multimap<TKey, TValue>`: the indexer auto-creates and stores a value collection for
  missing keys. This is a convenience that removes repetitive code. Improve around it,
  never remove it. `TValue` is treated as nullable under NRT.
- `LazyMultimap<T>` is only ever used scoped and never shared, so it does not need to
  be thread-safe. Prefer lightweight changes over heavy locking.
- AI model metadata levels are `0 = Instant`, `1 = Balanced`, `2 = Deep Reasoning`.
  Favour common, cost-efficient text-generation models and keep at least one preferred
  level-0 model listed. Never mark a level-2 model as preferred. Remove deprecated
  model IDs rather than leaving placeholders. When reasoning about what a model can
  actually do, trust the vendor's official API documentation over this local metadata.
