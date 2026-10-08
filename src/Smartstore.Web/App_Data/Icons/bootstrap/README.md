# Updating the Bootstrap Icons library

This is the repeatable import recipe for developers and coding agents. A future
request can simply say: "Update Bootstrap Icons using this README."
Follow the parent icon README and repository instructions as well.

## Library contract

- System name: `bootstrap` (this directory).
- Short name: `bi`.
- Default variant: `light`, short name `l`, display name `Light`.
- Filled variant: `solid`, short name `s`, display name `Solid`.
- Each variant owns an uncompressed `icons.zip` with SVG entries at its root.
- Both variants use `defaultViewBox: "0 0 16 16"`; source viewBox attributes take precedence.
- `library.json`, `mapping.json` and `metadata.json` live here, above the variants.
- Preserve any `user` and `icons` folders inside variants. They are independent
  customization layers and are never deleted or repacked during an upstream update.

## 1. Identify the input and preserve local changes

Keep the downloaded release under the workspace `.temp/` directory. Confirm its
version using the supplied package manifest or CSS banner; do not infer "latest"
from the date. Identify the directory containing individual SVG icons. Do not import
an aggregate sprite such as `bootstrap-icons.svg` as an individual icon.

Before replacing anything, preserve the existing ZIPs and JSON files under a dedicated
`.temp/` update directory. Read the existing mappings, tags and variant settings.
Updates must retain developer edits rather than regenerate these files blindly.

The first import used `.temp/bootstrap-icons-1.13.2` and SVGs staged in this library's
root. The staged files were confirmed byte-identical to the supplied release.

## 2. Split variants and normalize entry names

Inspect the filename stem, excluding `.svg`:

| Source filename | Destination ZIP | Entry name |
| --- | --- | --- |
| `alarm.svg` | `light/icons.zip` | `alarm.svg` |
| `alarm-fill.svg` | `solid/icons.zip` | `alarm.svg` |

Only a terminal `-fill` selects `solid`. Every other individual SVG goes to `light`.
Remove exactly that suffix from solid ZIP entry names. Do not remove `-fill` from
other positions and do not change SVG bytes, paths, attributes, classes or comments.
An icon without a counterpart in the other variant remains valid. Never copy icons
between variants merely to make their inventories match.

Fail on duplicate normalized names within a variant. ZIP entries must be flat,
with no enclosing directory and no README or metadata files inside the archive.

## 3. Update library, mappings and metadata

Update `library.json` to the confirmed release version. Retain existing display
names, short names and defaults unless explicitly asked to change them.

`mapping.json` maps shared conceptual names to normalized BI names. It is library-wide:
values must not contain a library or variant selector. Mappings do not switch variants.
An icon may therefore resolve in `light` but not in `solid`, or vice versa.

Preserve existing assignments. For new concepts, consult the Bootstrap source-to-concept
relationships in [MIGRATION.md](../MIGRATION.md), followed by an exact available BI name.
Normalize terminal `-fill` there too. Choose approximations only where useful and record
them in an update review report. Update the migration reference when an agreed mapping
changes; never depend on temporary comparison views. If an upstream icon
is removed or renamed, report affected mappings instead of silently dropping them.
Verify each mapping target against the new inventories and report per-variant gaps.

Bootstrap Icons supplies no semantic search-tag metadata in this package. Build
`metadata.json` from the union of normalized names in both archives:

```json
{
  "icons": {
    "alarm": {
      "tags": ["clock", "reminder", "wake up"]
    },
    "example-without-extra-tags": {}
  }
}
```

Preserve manually maintained tags for retained icons. Add useful English synonyms
and related concepts for new icons, using these sources where the meaning matches:

- Existing HI tags connected through a shared conceptual mapping.
- Locally available FA search terms for equivalent icons.
- Curated family terms, such as `cart` to `shopping`/`checkout`, `person` to
  `user`/`account`, or `gear` to `settings`/`configuration`.

These are inferred search aids, not official BI metadata. Avoid unrelated tags,
labels, a separate alias field, and redundant terms already found in the icon name.
For redundancy checks, compare case-insensitively and treat hyphens, underscores
and spaces equivalently. Deduplicate and sort tags. Keep every available name even
when its metadata is `{}`; omit empty `tags` properties. Keep tag arrays on one line.
Review removed names and preserve their old metadata in the update backup/report.
Use alphabetical ordering, CRLF and the JSON indentation from `.editorconfig`.

## 4. Verify archives before deleting sources

Build replacement archives in the update workspace first. Use ZIP method **Stored**
(no compression, `ZIP_STORED` / method 0), not Deflate with a low compression level.
Before publishing or deleting anything, verify:

1. Every intended source has exactly one correctly named entry in its target variant.
2. Entry names are unique and flat, counts match the split, and all entries use Stored.
3. ZIP integrity checks pass and SHA-256 hashes of extracted entry bytes equal the sources.
4. JSON files parse, metadata keys equal the union of the inventories, and mapping
   targets exist in at least one variant. Report availability separately per variant.
5. The staging files have not changed since verification.

Publish the verified archives, then delete only the verified loose upstream SVGs
from their staging location in this library. Verify resolved deletion paths stay
inside that location; never recursively delete a variant, `user` or `icons` folder.
Keep the release download and update backup under `.temp/` for recovery.
Do not leave loose upstream SVG copies next to the ZIPs.

A normal application reload/watcher invalidation picks up changed archives and JSON.
Do not build or run tests without the user's authorization.

## First-import reference

Version 1.13.2 contained 2,078 individual SVG files:

- `light`: 1,408 entries.
- `solid`: 670 entries after removing terminal `-fill` from ZIP entry names.
- Metadata: 1,409 distinct names across both variants. `person-lines` existed only
  in solid in this release.
- Mapping: 250 shared concepts, including approximations needing visual review.

These are historical verification counts, not requirements for subsequent versions.
