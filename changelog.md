# Release Notes

## Smartstore 5.2.0

### Breaking Changes
- Removed Sofort provider from PayPal module (disabled by PayPal on 18.04.2024) 

### New Features

- **Conditional product attributes**
  - Makes the visibility of an attribute dependent on the selection state of other attributes.
  - Copying attributes, options and rules from one product to another.
- **Simplified checkout process**
  - Quick-Checkout allows to skip addresses, shipping and payment method selection if these are known.
  - One-click checkout for sales at retail terminals.
- **Essential specification attributes**
  - Specification attributes marked as an *essential feature* are displayed in the checkout.
- **Cart & wishlist enhancements**
  - Disable cart items via checkbox.
  - Show delivery time, stock info, weight, additional shipping surcharge and brand.
- **Grouped product enhancements**
  - Optional presentation of associated products as collapsible/expandable panels.
  - Added paging for associated products.
- Updated to **.NET 8**
  - Faster app startup
  - Increased overall performance
  - ~10 % less memory usage after app start
- New app setting for MS SQL Server compatibility level
- Enhanced database optimization and vacuum operations
- Vacuum single database table
- #909 Allow to control the product availability based on the existence of an attribute combination.
- MegaSearch: setting to place the search hits of unavailable products further back in the search list.
- Affiliates
  - #896 Added a cart rule for affiliates.
  - Added a button to remove the assignment of a customer to an affiliate on customer edit page.
- Import news from RSS news feeds.
- #971 Add a cart rule to check if the current customer is authenticated with a certain external authentication method.
- Reverse proxy: added support for `X-Forwarded-Prefix` header (for `PathBase` resolution)
- Page Builder
	- Added **AudioPlayer** block
- #858 Implemented Paypal package tracking
- #997 Added setting to disable display of product tags on the product detail page  

### Improvements

- #876 Changing password in backend via modal dialog.
- #871 Show the total media file size in dashboard stats.
- Theming
  - Revamped dashboard stats
  - Activate .spa-layout only on screen height > 600px (DataGrid is unusable in mobile landscape mode otherwise)
- Page Builder
  - Fixed *boxed titles* spacing and line-height issues
- Web API
  - Enabling CORS.
  - #928 mask the secret key in backend API user list.
  - #1057 add endpoints for `WalletHistory` entity.
  - #929 add endpoints for PageBuilder stories, story blocks and import/export.
- Security
  - #886 Replace CoreFTP with FluentFTP.
  - #1004 Add captcha to password recovery form.
- Add a setting for a maximum order age. For orders over this age, no more messages such as *shipped* or *delivered* may be sent to the buyer.
- Skrill: added support for new parameter `website_id` (required for Giropay and iDeal payments).
- GMC
  - Only export images (not videos or other media types).
  - Export *out of stock* if inventory management and the buy button are deactivated.
- #912 Add a setting to use the `CultureInfo.NativeName` in language selector instead of the language name maintained in backend.
- #968 Allow to specify a language in which the notification is to be sent for manually created gift cards.
- Added meta properties name and uploadDate for videos
- (DEV) Database migrations: Long running data seeders can now be run during the request stage to overcome app startup timeout issues.
- #965 Prevent adding of products to the shopping cart by system customers such as *builtin@search-engine-record.com*.
- Increased the default maximum file size of an avatar and added a customer setting for this in backend.
- Stripe: Update shipping address on confirm order.
- Addresses: make first and last name optional if a company name has been specified.
- #1012 Estimate shipping costs without rules if no shipping method was found with rules.
- PayPal: Orders were cancled when capturing was declined, now they are being voided instead.
- #1020 Prevent creation of unnecessary Stripe "payment intent".
- Added deletion of selected rows to the data grid of manufacturers, discounts, menus and topics.

### Bugfixes

- Fixed a new shipping address is used as the billing address in checkout.
- Fixed only the first product attribute of list type attributes was displayed on the cart and order page.
- Fixed an unavailable attribute was not grayed-out if the product has at least one non list-type attribute.
- Fixed cart page shows 0 bundle item price if per-item pricing is deactivated.
- #996 Limited to customer roles is not working for topics that are displayed as widgets.
- #914 Featured sorting on category and manufacturer pages not applied when using standard search.
- Product attributes are lost when navigating to *Ask Question* page multiple times.
- #1024 Apply preselected options of required attributes of added products when required products are automatically added to shopping cart.
- Fixed a product can only be added to the shopping cart with a quantity of 1 if the stock quantity is below 0.
- Fixed the discount amount of an order can have an incorrect value if a discount rule was applied during the subtotal calculation.
- #957 Fixed prices should not be hidden if the *Access Shopping Cart* permission has not been granted.
- Fixed tier prices of product bundles were not taken into account in product lists if the lowest possible price is to be displayed.
- #1041 Fixed `ArgumentException` "The resource object with key *DateHumanize_MultipleMonthsAgo* was not found (Parameter *resourceKey*)" in Czech language (probably also appears in others).
- Fixed `NullReferenceException` calling search page without any search term.
- Fixed `NullReferenceException` *typeFilter was null* when uploading a video.
- Fixed `NullReferenceException` on product detail page if the main menu is not published.
- MegaSearch: hits from an SKU search tend to appear too far back.
- Tax by region: fixed tax rate was not applied if asterisk placeholder character was saved for zip code.
- #921 IOException "The filename, directory name, or volume label syntax is incorrect" when `MediaSettings.AppendFileVersionToUrl` is activated.
- #922 Newsletter subscription event not triggered upon email confirmation.
- Language selector in off-canvas menu should show the same language name as in the main menu.
- #936 Password protected topic was not displayed after password has been entered.
- #955 Searching a product by product code in grids returns an infinite list.
- Fixed the reward points for purchases setting was not saved in multi-store mode.
- #960 Setting `ManufacturerItemsToDisplayInOffcanvasMenu` cannot be changed in backend.
- Fixed offcanvas cart issue in mobile browsers (buttons in the footer were sometimes truncated).
- Page Builder
  - Some radio button groups were not deselectable
  - Story min-height (medium | tall) often resulted in broken page layout
  - #991 topic target *homepage* was not imported correctly.
