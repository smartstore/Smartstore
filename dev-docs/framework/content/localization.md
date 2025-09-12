# Localization

Smartstore is an application where different languages can be easily downloaded and activated in the admin area. Over 30 languages are available for download in the backend via **Configuration > Regional Settings > Languages**. Activated languages are displayed in the frontend as well as in the backend.
Administrators can edit translations in the back office or import and export XML packages.

## Overview
Smartstore ships with a database-driven localization system that covers UI strings and entity properties. Each request is assigned a working language determined from the user preference, current store or route culture.

## Languages
`ILanguageService` manages configured languages. The current language is available from the work context:

```csharp
var language = _workContext.WorkingLanguage;
```

Key members of `ILanguageService` include:

- `IsMultiLanguageEnvironment(storeId)` – detect if a store has more than one published language.
- `GetAllLanguages(includeHidden, storeId, tracked)` / `GetAllLanguagesAsync(...)` – load all languages, optionally scoped to a store and including hidden ones.
- `IsPublishedLanguage(idOrSeoCode, storeId)` / `IsPublishedLanguageAsync(...)` – check whether a language with the given ID or SEO code is published.
- `GetMasterLanguageSeoCode(storeId)` / `GetMasterLanguageSeoCodeAsync(...)` – return the SEO code of the first active language.
- `GetMasterLanguageId(storeId)` / `GetMasterLanguageIdAsync(...)` – return the ID of the first active language.

## Resource strings
Text resources live in the `LocaleStringResource` table and can be retrieved through `ILocalizationService` or the `T` helper in controllers and views:

```csharp
var greeting = await _localizationService.GetResourceAsync("Common.Welcome");
```

```razor
@T("Common.Welcome")
```

## Annotating models
Attach the `LocalizedDisplay` attribute to model properties so labels pull their text from the resource table. By convention the resource key matches the property name:

```csharp
[LocalizedDisplay("Admin.Domain.Fields.MySetting")]
public bool MySetting { get; set; }
```

Decorating the class with `LocalizedDisplay` lets you provide a prefix so the properties can use shorter keys:

```csharp
[LocalizedDisplay("Admin.Domain.Fields.")]
public class ConfigurationModel : ModelBase
{
    [LocalizedDisplay("*MySetting")]
    public bool MySetting { get; set; }
}
```

### Display hints
To show a tooltip next to a label, create a second resource with the `.Hint` suffix or specify it inline:

```xml
<LocaleResource Name="Admin.Domain.Fields.MySetting.Hint">
    <Value>My setting does cool things</Value>
</LocaleResource>
```

```cshtml
<smart-label asp-for="MySetting" />
<smart-label asp-for="MySetting" sm-hint="Alternative way to set an explanation hint." />
```

### Enumerations
Values of enums are localized under the `Enums` namespace. Use `Html.GetLocalizedEnumSelectList` to populate a `<select>` element:

```csharp
public enum MyEnum
{
    Value1,
    Value2
}
```

```xml
<LocaleResource Name="Enums" AppendRootKey="false">
  <Children>
    <LocaleResource Name="MyEnum.Value1">
      <Value>Value 1</Value>
    </LocaleResource>
    <LocaleResource Name="MyEnum.Value2">
      <Value>Value 2</Value>
    </LocaleResource>
  </Children>
</LocaleResource>
```

```cshtml
<select asp-for="MyEnumSetting" asp-items="Html.GetLocalizedEnumSelectList(typeof(MyEnum))"></select>
```

## Localizing entities

Entity types that support translations implement `ILocalizedEntity`. Each property that should be translated must be annotated with `[LocalizedProperty]` so the framework can persist and display language-specific values:

```csharp
public class Product : BaseEntity, ILocalizedEntity
{
    [LocalizedProperty]
    public string Name { get; set; }

    [LocalizedProperty]
    public string ShortDescription { get; set; }
}
```

`LocalizedPropertyAttribute` marks a property as translatable. The system scans these attributes to render language tabs in the admin UI, include fields in import/export packages, and detect changes for cache invalidation. Setting `Translatable = false` keeps the property in the metadata but hides it from the translation interface.

Entities can additionally be decorated with `LocalizedEntityAttribute` to provide a custom key group or filter predicate for tooling and import/export routines.

Use `ILocalizedEntityService` to read and store per-language values:

```csharp
var localizedName = await _localizedEntityService.GetLocalizedAsync(product, p => p.Name, language.Id);
await _localizedEntityService.SaveLocalizedValueAsync(product, p => p.Name, "Localized name", language.Id);
```

### Descriptors and batch loading

A plugin for translations can gather text without querying individual modules. The
`ILocalizedEntityDescriptorProvider` exposes metadata for every type that implements
`ILocalizedEntity`. Each descriptor lists the entity's key group and all properties
marked with `[LocalizedProperty]` or contributed by `LocalizedSettingsLoader` and
`LocalizedCookieInfoLoader`.

`ILocalizedEntityLoader` uses these descriptors to load only the localizable properties
for a given entity type. It supports paging so large data sets can be processed in
small batches:

```csharp
var pager = _loader.LoadGroupPaged(descriptor, pageSize: 128);

while ((await pager.ReadNextPageAsync()).Out(out var batch))
{
    // process up to 128 entities with only their localizable fields populated
}
```

### LocalizedProperty metadata

The `LocalizedProperty` table now carries audit information and translation metadata:

| Property | Purpose |
| --- | --- |
| `IsHidden` | Marks master-language records that should not appear in the UI; translation plugins set this to `true` to hide redundant source text. |
| `CreatedOnUtc`, `CreatedBy`, `UpdatedOnUtc`, `UpdatedBy` | Filled by audit hooks; plugins that update records should set `UpdatedOnUtc` and `UpdatedBy` to keep their author information. |
| `TranslatedOnUtc` | Timestamp of the last translation. Entries with a `null` or older value can be picked up in the next translation run. |
| `MasterChecksum` | Hash of the source text used for the translation. If the hash changes the target value must be retranslated. |

### Tracking changes

To keep master texts in sync, a translation plugin can register an `AsyncDbSaveHook<ILocalizedEntity>`.
The hook watches for changes to localized properties and either inserts missing
`LocalizedProperty` rows or clears `TranslatedOnUtc` when a value is modified. This
allows incremental translation sessions that only handle entities which actually changed.

Process large translation jobs in batches, detach entities between saves and use
`async`/`await` for all I/O operations to reduce memory pressure and improve throughput.

## Module localization
Modules bundle resource files and register them during installation. See the [localizing modules](../../compose/modules/localizing-modules.md) guide for details.

## Adding resources via migrations
When the core or a module introduces new text resources, they can be added through a FluentMigrator migration. Implement `ILocaleResourcesProvider` and `IDataSeeder<SmartDbContext>` on the migration, call `MigrateLocaleResourcesAsync` in `SeedAsync`, and use `LocaleResourcesBuilder` to insert or update keys:

```csharp
[MigrationVersion("2024-03-29 18:00:00", "Core: my feature migration")]
internal class MyFeatureMigration : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
{
    public async Task SeedAsync(SmartDbContext context, CancellationToken ct = default)
        => await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);

    public void MigrateLocaleResources(LocaleResourcesBuilder builder)
    {
        builder.AddOrUpdate("ShoppingCart.SelectAllProducts",
            "Select all products",
            "Alle Artikel auswählen");
    }
}
```

During migrations these resource entries are merged into `LocaleStringResource` for all languages. 