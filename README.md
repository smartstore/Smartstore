<p align="center">
	<a href="https://www.smartstore.com" target="_blank" rel="noopener noreferrer">
		<img src="assets/smartstore-icon-whitebg.png" alt="Smartstore" width="120">
	</a>
</p>

<h3 align="center">
	<img src="assets/smartstore-text.png" alt="Smartstore" width="250">
</h3>
<h3 align="center"><strong>Ready. Sell. Grow.</strong></h3>
<p align="center">
    A modular, scalable and ultra-fast open-source all-in-one eCommerce platform built on ASP.NET Core 7.
</p>
<p align="center">
	<a href="#try-it-online">Try Online</a> ∙ 
	<a href="http://community.smartstore.com">Forum</a> ∙ 
	<a href="http://community.smartstore.com/marketplace">Marketplace</a> ∙ 
	<a href="http://translate.smartstore.com/">Translations</a>
</p>
<br/>
<p align="center">
  <img src="assets/sm5-devices.png" alt="Smartstore Demoshop" />
</p>

Smartstore is a cross-platform, modular, scalable and ultra-fast open source all-in-one eCommerce platform based on ASP.NET Core 7, Entity Framework, Vue.js, Sass, Bootstrap and more.

**Smartstore includes all the essential features to create multi-language, multi-store, multi-currency shops** targeting desktop or mobile devices and enables SEO-optimised, rich product catalogs with support for unlimited number of products and categories, variants, bundles, datasheets, ESD, discounts, coupons and much more.

A comprehensive set of tools for CRM & CMS, Sales, Marketing, Payment & Shipping Handling etc. makes Smartstore a powerful all-in-one solution that meets all your needs.

**Smartstore delivers a beautiful and configurable shop frontend out-of-the-box**, built with a high level design approach, including components like `Sass`, `Bootstrap` and others. The included *Flex* theme is modern, clean and fully responsive, giving shoppers the best possible shopping experience on any device.

The state-of-the-art architecture of Smartstore - with `ASP.NET Core 7`, `Entity Framework Core 7` and Domain Driven Design approach - makes it easy to extend, extremely flexible and basically fun to work with ;-)

