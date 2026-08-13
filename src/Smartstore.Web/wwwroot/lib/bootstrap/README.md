# Smartstore Bootstrap Fork

This directory contains a Bootstrap 4/5 hybrid. It is neither a stock Bootstrap 4 nor a stock Bootstrap 5 distribution and must not be replaced without porting the Smartstore SCSS customizations.

## Compatibility Matrix

| Area | Implementation |
| --- | --- |
| JavaScript | Unmodified Bootstrap **4.6.2** JavaScript, including its jQuery integration, Popper 1 API and unprefixed `data-toggle`, `data-target` and `data-dismiss` attributes. |
| Markup | Primarily Bootstrap 4 class names. Legacy directional utilities such as `ml-*`, `mr-*`, `pl-*` and `pr-*` are intentionally retained. |
| SCSS | Bootstrap 4 core with selected Bootstrap 5 SCSS, mixins, maps and component backports plus Smartstore-specific changes. |
| CSS custom properties | Partial Bootstrap 5 root variables use the `--bs-*` prefix. Component variables are mostly Smartstore-compatible, unprefixed variables such as `--btn-*`, `--nav-*` and `--modal-*`. |
| Forms | Bootstrap 4 form infrastructure combined with Bootstrap 5-style form checks, switches and floating labels. Bootstrap 4 custom forms are still present. |

## Differences from Stock Bootstrap 4

- Bootstrap 5 utility API, responsive utility generation and optional negative margins.
- `xxl` breakpoint and container, logical CSS properties and additional border/radius utilities.
- Bootstrap 5-style color system with emphasis, secondary, tertiary and subtle colors.
- Backported components and helpers including `form-switch`, `form-floating`, `btn-close`, placeholders, colored links and `text-bg-*`.
- Extensive CSS custom properties for runtime component and theme customization.
- Theme compilation is split into `bootstrap-head.scss` and `bootstrap-main.scss`, allowing theme variables to be injected between framework setup and component generation.

## Differences from Stock Bootstrap 5

- Bootstrap 4 JavaScript and Data API are retained; Bootstrap 5 `data-bs-*` attributes are not used by the bundled scripts.
- Bootstrap 4 class names and compatibility components remain, including `jumbotron`, media object, custom forms and legacy directional utilities.
- Spacing utilities use logical CSS internally but continue to emit the Bootstrap 4 names `ml/mr/pl/pr`, not `ms/me/ps/pe`.
- Component CSS variables do not generally use Bootstrap 5's `--bs-<component>-*` namespace.
- Smartstore-specific Sass variables and CSS variables customize buttons, badges, sizing, borders, shadows, RTL behavior and accessibility.
- The complete Bootstrap 5.3 color-mode/dark-mode infrastructure is not included.

When changing Bootstrap sources, preserve the public class and CSS-variable compatibility contract unless all Smartstore themes, views, modules and third-party integrations are migrated together.
