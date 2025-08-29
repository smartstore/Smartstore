# 🐥 Deploying modules

## Deploying modules

When a module is fully developed, it needs to be published to the store, so that it can be used. To do this, it is built in **release mode** and uploaded to the store using FTP or the **plugin-manager** in the backend. If the latter is chosen, the module must be packaged in a format that Smartstore can recognize and process. This functionality is provided by the **Smartstore Packager**.

### Versioning

`module.json` provides properties for _versioning_. The `Version` property defines the version of the module and `MinAppVersion` specifies the lowest supported Smartstore version that can run the module.

By convention, that the version number should always reflect the current Smartstore version, for which the module was built. If a hotfix has been applied to the module, the revision part of the version number is incremented.

Let's say the release version of a module is `5.0.1`. A hotfix would make the version `5.0.1.1`, and the next revision would be `5.0.1.2`, and so on.

### Build the Module in release mode

Modules that are to be deployed, must be built in **release mode**. If only a single module is to be built, set the Visual Studio solution configuration to `Release` and select `Build` from the module's context menu. The module will be built and placed in the path defined by the `OutputPath` in the project file.

Alternatively, you can run the file that builds the entire solution in **release mode**. Depending on the target platform, one of the following batch files can be used.

* `build/build.linux-x64.cmd`
* `build/build.osx-x64`
* `build/build.win-x64.cmd`
* `build/build.win-x86.cmd`

If you are just building a module, the version of the batch file is irrelevant because modules are built platform-independently.

{% hint style="info" %}
For more information about deployment in general, see [Deployment & Build](../../getting-started/deployment-and-build.md).
{% endhint %}

After successful execution, the compiled application can be found in _build/Artifacts_. The _Modules_ directory contains all modules that were compiled in **release mode**.

### Deployment via FTP or RDP

If access is granted via FTP or RDP, the modules can simply be uploaded to the _Modules_ directory. After restarting the application, modules can be installed using the **plugin-manager** (admin > configuration > plugins). If the modules are already installed, the application must be restarted to load the current modules.

{% hint style="info" %}
Because the modules are loaded by the running application, the page's app pool must be stopped before the files are uploaded.
{% endhint %}

### Packager

If a module is to be distributed to a third party, the module should be provided in a format that allows the third party to install it in their store independently.

This can be done using the **Smartstore Packager**. To use it, open the `Smartstore.Tools.sln` solution located in the Smartstore repository root. Build the `Smartstore.Packager` project from the _Tools_ directory. The resulting `Smartstore.Packager.exe` file can list all modules of a build directory. After starting the packager, select the directory in _Artifacts_, created by the batch file. Clicking on **ReadExtensions** will list all available modules. Now select the module you want to package, an output directory and press **Create Package**.

The packager creates a zip file using the naming pattern `Smartstore.Module.{module systemname}.{current module version}.zip` which will result in something like `Smartstore.Module.MyOrg.MyModule.5.0.zip`.

This packaged module can now be uploaded by any store owner. There is no need to stop the app pool, when using this method and Smartstore will automatically restart the application.
