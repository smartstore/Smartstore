# Menu icon migration

## Scope

- Admin sitemap and XML loader; platform settings and account menus.
- Menu definitions in public, enterprise, internal and customer module repositories.
- Shared menu templates: Navbar, Dropdown, LinkList and ListGroup; AccountDropdown.
- Admin menu templates: Admin and Settings; menu editor ItemList.
- HP menu templates: Main, SimpleLinkList and FooterLinkList.
- Database menu providers and edit-mode link metadata.

Prefer reviewed concepts. Keep concrete BI addresses for artwork without a reviewed
concept. Do not turn concrete library source names or legacy font aliases into new
concept definitions implicitly.

## IconClass audit

No FA-specific values assigned to NavigationItem.IconClass were found in workspace
source. The admin sitemap uses `flip-h` for return cases; the shared SVG utilities
support that class and all menu renderers preserve it through conditional `attr-class` merging.

Six account-dropdown definitions previously embedded `fa-fw` in Icon. They now use
concept names with `IconClass = "icon-fw"`. Icon color on database-backed items now
uses IconClass instead of being appended to the icon name.

Existing third-party extensions and persisted custom classes cannot be exhaustively
checked by a source audit. Update any FA sizing/animation classes there separately.

## Deferred consumers

Script-swapped icons remain deferred. TabStrip extensions were migrated in the
following step; see the TabStrip section in [MIGRATION.md](MIGRATION.md).
Smartstore.DimensionPricing now uses `fa:ruler-horizontal!@s` with
`IconClass = "icon-lg icon-fw"` for its two tabs.

The menu dropdown caret and navbar scrolling arrows are template controls, not
NavigationItem.Icon values, and remain outside this step.

## Compatibility and validation

IconLibrary is obsolete but continues to qualify unqualified Icon values. Explicit
addresses win regardless of initializer order. The old fluent overload produces an
address. Persisted legacy FA selections are rendered through concrete SVG addresses;
light/duotone use solid, and unavailable FA Pro drawings still need custom sources.

Static checks cover mapped menu source availability, sitemap XML, remaining menu
rendering branches and whitespace. All 66 newly introduced literal assignments
resolve to existing sources. All nine sitemap root nodes retain their attributes.
NavigationItemTests passed all six cases covering address precedence, initializer
order and empty/legacy inputs. Private module projects and browser appearance have
not been verified by a build or visual run in this step.

The admin sitemap root nodes retain their original Fontastic (`icm`) icons and
legacy rendering by request. Only their descendants participate in this migration.

The final Debug build of Smartstore.sln passed with zero warnings and zero errors,
including the root-node exception and conditional attribute changes.
