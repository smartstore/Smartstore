---
description: Instructions how to build Smartstore
---

# Deployment & Build

There are two options to build Smartstore and several ways to build Docker images and containers.

Build with the .NET 11 SDK selected by `global.json` (11.0.100 or a newer patch).
Open `Smartstore.slnx` for the platform or `Smartstore.Tools.slnx` for the packaging tools.

The runtime and ASP.NET Core packages use stable .NET 11 releases. The database stack
uses stable EF Core `11.0.0`, PostgreSQL provider `11.0.0` with Npgsql `11.0.0`,
and MySQL provider `11.0.0`. Update EF Core and its providers together, checking
their dependency constraints, and rerun the database tests.

## Option 1 - Publish the entry host project

1. Open the Smartstore solution in Visual Studio with .NET 11 support
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

The .NET 11 Linux images use Ubuntu 26.04 (`11.0-resolute`), including the PDF
dependency installer. The Windows image uses Nano Server LTSC 2025
(`11.0-nanoserver-ltsc2025`) and requires a compatible Windows container host.

### **dockerize.linux.sh**

1. Creates an Ubuntu 26.04 base image including the complete ASP.NET runtime.
2. Builds the solution.
3. Publishes a framework-specific application within the Linux container.
4. Installs the native _wkhtmltopdf_ library which is required to generate PDF files.

### **dockerize.linux.nobuild.sh**

Much faster, but requires that the application was previously built and is located in `build/artifacts/Community.{Version}.linux-x64`.

1. Uses the Ubuntu 26.04 ASP.NET runtime image.
2. Copies the build artifact.
3. Installs _wkhtmltopdf_ native library which is required to generate PDF files.

### **dockerize.windows.nobuild.sh**

Requires, that the application was previously built, is located in `build/artifacts/Community.{Version}.win-x64` and the Docker engine is running a Windows image.

1. Creates a Windows Nano Server base image including only the ASP.NET runtime dependencies.
2. Copies the build artifact.

## Creating Docker containers

To create a ready-to-run Docker container including a database server run `compose.{DbSystem}.sh`.

<table><thead><tr><th width="229">command</th><th>Description</th></tr></thead><tbody><tr><td><strong>compose.mysql.sh</strong></td><td>Creates a composite Docker container including the <strong>Smartstore</strong> application image and the latest <strong>MySql</strong> image.</td></tr><tr><td><strong>compose.sqlserver.sh</strong></td><td>Creates a composite Docker container including the <strong>Smartstore</strong> application image and the latest <strong>MS SQL Server</strong> image.</td></tr></tbody></table>
