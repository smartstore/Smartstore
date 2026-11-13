# Theme previews

Generates the `preview.png` images (1280 × 960) of the Flex theme family, shown in
the backend theme list.

| File | Purpose |
|---|---|
| `previews.html` | All preview scenes as SVG, one entry per theme folder |
| `render.ps1` | Screenshots each scene with headless Edge or Chrome |

## Usage

```powershell
# Render all themes into src/Smartstore.Web/Themes/<Theme>/wwwroot/images/preview.png
./tools/ThemePreviews/render.ps1

# Render selected themes into a scratch folder for review
./tools/ThemePreviews/render.ps1 -Themes FlexTidal, FlexRiso -OutDir $env:TEMP/theme-previews
```

To iterate on a scene, open `previews.html?theme=FlexTidal` in a browser.

## How a scene is built

Most themes share one storefront skeleton: menubar, shopbar with search, navigation,
hero, four products and footer. Its colours, fonts and corner radii mirror the theme's
`theme.config`, so keep them in sync when the theme variables change. Each theme adds
its own hero art (`hero.art`) and product illustrations (`items`).

A theme whose layout does not fit the skeleton supplies a complete `scene` instead,
as `FlexRiso` does.

## Notes

- Fonts are system fonts, except Barlow Semi Condensed, which is loaded from the
  `FlexBlack` theme.
- Rendering is deterministic: identical input produces byte-identical PNGs.
- A new theme needs an entry in `previews.html` keyed by its folder name, and the
  folder name in the default `-Themes` list of `render.ps1`.