- Forum:
  - #951 The forum post page counter is always incremented by 2 when the page is opened.
  - Fixed HTML links are not displayed in posts.
- Fixed a filter reset of the product grid does not work correctly.
- Fixed validation issues when saving guest customers.
- #1066 Web API: fixed schema validation errors for `MaxLength` attribute and OpenAPI `OperationId`.
- #1072 Missing customer welcome message after approval of the registration by admin.
- #897 Discount code input seems to be confirmed (border color and check icon)
- #964 Removed meta information from publication according to catalog settings.
- Fixed shoping cart MinOrderValidation 
- Added quantity information on non-editable wishlist page.
- Some external authentication methods (like AmazonPay) were not displayed on customer pages.
- Hitting the return key in the text field of a product variant resulted in a 404 status error.
- Fixed *QuantityBelowStoreOwnerNotification* was sent twice.
- #1001 MediaManager: fix *moov atom not found* ffmpeg issue in `VideoHandler`.
- Fixed the e-mail subject was not transferred when sending an e-mail from customer edit page.
- Fixed offcanvas problem whith mega sized page builder stories.
- PayPal: Fixed payment discount problem (discount from a formerly choosen payment method was applied).
- #1042 Fixed broken roxy file manager.
- #969 Promo badges are not rendered in frontend due to type mismatch.
- Google Analytics: Fixed problem with single quotation mark in category name.
- #983 Uploaded product variant file is lost after selecting any other variant option (in product details).
- Fixed missing line breaks for product attributes in the print/PDF view of orders.
- SEO: Marked product list filter option links as nofollow.
- SEO: fixed redirection error for TrailingSlashRule setting redirect.
- Hide the cookie manager for topics that need to be fully visible without being overlayed by the cookie manager dialog.
- #1091 Allow recursive cache access in `AlbumRegistry.GetAlbumDictionary()`
- #1088 Special characters (like Umlaut) are not displayed correctly in client-side messages.
- PayPal: Fixed VAT exempt & currency conversion problems


## Smartstore 5.1.0

### New Features

- Currency rounding:
  - Added currency properties and settings for rounding: midpoint rounding, skip rounding when displaying net prices, round unit price before or after quantity multiplication
  - Amounts are always rounded when calculating the order total to avoid rounding differences (usually of 1 cent)
- The **recycle bin** for products enables to restore and permanently delete products
- **payever** plugin (commercial)
- **Botsonic** plugin (commercial)
- Media Manager now allows image files to be re-processed based on the current media and post processor settings.
- Cart quantity input control now respects the product's available stock (as max input)
- #823 Added canonical URL to search pages
- #745 Page Builder: depend story visibility on user roles
- Added option to configure allowed characters for user names 
- #836 Added option to define additional lines for robots.txt

### Theming

- Infrastructure
  - Forked & customized **Bootstrap** framework. Our implementation combines version 4.6 and 5.x. We have not made any modifications to the JavaScript files; only SCSS has been customized. The primary objective is to maintain compatibility with the original Bootstrap documentation for developer convenience.
  - Logical CSS for better RTL support
  - Added subtle and emphasis colors to the color system
  - Revamped button styling
  - Revamped dropdown styling
  - Revamped select2 styling
  - Revamped pagination styling
  - Revamped modal window styling
  - Badges: new variants and modifiers `.badge-subtle`, `.badge-outline-*`, `.badge-ring`, `.badge-counter`
  - Revamped check & radio styling: new variants and modifiers `.form-check-solo`, `.form-check-{color}`, `.form-switch`, `.form-switch-lg`
  - Many improvements to input groups, button groups and collapsibles
  - Sass variables for all easings, contained in `_easings.scss`
  - Dropped responsive (rfs) spacing
