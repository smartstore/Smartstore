# Installation seed data

This folder holds the data that the installer writes into a new shop database:
mandatory data (currencies, countries, tax categories, settings, …) and the optional
sample catalog (categories, manufacturers, products, …), each in all installable languages.

It replaces the seed data that used to live in C# (`InvariantSeedData`, `EnUSSeedData`,
`DeDESeedData` in `src/Smartstore.Core/Platform/Installation`). C# only orchestrates the
installation (`InstallationDataSeeder`) and wires up database IDs; all data lives here.

## Folder layout

```
App_Data/Seed/
├── base/                         Base data, English, valid for every language
│   ├── core/                     Mandatory data
│   └── samples/                  Sample data (only installed on request)
├── cultures/
│   └── {culture}/                e.g. de-DE
│       ├── culture.json          Makes the culture installable (see below)
│       ├── resources/            Locale resources (*.smres.xml, head.txt), see below
│       ├── core/…                Overlays: same relative paths as in base/
│       └── samples/…
└── media/                        Sample images and downloads
```

`base/core/settings.json` is the only object file: `{ "TaxSettings": { "TaxBasedOn": "ShippingAddress" } }`.
It lists only values that differ from the defaults of the settings class. Values that hold
the ID of a seed record are written as `"$ref:{EntityType}:{key}"`, e.g. `"$ref:Currency:EUR"`.
Its overlay has the same shape and is merged field by field.

- **Base files** are JSON **arrays** of records.
- **Overlay files** are JSON **objects** and live under the same relative path as their
  base file, e.g. `base/core/currencies.json` ↔ `cultures/de-DE/core/currencies.json`.
  An overlay without a base file is an error.
- Files starting with `_` (e.g. `_defaults.json`) are never read as record files.
- JSON may contain `//` comments and trailing commas.

## Keys

Every record has a **stable, language-independent key**. Keys are never translated and are
compared case-insensitively.

- Where the entity has a natural business key, that field **is** the key
  (e.g. `sku`, `alias`, `twoLetterIsoCode`, `currencyCode`, `systemName`, `systemKeyword`).
- Otherwise the record has a synthetic `key` field in kebab-case ASCII,
  e.g. `"key": "in-store-pickup"`. It is not stored in the database.
- Which field is the key of an entity type is declared by its `SeedEntityDescriptor` in C#.
- Child records (e.g. the states of a country) are keyed within their parent and can be
  referenced as `{parentKey}/{childKey}`, e.g. `DE/BW`.

## References

Records refer to each other **by key only**, never by name or database ID. Reference fields
are named after the target without an `Id` suffix:

```jsonc
"taxCategory": "standard",
"categories": [ "golf", "sports" ]
```

The descriptor declares which fields are references. An unknown key fails the installation
with the file, record and field in the message.

## Overlay rules

An overlay object contains record keys (partial records) and directives:

| Entry | Effect |
|---|---|
| `"{key}": { … }` | Merged into the record with that key. An unknown key is an **error** (use `$add`). |
| `"$all": { … }` | Merged into every record, before the keyed patches. |
| `"$remove": [ "key", … ]` | Removes records. |
| `"$add": [ { … }, … ]` | Appends complete new records (after all other directives). |
| `"$mode": "replace"` + `"$items": [ … ]` | Drops all base records and uses `$items` instead. No keyed patches allowed. |
| `"$aliases": { "old": "new" }` | Lets references to `old` resolve to `new` (file level only, single hop). |

Merging inside a record:

- Nested objects are merged field by field.
- Declared **child collections** (e.g. `stateProvinces`) accept either an overlay object
  (merged by child key, same directives except `$aliases`) or an array (replaces the collection).
- All other values, including arrays of keys such as `categories`, are **replaced**.
- `null` sets a value to null.

Precedence, lowest first:

```
_defaults.json  →  base record  →  overlay $all  →  overlay keyed patch
```

A descriptor may declare a defaults file (e.g. `samples/catalog/products/_defaults.json`).
A culture can overlay the defaults file under the same path.

Example — Germany gets German tax categories, but products keep referring to `apparel`:

```jsonc
// cultures/de-DE/core/tax-categories.json
{
  "$mode": "replace",
  "$aliases": { "apparel": "standard", "books": "reduced" },
  "$items": [
    { "key": "standard", "name": "Normal" },
    { "key": "reduced",  "name": "Ermäßigt" },
    { "key": "tax-free", "name": "Befreit" }
  ]
}
```

## Long texts (`@file`)

A **top-level** string field with the value `"@file"` is replaced by the content of
`{jsonFileName}/{key}.{field}.html` next to the JSON file that sets the value:

```
base/samples/catalog/products/sports.json            "fullDescription": "@file"
base/samples/catalog/products/sports/P-7004.fullDescription.html
```

- `"@file:relative/path.html"` names the file explicitly (relative to the JSON file).
- In an overlay, the file is looked up next to the overlay file. If the overlay does not set
  the field, the base text is used.
- Characters that are invalid in file names (`< > : " / \ | ? *`) are replaced by `_` in the key.
- `@file` in nested fields is an error.

Rule of thumb: HTML longer than one line or about 200 characters goes into its own file.

## Locale resources

`cultures/{culture}/resources/` holds the complete UI resources of a language: one or more
`*.smres.xml` files and `head.txt`. `head.txt` contains the version of the newest migration whose
resources are included; resources of newer migrations are seeded on top during installation.

The installer imports these files **directly after creating the language** and before any
other data, so that seed records can reference resources (see below).

> **Release process:** the resource files are updated for each release. They used to live in
> `App_Data/Localization/App/{de|en}` and now live here.

## Resource references

A string value can reference a locale resource of the installed language:

```jsonc
"title": "@resource:Menu.ServiceMenu",             // whole value
"title": "Footer - {@resource:Footer.Info}"       // embedded, any number of times
```

References are resolved when the record is created, in any field and in child records. An
unknown key fails the installation. Resolving fails as well if the resources have not been
seeded yet; the installer guarantees the order.

## Further value conventions

| Convention | Example | Meaning |
|---|---|---|
| Relative date | `"specialPriceEndDateTimeUtc": "@now+1M"` | Installation time plus/minus months (`M`) or days (`d`). |
| Media reference | `"product/allstar_converse.jpg"` or `{ "file": "…", "name": "allstar-converse-charcoal" }` | Path below `media/`. `name` sets the media file name (SEO name) when it differs from the name derived from the path. |
| Publish condition | `"publishedWhen": "manufacturers"`, `"publishedWhen": "setting:CatalogSettings.CompareProductsEnabled"` | Menu items: published only if manufacturers exist / the setting is true. |
| Variant value names | `cultures/de-DE/samples/catalog/attribute-value-names.json`: `{ "black": "Schwarz" }` | Translates all variant attribute values with that alias. A product overlay can still override a single value. |

### Product records

| Field | Form |
|---|---|
| `categories`, `manufacturers` | Key, or `{ "category": key, "displayOrder": n, "isFeaturedProduct": bool, "isSystemMapping": bool }` (display order defaults to 1). |
| `pictures` | Media reference, optionally with `"displayOrder"` (default 1). `"from": "{sku}"` reuses the media file of that product's picture with the same path instead of creating a new one. |
| `specifications` | List without keys (the same option may occur twice); an overlay replaces the whole list. |
| `variantAttributes` | Keyed by `productAttribute` alias; `values` keyed by `alias`. A value `mediaFile` is a picture of the product (by path); any other path creates a separate media file (once per path). |
| `combinations` | List without keys (SKUs are not unique); `attributes` maps attribute alias to value alias(es). `"pictures": []` stores an empty media list, a missing field none. |
| `bundleItems` | Keyed by `product` (SKU). Created after all products. |
| `parentGroupedProduct`, `sampleDownload` | SKU of the grouped product; media path of the sample download. |
| `groupedProductConfiguration` | Object, e.g. `{ "collapsible": true, "headerFields": [ "image", "sku", "price" ] }`. |

## Languages

A culture is installable when `cultures/{culture}/culture.json` exists:

```json
{ "name": "Deutsch", "uniqueSeoCode": "de", "flagImageFileName": "de.png" }
```

