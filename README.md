| <div align="left">:information_source: **Work in progress: Porting Smartstore to cross-platform ASP.NET Core**</div> |
| --- |
| In this repository we are currently working hard on porting Smartstore from classic ASP.NET MVC to ASP.NET Core 5. This project is currently under development and is **not yet suitable for use in production environment**. For production, use the latest stable release from [Smartstore.NET repository](https://github.com/smartstore/SmartStoreNET). Detailed information about our porting strategy can be found below in the section **ASP.NET Core 5**. |

<br/>

<p align="center">
	<a href="https://www.smartstore.com" target="_blank" rel="noopener noreferrer">
		<img src="assets/smartstore-icon.png" alt="Smartstore.NET" width="200">
	</a>
</h1>

<br/>
<br/>

<h1 align="center">
	<img src="assets/smartstore-text.png" alt="Smartstore.NET" width="300">
</h1>
<p align="center"><strong>Ready. Sell. Grow.</strong></p>

<p align="center">
	<a href="#try-it-online">Try Online</a> ∙ 
	<a href="http://community.smartstore.com">Forum</a> ∙ 
	<a href="http://community.smartstore.com/marketplace">Marketplace</a> ∙ 
	<a href="http://translate.smartstore.com/">Translations</a>
</p>
<br/>

Smartstore is a free, open source, full-featured e-commerce solution for companies of any size. It is web standards compliant and incorporates the newest Microsoft web technology stack.

**Smartstore includes all essential features to create multilingual and multi-currency stores** targeting desktop or mobile devices and enabling SEO optimized rich product catalogs with support for an unlimited number of products and categories, variants, bundles, datasheets, ESD, discounts, coupons and many more.

A comprehensive set of tools for CRM & CMS, sales, marketing, payment & shipping handling, etc. makes Smartstore a powerful all-in-one solution fulfilling all your needs.

<br/>
<p align="center">
  <img src="assets/sm4-devices.png" alt="Smartstore.NET Demoshop" />
</p>
**Smartstore delivers a beautiful and configurable shop front-end out-of-the-box**, built with a design approach on the highest level, including components like `Bootstrap 4`, `Sass` and others. The supplied theme _Flex_ is modern, clean and fully responsive, giving buyers the best possible shopping experience on any device. 

The state-of-the-art architecture of Smartstore - with `ASP.NET Core 5`, `Entity Framework Core 5` and Domain Driven Design approach - makes it easy to extend, extremely flexible and essentially fun to work with ;-)

* **Website:** [http://www.smartstore.com/en/net](http://www.smartstore.com/en/net)
* **Forum:** [http://community.smartstore.com](http://community.smartstore.com)
* **Marketplace:** [http://community.smartstore.com/marketplace](http://community.smartstore.com/marketplace)
* **Translations:** [http://translate.smartstore.com/](http://translate.smartstore.com/)
* **Documentation:** [Smartstore Documentation in English](http://docs.smartstore.com/display/SMNET/SmartStore.NET+Documentation+Home)

<p>&nbsp;</p>

## ASP.NET Core 5
In this repository we are currently working hard on a cross-platform port of [Smartstore](https://github.com/smartstore/SmartStoreNET) to `ASP.NET Core 5`. For best code quality we decided to port **class by class** and adapt the existing code to the new environment. The porting is scheduled to be **completed by the beginning of the second quarter of 2021**. Once the first `ASP.NET Core` based release is published, Smartstore instances based on classic `ASP.NET MVC` can be upgraded seamlessly. To perform the upgrade, only the app files need to be replaced on your server - except for the `App_Data` directory - and **all data will be automatically transferred to the new system**. After the first public release in this repository upcoming development will only take place here. 

**Give our new repository a star** to stay up-to-date, get involved or just watch how we're doing. Learn all about the latest development, participate actively and last but not least, don't miss the day of release.    

**Important:** And once again... this project is currently under development and is **not yet suitable for use in production environment**. For production, use the latest stable release from [Smartstore.NET repository](https://github.com/smartstore/SmartStoreNET)..


## Highlights

### Technology & Design

* State of the art architecture thanks to `ASP.NET Core 5`, `Entity Framework Core 5` and Domain Driven Design
* Easy to extend and extremely flexible thanks to modular design
* Highly scalable thanks to full page caching and web farm support 
* A powerful theming engine lets you create themes & skins with minimum effort thanks to theme inheritance
* Point&Click Theme configuration
* Highly professional search framework based on Lucene.NET, delivering ultra fast faceted search results
* Powerful and lightning-fast media manager
* Powerful rule system for visual business rule creation
* Consistent and sophisticated use of modern components such as `Bootstrap 4`, `Vue.js`, `Sass` & more in the front and back end.
* Easy shop management thanks to modern and clean UI

### Features

* Unlimited number of products and categories
* Multi-Store support
* Multi-language and RTL support
* Media Manager
* Rule Builder
* Product Bundles
* RESTful WebApi
* CMS Page Builder
* CMS Menu Builder
* Modern, clean, SEO-optimized and fully responsive Theme based on Bootstrap 4
* Ultra fast search framework with faceted search support
* Extremely scalable thanks to output caching, REDIS & Microsoft Azure support
* Tree-based permission management (ACL) with inheritance support
* *Trusted Shops* precertification
* 100% compliant with German jurisdiction
* Sales-, Customer- & Inventory-management
* Comprehensive CRM features
* Powerful Discount System
* Powerful layered navigation in the shop
* Numerous Payment and Shipping Providers and options
* Sophisticated Marketing & Promotion capabilities (Gift cards, Reward Points, discounts of any type and more)
* Reviews & Ratings
* CMS (Blog, Forum, custom pages & HTML content etc.)
* and many more...



### System requirements

* Windows 7 SP1 (or higher) / Windows Server 2008 R2 SP1 (or higher)
* Mac OS X 10.11, 10.12
* Red Hat Enterprise Linux 7
* Ubuntu 14.04, 16.04, 17
* MS SQL Server 2008 Express (or higher), MySQL or SQLite

## Try it online

(Smartstore classic based on ASP.NET MVC) We have set up a live online demo for you so you are able to test Smartstore without local installation. Get a first impression and test all available features in the front- and in the backend. Please keep in mind that the backend demo is shared and other testers can modify data at the same time.

* [**Frontend**](https://demo.smartstore.com/frontend/en) (User: demo, PWD: 1234)
* [**Backend**](https://demo.smartstore.com/backend/en/login) (User: demo, PWD: 1234)

## License

Smartstore Community Edition is released under the [GPLv3 license](http://www.gnu.org/licenses/gpl-3.0.txt).