- Frontend
  - New grid breakpoint **xxl** (1400px)
  - Made components *rounder* by slightly increasing border-radius variables
  - Revamped product listing action bar styling (sorting, view mode, pagination)
  - On screens < md, the offcanvas window for product filter slides in from bottom and also provides the sorting options
  - Revamped offcanvas main menu
  - Revamped image gallery styling
  - Revamped cart, wishlist & order confirm
  - Fixed Slick slider dotted navigation responsiveness issues
  - Slightly improved InstantSearch box and dropdown
  - Revamped product tags component
  - Dropdown product quantity input (automatically rendered instead of spinner if possible quantities do not exceed 100).
  - Revamped checkout progress indicator
  - Revamped scroll-to-top button
  - Revamped cookie consent dialog styling
  - Dropped *Triple Date Picker* component in favour of browser native date picker
  - Unstyled links are underlined by default
  - New responsive and collapsible MyAccount menu with avatar image, customer name and email address in header.
- Backend
  - Revamped Configuration / Settings and made settings menu responsive
  - Revamped plugin and provider managers
  - New colorpicker component with swatches
  - Revamped number input styling
  - Locale editor tab navigation has been redesigned and is now more responsive

### Improvements

- Increased performance:
  - Added hash codes to attribute combinations. With a large number of attribute combinations, variants are found much faster now.
  - Fixed slow loading of product categories for a large number of categories. Price calculation and product export were affected.
  - MediaSearcher always performed a LIKE search for terms (the equals operator was missing)
  - #820 More scalable media service dupe file detection
- Improved plugin & provider manager UI
- MegaSearch: #842 added hit count for availability filter. Hide filter if it leads to no further hits.
- Use billing address if there is no shipping address and tax calculation is based on shipping address.
- #580 Added caching for live currency exchange rates.
- #767 Handle tier prices in depending prices module.
- #378 Remove the assignments of products to a tax category when the tax category is deleted.
- Blogs and news: added counter for pageviews and author field. Both displayed for admins only.
- Product tags:
  - #388 Ajaxify product tag selection in backend.
  - Added search panel to product tags grid.
- Web API:
  - #142 Take back in stock notifications into account.
  - #805 Add endpoints to assign discounts to entities.
  - #821 Add endpoints for RecurringPayment and RecurringPaymentHistory.
  - Add endpoints for the recycle bin of products.
- Import:
  - #390 Add a setting for whether to send the completion email.
  - #377 Import cross- and checkout-selling products.
- #503 Don't round quantity unit amount for PAnGV.
- #403 Added preview image link of NewsItem to RSS feed.
- #276 Enable to set the time of day for start and end date of discounts.
- #486 Add setting to capture payment when order status changes to *complete*.
- #782 Make the total weight of a shipment editable.
- #782 Enable to mark shipments of an order as *shipped* or *delivered* via orders grid.
- Added ability to edit delivery time in product grid
- #807 Enable absolute Paths for DataExchangeSettings.ImageImportFolder.
- #786 Replace TripleDatePicker with native input type date.
- #804 Implemented the new TrustBadge integration mode, including new settings for positioning, color scheme, etc. 
- #819 Fix zoom on product detail page when there is a large right column.
- PayPal credit card: Removed address fields and integrated Strong Customer Authentication (SCA) instead.
- #834 Make the expiration date of visitor cookies of guests configurable.
- #493 Display quantity name plural when quantity unit is more then 1.
- #847 EU check VAT service: switch from SOAP to REST API
- #763 Display admin edit button for public entities on touch displays  

### Bugfixes

- Price calculation:
  - Rounding differences between the subtotal and the sum of the line totals.
  - Manufacturer discount is ignored as soon as an attribute with a linked product is selected.
- Product lists:
  - Fixed do not show tax info in product lists if the product is tax exempt.
  - Fixed call for price note not displayed in product lists.
- MegaSearch:
  - Fixed incorrect search results when a multistore has different default languages configured.
  - Fixed an incorrect second search result, executed via a spell checker suggestion, when the first search did not return any hits.
- Import:
  - Fixed localized properties were not updated during import.
  - Localized SeName was only updated when import file also contained a non-localized SeName column.
  - Fixed duplicate imported images if they were assigned to several products within a batch.
- Fixed the category navigation no longer shows the number of contained products after reindexing.
- Fixed products associated to a grouped product cannot be deleted via associated products grid.
- Fixed not yet awarded reward points were reduced when an order was deleted.
- Checkout attributes:
  - Fixed wrong tax was applied to checkout attributes in checkout.
  - Fixed checkout attributes that are not active anymore should not be applied.
- Web API: 
  - Fixed 404 file swagger.json not found when opening Swagger documentation in a virtual directory.
  - #815 Import of customers via WebApi/OData sets PasswordFormat to 'clear'.
