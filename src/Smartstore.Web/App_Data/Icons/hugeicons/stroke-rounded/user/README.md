# User icon customizations

This folder contains integrator SVG replacements and additions for this library
variant. Preserve its contents during application and icon library updates.
Smartstore-supplied corrections belong in the sibling `../overrides/` folder.

Icons resolve by filename in this order: **user > overrides > icons.zip**.

- A matching SVG filename replaces both the system override and the archived icon.
- A new SVG filename adds an icon to this variant.
- The filename without `.svg` is the exact, case-sensitive icon ID.
- Keep SVG files directly in this folder; subdirectories are not searched.
- Non-SVG files, including this README, are not icon assets.

Removing a user file reveals the system override, or the archived icon when no
system override exists. An invalid user SVG raises an error; resolution does not
silently fall back to a lower layer.

Optional English search tags belong in `../../metadata.json`, shared by all
variants of this library. Conceptual mappings belong in `../../mapping.json`.
Preserve these customizations during updates as well.

See [the icon library concept](../../../README.md) for configuration, supported
SVG content, discovery and metadata conventions.