The culture code is the folder name. Only one language is installed per shop: the overlays
of that culture are applied on top of the base data.

### Adding a language

1. Create `cultures/{culture}/culture.json`.
2. Put the locale resources into `cultures/{culture}/resources/` (`*.smres.xml` and `head.txt`).
3. Add overlays for the files that need translation, mirroring the paths in `base/`.
   Only list the fields that differ.
4. Run the seed tests (see below).

### Adding or changing a record

1. Edit the base file. Give a new record a key following the rules above.
2. Add translations to the overlays of every culture that translates that file.
3. Run the seed tests and review the snapshot diff.

## Validation and tests

The loader fails loudly on unknown fields, unknown overlay keys, duplicate keys,
unresolvable references and missing `@file` files. Messages name
`file → record → field`.

Tests in `test/Smartstore.Core.Tests/Platform/Installation`:

| Test | Purpose |
|---|---|
| `Seeding/*Tests` | Unit tests of merger, data source, factory and resolver. |
| `SeedSnapshot/InstallationSeedSnapshotTests` | Runs the real installation seeder for `en-US` and `de-DE` against SQLite and compares a normalized snapshot of all seeded tables with `SeedSnapshot/Baseline/{culture}`. |

The snapshot protects against unintended changes. To accept an **intended** change, run the
tests with `SMARTSTORE_UPDATE_SEED_SNAPSHOT=1` and review the baseline diff before committing it.

## Data files

In installation order. `core/` is always installed, `samples/` only with sample data.

| Area | Files |
|---|---|
| Locale resources | `cultures/{culture}/resources/*` |
| Currencies, store | `core/currencies.json`, `core/stores.json` |
| Measures, tax categories (with `rate`, `$aliases`), countries and states | `core/measure-dimensions.json`, `core/measure-weights.json`, `core/tax-categories.json`, `core/countries.json` |
| Shipping, delivery, quantity units | `core/shipping-methods.json` (+ `samples/`), `core/delivery-times.json`, `core/quantity-units.json` |
| Customer roles, admin address, system customers | `core/customer-roles.json` (+ `samples/`), `core/admin-address.json`, `core/system-customers.json` |
| Email accounts, topics (with `slug` overrides), price labels | `core/email-accounts.json`, `core/topics.json`, `core/price-labels.json` |
| Settings (`$ref` values) | `core/settings.json` |
| Activity log types, templates, scheduled tasks | `core/activity-log-types.json`, `core/product-templates.json`, `core/category-templates.json`, `core/manufacturer-templates.json`, `core/tasks.json` |
| Menus (`@resource` titles, `publishedWhen`) | `core/menus.json` |
| Specification and product attributes, option sets | `samples/catalog/specification-attributes.json`, `product-attributes.json`, `product-attribute-option-sets.json` |
| Campaign, rule sets, discounts | `samples/campaigns.json`, `samples/rule-sets.json`, `samples/discounts.json` |
| Categories and manufacturers (with pictures and slugs) | `samples/catalog/categories.json`, `samples/catalog/manufacturers.json` |
| Product tags and products (pictures, specifications, tier prices, downloads, grouped and bundle products, variants, combinations) | `samples/catalog/product-tags.json`, `samples/catalog/products/*.json`, `cultures/{culture}/samples/catalog/attribute-value-names.json` |

Message templates are not part of this folder; they are imported from `App_Data/EmailTemplates`.
Descriptors (files, keys, references) are defined in
`src/Smartstore.Core/Platform/Installation/Seeding/InstallationSeedDescriptors.cs`.

## Rules for AI agents

- Never translate or rename keys. They are referenced from other files and cultures.
- Put translations only into `cultures/{culture}/…`. Base files are English.
- Do not add a field to an overlay that is not in the base record unless the entity really
  has that property; unknown fields fail the installation.
- Prefer `$all`, `$add`, `$remove` over copying whole files with `$mode: "replace"`.
- After every change, run
  `dotnet test test/Smartstore.Core.Tests --filter "FullyQualifiedName~Installation"`.
  A snapshot diff is only acceptable when the data change was intended.
- Sample images and downloads live in `media/`. Reference them by their path below `media/`.
  Every media reference creates its own media file.