- Fixed incorrect message when applying a discount coupon code on cart page.
- Fixed the saving of multistore settings for default tax and default shipping address.
- Fixed a scripting issue where the input focus of the direct debit payment form was mistakenly set to a wrong input element.
- Fixed missing cache clearing after importing localized properties.
- Output Cache: missing `Content-Type` header when serving page from cache
- #531 Error reading import file with localized values of languages with the same UniqueSeoCode.
- Fixed discounts assigned to categories and limited to customer roles should be excluded from export and exported prices.
- Fixed MainPictureId not applied on product edit page if missing and if there is only one picture assigned to a product.
- Fixed product image gallery issue where no image was displayed at all.
- #843 Additional shipping charge displayed in product details even if free shipping is activated.
- Fixed "The requested service 'Other (Smartstore.Core.Rules.IRuleProvider)' has not been registered" when creating a rule.
- Fixed #792 Downloaded language sometimes cannot be deleted when using SQLite.
- Stripe: Fixed incorrect system name in several places.
- Gift cards were not generated according to the order item quantity during order processing.
- Fixed incorrect cart validation if minimum cart quantity and quantity step were configured for a product.
- #810 Doubleclicking login button can lead to 400 BadRequest error page.
- Fixed arithmetic overflow SqlException in `ShippingByWeight` and `ShippingByTotal` four decimal places migrations.
- #776 PayPal credit card payment fails due to missing session PayPalOrderId entry.
- #797 Incorrect validation when product can be added to the cart in single positions.
- Manufacturer pictures were not displayed on product detail pages.
- #828 Queued email identifier is 0 for order completed email.
- #873 Category preview may show 404 page if the category is limited to a certain store.
- Do not fallback to "Product is not available" delivery info on product detail page if the product is available.
- #839 Output cache must be invalidated when menu items are added or edited.
- OpenTrans: fixes RuntimeBinderException "cannot perform runtime binding on a null reference" when exporting shipping address.
- Brand pictures weren't displayed in product lists.
- Skin select2 if remote url is used (rules in admin area were unusable on touch devices).
- When using Stripe or PayPal on cart page checkout attributes were reseted.


## Smartstore 5.0.5

### Breaking Changes

- `Store.SecureUrl` and `Store.ForceSslForAllPages` are deprecated now. By default, all pages are secured if `Store.SslEnabled` is true.
- Reindexing of MegaSearch search index required to include new category tree path.

### New Features

- **PersonalPromo** plugin (commercial)
- **Wallet** plugin (commercial)
- #251 MegaSearch: New option, when enabled, search hits must contain all search terms (logical AND operation). The more terms you enter, the fewer hits will be returned.
- Reverse proxy configuration using `ForwardedHeadersMiddleware`
- SqliteDataProvider: 
  - Fixed case-insensitive `Contains()` and `Like()` failing on non-ascii characters.
  - Implemented BackupRestore feature
- Web API:
  - #618 added an endpoint for adding a product to the shopping cart.
  - #709 added an endpoint that returns order data prepared for an invoice (including variant SKU).
  - #717 added `GiftCard`, `GiftCardUsageHistory`, `DiscountUsageHistory`, `CheckoutAttribute` and `CheckoutAttributeValue`.
  - #723 store the SKU of the ordered product variant at the `OrderItem` entity.
- Payment method brand icons can be displayed on product detail pages
- Added settings for more social links (Flickr, LinkedIn, Xing, TikTok, Snapchat, Vimeo, Tumblr, Ello, Behance) to be displayed in the footer.
- SEO: trailing slash options for internal links (*Append trailing slash to links*, *Trailing slash mismatch rule*)
- #480 Product export: add a filter for categories and an option to include all sub-categories.
- #437 Rule builder: add cart rule for delivery time.
- #559 Forum: add setting to prevent registered customers from posting forum topics and posts.

### Improvements

- Theming
  - Better usability of the backend on small mobile devices (especially DataGrid)
  - Updated FontAwesome library from version 6.0.0 to version 6.4.0
  - Checkout / Payment page can display payment method brand icons
- Increased performance: category tree path to filter products by categories.
- DataGrid now remembers the search filter state across requests
- Better PayPal implementation: added providers for every single payment option.
- New user agent parser with much better bot, mobile/tablet detection (but less accurate platform and device detection)
- #416 Make the language name localizable.
- Specify custom database collation during installation.
- Added a search filter for country grid.
- Add X-Frame-Options `SAMEORIGIN` to response headers
- Use `308 - Permanent redirect` status code for HTTPS redirection (instead of `301 - Moved permanently`)
- (DEV) Model mapping (`IMapper<TFrom, TTo>`):
  - Composite mappers: multiple mappers for a single type pair
  - Named mappers
  - Mapper lifetimes (transient, scoped, singleton)
- Search
  - Search box UI improvements
  - #441 Enable multistore configuration for SearchSettings.SearchFields.
- #658 Limit number of product clones to generate either via UI or code (or both)
- #736 Backend > Product grid: Search By SKU should also search for GTIN & MPN
- #682 Allow to set the default email account per shop.
- Google Analytics: Added option to enable admin to prohibit the script from being loaded without explicit consent by the visitor
- #406 Exclude current product from recently viewed products block on product detail page.
- Added setting to automatically display CookieManager if the shop visitor is from the EU.

### Bugfixes

