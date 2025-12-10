# Tenants

## Tenants-Folder

{% hint style="info" %}
Most changes made here require you to restart your Smartstore instance.
{% endhint %}

Contains at least one Tenant-Folder, usually named `Default`. Every directory inside the Tenants-Folder acts as an instance-profile. The folder is located at \src\Smartstore.Web\App\_Data\\**Tenants**\\.

{% hint style="info" %}
If you have multiple workspaces and want the ability to switch between them quickly, create a file `Current.txt` containing the desired workspace name.
{% endhint %}

## The Tenant-Folder

The Tenant-Folder contains information about store-workspaces, including:

* Database connection
* Search index
* Media files
* Export files
* Asset bundles cache
* Data protection keys
* Temporary files

The folder is located at \src\Smartstore.Web\App\_Data\Tenants\\**YourInstanceName**\\

### Settings.txt

This file contains the following information:

<table><thead><tr><th width="214">Key</th><th width="179">Desciption</th><th>Sample value</th></tr></thead><tbody><tr><td>AppVersion</td><td>The current Smartstore version</td><td><code>5.1.0.0</code></td></tr><tr><td>DataProvider</td><td>The database provider</td><td><code>SqlServer</code></td></tr><tr><td>DataConnectionString</td><td>Database connection-string</td><td><code>Data Source=your_host;Initial Catalog=</code><mark style="color:green;"><code>your_database</code></mark><code>;Integrated Security=False;User ID=</code><mark style="color:green;"><code>your_username</code></mark><code>;Password=</code><mark style="color:green;"><code>your_password</code></mark><code>;Enlist=False;Pooling=True;Min Pool Size=1;Max Pool Size=1024;Multiple Active Result Sets=True;Encrypt=False;User Instance=False;MultipleActiveResultSets=True</code></td></tr></tbody></table>

{% hint style="info" %}
Change the database information of your store using `DataConnectionString`.
{% endhint %}

### InstalledModules.txt

This file contains all of your store's installed modules. The names are separated by new lines.

{% code title="InstalledModules.txt" %}
```
Smartstore.ShippingByWeight
Smartstore.Shipping
Smartstore.Tax
Smartstore.OfflinePayment
```
{% endcode %}

{% hint style="info" %}
If a module is interrupting your development, but you don't want to deinstall it, simply remove it from this file.

After a restart, Smartstore will be loaded without the module.
{% endhint %}

### \_temp

This is the temporary directory. The content is removed on a regular basis.

### DbBackups

This directory contains all database backups, created in the maintanance tab.

{% hint style="info" %}
If you need to share your database for debugging, this is the place to go.
{% endhint %}

### ExportProfiles

This directory contains all export profiles. Subdirectories are named after the export module that was used and contain a log file and a directory `Content`.

<table data-header-hidden><thead><tr><th width="177"></th><th></th></tr></thead><tbody><tr><td>log.txt</td><td>The Log-file containing information about the latest export.</td></tr><tr><td>Content</td><td>This directory contains the export-data.</td></tr></tbody></table>

{% hint style="info" %}
If any errors occured during export, you'll find the stack trace in `log.txt`.
{% endhint %}

### Media\Storage

This directory contains all the media uploaded to your store.

### PageBuilder

Contains PageBuilder templates, that can be selected when creating a new Story.

### Sitemaps

Contains all created sitemaps of your store.
