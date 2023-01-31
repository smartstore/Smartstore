# Instructions how to build Smartstore



There are two options to build Smartstore and several ways to build Docker images and containers.

## Option 1 - Publish the entry host project

1. Open the Smartstore solution in Visual Studio 2022
2. Use **Release** configuration
3. (Re)build the solution
4. Publish host project **Smartstore.Web**

## Option 2 - Run a build script

Run the build script corresponding with your target platform in the **build** directory: `build.{Platform}.cmd`. The resulting build is placed in the **build/artifacts/Community.{Version}.{Platform}/** directory. In addition, a zip archive is automatically created in **build/artifacts/**.

By default, the build script produces a platform-specific, self-contained application that includes: the ASP.NET runtime and libraries, the Smartstore application, and its dependencies. You can run it on any machine that doesn’t have the .NET runtime installed.

Smartstore uses Nuke ([https://nuke.build/](https://nuke.build/)) as its _build automation solution_, which makes it easy to customize the build process by editing the `src/Smartstore.Build/Smartstore.Build/Build.cs` file.

## Info about _src/Smartstore.Web/Modules_ directory

When the solution is built, all modules in `src/Smartstore.Modules/` are detected, compiled, and placed in the `src/Smartstore.Web/Modules/` directory. The application runtime uses this directory as a source from which to dynamically load modules. During development, however, the _Modules_ directory is irrelevant. You can safely delete it at any time.

## Creating Docker images

To create a Docker image run `build/dockerize.{Platform}[.nobuild].sh`.

### **dockerize.linux.sh**

1. Creates a Debian Linux base image including the complete ASP.NET runtime.
2. Builds the solution.
3. Publishes a framework-specific application within the Linux container.
4. Installs the native _wkhtmltopdf_ library which is required to generate PDF files.

### **dockerize.linux.nobuild.sh**

Much faster, but requires that the application was previously built and is located in `build/artifacts/Community.{Version}.linux-x64`.

1. Creates a Debian Linux base image including only the ASP.NET runtime dependencies.
2. Copies the build artifact.
3. Installs _wkhtmltopdf_ native library which is required to generate PDF files.

### **dockerize.windows.nobuild.sh**

Requires, that the application was previously built, is located in `build/artifacts/Community.{Version}.win-x64` and the Docker engine is running a Windows image.

1. Creates a Windows Nano Server base image including only the ASP.NET runtime dependencies.
2. Copies the build artifact.

## Creating Docker containers

To create a ready-to-run Docker container including a database server run `compose.{DbSystem}.sh`.

| command                  | Description                                                                                                                 |
| ------------------------ | --------------------------------------------------------------------------------------------------------------------------- |
| **compose.mysql.sh**     | Creates a composite Docker container including the **Smartstore** application image and the latest **MySql** image.         |
| **compose.sqlserver.sh** | Creates a composite Docker container including the **Smartstore** application image and the latest **MS SQL Server** image. |