- App restart/recycle could occasionally leak or fail
- PdfConverter failed after cron job cleaned up temporary files in App_Data directory
- Link from Admin area to a named area-less frontend route did not properly prepend culture code
- Identity:
  - Fixed *checkout loop* after logout.
  - #753 During registration a new customer is created although the password is invalid.
- Export:
  - `IWorkContext` CurrentCustomer, WorkingLanguage and WorkingCurrency must be set according to projection.
  - Price calculation must respect `TaxSettings` of projected store.
  - Fixed wrong exported price when attribute combinations exported as products and price display type is set to lowest price.
  - When creating a profile, the public folder name of another profile should not be copied, but a unique, new folder name should be used.
- Fixed picture and color control were not displayed when editing a product attribute option.
- Fixed display of orders in MyAccount area when `OrderSettings.DisplayOrdersOfAllStores` was set to `true`
- Fixed RTL theme Sass parsing error
- When an order was placed, the stock quantity of attribute combinations were not updated if the stock is managed by attributes.
- Fixed "also purchased products" should not display unpublished products.
- #651 Fixed product can be added to cart even if the availability limit was reached.
- #748 The "Recently viewed products" section displays items in stores to which it has not been assigned.
- Logout was not possible after new customer registrations.
- Fixed unpublished products should be assignable as "promoted" to a product.
- `SequentialDataReader`: fixed a problem where occasionally nullable string fields were not read
- UrlRewriter: raw rules were not loaded from legacy storage
- #731 Entering invalid URL as menu item url results in unrecoverable error.
- Fixed a topic was not editable if a menu link with an invalid target was associated.
- `MainMenuShrinker` was missing.
- Added missing properties in customer grid.
- Fixed popular product tags were not loaded based on the frequency of their product assignments on homepage.
- Fixed after a login the user was not redirected to the previous page.
- Fixed problem where bundles couldn't be added to the basket if bundleitems had attributes.
- GMC: when there was no Google category defined in a product, copying the product threw `NullReferenceException`.
- Fixed attribute filters of bundle items were not loaded in bundle item dialog.
- GoogleAnalytics:
  - Fixed a `NullReferenceException` in `GoogleAnalyticsViewComponent`.
  - Fixed `KeyNotFoundException` in `GoogleAnalyticsScriptHelper.GetOrderCompletedScriptAsync`.
- Fixed incorrect order of product attributes on the shopping cart page.
- Fixed #687 Product `MainPictureId` changed each time a new picture is uploaded.
- Fixed ArgumentNullException in `FixProductMainPictureId` when uploading new image on product edit page.
- Fixed links to assigned shipping methods were not displayed when editing a rule.
- Fixed SearchLog did not show top search terms in instant search.
- Fixed redirect to login page instead of an error message when a guest wants to checkout and anonymous checkout is not allowed.
- Fixed the category number in category navigation was not displayed when the catalog setting `ShowCategoryProductNumberIncludingSubcategories` was changed.
- #681 For a message template, the assignment to the e-mail account is not removed when the e-mail account is deleted.
- #704 Cart page displays outdated discount information when applying or removing a discount coupon code.
- #751 Summernote: Inserting a table places it at the top of the HTML editor
- Fixed rare bug where shipping rates wouldn't be applied due to rounding issues
- ShippingByWeight: fixed missing `SmallQuantityThreshold` in initial migration.
- #619 Shipping by weight and by total should use 4 instead of 2 decimal places for currency values.
- For a BundleItem with attribute filters, the attribute selection on the product detail page disappeared as soon as an attribute was selected.
- #724 Selecting other languages resets selected sorting/items per page setting 
- Legacy widget zones weren't considered anymore
- DevTools: Some widget zones weren't displayed due to incorrect RegEx
- NumberInput: Min Max should be handled by validation framework or in frontend by native HTML elements.
- Fixed RuntimeBinderException in SettingController when no search fields are saved.
- Fixed rule assigned to payment method only applied after clearing the cache.
- Admin dashboard: fixed percentage value for last 28 days in order and customer registrations statistics.

## Smartstore 5.0.4

### New Features

- **PostgreSQL** database support
- **SQLite** database support
- **easyCredit** plugin (commercial)
- **DependingPrices** plugin (commercial). Enables configuration of prices depending on customer groups, language, store or customer number.
- Embedded Base64 image offloader: finds embedded images in long HTML descriptions, extracts and saves them to the media storage.
- New option for mail accounts to configure SMTP connection encryption.
- New app system settings: `UsePooledDbContextFactory`, `UseDbCache`, `UseSequentialDbDataReader`
- Database table statistics in System / System Info (row count, total space, used space)

### Improvements

- Memory management
  - Fixed several memory leaks
  - Disabled DbContext pooling (causes memory leaks)
  - New sequential data reader (disabled by default) uses significantly less memory than the built-in reader. Should be enabled if HTML descriptions are extremely large (> 1 MB).
  - More aggressive garbage collector
  - App now uses significantly less memory under heavy load
- Added price settings for discount requirements to be validated in product lists.
- Faster loading of product lists that contain bundles with per-item pricing.
- MegaSearch:
	- A significant increase in search speed, especially when dealing with large amounts of data.
	- Faster indexing.
	- Word stemming configurable for all languages.
