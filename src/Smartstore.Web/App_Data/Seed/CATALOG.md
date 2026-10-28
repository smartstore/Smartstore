# Demo catalog

The sample data that the installer adds on request ("Install sample data") is a small demo catalog.
Every product exists to show a feature of Smartstore, and its long description explains that feature
in plain words. This file describes the catalog so that it can be extended consistently, e.g. when a
new feature should be demonstrated. File formats and loader rules are described in [README.md](README.md).

## Principles

- **Fictional brands and products.** No real brands, no third-party product photos. Timeless goods such as
  olive oil or golf balls are fine, but always without a brand.
- **One product, one purpose.** Each product shows one larger feature or a few small, related ones.
  Fewer products are better than products without a purpose.
- **Texts explain.** The long description first describes the product in one or two sentences, then says
  what the product shows in the shop (`What this product shows:`), without technical terms.
- **Realistic.** The shop has to look like a real shop: plausible prices, stock, delivery times, reviews
  and discounts, but not on every product. Some products have no reviews, only a few are marked as new.
- **Timeless.** Dates are relative to the installation (`@now-5d`, `@now+1M`), never fixed.
- **Demo only.** Settings and core data that only the demo shop needs go into the sample overlays
  (`base/samples/core/…`), never into `base/core/…`.

## Brands and SKUs

| Group | Brand | SKU prefix | Category (alias) |
|---|---|---|---|
| Food & delicacies | Casa Vireluna | `FD-` | `Delicacies` |
| Golf & sports | Fairway Vennix | `GF-` | `Golf and Sports` |
| Apparel | Norlumera | `FS-` | `Apparel` |
| Furniture & design | Lindavero | `FN-` | `Furniture and Design` |
| Digital products | Pixelmoor | `DG-` | `Digital` |
| Vouchers | – | `GC-` | `Vouchers` |

SKUs are `{prefix}{number}`, starting at 1001, e.g. `FN-1002`. Combinations append the variant, e.g.
`FS-1001-NVY-M`. SKUs are stable: other modules and the documentation refer to them.

## Products and features

| SKU | Product | Shows |
|---|---|---|
| FD-1001 | Extra virgin olive oil 500 ml | Base price per litre, reduced tax rate, specifications |
| FD-1002 | Roasted coffee | Combinations with own SKU, price, base price and delivery time; picture per pack size (assigned to the combinations) |
| FD-1003 | Coffee subscription | Recurring product, date picker, multi-line text box |
| FD-1004 | Mediterranean herb mix | Simple product, specification filters, low stock, new |
| FD-1005 | Gourmet gift box | Bundle with per-item pricing and item discounts, new |
| FD-1006 | Sea salt mill | Out of stock with back-in-stock notification |
| FD-1007 | Spiced Christmas cookies | Available from a future date |
| GF-1001 | Golf balls | Pack size variant with price and weight adjustment, tier prices |
| GF-1002 | Golf tees, 50 pieces | Quantity unit and minimum order quantity |
| GF-1003 | Leather golf glove | Required variants without preselection (radio list and dropdown) |
| GF-1004 | Laser rangefinder | Call for price |
| GF-1005 | Golf starter set | Bundle with a fixed price |
| GF-1006 | Lightweight stand bag | Compare price with price label, low stock |
| GF-1007 | Divot repair tool | Simple accessory, bundle item, new |
| FS-1001 | Rain jacket | Colour swatches with pictures, combinations, greying out sold-out combinations |
| FS-1002 | Ribbed knit sweater | Multicolour swatches, swatch sizes and aspect ratio, radio buttons for the material (no colour, so that it is not mistaken for the colour attribute) with a surcharge |
| FS-1003 | Leather sneaker | Fixed price per combination, sold-out sizes without greying out |
| FS-1004 | Merino beanie | "New" label |
| FS-1005 | Wool scarf | Special price with end date, quantity input instead of a dropdown |
| FS-1006 | Organic T-shirt with print | Personalisation with a text box shown only for "With print" |
| FN-1001 | Lounge chair | Configurator: surcharges, price reduction, weight adjustment, checkboxes, linked product; picture per cover and frame (combinations for every extras state) |
| FN-1002 | Arc floor lamp | Final price in swatches, combination with own price, weight and delivery time, required product |
| FN-1003 | Oslo collection | Grouped product |
| FN-1004–FN-1006 | Oslo side table, shelf, footstool | Items of the grouped product; shelf without quantity control |
| FN-1007 | LED bulb | Required by the floor lamp, added to the cart automatically |
| DG-1001 | E-book | Download after payment, sample download, reduced tax rate |
| DG-1003 | Photo software | Download with licence agreement, download limit and expiry, file version |
| GC-1001 | Gift card | Virtual gift card, amount as combination price |
| GC-1002 | Gift card with amount of your choice | Price entered by the customer |
| GC-1003 | Donation | Price entered by the customer, tax-free, no shipping |

Across the catalog: reviews with different ratings, delivery times, stock levels, specifications for
filtering (essential ones are also shown in the checkout), product tags, category icons and badges, a sale category,
a product discount, a category discount, a coupon code (`WELCOME10`) and free shipping from a cart subtotal.

Related products ("similar products" on the product page) and cross-sell products (suggestions in the shopping cart) are set on FD-1001, FD-1002, FD-1004, GF-1001, GF-1002, FS-1001, FS-1002, FS-1004 (related products) and additionally FN-1001 (cross-sell: the floor lamp; furniture has no related products because they do not fit the feature). Example: the olive oil lists the herb mix, the sea salt mill and the gourmet gift box as related products and suggests the mill in the cart; the rain jacket suggests the beanie and the scarf.

Some products also show features of optional modules. Their texts say so in a separate paragraph
("The plugin <strong>…</strong> …"), and the module seeds its own data during the installation.

## Extending the catalog

1. Check the table above: if a fitting product exists, add the feature there instead of creating a product.
2. Use the next free SKU of the group, the group's brand and its category.
3. Write the long description in the base file (English) and the culture overlays, as readable HTML
   (see README). Link every mention of another product or category.
4. Keep derived product properties consistent (e.g. tier prices, combinations, discounts); the loader
   derives most of them, see `InstallationDataSeeder.Products.cs`.
5. Run the seed tests, review the snapshot diff and update the table above.

All pictures are final images (brand logos `media/brand/*.png`, category images `media/category/*.jpg` as text-free scenes without products, so that they do not repeat the product pictures in lists, `media/product/fd-*.jpg`, `gf-*.jpg`, `fs-*.jpg`, `fn-*.jpg`, `dg-*.jpg`, `gc-*.jpg`). Pictures with printed text exist once per language: the e-book cover (`dg-1001.jpg`, `dg-1001-de.jpg`) and the gift cards (`gc-1001-25.jpg` and `gc-1001-25-de.jpg`, …); the de-DE overlay sets the German ones, including the pictures of the combinations of the gift card.
