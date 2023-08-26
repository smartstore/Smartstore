# Release Notes

## Smartstore 5.1.0

### New Features

- **Botsonic** plugin (commercial)
- #745 Page Builder: depend story visibility on user roles.
- Added option to configure allowed characters for user names 

### Improvements

- Increased performance: added hash codes to attribute combinations. With a large number of attribute combinations, variants are found much faster now.
- Improved plugin & provider manager UI
- Use billing address if there is no shipping address and tax calculation is based on shipping address.
- #580 Added caching for live currency exchange rates.
- #767 Handle tier prices in depending prices module.
- #378 Remove the assignments of products to a tax category when the tax category is deleted.
- Blog: added counter for pageviews.
- Product tags:
  - #388 Ajaxify product tag selection in backend.
  - Added search panel to product tags grid.
- #503 Don't round quantity unit amount for PAnGV.
- #403 Added preview image link of NewsItem to RSS feed.
- #390 Import: add a setting whether to send the completion email.
- #276 Enable to set the time of day for start and end date of discounts.
- #142 Web API do not take back in stock notifications into account.
- #486 Add setting to capture payment when order status changes to *complete*.
- #782 Make the total weight of a shipment editable.
- #782 Enable to mark shipments of an order as *shipped* or *delivered* via orders grid.

### Bugfixes

- Price calculation:
  - Rounding differences between the subtotal and the sum of the line totals.
  - Manufacturer discount is ignored as soon as an attribute with a linked product is selected.
- MegaSearch:
  - Fixed incorrect search results when a multistore has different default languages configured.
  - Fixed an incorrect second search result, executed via a spell checker suggestion, when the first search did not return any hits.
- Fixed not yet awarded reward points were reduced when an order was deleted.
- Web API: fixed 404 file swagger.json not found when opening Swagger documentation in a virtual directory.
- Fixed a scripting issue where the input focus of the direct debit payment form was mistakenly set to a wrong input element.
- Fixed missing cache clearing after importing localized properties.
- Output Cache: missing `Content-Type` header when serving page from cache
- #531 Error reading import file with localized values of languages with the same UniqueSeoCode.
- Import:
  - Fixed localized properties were not updated during import.
  - Localized SeName was only updated when import file also contained a non-localized SeName column.
- Fixed MainPictureId not applied on product edit page if missing and if there is only one picture assigned to a product.


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