- Added `data-invariant` attribute to number input controls 
- Closed #543 Google Category (GMC) always get lost when copying a product
- Ajax request to external URLs do not add X-XSRF-Tokens anymore
- Definition of several preselected product variant attribute values is now prohibited

### Bugfixes

- #557 If the state is optional for addresses, none should be preselected when creating them.
- #608 Build DeleteGuestCustomers query with LINQ again.
- Fixed ArgumentException "The source argument contains duplicate keys" in case of menus with duplicate system names.
- Fixed SqlClient deadlock exception when resolving guest account by client identity.
- MySQL: fixed migration failure when UTC_TIMESTAMP was used as default value for date columns.
- High data payload: 
	- Fixed InvalidOperationException "A second operation was started on this context instance before a previous operation completed" when opening category (and others) edit page.
	- Fixed many product tags blocks the loading of the product edit page due to initialization of the product tag selection box.
- Fixed discount coupon code could not be applied in some cases.
- PostFinance: fixed "The specified refund amount CHF needs to be rounded to a maximum of 2 decimal places".
- Fixed ArgumentNullException in ProcessImageQuery.Add if name is null.
- Fixed price adjustment of attributes was saved only with two decimal places.
- Fixed missing inventory adjustment when the stock quantity was changed through product grid.
- The link to remove a search filter must not contain a page index, otherwise inconsistent search results will occur.
- Fixed InvalidCastException "Unable to cast object of type 'Newtonsoft.Json.Linq.JObject' to type 'Smartstore.Core.Search.Facets.FacetGroup'" when OutputCache is active.
- Category or manufacturer discount was not applied if no other changes were made to the category or manufacturer except for the discount assignment.
- Cart rules assigned to a payment method were not applied in checkout.
- MegaSearch: fixed a memory leak during indexing.
- Fixed native validation for quantity inputs
- Google Analytics: Fixed rendering of prices with thousands separator
- #621 Cart item quantity couldn't be updated if cart item validation returned errors
- #628 sort Triple DatePicker year descending   
- #622 Tab created with Event.cs does not work on Mobile.
- FileManager: Files weren't displayed correctly in backend.
- A product should be found by its SKU if there is a deleted product with identical SKU.
- Links to assigned entities sometimes not displayed on rule edit page.


## Smartstore 5.0.3

### New Features

- (DEV) New `WebhookEndpointAttribute` endpoint metadata. Suppresses creation of guest accounts for webhook calls.
- PayPal: 
	- Added a window to display PayPal account information for support issues 
	- Added setting for upper limit for Pay Upon Invoice 
	- Added option to turn off PayPal buttons on shopping cart page

### Improvements

- Allows to delete and filter assignments of customers to customer roles that were automatically created by a rule.

### Bugfixes

- Installation: changing database connection settings has no effect until app restart
- Fixed HTTP 400 `BadRequest` issue on saving AJAX grid changes
- Web API: 
  - Fixed wrong $metadata configuration of `System.String[]` as a complex type instead of `ICollection<string>`.
  - Fixed `InvalidOperationException` in `Microsoft.OData.Client` using MediaFiles and MediaFolders endpoints.
  - Fixed `InvalidOperationException` in `Microsoft.OData.Client` "An unexpected 'StartObject' node was found for property named 'Size' when reading from the JSON reader. A 'PrimitiveValue' node was expected.".
- Output Cache:
  - Invoking CookieManager view component throws because antiforgery token cannot be written
- Theming
  - Fixed top description displayed instead of bottom description on manufacturer page.
  - Instant search must not display default thumbs if ShowProductImagesInInstantSearch is disabled.
  - Fixed AOS init problem
  - Multiple file uploader instances in a single page did not work
  - Product box in listings must not close when entering the bottom action drop
- Selected tabs were no longer remembered across requests
- Fixed `NullReferenceException` when deleting a shopping cart item.
- Fixed export file count was always 0 in export profile list.
- Fixed `FileNotFoundException` when uploading an import file.
- PayPal: Fixed error that occurs when shipping fees were to high for initially authorized order amount 
- Fixed reward points calculation
- #602 Implemented server side validation of payment data
- #603 Fixed after payment validation failure the data entry form is resetted.
- Fixed CheckoutState change tracking issues
- Fixed IBAN radio button issue when using direct debit offline payment.
- Avoids *deals* label in product lists if the current date is not within the date range of an assigned discount.
- #612 Emails are sent with the email priority low
- Fixed problem with media display for variant attribute combinations 
- Billiger: fixed export profile configuration must not contain store scope selection


## Smartstore 5.0.2

### Breaking Changes

- (DEV) Product._ProductPictures_ renamed to _ProductMediaFiles_

### New Features

- Updated to **.NET 7**
- **Web API** plugin
- **Stripe Elements** plugin
- **BeezUp** (commercial plugin)
- **ElmarShopInfo** (commercial plugin)
- **Shopwahl** (commercial plugin)
- **CartApproval** (commercial plugin)
- New app setting: `DbDefaultSchema`
- (DEV) New action filter attribute `DisallowRobotAttribute`

