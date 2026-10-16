# Installation seed data

This folder holds the data that the installer writes into a new shop database:
mandatory data (currencies, countries, tax categories, settings, …) and the optional
sample catalog (categories, manufacturers, products, …), each in all installable languages.

It replaces the seed data that used to live in C# (`InvariantSeedData`, `EnUSSeedData`,
`DeDESeedData` in `src/Smartstore.Core/Platform/Installation`).

> **Status: migration in progress.** The loader infrastructure exists
> (`src/Smartstore.Core/Platform/Installation/Seeding`), but no entity type reads its data
> from this folder yet. Until an area is migrated, its data still comes from the C# classes.
> The table under [Migrated areas](#migrated-areas) lists what is live.

## Folder layout

```
App_Data/Seed/
├── base/                         Base data, English, valid for every language
│   ├── core/                     Mandatory data
│   └── samples/                  Sample data (only installed on request)
├── cultures/
│   └── {culture}/                e.g. de-DE
│       ├── culture.json          Makes the culture installable (see below)
│       ├── core/…                Overlays: same relative paths as in base/
│       └── samples/…
└── media/                        Sample images and downloads
```

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

## Languages

A culture is installable when `cultures/{culture}/culture.json` exists:

```json
{ "name": "Deutsch", "uniqueSeoCode": "de", "flagImageFileName": "de.png" }
```

The culture code is the folder name. Only one language is installed per shop: the overlays
of that culture are applied on top of the base data.

### Adding a language

1. Create `cultures/{culture}/culture.json`.
2. Add overlays for the files that need translation, mirroring the paths in `base/`.
   Only list the fields that differ.
3. Run the seed tests (see below).

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

The snapshot must stay identical while data moves from C# into this folder. To accept an
**intended** change, run the tests with `SMARTSTORE_UPDATE_SEED_SNAPSHOT=1` and review the
baseline diff before committing it.

## Migrated areas

| Area | Files | Status |
|---|---|---|
| — | — | Nothing migrated yet |

## Rules for AI agents

- Never translate or rename keys. They are referenced from other files and cultures.
- Put translations only into `cultures/{culture}/…`. Base files are English.
- Do not add a field to an overlay that is not in the base record unless the entity really
  has that property; unknown fields fail the installation.
- Prefer `$all`, `$add`, `$remove` over copying whole files with `$mode: "replace"`.
- After every change, run
  `dotnet test test/Smartstore.Core.Tests --filter "FullyQualifiedName~Installation"`.
  A snapshot diff is only acceptable when the data change was intended.
- Do not edit `src/Smartstore.Web/App_Data/Samples` for migrated areas; sample media moves
  to `media/`.
