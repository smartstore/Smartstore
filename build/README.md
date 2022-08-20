# Instructions how to build Smartstore



### Option 1 - by publishing the entry host project

1. Open the Smartstore solution in Visual Studio 2022
2. Use **Release** configuration
3. (Re)build the solution
4. Publish host project **Smartstore.Web**



### Option 2 - by running a build script

Run the build script corresponding to your target platform in the directory **build**: `build.{Platform}.cmd`. The result build will be placed in the directory `build/artifacts/Community.{Version}.{Platform}`. Also, a zip archive is created automatically in **build/artifacts/**.

By default, the build script produces a platform-dependent, self-contained application that includes the ASP.NET runtime and libraries, the Smartstore application and its dependencies. You can run it on any machine that doesn't have the .NET runtime installed.

Smartstore uses Nuke (https://nuke.build/) as build automation solution, which makes it easy to customize the build process by editing the file `src/Smartstore.Build/Smartstore.Build/Build.cs`.



### Info about "src/Smartstore.Web/Modules" directory

While building the solution, all modules in `src/Smartstore.Modules/` are discovered, compiled and placed in the `src/Smartstore.Web/Modules/` directory. The application runtime uses this directory as a source from which modules are dynamically loaded from. However, during development, the "Modules" directory is irrelevant. You can safely delete it at any time.



### Creating Docker images

To create a Docker image run `build/dockerize.{Platform}[.nobuild].sh`.

##### dockerize.linux.sh

Creates a Debian Linux base image including the complete ASP.NET runtime, builds the solution and publishes a framework-dependent application within the Linux container. Also installs **wkhtmltopdf** native library which is required to generate PDF files.

##### dockerize.linux.nobuild.sh

Much faster, but requires that the application was previously built and is located in `build/artifacts/Community.{Version}.linux-x64`. Creates a Debian Linux base image including the ASP.NET runtime dependencies only and copies the build artifact. Also installs **wkhtmltopdf** native library which is required to generate PDF files.

##### dockerize.windows.nobuild.sh

Creates a Windows Nano Server base image including the ASP.NET runtime dependencies only and copies the build artifact. Requires that the application was previously built and is located in `build/artifacts/Community.{Version}.win-x64`. Also requires that the Docker engine is running a Windows image.



### Creating Docker containers

To create a ready-to-run Docker container including a database server run `compose.{DbSystem}.sh`. 

##### compose.mysql.sh

Creates a composite Docker container containing the **smartstore** application image and the latest **MySql** image.

##### compose.sqlserver.sh

Creates a composite Docker container containing the **smartstore** application image and the latest **MS SQL Server** image.