### Improvements

- ~10 % faster app startup and TTFB
- ~10 % less RAM usage
- Significantly faster attribute combination scanning for large combination sets (1.000+)

### Bugfixes

- `LocaleStringResource` table could contain many dupe records.
- Rule sets were not applied to shipping methods in checkout.
- `ArgumentNullException` when deleting an image assignment on product edit page.
- Despite activated export profile option **per store** no records were exported to a separate file.
- Sometimes Page Builder reveal effects did not run on page load, only on windows resize.
- Product details showed wrong related products.
- Fixed wrong implementation of ByRegionTaxProvider
- Fixed product linkage of product detail ask question message
- Fixed password change issue with user records without username
- Settings couldn't be saved in several places (in migrated shop) 
- Fixed add required products automatically
- DbCache:
  - Fix "Collection was modified; enumeration operation may not execute"
  - Fix "Index was outside the bounds of the array"
- #577 PdfSettings.Enabled displayed twice and PdfSettings.LetterPageSizeEnabled was missing.
- Topics which are rendered as widgets were published to sitemap 
- Redirection problems with changing language & ContactUs page
- Multistore settings couldn't be saved
- File upload for a product attribute is no longer possible once another attribute has been changed.
- Fixes NullReferenceException when placing an order with an invalid email display name.
- Fixed link generation issue: `pathBase` is stripped when target endpoint requires culture code
- Fixed DbUpdateException when deleting a customer address via backend.
- Routing: non-slug, unlocalized system routes did not redirect to localized route
- UrlRewriter: fixed greedy matching (`/en/` should not match `/men/`)
- Fixed RuleSets could not be added or removed from a shipping method.
- Fixed wrong SKU in order XML export if the order contains multiple variants of the same product.
- Fixed payment fee was always displayed in primary currency in checkout.
- Several PayPal fixes

## Smartstore 5.0.1

### Breaking Changes

- (DEV) Product.**OldPrice** renamed to **ComparePrice**

### New Features

- Pricing & GDPR
  - Compliance with **Omnibus Directive**
    - Product reviews: display a **Verified Purchase** badge
    - Label crossed out compare prices with "Lowest" or "Lowest recent price"
  - Free configuration of compare **price labels**, e.g. "MSRP", "Regular", "Before", "Instead of", "Lowest" etc.
  - **Discount badges**, e.g. "Deal", "Limited offer", "Black Friday" etc.
  - **Offer countdown**, e.g. "Ends in 2 days, 3 hours"
  - New pricing settings
    - Always display retail price
    - Default compare price label
    - Default regular price label
    - Offer price replaces regular price
    - Always display retail price
    - Show offer countdown remaining hours
    - Show offer badge
    - Show offer badge in lists
    - Show saving badge in lists
    - Offer badge label
    - Offer badge style
    - Limited offer badge label
    - Limited offer badge style
    - Show price label in lists
    - Show retail price saving
- **EmailReminder** (commercial plugin)
- **DirectOrder** (commercial plugin)
- **Billiger.de** (commercial plugin)
- **Google Remarketing** (commercial plugin)
- **File Manager** (commercial plugin)
- **GiroCode** (commercial plugin)
- **IPayment** (commercial plugin)
- PayPal
	- Added **RatePay** widget
	- Added **Pay per invoice** payment method 
	- Added **PayPal onboarding** to module configuration (handles simple configuration via direct email login without the need to create an app on the PayPal developer page). 
- **cXmlPunchout** (commercial plugin)
- **OCI Punchout** (commercial plugin)
- **BizUrlMapper** (commercial plugin)
- Added **Barcode** encoding and generation infrastructure:
  - Can encode: EAN, QRCode, UPCA, UPCE, Aztec, Codabar, Code128, Code39, Code93, DataMatric, KixCode, PDF417, RoyalMail, TwoToFive
  - Can generate: Image (any type), SVG drawing
- MediaManager: display image **IPTC and EXIF metadata**
- MediaManager: added internal admin comment field
- (DEV) New TagHelpers
  - `sm-suppress-if-empty`: suppresses tag output if child content is empty (due to some conditional logic).
  - `sm-suppress-if-empty-zone`: suppresses parent tag output if a specified child zone is empty or whitespace.
- (DEV) Embedded/Inline mail attachments
- (DEV) Localized entity metadata: `ILocalizedEntityDescriptorProvider`, `ILocalizedEntityLoader`
- (DEV) New setting `SmtpServerTimeout` in *appsettings.json*

### Improvements

- Increased performance
  - Faster app startup
  - ~100 MB less memory usage after app start