* :house: **Website:** [http://www.smartstore.com](http://www.smartstore.com)
* :speech_balloon: **Forum:** [http://community.smartstore.com](http://community.smartstore.com)
* :mega: **Marketplace:** [http://community.smartstore.com/marketplace](http://community.smartstore.com/marketplace)
* :earth_americas: **Translations:** [http://translate.smartstore.com/](http://translate.smartstore.com/)
* :blue_book: **Documentation:** [Smartstore Documentation in English](https://smartstore.atlassian.net/wiki/spaces/SMNET60/pages/2511044691/Getting+Started)
* ▶️ **Azure Marketplace:** [https://azuremarketplace.microsoft.com](https://azuremarketplace.microsoft.com/marketplace/apps/smartstore-ag.smartstorenet?tab=Overview)
<p>&nbsp;</p>

## Technology & Design

* State-of-the-art architecture with `ASP.NET Core 7`, `Entity Framework Core 7` and domain-driven design
* Cross-platform: run it on Windows, Linux, or Mac
* Supports `Docker` out of the box for easy deployment
* Composable, extensible and highly flexible due to modular design
* Highly scalable with full page caching and web farm support 
* Powerful theme engine allows you to create or customise themes & skins with minimal effort thanks to theme inheritance
* Point&Click theme configuration
* Liquid template engine: highly flexible templating for emails and campaigns with auto-completion and syntax highlighting
* Html to PDF converter: creates PDF documents from regular HTML templates, radically simplifying the customisation of PDF output
* Consistent and sophisticated use of modern components such as `Vue.js`, `Sass`, `Bootstrap` & more in the front and back end.
* Easy shop management thanks to modern and clean UI
<p>&nbsp;</p>

## Key Features



<p>
  <img src="assets/sm5-screens.png" alt="Smartstore Screenshots" />
</p>


* Multi-store support
* Multi-language and full RTL (Right-to-Left) and Bidi(rectional) support
* Multi-currency support
* Product bundles, variants, attributes, ESD, tier pricing, cross-selling and more
* Sophisticated marketing & promotion capabilities (gift cards, reward points, discounts of any kind, and more)
* Reviews & Ratings
* Media Manager: powerful and lightning fast media file explorer
* Rule Builder: powerful rule system for visual business rule creation with dozens of predefined rules out-of-the-box
* Search framework with faceted search support. Ultra-fast search results, even with millions of items!
* Extremely scalable through output caching, REDIS & Microsoft Azure support
* Tree-based permission management (ACL) with inheritance support
* Sophisticated import/export framework (profiles, filters, mapping, projections, scheduling, deployment... basically everything!)
* CMS Page Builder: Create compelling content that drives sales. No coding required thanks to a powerful WYSIWYG editor using the revolutionary CSS grid system
* Blog, forum, polls, custom pages & HTML content etc.
* CMS Menu Builder: visual manager for all kinds of menus. Modify existing menus or create your own and place them anywhere you want.
* Modern, clean, SEO-optimized and fully responsive `Bootstrap`-based theme
* Support for hierarchical SEO slugs, e.g.: *samsung/galaxy/s22/32gb/white*
* *Trusted Shops* pre-certification and full EU-GDPR compliance
* 100% compliant with German law
* Sales-, Customer- & Inventory Management
* Comprehensive CRM features
* Powerful layered shop navigation
* Numerous payment and shipping providers and options
* Wallet: allows full or partial payment of orders via credit account
* TinyImage: achieves ultra-high image compression rates (up to 80%!) and enables WebP support
* Preview Mode: virtually test themes and stores more easily
* RESTful WebApi (coming soon)
<p>&nbsp;</p>

## Getting Started

### System requirements

#### Supported operating systems

* Windows 10 (or higher) / Windows Server 2012 R2 (or higher)
* Ubuntu 14.04+
* Debian 11+
* Mac OS X 10.11+

#### Supported database systems

- Microsoft SQL Server 2008 Express (or higher)
- MySQL 8.0+
- PostgreSQL 11+
- SQLite 3.31+

### Upgrade from Smartstore.NET 4.2

Smartstore 5 is a port of [Smartstore.NET 4](https://github.com/smartstore/SmartStoreNET) - based on the classic .NET Framework 4.7.2 – to the new `ASP.NET Core 7` platform. Smartstore instances based on classic `ASP.NET MVC` can be upgraded seamlessly. To [upgrade](https://smartstore.atlassian.net/wiki/spaces/SMNET50/pages/1956118609/Updating+from+Previous+Versions), all you need to do is replace the application files on your server - except for the `App_Data` directory - and **all your data will automatically be transferred to the new system**. [See the documentation for detailed information on installing or upgrading your store](https://smartstore.atlassian.net/wiki/spaces/SMNET50/pages/1956118822/Installing+Smartstore).

 :information_source: Upgrading from versions older than 4.2 is not possible. Therefore, you should migrate Smartstore.NET to version 4.2 first and then upgrade to Smartstore 5.

### Visual Studio

- Clone the repository using the command `git clone https://github.com/smartstore/Smartstore.git` and checkout the `main` branch.
- Download Visual Studio 2022 (any edition) from https://www.visualstudio.com/downloads/
- Open `Smartstore.sln` and wait for Visual Studio to restore all NuGet packages
- Make sure `Smartstore.Web` is the startup project and run it

### Repository Structure

- [`Smartstore`](https://github.com/smartstore/Smartstore/tree/main/src/Smartstore) contains common low-level application-agnostic stuff like bootstrapper, modularity engine, caching, pub/sub, imaging, type conversion, IO, templating, scheduling, various utilities, common extension methods etc.
- [`Smartstore.Data`](https://github.com/smartstore/Smartstore/tree/main/src/Smartstore.Data) contains database providers
- [`Smartstore.Core`](https://github.com/smartstore/Smartstore/tree/main/src/Smartstore.Core) contains application specific modules like catalog, checkout, identity, security, localization, logging, messaging, rules engine, search engine, theme engine, migrations etc.
- [`Smartstore.Web.Common`](https://github.com/smartstore/Smartstore/tree/main/src/Smartstore.Web.Common) contains common web features like custom MVC infrastructure, bundling, TagHelpers, HtmlHelpers etc.
- [`Smartstore.Modules`](https://github.com/smartstore/Smartstore/tree/main/src/Smartstore.Modules) contains all module/plugin projects
- [`Smartstore.Web`](https://github.com/smartstore/Smartstore/tree/main/src/Smartstore.Web) is the entry host project that contains controllers, model classes, themes, static assets etc.

<p>&nbsp;</p>

## Build Smartstore

### Option 1 - by publishing the entry host project

1. Open the Smartstore solution in Visual Studio 2022
2. Use **Release** configuration
3. (Re)build the solution
4. Publish host project **Smartstore.Web**

### Option 2 - by running a build script

Run the build script corresponding to your target platform in the **build** directory: `build.{Platform}.cmd`. The resulting build will be placed in the `build/artifacts/Community.{Version}.{Platform}` directory. A zip archive in **build/artifacts/** is created automatically.

By default, the build script produces a platform-specific, self-contained application that includes the ASP.NET runtime and libraries, the Smartstore application and its dependencies. You can run it on any machine that doesn't have the .NET runtime installed.

Smartstore uses Nuke (https://nuke.build/) as its build automation solution, which makes it easy to customize the build process by editing `src/Smartstore.Build/Smartstore.Build/Build.cs`.

### About the "src/Smartstore.Web/Modules" directory

While building the solution, all modules in `src/Smartstore.Modules/` are detected, compiled and placed in the `src/Smartstore.Web/Modules/` directory. The application runtime uses this directory as a source from which modules are 
loaded dynamically. During development, however, the "Modules" directory is irrelevant. You can safely delete it at any time.

### Creating Docker images

To create a Docker image, run `build/dockerize.{Platform}[.nobuild].sh`.

##### dockerize.linux.sh

Creates a Debian Linux base image including the complete ASP.NET runtime, builds the solution and publishes a framework-dependent application inside the Linux container. It also installs the native **wkhtmltopdf** library needed to generate PDF files.

##### dockerize.linux.nobuild.sh

Much faster, but requires that the application has already been built and is located in `build/artifacts/Community.{Version}.linux-x64`. Creates a Debian Linux base image with only the ASP.NET runtime dependencies and copies the build artifact. It also installs the native **wkhtmltopdf** library needed to generate PDF files.

##### dockerize.windows.nobuild.sh

Creates a Windows Nano Server base image with only the ASP.NET runtime dependencies and copies the build artifact. Requires that the application has already been built and is located in `build/artifacts/Community.{Version}.win-x64`. It also requires that the Docker engine is running a Windows image.

### Creating Docker containers

To create a ready-to-run Docker container with a database server run `compose.{DbSystem}.sh`. 

##### compose.mysql.sh

Creates a composite Docker container containing the **smartstore** application image and the latest **MySql** image.

##### compose.sqlserver.sh

Creates a composite Docker container containing the **smartstore** application image and the latest **MS SQL Server** image.
<p>&nbsp;</p>

## Try it online

We have set up a live online demo for you to test Smartstore without a local installation. Get a first impression and test all available features in the frontend and backend. Please note that the backend demo is shared and other testers can modify data at the same time.

* [**Frontend**](https://core.smartstore.com/frontend/en) (User: demo, PWD: 1234)
* [**Backend**](https://core.smartstore.com/backend/admin/) (User: demo, PWD: 1234)
<p>&nbsp;</p>

## License

Smartstore Community Edition is released under the [AGPL license](https://www.gnu.org/licenses/agpl-3.0.de.html).

**Add a star to our repository** to stay up-to-date, get involved or just watch how we're doing. Learn about the latest developments, actively participate and don't miss out on new releases.
