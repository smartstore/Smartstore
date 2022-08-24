# Release Notes

## Smartstore 5.0.1

### New Features

- New plugin: *Google Remarketing*

### Improvements

- (DEV) New methods: TabFactory `InsertBeforeAsync()` and `InsertAfterAsync()`
- (DEV) DataGrid row editing: handle prefixed controls correctly (e.g. "CustomProperties")
- Additional fees are not allowed by PayPal, therefore removed the feature
- Added cacheable routes for *Google Analytics* widgets

### Bugfixes

- `LocalFile` did not implement `CreateFileAsync()` correctly, which led to PackageInstaller, PageBuilder thumbnail cache and PublicFolderPublisher throwing NotImplementedException
- Media legacy url redirection did not work: `TemplateMatcher` does not evaluate inline constraints anymore
- *MediaManager* always displayed current date instead of file's last updated date
- *MegaMenu*: fixed badge styling issues
- Fixed "Unknown schema or invalid link expression 'mailto:...'
- Memory cache: parallel key enumeration sometimes failed
- Fixed *Google Analytics* number formatting issues


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