- FLEX theme: pure CSS responsive tabs (tabs transform to accordion on screens smaller than md)
- Sticky image gallery in product detail
- (DEV) New methods: TabFactory `InsertBefore()`,`InsertAfter()`, `InsertBeforeAny()`, `InsertAfterAny()`, `InsertAt()`
- (DEV) New attribute for `tab` TagHelper: `sm-hide-if-empty`
- (DEV) New rendering extension method: `IHtmlContent.HasValue()`
- (DEV) New rendering extension method: `IHtmlHelper.RenderZoneAsync()`
- (DEV) DataGrid row editing: handle prefixed controls correctly (e.g. "CustomProperties")
- Additional fees are not allowed by PayPal, therefore removed the feature
- Added cacheable routes for *Google Analytics* widgets
- (DEV) Made MediaImporter more generic
- Removed preconfigured Google Fonts retrieval from Google servers from themes AlphaBlack & AlphaBlue  

### Bugfixes

- `LocalFile` did not implement `CreateFileAsync()` correctly, which led to PackageInstaller, PageBuilder thumbnail cache and PublicFolderPublisher throwing `NotImplementedException`
- Media legacy url redirection did not work: `TemplateMatcher` does not evaluate inline constraints anymore
- *MediaManager* always displayed current date instead of file's last updated date
- *MegaMenu*: fixed badge styling issues
- Fixed "Unknown schema or invalid link expression 'mailto:...'
- Memory cache: parallel key enumeration sometimes failed
- Fixed *Google Analytics* number formatting issues
- Several fixes for laying Emails on a local directory
- Removed payment fee configuration from PayPal plugin
- Fixed Drag&Drop of images for HTML-Editor
- Fixed saving of emails on disk
- After installation of modules with custom Sass imports: bundle disk cache was not invalidated
- #539 Fixed flickering on hovering over product image on product detail page
- #552 <meta itemprop="availability"..> should not be rendered twice
- Fixed theme preview tool display 
- Fixed creating of SeoSlugs with special chars for installation 


## Smartstore 5.0.0

Smartstore 5 is a port of Smartstore 4 - which is based on the classic .NET Framework 4.7.2 - to the new ASP.NET Core 6 platform.

### Highlights

- Smartstore 5 is now **cross-platform** and supports **Linux** and **macOS** alongside **Microsoft Windows**. This means that Smartstore can be run on almost any hosting server, whether dedicated, cloud or shared.
- In addition to **Microsoft SQL Server**, Smartstore now supports **MySQL**. **PostgreSQL** is in planning and will follow soon.
- Smartstore 5 is one of the **fastest out-of-the-box e-commerce solutions in the world**! A small store with less than 1,000 items and a few dozen categories can achieve an average *Time to First Byte* (TTFB) of far below 100 milliseconds... even without output cache or other performance measures.
  - Compared to Smartstore 4, 10x faster in some areas
  - Significantly less memory consumption (approx. 50%)
  - Even low-cost (cloud) hosting delivers high performance
- Powerful **DataGrid** in the backend
  - Developed in-house, no more 3rd party libaries with annoying license restrictions
  - Intuitive, feature-rich and flexible
  - Supports row selection, multi-column sorting, column reordering, column toggling, paging etc.
  - Grid state is persisted in browser's local storage and restored on page refresh
  - Search filter expressions: run complex search queries, e.g. `(*jacket or *shirt) and !leather*`
  - (DEV) `datagrid` TagHelper which lets you control every aspect of the grid
- Frontend & backend **facelifting**
- Create, manage and restore **database backups** in the backend
- More **external authentication** providers: Google, Microsoft, LinkedIn coming soon
- Advanced settings for **image processing**: compression, quality, response caching etc.

### Breaking or Significant Changes

- **Blog**, **News**, **Forum** and **Polls** are now external commercial plugins
- No support for Microsoft SQL Server Compact Edition (SQLCE) anymore
- Payment providers need to be reconfigured (API Key etc.)

### Project Status

- Except for the **WebApi** plugin, the open source Community Edition has been fully ported (WebApi will follow soon)
- Already ported commercial plugins: 
  - Azure
  - BMEcat
  - Common Export Providers
  - Content Slider
  - ETracker
  - GDPR
  - Guenstiger
  - Media Manager
  - Mega Menu
  - Mega Search
  - Mega Search Plus
  - OpenTrans
  - Order Number Formatter
  - Output Cache
  - PageBuilder
  - PdfExport
  - PostFinance
  - Redis
  - SearchLog
  - Skrill
  - Sofortueberweisung
  - TinyImage
  - TrustedShops
  - UrlRewriter
  - *Other commercial plugins developed by Smartstore will follow soon*
- Obsolete plugins that will not be ported: 
  - AccardaKar
  - BizImporter
  - EasyCredit
  - NewsImporter
  - LeGuide Shopwahl

### Development

- No proprietary Unit of Work & Repository Pattern anymore: gave up `IDbContext` and `IRepository<T>` in favor of `DbContext` and `DbSet<T>`
- Less and more lightweight service classes by removing all generic CRUD stuff
- Much easier plugin development
- Async function calls all the way through
- Database schema did not change and therefore remains backward compatible. Still we STRONGLY recommend to create a backup before upgrading
- Extremely powerful widget system
- Large TagHelper library with 50+ custom helpers
- On-demand deployment of native libraries via NuGet
- Custom entities in plugin projects can now define navigation properties to entities in the application core