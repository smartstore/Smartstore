# View precompilation

Razor views are compiled at runtime by default. The first request after deployment therefore triggers a compilation step that can take noticeable time and potentially fail only in production. Precompiling views at build time avoids that delay and surfaces syntax errors early.

## Default configuration

Smartstore ships with [`Smartstore.Razor.props`](../../src/Smartstore.Build/Smartstore.Razor.props) which enables build- and publish-time compilation for all projects referencing it. The props file turns on the modern Razor SDK switches and disables the build server:

```xml
<PropertyGroup>
  <AddRazorSupportForMvc>true</AddRazorSupportForMvc>
  <MvcRazorCompileOnBuild>false</MvcRazorCompileOnBuild>
  <MvcRazorCompileOnPublish>true</MvcRazorCompileOnPublish>
  <RazorCompileOnBuild>true</RazorCompileOnBuild>
  <RazorCompileOnPublish>true</RazorCompileOnPublish>
  <UseRazorBuildServer>false</UseRazorBuildServer>
</PropertyGroup>
```

With these settings every `dotnet build` or `dotnet publish` creates a `<project>.Views.dll` alongside the main assembly so views are ready to execute.

## Building with precompiled views

To produce precompiled views for the web host simply run:

```bash
dotnet publish src/Smartstore.Web/Smartstore.Web.csproj -c Release
```

The publish output contains `Smartstore.Web.dll` and `Smartstore.Web.Views.dll` which ASP.NET Core loads automatically at startup.

## Skipping compilation in development

During rapid prototyping you can skip view compilation by building the special `DebugNoRazorCompile` configuration:

```bash
dotnet build src/Smartstore.Web/Smartstore.Web.csproj -c DebugNoRazorCompile
```

This configuration sets `RazorCompileOnBuild` to `false`, shortening build time at the cost of a slower first request.

Modules can also opt out individually by adding to their project file:

```xml
<PropertyGroup>
  <RazorCompileOnBuild>false</RazorCompileOnBuild>
</PropertyGroup>
```

## Benefits

* Faster cold start because no runtime view compilation is necessary.
* Compilation errors surface during CI/CD rather than at runtime.
* Precompiled views can be trimmed and optimized as part of the build pipeline.

Precompiling views is a low‑effort optimization that improves reliability and perceived performance for production deployments.