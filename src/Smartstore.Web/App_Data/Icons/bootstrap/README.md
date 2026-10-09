# Updating the Bootstrap Icons library

This is the repeatable import recipe for developers and coding agents. A future
request can simply say: "Update Bootstrap Icons using this README."
Follow the parent icon README and repository instructions as well.

## Library contract

- System name: `bootstrap`; short name: `bi`.
- There is exactly one variant: `default`, with no display name or short name.
- All upstream icons belong to `default/icons.zip`, including filled icons.
- Preserve original filenames exactly: `alarm.svg`, `alarm-fill.svg`,
  `building-fill-add.svg`, `person-check-fill.svg` and `person-fill-check.svg`
  are independent entries. Never remove or relocate `fill` tokens.
- The ZIP is uncompressed (Stored), with SVG entries at its root.
- `defaultViewBox` is `0 0 16 16`; source viewBox attributes take precedence.
- `library.json`, `mapping.json` and `metadata.json` live above the variant.
- Preserve `default/user` and `default/icons` customization layers. Never delete
  or repack their files during an upstream update.
- The IconBrowser omits the variant label for `default`, showing the library name
  in both its menu and the compact button.

Bootstrap does not provide the light/solid variants previously synthesized here.
Do not reintroduce that split unless explicitly requested.

## 1. Identify the input and preserve local changes

Keep downloads under `.temp/`. Confirm the release version from its supplied
package manifest or CSS banner. Do not infer the latest version from the date.
Identify individual SVG files; exclude the aggregate sprite `bootstrap-icons.svg`.

Before replacing anything, back up the current ZIP and JSON files under a dedicated
`.temp/` update directory. Read mappings, tags and variant settings first, and retain
local edits. The current release source is `.temp/bootstrap-icons-1.13.2`.

## 2. Build the single archive

Copy every individual upstream SVG into `default/icons.zip` under its original
filename. Do not change drawing bytes, attributes, classes or comments.

Fail on duplicate filenames. Entries must be flat: no enclosing folder, README,
metadata, or aggregate sprite inside the ZIP. Build the replacement archive under
`.temp/` first. Use ZIP method **Stored** (`ZIP_STORED`, method 0), not Deflate with
a low compression level.

## 3. Update library, mappings and metadata

Update `library.json` to the confirmed release version. Preserve its display name,
short name, icon address and custom settings. Keep `defaultVariant: "default"` and
only the `default` entry under `variants`. Do not give that variant a display name
or short name. The library icon can be addressed as `bi:bootstrap@default`.

`mapping.json` maps shared concepts to exact upstream names. Preserve assignments;
use names ending in `-fill` where a concept should display the filled drawing.
Check targets against the archive and customization layers. Do not normalize names
or switch variants. Report missing targets instead of dropping mappings silently.
The [migration reference](../MIGRATION.md) documents existing FA/BI-to-HI concepts.

Build `metadata.json` from the complete available-name inventory:

```json
{
  "icons": {
    "alarm": {
      "tags": ["clock", "reminder", "wake up"]
    },
    "alarm-fill": {
      "tags": ["clock", "reminder", "wake up"]
    },
    "example-without-extra-tags": {}
  }
}
```

Keep manually maintained metadata for retained names. BI supplies no semantic tags
in this package. Infer useful English synonyms from existing HI/FA metadata and
related icon families. A filled icon may share its counterpart's tags, but remains
a distinct metadata entry. Avoid redundant tags already found in the name; omit
empty `tags` properties while keeping the icon's empty object. Sort names and tags,
keep tag arrays on one line, and use CRLF and the repository's JSON indentation.
Preserve metadata for removed icons in the backup/report rather than discarding it.

## 4. Verify before publishing or deleting sources

1. Every individual source has exactly one identically named archive entry.
2. Entry names are unique and flat, and all entries use Stored compression.
3. ZIP integrity passes; SHA-256 hashes of extracted bytes match the sources.
4. JSON parses, metadata covers the available names, and mapping targets exist.
5. Configured icon addresses and kit sources use `@default` or omit the variant.
6. Source files have not changed during staging and verification.

Publish the verified archive and JSON. Delete only verified loose upstream files
from their staging location, after confirming absolute paths remain inside it.
Never recursively delete `default`, `user`, or `icons`. Keep the release download
and update backup under `.temp/` for recovery. Do not leave loose upstream copies
beside the archive. The catalog watcher picks up changed files; cached sprites
and manifests receive new revisions. Do not build without user authorization.

## Current reference: 1.13.2

- `default/icons.zip`: 2,078 individual SVGs under original upstream filenames.
- Metadata: 2,078 distinct entries.
- Mapping: 250 shared concepts; existing targets remain unchanged.
- No synthetic variants and no name-collision exceptions are needed.

The former light/solid archives were consolidated without changing SVG bytes.
Their shared metadata was expanded to the original names, preserving tags.
Explicit old addresses must be migrated: for example, `bi:alarm@solid` becomes
`bi:alarm-fill@default`, while `bi:alarm@light` becomes `bi:alarm@default`.
Recover exact original names from the source SVG's `bi-*` class or the official
release when an old interior-fill name was normalized. Do not assume that adding
`-fill` to the end always recovers the correct upstream name.
