# Release Notes

## Smartstore 5.1.0

### New Features

- Updated to **.NET 7**
- **BeezUp** (commercial plugin)
- **ElmarShopInfo** (commercial plugin)
- **Shopwahl** (commercial plugin)
- New app setting: `DbDefaultSchema`

### Improvements

- ~10 % faster app startup and TTFB
- ~10 % less RAM usage

### Bugfixes



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