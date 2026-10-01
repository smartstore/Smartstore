# Linux wkhtmltopdf release gate

This tests the actual Smartstore `NativeLibraryManager`, `NativeLibraryInstaller`,
`WkHtmlCommandBuilder` and `WkHtmlToPdfConverter`, not a reimplementation of their CLI.
No running shop/database is required for the technical probe. Host/DI interfaces
are mocked; the PDF/native implementations are real.

## Run on a working Docker host

Use a checkout of this Smartstore revision and the two locally packed native
packages. They do not need to be published to nuget.org. Put them below a directory
with `linux-x64/` and `linux-arm64/` subdirectories, as in Smartstore.NativeLibraries.
The build requires internet access for .NET dependencies and Ubuntu packages.
The probe seeds NuGet's cache from the local native packages, then calls the real
installer for lazy deployment. It does not test downloading the published native
package from nuget.org; that is a separate post-publication check.

From the Smartstore checkout:

```sh
bash build/wkhtml-tests/run.sh /path/to/wkhtmltopdf https://your-shop.example/path/to/public-logo.png
```

The URL must serve an actual PNG/JPEG over trusted HTTPS without authentication.
Use a resource from the intended hosting environment. HTTPS image decoding is
checked by the presence of an embedded image in the resulting PDF, not only exit code.
This does not establish that old Qt enforces modern TLS security policy.

Four combinations run: amd64/arm64, each with a system installation and with only
system dependencies plus genuine lazy NuGet deployment. The final test image uses
the same `aspnet:10.0-noble` base and dependency installer as production; Python
and Poppler are added only to the test image.

Each architecture should also be tested on a native host before release. Emulated
ARM64 on an x64 Docker host is useful, but must not be recorded as a native ARM64
execution test. The script records the Docker daemon architecture. To test only
the native architecture on each respective host:

```sh
WKHTML_TEST_ARCHES=arm64 bash build/wkhtml-tests/run.sh /path/to/wkhtmltopdf https://your-shop.example/logo.png
```

## Automated assertions

- The real manager selects `/usr/local/bin/wkhtmltopdf` in system mode.
- Lazy mode starts without an installed tool and uses the real NuGet installer.
- Both paths select the exact pinned hash for their architecture.
- `ldd` reports no missing libraries; version is `0.12.6.1 (with patched qt)`.
- The real converter renders PNG/JPEG, JavaScript and Unicode into a multi-page PDF.
- HTML header/footer and correct page numbers are present on every page.
- Table headers repeat; the last table row is present.
- A second PDF embeds an image obtained over HTTPS.

Reports, logs, PDFs, extracted text and rendered page PNGs stay under
`.temp/wkhtml-tests/results/<rid>/<mode>/`. The `passed` flag only covers automated
technical checks. `visual_review` and `real_shop_templates` remain `pending`.
Review every rendered page for overlaps, clipping, missing glyphs and table breaks.

## Real shop documents: required manual acceptance

The synthetic invoice fixture is not a Smartstore invoice template. For complete
acceptance, run the actual shop on each target architecture and verify both paths:

1. Build the real image using its matching published application artifact.
2. System path: confirm the application log selects `/usr/local/bin/wkhtmltopdf`.
3. Lazy path: create a disposable test image using the installer's
   `--dependencies-only` mode, with no existing wkhtml executable in the application
   folder. Start with the same staged NuGet cache or, after publication, allow the
   native package download. Do not remove tools from a running production shop.
4. Generate real invoice, delivery-note and configured/custom PDF documents.
   Include a long multi-page order, HTML header/footer, page numbering, Unicode,
   local and HTTPS images, and the store's actual fonts.
5. Save the actual PDFs under `.temp`, render every page with `pdftoppm -png`,
   and compare them visually with approved existing outputs. Check pagination,
   repeated table headers, totals, font substitution and missing resources.
6. Record selected executable/version/hash, image digest, architecture, template
   revision and results. Do not approve the release until both technical and real
   template checks pass on both architectures.

Production installs write a package-version sidecar beside the system executable.
Linux's converter now requires native package revision `0.12.6.1`; older deployed
OpenSSL-1.1 builds are replaced. Custom system installations without compatible
version metadata can fall back to the native package.
