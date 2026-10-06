# Icon overrides

This folder holds custom SVG files for the HugeIcons Stroke Rounded variant.
Keep local changes here so that `icons.zip` can be replaced when updating the
library without losing those changes.

The intended lookup convention is:

- An SVG with the same filename as an icon in `icons.zip` replaces that icon.
- An SVG with a new filename adds an icon to this variant.
- The filename without `.svg` is the icon ID. Keep files directly in this folder.

For example, `shopping-cart-02.svg` overrides the corresponding archived icon.
Only SVG files are icon assets; this README is not an icon.

Optional English search tags belong in `../../metadata.json`, shared by all
variants of this library. Conceptual icon mappings belong in `../../mapping.json`.

See [the icon library concept](../../../README.md) for configuration, discovery,
and metadata conventions.

This convention describes the planned behavior; the icon loader must implement it.
