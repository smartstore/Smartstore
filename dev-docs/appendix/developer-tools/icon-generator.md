# Smartstore Icon Generator

The Smartstore Icon Generator is a browser-based developer tool for creating and updating Smartstore's reduced Bootstrap Icons sprite. It compares different versions of the icon collection and lets you select which icons should be included in the application.

The [source code is available on GitHub](https://github.com/smartstore/Smartstore/tree/main/tools/Smartstore.IconGenerator).

{% hint style="info" %}
The tool runs locally in your browser. Selected files are not uploaded.
{% endhint %}

Open [`tools/Smartstore.IconGenerator/index.html`](../../../tools/Smartstore.IconGenerator/index.html) in your browser to use it.

## Understand the Smartstore icon files

Smartstore keeps two SVG sprites under `src/Smartstore.Web/wwwroot/lib/bi/`:

| File | Purpose |
| --- | --- |
| `bootstrap-icons-all.svg` | Complete locally distributed Bootstrap Icons collection; excluded from publishing |
| `bootstrap-icons.svg` | Reduced sprite used by the application |

The generator compares three files:

| Input | Meaning in the Smartstore workflow |
| --- | --- |
| **Remote** | The latest complete Bootstrap Icons SVG sprite obtained from upstream |
| **Local** | Smartstore's current `bootstrap-icons-all.svg` |
| **Subset** | Smartstore's current `bootstrap-icons.svg` |

"Remote" refers to the newest external version of the icon collection. It is still selected through a regular local file picker; the generator does not download it.

Only the Remote input is mandatory.

## Update the Smartstore subset

1. Obtain the latest complete Bootstrap Icons SVG sprite.
2. Open the generator in your browser.
3. Choose the files:
   * **Remote:** the newly obtained complete sprite.
   * **Local:** [`bootstrap-icons-all.svg`](../../../src/Smartstore.Web/wwwroot/lib/bi/bootstrap-icons-all.svg).
   * **Subset:** [`bootstrap-icons.svg`](../../../src/Smartstore.Web/wwwroot/lib/bi/bootstrap-icons.svg).
4. Select **Read files**.
5. Review newly introduced, removed, and currently selected icons.
6. Click an icon preview to include or exclude it. Icons from the supplied Subset file are selected automatically.
7. Select **Export**.
8. Either copy the generated SVG from the dialog or click **download**. Downloads use the filename `icon_set.svg`.
9. Replace `bootstrap-icons.svg` with the exported file.
10. When updating to a new upstream version, separately replace `bootstrap-icons-all.svg` with the new complete sprite. The generator exports only the selected subset.

After rebuilding Smartstore, the updated icons are available to the application.

## Read the icon grid

An icon's appearance indicates where it was found:

| Indicator | Meaning |
| --- | --- |
| Blue selection outline | Included in the loaded Subset file or selected manually |
| **NEW** | Present only in Remote |
| **LOCAL** | Present only in Local |
| **SUBSET** | Present in Remote and Subset, but missing from Local |
| No badge | Shared by multiple collections without a special difference |

The counts beside the three file inputs show how many symbols were read from each file.

When the same symbol ID exists in several inputs, the generator gives precedence to the first loaded definition: Remote, then Local, then Subset. An existing selected icon is therefore exported with the latest Remote drawing data when that icon is available upstream.

Icons that disappeared from Remote remain available for selection if they still exist in Local or Subset.

## Search and filter icons

The text field filters icons by symbol ID. Matching is substring-based and case-sensitive.

The checkboxes can be combined:

* **New** shows icons found only in Remote.
* **Used** shows currently selected icons.
* Selecting both shows selected icons that are also classified as new.

Filtering only changes what is visible. Hidden selected icons remain selected and are included in the export.

Clicking an icon name selects its text for copying. Click the icon preview itself to toggle its inclusion.

## Create a new subset

The Local and Subset inputs are optional. To create a subset from scratch:

1. Load the complete Bootstrap Icons collection as Remote.
2. Select **Read files**.
3. Find and select the required icons.
4. Export the resulting sprite.

Loading Local or Subset also allows a new subset to retain icons that are no longer contained in Remote.

## Understand browser persistence

The generator stores the contents and names of the three most recently loaded files in the browser's local storage. On the next visit, it attempts to restore and render them automatically.

Selecting and reading another file replaces the stored version for that input. To remove the cached inputs, clear the browser data associated with the page.
