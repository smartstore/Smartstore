# System icons

This folder contains SVG corrections and additions supplied by Smartstore for
this library variant. It is maintained with the application and may be replaced
during updates. Put integrator customizations in the sibling `../user/` folder.

Icons resolve by filename in this order: **user > icons > icons.zip**.

- A matching SVG filename replaces the archived icon unless a user file takes priority.
- A new SVG filename adds an icon to this variant.
- The filename without `.svg` is the exact, case-sensitive icon ID.
- Keep SVG files directly in this folder; subdirectories are not searched.
- Non-SVG files, including this README, are not icon assets.

Removing a system override reveals the archived icon if present. An invalid SVG
in the selected layer raises an error; resolution does not silently fall back.

Optional English search tags belong in `../../metadata.json`, shared by all
variants of this library. Conceptual mappings belong in `../../mapping.json`.

See [the icon library concept](../../../README.md) for configuration, supported
SVG content, discovery and metadata conventions.
